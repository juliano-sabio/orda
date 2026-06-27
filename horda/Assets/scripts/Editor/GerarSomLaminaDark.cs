#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para a Fúria de Lâminas (rajada metálica + clang).
// Salva em Assets/Resources/Sons/ (SomSkill carrega via Resources.Load).
// Menu: Tools/Sons/Gerar Sons Dark (Lamina)
public static class GerarSomLaminaDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3  = 1.18921f;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Lamina)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("lamina_furia_dark",   Reverb(GerarFuria(),   0.4f, 6, 24f, 0.3f));
        GravarWav("lamina_impacto_dark", Reverb(GerarImpacto(), 0.45f, 6, 20f, 0.3f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Lamina gerada em " + PASTA + "/ (lamina_furia_dark, lamina_impacto_dark)");
    }

    // Fúria: desencadear das lâminas — multi-swish metálico, shing dissonante, tom agressivo descendente.
    static float[] GerarFuria()
    {
        int dur = (int)(SR * 0.35f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Clamp01(t * 40f) * Mathf.Exp(-t * 7f);

            // Multi-swish: vários "cortes" de ruído em tempos ligeiramente diferentes
            float janelas = 0f;
            for (int k = 0; k < 3; k++)
                janelas += Mathf.Exp(-Mathf.Pow((t - (0.03f + k * 0.04f)) / 0.028f, 2f));
            float swish = (Random.value * 2f - 1f) * janelas * 0.4f;

            // Shing metálico dissonante
            float shing = (Mathf.Sin(2f * Mathf.PI * 800f * t)
                        +  Mathf.Sin(2f * Mathf.PI * 800f * MINOR3 * t)
                        +  Mathf.Sin(2f * Mathf.PI * 1100f * TRITONE * t)) * Mathf.Exp(-t * 12f) * 0.12f;

            // Tom agressivo descendente
            float fTone = Mathf.Lerp(600f, 150f, Mathf.Clamp01(t * 3f));
            float tone  = Mathf.Sin(2f * Mathf.PI * fTone * t) * Mathf.Exp(-t * 8f) * 0.4f;

            // Pulso grave
            float low = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 10f) * 0.4f;

            float raw = swish + shing + tone + low;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = sign * Mathf.Pow(Mathf.Abs(raw), 0.8f) * env;
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Impacto: clang metálico dissonante curto.
    static float[] GerarImpacto()
    {
        int dur = (int)(SR * 0.18f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env  = Mathf.Exp(-t * 12f);
            float clang = (Mathf.Sin(2f * Mathf.PI * 500f * t) * 0.5f
                        +  Mathf.Sin(2f * Mathf.PI * 500f * MINOR3 * t) * 0.3f
                        +  Mathf.Sin(2f * Mathf.PI * 500f * TRITONE * t) * 0.2f) * Mathf.Exp(-t * 9f) * 0.45f;
            float tick = (Random.value * 2f - 1f) * Mathf.Exp(-t * 40f) * 0.4f;
            float sub  = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 15f) * 0.3f;
            float raw = (clang + tick + sub) * env;
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
