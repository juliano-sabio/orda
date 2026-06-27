#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para a Chuva de Estrelas (queda de meteoro + crash sombrio).
// Salva em Assets/Resources/Sons/ (SomSkill carrega via Resources.Load).
// Menu: Tools/Sons/Gerar Sons Dark (Estrela)
public static class GerarSomEstrelaDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3  = 1.18921f;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Estrela)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("estrela_queda_dark",   Reverb(GerarQueda(),   0.35f, 5, 26f, 0.22f));
        GravarWav("estrela_impacto_dark", Reverb(GerarImpacto(), 0.5f,  8, 26f, 0.4f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Estrela gerada em " + PASTA + "/ (estrela_queda_dark, estrela_impacto_dark)");
    }

    // Queda: meteoro descendo — assobio com pitch descendente, vento e rumble grave crescendo.
    static float[] GerarQueda()
    {
        int dur = (int)(SR * 0.4f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Pow(Mathf.Clamp01(t / 0.4f), 1.2f); // cresce ao se aproximar
            float fWhistle = Mathf.Lerp(1200f, 200f, Mathf.Clamp01(t * 2.5f));
            float whistle  = Mathf.Sin(2f * Mathf.PI * fWhistle * t) * 0.5f;
            float overtone = Mathf.Sin(2f * Mathf.PI * fWhistle * MINOR3 * t) * 0.2f;
            float vento    = (Random.value * 2f - 1f) * 0.3f;
            float sub      = Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.2f;
            s[i] = Mathf.Clamp((whistle + overtone + vento + sub) * env, -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Impacto: crash sombrio — sub cavernoso, estrondo, ressonância dissonante e rosnado descendente.
    static float[] GerarImpacto()
    {
        int dur = (int)(SR * 0.45f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env   = Mathf.Exp(-t * 5f);
            float sub   = Mathf.Sin(2f * Mathf.PI * 45f * t) * Mathf.Exp(-t * 4f);
            float crash = (Random.value * 2f - 1f) * Mathf.Exp(-t * 7f) * 0.8f;
            float ring  = (Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.5f
                        +  Mathf.Sin(2f * Mathf.PI * 180f * MINOR3 * t) * 0.3f
                        +  Mathf.Sin(2f * Mathf.PI * 180f * TRITONE * t) * 0.2f) * Mathf.Exp(-t * 6f) * 0.4f;
            float fGrowl = Mathf.Lerp(160f, 55f, Mathf.Clamp01(t * 1.5f));
            float growl = Mathf.Sin(2f * Mathf.PI * fGrowl * t) * Mathf.Exp(-t * 5f) * 0.4f;
            float raw = (sub * 0.7f + crash * 0.7f + ring + growl) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.65f), -1f, 1f);
        }
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
