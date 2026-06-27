#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para a ultimate Tempestade Elétrica:
// início (surto de tempestade), loop ambiente (ronco de trovão) e raio (queda).
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Tempestade)
public static class GerarSomTempestadeDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Tempestade)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("tempestade_inicio", Reverb(GerarInicio(), 0.5f, 8, 34f, 0.4f));
        GravarWav("tempestade_loop",   GerarLoop());           // SEM reverb (precisa loopar sem emenda)
        GravarWav("tempestade_raio",   Reverb(GerarRaio(),   0.45f, 6, 26f, 0.32f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Tempestade gerada em " + PASTA + "/ (inicio, loop, raio)");
    }

    // Início: a tempestade se forma — ronco subindo + surto elétrico + boom no pico.
    static float[] GerarInicio()
    {
        int dur = (int)(SR * 0.7f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 12f);
            float build = Mathf.Clamp01(t / 0.4f);
            float rumble = (Random.value * 2f - 1f) * (0.3f + 0.7f * build) * 0.35f;
            float swell = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(60f, 140f, build) * t) * (0.3f + 0.7f * build) * 0.3f;
            float p2 = Mathf.Max(0f, t - 0.4f);
            float ativo2 = t >= 0.4f ? 1f : 0f;
            float boom = Mathf.Sin(2f * Mathf.PI * 45f * t) * Mathf.Exp(-p2 * 4f) * 0.5f * ativo2;
            float crack = (Random.value * 2f - 1f) * Mathf.Exp(-Mathf.Abs(t - 0.4f) * 10f) * 0.3f;
            float raw = (rumble + swell + boom + crack) * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.7f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Loop ambiente (2s): ronco de trovão rolando + vento grave + crepitar tênue.
    // LFOs em frequências que completam ciclos inteiros em 2s -> looping sem emenda.
    static float[] GerarLoop()
    {
        int dur = (int)(SR * 2.0f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float rumble = (Random.value * 2f - 1f) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.5f * t)) * 0.3f;
            float vento  = Mathf.Sin(2f * Mathf.PI * 40f * t) * 0.2f + Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.15f;
            float crepit = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 3f * t)) * 0.08f;
            s[i] = Mathf.Clamp(rumble + vento + crepit, -1f, 1f);
        }
        SuavizarRuido(s, 3); // low-pass forte = ronco profundo
        return Normalizar(s);
    }

    // Raio: queda do céu — estalo seco + zap descendente + baque + ronco curto.
    static float[] GerarRaio()
    {
        int dur = (int)(SR * 0.35f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 7f);
            float crack = (Random.value * 2f - 1f) * Mathf.Exp(-t * 30f) * 0.7f;
            float zap = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(800f, 150f, Mathf.Clamp01(t * 5f)) * t) * Mathf.Exp(-t * 9f) * 0.4f;
            float baque = Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-t * 12f) * 0.5f;
            float rumble = (Random.value * 2f - 1f) * Mathf.Exp(-t * 7f) * 0.3f;
            float raw = (crack * 0.7f + zap + baque + rumble * 0.5f) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.7f), -1f, 1f);
        }
        SuavizarRuido(s, 1);
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
