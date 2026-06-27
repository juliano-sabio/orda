#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Som procedural "dark fantasy" para PEGAR uma carta de EVOLUÇÃO: empoderamento épico
// (swell ascendente -> impacto + acorde menor grave + shimmer, com cauda longa).
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Evolucao)
public static class GerarSomEvolucaoDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Evolucao)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("evolucao_select_dark", Reverb(GerarEvolucao(), 0.5f, 9, 32f, 0.42f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Evolucao gerada em " + PASTA + "/ (evolucao_select_dark)");
    }

    // Empoderamento: energia subindo -> impacto grave + acorde de Lá menor (A,C,E) + shimmer.
    static float[] GerarEvolucao()
    {
        int dur = (int)(SR * 0.7f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 25f); // anti-click

            // Fase 1: swell ascendente (energia se acumulando até ~0.3s)
            float subir = Mathf.Clamp01(t / 0.3f);
            float fSweep = Mathf.Lerp(90f, 300f, subir);
            float swellDecay = t < 0.3f ? 1f : Mathf.Exp(-(t - 0.3f) * 7f);
            float swell = Mathf.Sin(2f * Mathf.PI * fSweep * t) * subir * swellDecay * 0.4f;
            float air = (Random.value * 2f - 1f) * 0.2f * subir * (t < 0.3f ? 1f : 0.15f);

            // Fase 2: impacto + acorde menor grave, a partir de 0.3s
            float p2 = Mathf.Max(0f, t - 0.3f);
            float ativo2 = t >= 0.3f ? 1f : 0f;
            float boom = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-p2 * 5f) * 0.6f * ativo2;
            float chordEnv = Mathf.Exp(-p2 * 3.5f) * ativo2;
            float chord = (Mathf.Sin(2f * Mathf.PI * 110.00f * t)        // Lá
                        +  Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.7f  // Dó (terça menor)
                        +  Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.6f) // Mi (quinta)
                        * chordEnv * 0.22f;
            float shimmer = (Mathf.Sin(2f * Mathf.PI * 880f * t)
                          +  Mathf.Sin(2f * Mathf.PI * 1318f * t) * 0.5f) * Mathf.Exp(-p2 * 5f) * ativo2 * 0.1f;

            float raw = (swell + air + boom + chord + shimmer) * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
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
