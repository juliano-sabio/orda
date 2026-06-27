#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para as Garras do Abismo (erupção demoníaca + dissipar).
// Salva em Assets/Resources/Sons/ (SomSkill carrega via Resources.Load).
// Menu: Tools/Sons/Gerar Sons Dark (Garra)
public static class GerarSomGarraDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Garra)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("garra_erupcao_dark",  Reverb(GerarErupcao(),  0.5f, 8, 28f, 0.4f));
        GravarWav("garra_dissipar_dark", Reverb(GerarDissipar(), 0.4f, 6, 30f, 0.3f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Garra gerada em " + PASTA + "/ (garra_erupcao_dark, garra_dissipar_dark)");
    }

    // Erupção: garras rasgam o chão — estouro de terra, rosnado demoníaco (AM gutural), sub e "shing" sombrio.
    static float[] GerarErupcao()
    {
        int dur = (int)(SR * 0.45f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            // Estouro de terra (ruído com ataque seco)
            float tear = (Random.value * 2f - 1f) * Mathf.Exp(-t * 10f) * 0.7f;
            // Rosnado demoníaco: tom grave descendente com tremor gutural (AM)
            float fGrowl = Mathf.Lerp(140f, 50f, Mathf.Clamp01(t * 1.2f));
            float am = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 30f * t);
            float growl = Mathf.Sin(2f * Mathf.PI * fGrowl * t) * am * Mathf.Exp(-t * 4f) * 0.6f;
            // Sub boom
            float sub = Mathf.Sin(2f * Mathf.PI * 42f * t) * Mathf.Exp(-t * 3.5f) * 0.6f;
            // "Shing" sombrio das garras (dissonante, breve)
            float shing = (Mathf.Sin(2f * Mathf.PI * 700f * t)
                        +  Mathf.Sin(2f * Mathf.PI * 700f * TRITONE * t)) * Mathf.Exp(-t * 18f) * 0.12f;
            float raw = tear * 0.6f + growl + sub * 0.7f + shing;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = sign * Mathf.Pow(Mathf.Abs(raw), 0.65f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Dissipar: garras recolhem pro abismo — whoosh descendente "sugado" + sub que some.
    static float[] GerarDissipar()
    {
        int dur = (int)(SR * 0.3f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env   = Mathf.Clamp01(t * 8f) * Mathf.Exp(-t * 6f); // sobe e cai
            float swell = Mathf.Clamp01(t * 6f);
            float noise = (Random.value * 2f - 1f) * 0.4f * swell;
            float fDown = Mathf.Lerp(400f, 60f, Mathf.Clamp01(t * 2f));
            float tone  = Mathf.Sin(2f * Mathf.PI * fDown * t) * 0.4f;
            float sub   = Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.3f;
            s[i] = Mathf.Clamp((noise * 0.5f + tone + sub) * env, -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    static void SuavizarRuido(float[] s, int janela)
    {
        if (janela < 1) return;
        var c = (float[])s.Clone();
        for (int i = 0; i < s.Length; i++)
        {
            float soma = 0f; int n = 0;
            for (int k = -janela; k <= janela; k++)
            {
                int j = i + k;
                if (j < 0 || j >= s.Length) continue;
                soma += c[j]; n++;
            }
            s[i] = soma / n;
        }
    }

    static float[] Reverb(float[] dry, float decay, int taps, float spreadMs, float wetMix)
    {
        int spread = Mathf.Max(1, (int)(SR * spreadMs / 1000f));
        int extra = spread * taps + SR / 6;
        var wet = new float[dry.Length + extra];
        for (int i = 0; i < dry.Length; i++) wet[i] += dry[i];
        for (int tap = 1; tap <= taps; tap++)
        {
            float g = Mathf.Pow(decay, tap) * wetMix;
            int d = spread * tap + Random.Range(-spread / 3, spread / 3);
            if (d < 1) d = 1;
            for (int i = 0; i < dry.Length; i++)
            {
                int j = i + d;
                if (j < wet.Length) wet[j] += dry[i] * g;
            }
        }
        return Normalizar(wet);
    }

    static float[] Normalizar(float[] s)
    {
        float peak = 0f;
        for (int i = 0; i < s.Length; i++)
            if (Mathf.Abs(s[i]) > peak) peak = Mathf.Abs(s[i]);
        if (peak > 0.001f)
            for (int i = 0; i < s.Length; i++)
                s[i] = Mathf.Clamp(s[i] / peak * 0.9f, -1f, 1f);
        return s;
    }

    static void GravarWav(string nome, float[] samples)
    {
        string path = PASTA + "/" + nome + ".wav";
        string fullPath = Application.dataPath + "/../" + path;

        using var fs = new FileStream(fullPath, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        int dataLen = samples.Length * 2;
        bw.Write(new byte[] { 82, 73, 70, 70 });    // "RIFF"
        bw.Write(36 + dataLen);
        bw.Write(new byte[] { 87, 65, 86, 69 });    // "WAVE"
        bw.Write(new byte[] { 102, 109, 116, 32 }); // "fmt "
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(SR);
        bw.Write(SR * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(new byte[] { 100, 97, 116, 97 });  // "data"
        bw.Write(dataLen);
        for (int i = 0; i < samples.Length; i++)
            bw.Write((short)(samples[i] * 32767f));
    }
}
#endif
