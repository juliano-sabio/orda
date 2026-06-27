#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para a ultimate Domo Retardante (dilatação temporal):
// início (domo materializa), loop ("tempo lento") e fim (dissipa).
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Domo)
public static class GerarSomDomoDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Domo)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("domo_inicio", Reverb(GerarInicio(), 0.45f, 7, 30f, 0.32f));
        GravarWav("domo_loop",   GerarLoop());          // SEM reverb (loop sem emenda)
        GravarWav("domo_fim",    Reverb(GerarFim(),    0.4f, 6, 26f, 0.3f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Domo gerado em " + PASTA + "/ (inicio, loop, fim)");
    }

    // Início: domo se forma — warp descendente (tempo dobrando) + zumbido ressonante + lock grave.
    static float[] GerarInicio()
    {
        int dur = (int)(SR * 0.6f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 15f);
            float fWarp = Mathf.Lerp(600f, 120f, Mathf.Clamp01(t * 2.5f));
            float warp = Mathf.Sin(2f * Mathf.PI * fWarp * t) * Mathf.Exp(-t * 4f) * 0.4f;
            float hum = (Mathf.Sin(2f * Mathf.PI * 110f * t)
                      +  Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.5f) * Mathf.Clamp01(t / 0.4f) * 0.3f;
            float shimmer = (Mathf.Sin(2f * Mathf.PI * 700f * t)
                          +  Mathf.Sin(2f * Mathf.PI * 1050f * t) * 0.5f) * Mathf.Exp(-t * 6f) * 0.1f;
            float p2 = Mathf.Max(0f, t - 0.35f);
            float ativo2 = t >= 0.35f ? 1f : 0f;
            float lockBoom = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-p2 * 5f) * 0.4f * ativo2;
            float sub = Mathf.Sin(2f * Mathf.PI * 50f * t) * Mathf.Clamp01(t / 0.3f) * 0.25f;
            s[i] = Mathf.Clamp((warp + hum + shimmer + lockBoom + sub) * master, -1f, 1f);
        }
        SuavizarRuido(s, 1);
        return Normalizar(s);
    }

    // Loop (2s): zumbido de "tempo lento" — díade ressonante + warble (FM lento) + shimmer + sub.
    // Frequências/LFOs múltiplos de 0.5Hz -> ciclos inteiros em 2s -> loop sem emenda.
    static float[] GerarLoop()
    {
        int dur = (int)(SR * 2.0f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float hum = (Mathf.Sin(2f * Mathf.PI * 110f * t)
                      +  Mathf.Sin(2f * Mathf.PI * 130f * t) * 0.6f) * 0.28f;
            float warble = Mathf.Sin(2f * Mathf.PI * 137f * t + 2f * Mathf.Sin(2f * Mathf.PI * 1f * t)) * 0.15f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * 660f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.5f * t)) * 0.06f;
            float sub = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.2f;
            s[i] = Mathf.Clamp(hum + warble + shimmer + sub, -1f, 1f);
        }
        SuavizarRuido(s, 1);
        return Normalizar(s);
    }

    // Fim: o domo dissipa — "un-warp" ascendente (tempo volta) + pop + shimmer sumindo.
    static float[] GerarFim()
    {
        int dur = (int)(SR * 0.4f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 5f);
            float fUp = Mathf.Lerp(120f, 500f, Mathf.Clamp01(t * 3f));
            float up = Mathf.Sin(2f * Mathf.PI * fUp * t) * Mathf.Exp(-t * 5f) * 0.35f;
            float pop = (Random.value * 2f - 1f) * Mathf.Exp(-t * 20f) * 0.3f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * 800f * t) * Mathf.Exp(-t * 8f) * 0.1f;
            float sub = Mathf.Sin(2f * Mathf.PI * 50f * t) * Mathf.Exp(-t * 7f) * 0.25f;
            s[i] = Mathf.Clamp((up + pop + shimmer + sub) * env, -1f, 1f);
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
