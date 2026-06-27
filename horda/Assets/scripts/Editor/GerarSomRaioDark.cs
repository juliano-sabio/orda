#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para a ultimate Raio Certeiro (carga + trovão + ricochete).
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Raio)
public static class GerarSomRaioDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Raio)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("raio_carga",   Reverb(GerarCarga(),    0.35f, 5, 24f, 0.2f));
        GravarWav("raio_disparo", Reverb(GerarDisparo(),  0.5f,  9, 34f, 0.4f));
        GravarWav("raio_bounce",  Reverb(GerarBounce(),   0.4f,  5, 18f, 0.25f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Raio gerado em " + PASTA + "/ (raio_carga, raio_disparo, raio_bounce)");
    }

    // Carga: energia elétrica acumulando — crepitar AM crescente, whine subindo e hum grave.
    static float[] GerarCarga()
    {
        int dur = (int)(SR * 0.45f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 15f);
            float prog = Mathf.Clamp01(t / 0.45f);
            float intens = 0.3f + 0.7f * prog;
            float crackle = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 40f * t)) * 0.3f * intens;
            float whine = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(200f, 900f, prog) * t) * 0.2f * intens;
            float hum = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.25f * intens;
            s[i] = Mathf.Clamp((crackle + whine + hum) * master, -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Disparo: trovão sombrio — estalo seco + zap descendente + ronco grave + rumble + fio dissonante.
    static float[] GerarDisparo()
    {
        int dur = (int)(SR * 0.6f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 4f);
            float estalo = (Random.value * 2f - 1f) * Mathf.Exp(-t * 25f) * 0.8f;
            float zap = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 150f, Mathf.Clamp01(t * 4f)) * t) * Mathf.Exp(-t * 7f) * 0.4f;
            float ronco = Mathf.Sin(2f * Mathf.PI * 45f * t) * Mathf.Exp(-t * 3f) * 0.6f;
            float rumble = (Random.value * 2f - 1f) * Mathf.Exp(-t * 4f) * 0.4f;
            float fio = Mathf.Sin(2f * Mathf.PI * 300f * TRITONE * t) * Mathf.Exp(-t * 9f) * 0.1f;
            float raw = (estalo * 0.7f + zap + ronco + rumble * 0.5f + fio) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.65f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Ricochete: estalo elétrico curto — crack + zing dissonante + sub.
    static float[] GerarBounce()
    {
        int dur = (int)(SR * 0.16f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 16f);
            float crack = (Random.value * 2f - 1f) * Mathf.Exp(-t * 45f) * 0.6f;
            float zing = (Mathf.Sin(2f * Mathf.PI * 700f * t)
                       +  Mathf.Sin(2f * Mathf.PI * 700f * TRITONE * t) * 0.5f) * Mathf.Exp(-t * 14f) * 0.3f;
            float sub = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 18f) * 0.3f;
            float raw = (crack * 0.6f + zing + sub) * env;
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
