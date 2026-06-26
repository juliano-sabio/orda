#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para a Lança de Luz (arremesso + perfuração sombrios).
// Versão melhorada: camadas mais ricas + cauda de reverb pra dar espaço/cinematografia.
// Salva em Assets/Resources/Sons/ (SomSkill carrega via Resources.Load).
// Menu: Tools/Sons/Gerar Sons Dark (Lanca)
public static class GerarSomLancaDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3 = 1.18921f; // terça menor
    const float TRITONE = 1.41421f; // trítono (diabolus in musica) — bem sombrio

    [MenuItem("Tools/Sons/Gerar Sons Dark (Lanca)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("lanca_disparo_dark", Reverb(GerarDisparo(), 0.45f, 7, 28f, 0.28f));
        GravarWav("lanca_impacto_dark", Reverb(GerarImpacto(), 0.5f,  8, 24f, 0.35f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Lanca (melhorada) gerada em " + PASTA + "/");
    }

    // Disparo: arremesso — build-up de ar, "thwoom" descendente forte, shimmer mágico e empurrão grave.
    static float[] GerarDisparo()
    {
        int dur = (int)(SR * 0.42f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            // Envelope com ataque suave e corpo sustentado
            float env = Mathf.Clamp01(t * 35f) * Mathf.Exp(-t * 6.5f);

            // Whoosh: ruído filtrado (low-passado por média simples depois) que sobe e cai
            float whooshEnv = Mathf.Exp(-Mathf.Pow((t - 0.06f) / 0.06f, 2f));
            float whoosh = (Random.value * 2f - 1f) * whooshEnv * 0.55f;

            // "Thwoom": tom grave descendente forte (a lança rasgando o ar)
            float fMain = Mathf.Lerp(480f, 80f, Mathf.Clamp01(t * 3.2f));
            float thwoom = Mathf.Sin(2f * Mathf.PI * fMain * t) * Mathf.Exp(-t * 6f) * 0.6f;
            // 2º harmônico sutil pra encorpar
            float thwoom2 = Mathf.Sin(2f * Mathf.PI * fMain * 2f * t) * Mathf.Exp(-t * 9f) * 0.18f;

            // Shimmer mágico: partials altos dissonantes que cintilam e somem
            float shEnv = Mathf.Exp(-t * 13f);
            float shimmer = (Mathf.Sin(2f * Mathf.PI * 900f * t)
                          +  Mathf.Sin(2f * Mathf.PI * 900f * MINOR3 * t)
                          +  Mathf.Sin(2f * Mathf.PI * 900f * TRITONE * t)) * shEnv * 0.10f;

            // Empurrão grave
            float push = Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-t * 10f) * 0.45f;

            float raw = whoosh + thwoom + thwoom2 + shimmer + push;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = sign * Mathf.Pow(Mathf.Abs(raw), 0.85f) * env; // saturação suave
        }
        SuavizarRuido(s, 2); // dá "corpo" ao whoosh (low-pass leve)
        return Normalizar(s);
    }

    // Impacto: perfuração — estocada nítida + baque grave + ressonância metálica dissonante longa.
    static float[] GerarImpacto()
    {
        int dur = (int)(SR * 0.32f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 9f);

            // Transiente de estocada (clique seco + ruído)
            float estoc = (Random.value * 2f - 1f) * Mathf.Exp(-t * 38f) * 0.7f;
            float click = Mathf.Sin(2f * Mathf.PI * 1200f * t) * Mathf.Exp(-t * 60f) * 0.4f;

            // Baque grave com leve queda de tom
            float fThud = Mathf.Lerp(110f, 70f, Mathf.Clamp01(t * 6f));
            float baque = Mathf.Sin(2f * Mathf.PI * fThud * t) * Mathf.Exp(-t * 12f) * 0.6f;
            float sub   = Mathf.Sin(2f * Mathf.PI * 48f * t) * Mathf.Exp(-t * 7f) * 0.4f;

            // Ressonância metálica dissonante (mais longa)
            float ring = (Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.5f
                       +  Mathf.Sin(2f * Mathf.PI * 240f * MINOR3 * t) * 0.3f
                       +  Mathf.Sin(2f * Mathf.PI * 240f * TRITONE * t) * 0.2f) * Mathf.Exp(-t * 8f) * 0.4f;

            float raw = (estoc + click + baque + sub + ring) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.78f), -1f, 1f);
        }
        return Normalizar(s);
    }

    // Low-pass simples (média móvel) — suaviza o ruído pra soar menos "estática".
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

    // Reverb simples: somatório de taps decaindo (densos) pra dar cauda/espaço.
    static float[] Reverb(float[] dry, float decay, int taps, float spreadMs, float wetMix)
    {
        int spread = Mathf.Max(1, (int)(SR * spreadMs / 1000f));
        int extra = spread * taps + SR / 6;
        var wet = new float[dry.Length + extra];

        for (int i = 0; i < dry.Length; i++) wet[i] += dry[i]; // sinal seco

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
