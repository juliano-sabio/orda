#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" (espectrais/bestiais) para o boss Criatura da Noite (BossCaveira):
// projétil, emboscada, investida, grito sônico, garras das sombras e transição de fase 2.
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Caveira)
public static class GerarSomCaveiraDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3  = 1.18921f;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Caveira)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("caveira_disparo",   Reverb(GerarDisparo(),   0.35f, 5, 20f, 0.25f));
        GravarWav("caveira_emboscada", Reverb(GerarEmboscada(), 0.45f, 7, 28f, 0.4f));
        GravarWav("caveira_investida", Reverb(GerarInvestida(), 0.4f,  6, 24f, 0.3f));
        GravarWav("caveira_grito",     Reverb(GerarGrito(),     0.5f,  9, 34f, 0.45f));
        GravarWav("caveira_garras",    Reverb(GerarGarras(),    0.35f, 5, 18f, 0.25f));
        GravarWav("caveira_fase2",     Reverb(GerarFase2(),     0.5f,  9, 36f, 0.45f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Caveira gerado em " + PASTA + "/ (caveira_disparo, caveira_emboscada, caveira_investida, caveira_grito, caveira_garras, caveira_fase2)");
    }

    // Projétil: dardo espectral oco — sopro ruidoso + tom oco menor descendente + assobio fantasmagórico.
    static float[] GerarDisparo()
    {
        int dur = (int)(SR * 0.35f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 40f);
            float env = Mathf.Exp(-t * 8f);
            float f = Mathf.Lerp(300f, 135f, Mathf.Clamp01(t * 3f));
            float tom = (Mathf.Sin(2f * Mathf.PI * f * t)
                      +  Mathf.Sin(2f * Mathf.PI * f * MINOR3 * t) * 0.4f) * 0.35f;
            float sopro = (Random.value * 2f - 1f) * Mathf.Exp(-t * 11f) * 0.4f;
            float assobio = Mathf.Sin(2f * Mathf.PI * 1100f * t) * Mathf.Exp(-t * 9f) * 0.1f;
            float raw = (tom + sopro + assobio) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Emboscada: materializar das sombras — sopro crescente + whoosh grave subindo + baque grave ao surgir.
    static float[] GerarEmboscada()
    {
        int dur = (int)(SR * 0.5f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float prog = Mathf.Clamp01(t / 0.5f);
            float master = Mathf.Clamp01(t * 12f);
            float sopro = (Random.value * 2f - 1f) * prog * 0.4f;
            float whoosh = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(110f, 320f, prog) * t) * 0.28f * prog;
            float baque = Mathf.Sin(2f * Mathf.PI * 58f * t) * Mathf.Exp(-Mathf.Abs(t - 0.42f) * 9f) * 0.55f;
            float diss = Mathf.Sin(2f * Mathf.PI * 240f * TRITONE * t) * prog * 0.12f;
            float raw = (sopro + whoosh + baque + diss) * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Investida: arremetida veloz — rajada de vento (ruído em sino) + rosnado grave + tom doppler descendente.
    static float[] GerarInvestida()
    {
        int dur = (int)(SR * 0.6f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float prog = Mathf.Clamp01(t / 0.6f);
            // Envelope em sino: a rajada "passa" pelo ouvinte
            float sino = Mathf.Exp(-Mathf.Pow((t - 0.28f) * 6f, 2f));
            float rajada = (Random.value * 2f - 1f) * sino * 0.5f;
            float rosnado = Mathf.Sin(2f * Mathf.PI * 90f * t) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 18f * t)) * 0.4f;
            float doppler = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(420f, 180f, prog) * t) * sino * 0.25f;
            float raw = (rajada + rosnado * sino + doppler);
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.75f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Grito sônico (assinatura): guincho dilacerante — harmônicos dissonantes com vibrato + raspagem + sub.
    static float[] GerarGrito()
    {
        int dur = (int)(SR * 0.9f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float prog = Mathf.Clamp01(t / 0.9f);
            float master = Mathf.Clamp01(t * 30f);
            float env = Mathf.Exp(-t * 2.2f);
            float vibrato = Mathf.Sin(2f * Mathf.PI * 30f * t) * 18f;
            float f = Mathf.Lerp(620f, 300f, prog) + vibrato;
            float grito = (Mathf.Sin(2f * Mathf.PI * f * t)
                        +  Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.6f
                        +  Mathf.Sin(2f * Mathf.PI * f * TRITONE * t) * 0.4f
                        +  Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.3f) * 0.22f;
            float raspa = (Random.value * 2f - 1f) * 0.35f;
            float sub = Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.3f;
            float raw = (grito + raspa + sub) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.6f), -1f, 1f);
        }
        SuavizarRuido(s, 1);
        return Normalizar(s);
    }

    // Garras das sombras: retalhar — 3 golpes ruidosos secos + rasgo descendente + sub.
    static float[] GerarGarras()
    {
        int dur = (int)(SR * 0.45f);
        var s = new float[dur];
        float[] golpes = { 0.02f, 0.11f, 0.20f };
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float slash = 0f;
            foreach (float ti in golpes)
                slash += (Random.value * 2f - 1f) * Mathf.Exp(-Mathf.Abs(t - ti) * 60f) * 0.5f;
            float rasgo = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(520f, 140f, Mathf.Clamp01(t * 3f)) * t) * Mathf.Exp(-t * 7f) * 0.25f;
            float sub = Mathf.Sin(2f * Mathf.PI * 80f * t) * Mathf.Exp(-t * 9f) * 0.35f;
            float raw = (slash + rasgo + sub);
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.75f), -1f, 1f);
        }
        SuavizarRuido(s, 1);
        return Normalizar(s);
    }

    // Fase 2: surto bestial crescente — rosnado subindo + swell dissonante + ruído, culminando num rugido.
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
            float rosnado = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(45f, 105f, prog) * t) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 22f * t)) * 0.5f;
            float swell = (Mathf.Sin(2f * Mathf.PI * 150f * t)
                        +  Mathf.Sin(2f * Mathf.PI * 150f * TRITONE * t) * 0.6f) * Mathf.Lerp(0.1f, 0.4f, prog);
            float ruido = (Random.value * 2f - 1f) * Mathf.Lerp(0.08f, 0.4f, prog) * 0.4f;
            float rugido = Mathf.Sin(2f * Mathf.PI * 320f * MINOR3 * t) * Mathf.Exp(-Mathf.Abs(t - 0.72f) * 9f) * 0.25f;
            float raw = (rosnado + swell + ruido + rugido) * master * fadeOut * (0.4f + 0.6f * prog);
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
