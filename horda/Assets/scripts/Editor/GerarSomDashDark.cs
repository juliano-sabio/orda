#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais do dash: "usar" (investida/whoosh com zip descendente e sub-thump) e
// "coletar" (blip energético ascendente com shimmer de espírito). Salva em Assets/Resources/Sons/.
// Menu: Tools/Sons/Gerar Sons Dark (Dash)
public static class GerarSomDashDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Dash)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("dash_usar",    Reverb(GerarUsar(),    0.30f, 5, 18f, 0.22f));
        GravarWav("dash_coletar", Reverb(GerarColeta(),  0.40f, 7, 26f, 0.35f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Dash gerado em " + PASTA + "/ (dash_usar, dash_coletar)");
    }

    // Usar dash: sub-thump grave no impulso + whoosh de ar (ruído passa-baixa com corte caindo,
    // ar passando) + um "zip" descendente (a investida cortando o espaço). Curto e seco.
    static float[] GerarUsar()
    {
        int dur = (int)(SR * 0.32f);
        var s = new float[dur];
        float lp = 0f; // estado do passa-baixa do ruído
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float p = t / 0.32f;
            float env = Mathf.Clamp01(t * 80f) * Mathf.Exp(-t * 9f);

            // Whoosh: ruído filtrado com corte caindo (ar passando cada vez mais "grave")
            float n   = Random.value * 2f - 1f;
            float cut = Mathf.Lerp(0.55f, 0.08f, p);
            lp += (n - lp) * cut;
            float swoosh = lp * 1.2f;

            // Zip da investida: seno varrendo de agudo pra grave, decaindo rápido
            float freq = Mathf.Lerp(720f, 180f, Mathf.Pow(p, 0.7f));
            float zip  = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f * Mathf.Exp(-t * 11f);

            // Sub-thump grave no arranque
            float thump = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 30f) * 0.4f;

            float raw  = (swoosh + zip) * env + thump;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.9f), -1f, 1f);
        }
        return Normalizar(s);
    }

    // Coletar dash: blip energético ascendente (seno varrendo pra cima com harmônicos) + shimmer
    // etéreo agudo com vibrato (toque de espírito). Distinto do XP (que são dois toques metálicos).
    static float[] GerarColeta()
    {
        int dur = (int)(SR * 0.4f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float p = t / 0.4f;
            float env = Mathf.Clamp01(t * 120f) * Mathf.Exp(-t * 6f);

            // Corpo: varredura ascendente 520→1500 Hz com harmônicos (energia carregando)
            float freq = Mathf.Lerp(520f, 1500f, Mathf.Pow(p, 0.5f));
            float body = (Mathf.Sin(2f * Mathf.PI * freq * t)
                       +  Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.4f
                       +  Mathf.Sin(2f * Mathf.PI * freq * 3f * t) * 0.18f) * 0.4f;

            // Shimmer de espírito (agudo, vibrato, decai devagar)
            float vib     = Mathf.Sin(2f * Mathf.PI * 7f * t) * 10f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * (2600f + vib) * t) * Mathf.Exp(-t * 4f) * 0.1f;

            float raw  = body * env + shimmer;
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
