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

        GravarWav("tempestade_inicio", Reverb(GerarInicio(), 0.55f, 9, 38f, 0.42f));
        GravarWav("tempestade_loop",   GerarLoop());           // SEM reverb (precisa loopar sem emenda)
        GravarWav("tempestade_raio",   Reverb(GerarRaio(),   0.4f,  5, 20f, 0.24f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Tempestade gerada em " + PASTA + "/ (inicio, loop, raio)");
    }

    // Início (suave): a tempestade se forma devagar — ronco grave subindo + hum quente +
    // boom arredondado no pico (sem estalo seco). Bem menos ruído branco que a versão antiga.
    static float[] GerarInicio()
    {
        int dur = (int)(SR * 0.8f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 8f);            // ataque mais lento/suave
            float build = Mathf.Clamp01(t / 0.5f);
            float swell = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(55f, 120f, build) * t) * (0.3f + 0.7f * build) * 0.34f;
            float subhum = Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.16f;               // sustentação quente
            float sub = Mathf.Sin(2f * Mathf.PI * 38f * t) * 0.14f * build;          // grave que entra
            float p2 = Mathf.Max(0f, t - 0.5f);
            float ativo2 = t >= 0.5f ? 1f : 0f;
            float boom = Mathf.Sin(2f * Mathf.PI * 42f * t) * Mathf.Exp(-p2 * 3.5f) * 0.45f * ativo2; // boom rolando
            float raw = (swell + subhum + sub + boom) * master;                       // SEM ruído branco
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.9f), -1f, 1f);
        }
        SuavizarRuido(s, 3);
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
            // Ronco 100% tonal (sem ruído branco = sem chiado). LFOs a 0.5/1Hz completam ciclos
            // inteiros em 2s -> loop sem emenda; dão o "trovão rolando" pela amplitude.
            float lfo1 = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
            float lfo2 = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 1.0f * t + 1.2f);
            float bed = Mathf.Sin(2f * Mathf.PI * 20f * t) * 0.10f
                      + Mathf.Sin(2f * Mathf.PI * 30f * t) * 0.13f * lfo1
                      + Mathf.Sin(2f * Mathf.PI * 40f * t) * 0.20f * lfo2
                      + Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.12f * lfo1;
            s[i] = Mathf.Clamp(bed, -1f, 1f);
        }
        SuavizarRuido(s, 3);
        return Normalizar(s);
    }

    // Raio (suave): queda do céu — estalo mais macio + zap descendente arredondado + baque grave.
    // Menos ruído seco e menos grit que a versão antiga, mantendo o impacto.
    static float[] GerarRaio()
    {
        int dur = (int)(SR * 0.4f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 6f);
            // Zap elétrico brilhante descendo do agudo pro grave
            float zap = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 140f, Mathf.Clamp01(t * 4f)) * t) * Mathf.Exp(-t * 7f) * 0.4f;
            // "zzzt" elétrico: tom médio com modulação de amplitude rápida (dá o caráter de raio, SEM ruído)
            float buzz = Mathf.Sin(2f * Mathf.PI * 600f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 90f * t)) * Mathf.Exp(-t * 14f) * 0.25f;
            // Crack curto e brilhante feito de harmônicos agudos (em vez de ruído branco)
            float crack = (Mathf.Sin(2f * Mathf.PI * 1500f * t) + Mathf.Sin(2f * Mathf.PI * 2200f * t) * 0.6f) * Mathf.Exp(-t * 35f) * 0.18f;
            float snap = (Random.value * 2f - 1f) * Mathf.Exp(-t * 50f) * 0.10f; // clique minúsculo pro ataque
            // Impacto grave (o raio "batendo" no inimigo)
            float baque = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 8f) * 0.6f;
            float sub = Mathf.Sin(2f * Mathf.PI * 35f * t) * Mathf.Exp(-t * 6f) * 0.3f;
            float raw = (zap + buzz + crack + snap * 0.5f + baque + sub) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
        }
        SuavizarRuido(s, 1); // janela curta preserva o brilho do crack
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
