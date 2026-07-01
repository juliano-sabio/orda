#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" (fogo arcano) para o mob slime_maga:
// carga da bola de fogo, disparo e explosão.
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Slime Maga)
public static class GerarSomSlimeMagaDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Slime Maga)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("slimemaga_carga",    Reverb(GerarCarga(),    0.3f,  4, 18f, 0.15f));
        GravarWav("slimemaga_disparo",  Reverb(GerarDisparo(),  0.35f, 5, 20f, 0.25f));
        GravarWav("slimemaga_explosao", Reverb(GerarExplosao(), 0.45f, 7, 30f, 0.4f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Slime Maga gerado em " + PASTA + "/ (slimemaga_carga, slimemaga_disparo, slimemaga_explosao)");
    }

    // Carga: fogo acumulando — crepitar AM crescente + hum aquecendo + brilho sutil, intensificando.
    static float[] GerarCarga()
    {
        int dur = (int)(SR * 1.0f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 10f);
            float prog = Mathf.Clamp01(t / 1.0f);
            float intens = 0.25f + 0.75f * prog;
            float crackle = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 30f * t)) * 0.3f * intens;
            float hum = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(70f, 130f, prog) * t) * 0.25f * intens;
            float brilho = Mathf.Sin(2f * Mathf.PI * 600f * t) * 0.1f * intens * prog;
            s[i] = Mathf.Clamp((crackle + hum + brilho) * master, -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Disparo: lançamento da bola de fogo — whoosh flamejante + baque grave descendente + cauda crepitante.
    static float[] GerarDisparo()
    {
        int dur = (int)(SR * 0.4f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 40f);
            float env = Mathf.Exp(-t * 7f);
            float whoosh = (Random.value * 2f - 1f) * Mathf.Exp(-t * 9f) * 0.4f;
            float whump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(180f, 80f, Mathf.Clamp01(t * 4f)) * t) * Mathf.Exp(-t * 6f) * 0.5f;
            float crackle = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 40f * t)) * Mathf.Exp(-t * 8f) * 0.25f;
            float raw = (whoosh + whump + crackle) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.75f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Explosão: estouro em área — boom grave + rajada ruidosa + crepitar + corpo dissonante.
    static float[] GerarExplosao()
    {
        int dur = (int)(SR * 0.6f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 60f);
            float env = Mathf.Exp(-t * 4f);
            float boom = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 3f) * 0.6f;
            float burst = (Random.value * 2f - 1f) * Mathf.Exp(-t * 7f) * 0.5f;
            float crackle = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 45f * t)) * Mathf.Exp(-t * 5f) * 0.3f;
            float corpo = Mathf.Sin(2f * Mathf.PI * 140f * TRITONE * t) * Mathf.Exp(-t * 5f) * 0.2f;
            float raw = (boom + burst + crackle + corpo) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.65f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
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
