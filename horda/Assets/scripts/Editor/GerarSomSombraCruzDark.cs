#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para Sombras em Cruz (desencadear sombrio + impacto).
// Salva em Assets/Resources/Sons/ (SomSkill carrega via Resources.Load).
// Menu: Tools/Sons/Gerar Sons Dark (Sombra Cruz)
public static class GerarSomSombraCruzDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Sombra Cruz)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("sombracruz_disparo_dark", Reverb(GerarDisparo(), 0.45f, 7, 28f, 0.35f));
        GravarWav("sombracruz_impacto_dark", Reverb(GerarImpacto(), 0.45f, 6, 22f, 0.3f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Sombra Cruz gerada em " + PASTA + "/ (sombracruz_disparo_dark, sombracruz_impacto_dark)");
    }

    // Disparo: energia sombria irrompendo em cruz — whoosh escuro, tom grave descendente, dissonância e sub.
    static float[] GerarDisparo()
    {
        int dur = (int)(SR * 0.4f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Clamp01(t * 45f) * Mathf.Exp(-t * 6f);
            float whoosh = (Random.value * 2f - 1f) * Mathf.Exp(-t * 7f) * 0.5f;
            float fTone = Mathf.Lerp(420f, 90f, Mathf.Clamp01(t * 2.5f));
            float tone  = Mathf.Sin(2f * Mathf.PI * fTone * t) * Mathf.Exp(-t * 5f) * 0.5f;
            float diss  = (Mathf.Sin(2f * Mathf.PI * 200f * t)
                        +  Mathf.Sin(2f * Mathf.PI * 200f * TRITONE * t)) * Mathf.Exp(-t * 7f) * 0.2f;
            float sub   = Mathf.Sin(2f * Mathf.PI * 50f * t) * Mathf.Exp(-t * 5f) * 0.5f;
            float raw = whoosh + tone + diss + sub;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = sign * Mathf.Pow(Mathf.Abs(raw), 0.7f) * env;
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Impacto: golpe sombrio — transiente seco + anel dissonante grave + sub.
    static float[] GerarImpacto()
    {
        int dur = (int)(SR * 0.22f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env   = Mathf.Exp(-t * 10f);
            float slash = (Random.value * 2f - 1f) * Mathf.Exp(-t * 32f) * 0.6f;
            float ring  = (Mathf.Sin(2f * Mathf.PI * 260f * t) * 0.5f
                        +  Mathf.Sin(2f * Mathf.PI * 260f * TRITONE * t) * 0.3f) * Mathf.Exp(-t * 8f) * 0.4f;
            float sub   = Mathf.Sin(2f * Mathf.PI * 65f * t) * Mathf.Exp(-t * 12f) * 0.4f;
            float raw = (slash + ring + sub) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
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
