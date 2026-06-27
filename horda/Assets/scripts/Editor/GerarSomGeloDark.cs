#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para os Cristais de Gelo (estilhaço gélido + estilhaçar).
// Estilo "dark ice": cristalino, mas com graves e dissonância (trítono).
// Salva em Assets/Resources/Sons/ (SomSkill carrega via Resources.Load).
// Menu: Tools/Sons/Gerar Sons Dark (Gelo)
public static class GerarSomGeloDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Gelo)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("gelo_disparo_dark", Reverb(GerarDisparo(), 0.35f, 5, 22f, 0.25f));
        GravarWav("gelo_impacto_dark", Reverb(GerarImpacto(), 0.45f, 7, 24f, 0.32f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Gelo gerado em " + PASTA + "/ (gelo_disparo_dark, gelo_impacto_dark)");
    }

    // Disparo: estilhaço gélido — "ting" cristalino dissonante + zip descendente + grave escuro.
    static float[] GerarDisparo()
    {
        int dur = (int)(SR * 0.2f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Clamp01(t * 50f) * Mathf.Exp(-t * 12f);
            float cryst = (Mathf.Sin(2f * Mathf.PI * 1600f * t)
                        +  Mathf.Sin(2f * Mathf.PI * 2100f * t) * 0.6f
                        +  Mathf.Sin(2f * Mathf.PI * 1600f * TRITONE * t) * 0.4f) * Mathf.Exp(-t * 16f) * 0.25f;
            float fZip = Mathf.Lerp(900f, 300f, Mathf.Clamp01(t * 5f));
            float zip  = Mathf.Sin(2f * Mathf.PI * fZip * t) * Mathf.Exp(-t * 10f) * 0.4f;
            float low  = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 14f) * 0.3f;
            float tick = (Random.value * 2f - 1f) * Mathf.Exp(-t * 45f) * 0.15f;
            s[i] = Mathf.Clamp((cryst + zip + low + tick) * env, -1f, 1f);
        }
        return Normalizar(s);
    }

    // Impacto: estilhaçar de gelo — cacho de parciais cristalinas + estalo + sub frio + ressonância dissonante.
    static float[] GerarImpacto()
    {
        int dur = (int)(SR * 0.28f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 9f);
            float shatter = (Mathf.Sin(2f * Mathf.PI * 1400f * t)
                          +  Mathf.Sin(2f * Mathf.PI * 1850f * t) * 0.7f
                          +  Mathf.Sin(2f * Mathf.PI * 2300f * t) * 0.5f
                          +  Mathf.Sin(2f * Mathf.PI * 1400f * TRITONE * t) * 0.4f) * Mathf.Exp(-t * 12f) * 0.2f;
            float crack = (Random.value * 2f - 1f) * Mathf.Exp(-t * 30f) * 0.5f;
            float sub   = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 10f) * 0.4f;
            float ring  = Mathf.Sin(2f * Mathf.PI * 300f * t) * Mathf.Exp(-t * 8f) * 0.25f;
            float raw = (shatter + crack * 0.6f + sub + ring) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.85f), -1f, 1f);
        }
        return Normalizar(s);
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
