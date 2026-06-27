#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para o Corte Fantasma (lâmina espectral teleguiada).
// Salva em Assets/Resources/Sons/ (SomSkill carrega via Resources.Load).
// Menu: Tools/Sons/Gerar Sons Dark (Corte Fantasma)
public static class GerarSomCorteFantasmaDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3  = 1.18921f;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Corte Fantasma)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("corte_lancamento_dark", Reverb(GerarLancamento(), 0.4f, 6, 25f, 0.3f));
        GravarWav("corte_impacto_dark",    Reverb(GerarImpacto(),    0.5f, 7, 22f, 0.34f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Corte Fantasma gerado em " + PASTA + "/ (corte_lancamento_dark, corte_impacto_dark)");
    }

    // Lançamento: liberação da lâmina espectral — "fwip" etéreo (sobe e desce) com shimmer dissonante.
    static float[] GerarLancamento()
    {
        int dur = (int)(SR * 0.28f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Clamp01(t * 45f) * Mathf.Exp(-t * 9f);

            float swishEnv = Mathf.Exp(-Mathf.Pow((t - 0.04f) / 0.04f, 2f));
            float swish = (Random.value * 2f - 1f) * swishEnv * 0.5f;

            // Tom espectral: sobe rápido e cai (release da lâmina)
            float fTone = t < 0.06f
                ? Mathf.Lerp(300f, 1000f, t / 0.06f)
                : Mathf.Lerp(1000f, 250f, Mathf.Clamp01((t - 0.06f) * 4f));
            float tone = Mathf.Sin(2f * Mathf.PI * fTone * t) * Mathf.Exp(-t * 8f) * 0.4f;

            float shimmer = (Mathf.Sin(2f * Mathf.PI * 1400f * t)
                          +  Mathf.Sin(2f * Mathf.PI * 1400f * MINOR3 * t)
                          +  Mathf.Sin(2f * Mathf.PI * 1400f * TRITONE * t)) * Mathf.Exp(-t * 15f) * 0.07f;

            float low = Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 12f) * 0.25f;

            float raw = swish + tone + shimmer + low;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = sign * Mathf.Pow(Mathf.Abs(raw), 0.85f) * env;
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Impacto: corte espectral — fatia nítida + ressonância dissonante + baque leve.
    static float[] GerarImpacto()
    {
        int dur = (int)(SR * 0.2f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env  = Mathf.Exp(-t * 11f);
            float fatia = (Random.value * 2f - 1f) * Mathf.Exp(-t * 40f) * 0.7f;
            float ring  = (Mathf.Sin(2f * Mathf.PI * 380f * t) * 0.5f
                        +  Mathf.Sin(2f * Mathf.PI * 380f * MINOR3 * t) * 0.3f
                        +  Mathf.Sin(2f * Mathf.PI * 380f * TRITONE * t) * 0.2f) * Mathf.Exp(-t * 8f) * 0.45f;
            float thud  = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 14f) * 0.35f;
            float raw = (fatia + ring + thud) * env;
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
