#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" (arcanos) para o boss Maga Slime:
// projétil normal, projétil especial, carga do raio, disparo do raio e transição de fase 2.
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Maga)
public static class GerarSomMagaDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3  = 1.18921f;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Maga)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("maga_disparo",      Reverb(GerarDisparo(),      0.35f, 5, 20f, 0.25f));
        GravarWav("maga_especial",     Reverb(GerarEspecial(),     0.45f, 7, 30f, 0.4f));
        GravarWav("maga_raio_carga",   Reverb(GerarRaioCarga(),    0.35f, 5, 22f, 0.2f));
        GravarWav("maga_raio_disparo", Reverb(GerarRaioDisparo(),  0.5f,  9, 32f, 0.4f));
        GravarWav("maga_fase2",        Reverb(GerarFase2(),        0.5f,  9, 36f, 0.45f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Maga gerado em " + PASTA + "/ (maga_disparo, maga_especial, maga_raio_carga, maga_raio_disparo, maga_fase2)");
    }

    // Projétil normal: lançamento arcano curto — whoosh sombrio + tom menor descendente + brilho dissonante.
    static float[] GerarDisparo()
    {
        int dur = (int)(SR * 0.38f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 40f);
            float env = Mathf.Exp(-t * 7f);
            float f = Mathf.Lerp(380f, 165f, Mathf.Clamp01(t * 3f));
            float tom = (Mathf.Sin(2f * Mathf.PI * f * t)
                      +  Mathf.Sin(2f * Mathf.PI * f * MINOR3 * t) * 0.5f) * 0.4f;
            float whoosh = (Random.value * 2f - 1f) * Mathf.Exp(-t * 10f) * 0.35f;
            float brilho = Mathf.Sin(2f * Mathf.PI * 900f * TRITONE * t) * Mathf.Exp(-t * 12f) * 0.12f;
            float raw = (tom + whoosh + brilho) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Projétil especial: feitiço carregado e pesado — sub grave + lamento descendente + tritono + corpo ruidoso.
    static float[] GerarEspecial()
    {
        int dur = (int)(SR * 0.6f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 25f);
            float env = Mathf.Exp(-t * 4f);
            float sub = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 3f) * 0.6f;
            float lamento = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(520f, 120f, Mathf.Clamp01(t * 3f)) * t) * Mathf.Exp(-t * 5f) * 0.4f;
            float diss = Mathf.Sin(2f * Mathf.PI * 300f * TRITONE * t) * Mathf.Exp(-t * 6f) * 0.2f;
            float corpo = (Random.value * 2f - 1f) * Mathf.Exp(-t * 7f) * 0.4f;
            float raw = (sub + lamento + diss + corpo * 0.6f) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.7f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Carga do raio: energia arcana acumulando — whine subindo + crepitar AM + hum grave + overtom menor.
    static float[] GerarRaioCarga()
    {
        int dur = (int)(SR * 0.8f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 12f);
            float prog = Mathf.Clamp01(t / 0.8f);
            float intens = 0.3f + 0.7f * prog;
            float whine = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(180f, 760f, prog) * t) * 0.22f * intens;
            float crackle = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 35f * t)) * 0.28f * intens;
            float hum = Mathf.Sin(2f * Mathf.PI * 58f * t) * 0.25f * intens;
            float over = Mathf.Sin(2f * Mathf.PI * 220f * MINOR3 * t) * 0.12f * intens;
            s[i] = Mathf.Clamp((whine + crackle + hum + over) * master, -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Disparo do raio: feixe arcano intenso — tom lancinante + chiado + zap descendente + ronco grave.
    static float[] GerarRaioDisparo()
    {
        int dur = (int)(SR * 0.7f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 50f);
            float env = Mathf.Exp(-t * 3.5f);
            float sear = (Mathf.Sin(2f * Mathf.PI * 520f * t)
                       +  Mathf.Sin(2f * Mathf.PI * 520f * TRITONE * t) * 0.5f) * 0.3f;
            float sizzle = (Random.value * 2f - 1f) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 60f * t)) * 0.3f;
            float zap = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(1200f, 300f, Mathf.Clamp01(t * 2f)) * t) * Mathf.Exp(-t * 5f) * 0.3f;
            float roar = Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.4f;
            float raw = (sear + sizzle + zap + roar) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.7f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Fase 2 ("MODO FÚRIA"): surto sombrio crescente — ronco subindo + swell dissonante + ruído, culminando.
    static float[] GerarFase2()
    {
        int dur = (int)(SR * 1.0f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float prog = Mathf.Clamp01(t / 1.0f);
            float master = Mathf.Clamp01(t * 8f);
            float fadeOut = Mathf.Clamp01((1.0f - t) / 0.25f);
            float roar = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(48f, 110f, prog) * t) * 0.5f;
            float swell = (Mathf.Sin(2f * Mathf.PI * 140f * t)
                        +  Mathf.Sin(2f * Mathf.PI * 140f * TRITONE * t) * 0.6f) * Mathf.Lerp(0.1f, 0.4f, prog);
            float ruido = (Random.value * 2f - 1f) * Mathf.Lerp(0.08f, 0.38f, prog) * 0.4f;
            // Batida de clímax perto do fim
            float climax = Mathf.Sin(2f * Mathf.PI * 300f * MINOR3 * t) * Mathf.Exp(-Mathf.Abs(t - 0.7f) * 10f) * 0.25f;
            float raw = (roar + swell + ruido + climax) * master * fadeOut * (0.4f + 0.6f * prog);
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.75f), -1f, 1f);
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
