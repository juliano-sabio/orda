#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Som procedural curto e suave para a coleta de XP (blip cristalino ascendente).
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (XP)
public static class GerarSomXpDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;

    [MenuItem("Tools/Sons/Gerar Sons Dark (XP)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("xp_coletar", Reverb(GerarColeta(), 0.4f, 8, 30f, 0.4f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] XP gerado em " + PASTA + "/ (xp_coletar)");
    }

    // Coleta: moedinha com toque de espírito — dois toques metálicos ascendentes (B5→E6, tipo
    // "ding-ding") com harmônicos levemente inarmônicos (brilho de moeda), mais um shimmer etéreo
    // agudo com vibrato e um sopro aéreo que perduram (a "alma" do espírito). Reverb longo na cauda.
    static float[] GerarColeta()
    {
        int dur = (int)(SR * 0.5f);
        var s = new float[dur];
        const float t1 = 0.05f;    // duração do 1º toque
        const float f1 = 988f;     // B5
        const float f2 = 1319f;    // E6
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            bool  n2   = t >= t1;
            float fn   = n2 ? f2 : f1;
            float tn   = n2 ? (t - t1) : t;                       // tempo dentro da nota
            float env  = (n2 ? Mathf.Exp(-tn * 7f) : Mathf.Exp(-tn * 30f)) * Mathf.Clamp01(tn * 200f);

            // Timbre metálico de moeda (fundamental + harmônicos, alguns inarmônicos = brilho)
            float metal = (Mathf.Sin(2f * Mathf.PI * fn * t)
                        +  Mathf.Sin(2f * Mathf.PI * fn * 2f * t) * 0.5f
                        +  Mathf.Sin(2f * Mathf.PI * fn * 3.01f * t) * 0.28f
                        +  Mathf.Sin(2f * Mathf.PI * fn * 4.2f * t) * 0.15f) * 0.4f;

            // Toque de espírito: shimmer etéreo agudo com vibrato, decaindo devagar
            float vib     = Mathf.Sin(2f * Mathf.PI * 6f * t) * 8f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * (f2 * 2f + vib) * t) * Mathf.Exp(-t * 3.5f) * 0.12f;
            // Sopro aéreo bem sutil (fantasmagórico)
            float ar = (Random.value * 2f - 1f) * Mathf.Exp(-t * 6f) * 0.05f;

            float raw = metal * env + shimmer + ar;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.85f), -1f, 1f);
        }
        return Normalizar(s);
    }

    static float[] Reverb(float[] dry, float decay, int taps, float spreadMs, float wetMix)
    {
        int spread = Mathf.Max(1, (int)(SR * spreadMs / 1000f));
        int extra = spread * taps + SR / 4;
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
