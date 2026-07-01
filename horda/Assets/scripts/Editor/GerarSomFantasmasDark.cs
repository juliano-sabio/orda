#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" (espectrais elementais) para os fantasmas elementais:
// elétrico, fogo, gelo e veneno — cada um no ataque à distância do respectivo fantasma.
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Fantasmas)
public static class GerarSomFantasmasDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3  = 1.18921f;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Fantasmas)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("fantasma_eletrico", Reverb(GerarEletrico(), 0.35f, 5, 20f, 0.25f));
        GravarWav("fantasma_fogo",     Reverb(GerarFogo(),     0.35f, 5, 20f, 0.25f));
        GravarWav("fantasma_gelo",     Reverb(GerarGelo(),     0.4f,  6, 26f, 0.35f));
        GravarWav("fantasma_veneno",   Reverb(GerarVeneno(),   0.4f,  6, 24f, 0.3f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Fantasmas gerado em " + PASTA + "/ (fantasma_eletrico, fantasma_fogo, fantasma_gelo, fantasma_veneno)");
    }

    // Elétrico: zap espectral — crepitar seco + zap descendente + whine agudo + undertom fantasmagórico.
    static float[] GerarEletrico()
    {
        int dur = (int)(SR * 0.4f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 45f);
            float env = Mathf.Exp(-t * 7f);
            float crackle = (Random.value * 2f - 1f) * Mathf.Exp(-t * 20f) * 0.6f;
            float zap = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 200f, Mathf.Clamp01(t * 4f)) * t) * Mathf.Exp(-t * 8f) * 0.4f;
            float whine = Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 10f) * 0.15f;
            float fantasma = Mathf.Sin(2f * Mathf.PI * 320f * t) * Mathf.Exp(-t * 5f) * 0.2f;
            float raw = (crackle + zap + whine + fantasma) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.7f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Fogo: labareda espectral — whoosh ruidoso + baque grave descendente + crepitar + undertom menor.
    static float[] GerarFogo()
    {
        int dur = (int)(SR * 0.45f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 40f);
            float env = Mathf.Exp(-t * 6f);
            float whoosh = (Random.value * 2f - 1f) * Mathf.Exp(-t * 8f) * 0.45f;
            float whump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(200f, 90f, Mathf.Clamp01(t * 4f)) * t) * Mathf.Exp(-t * 6f) * 0.4f;
            float crackle = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 40f * t)) * Mathf.Exp(-t * 7f) * 0.25f;
            float fantasma = Mathf.Sin(2f * Mathf.PI * 260f * MINOR3 * t) * Mathf.Exp(-t * 5f) * 0.15f;
            float raw = (whoosh + whump + crackle + fantasma) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.7f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Gelo: estilhaço cristalino — "tink" vítreo agudo + brilho ascendente + sopro gélido + undertom frio.
    static float[] GerarGelo()
    {
        int dur = (int)(SR * 0.45f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float prog = Mathf.Clamp01(t / 0.45f);
            float master = Mathf.Clamp01(t * 40f);
            float env = Mathf.Exp(-t * 7f);
            float tink = (Mathf.Sin(2f * Mathf.PI * 1200f * t)
                       +  Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.5f) * Mathf.Exp(-t * 12f) * 0.3f;
            float brilho = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(600f, 900f, prog) * t) * Mathf.Exp(-t * 6f) * 0.2f;
            float hiss = (Random.value * 2f - 1f) * Mathf.Exp(-t * 10f) * 0.2f;
            float low = Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 5f) * 0.2f;
            float raw = (tink + brilho + hiss + low) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.85f), -1f, 1f);
        }
        SuavizarRuido(s, 1);
        return Normalizar(s);
    }

    // Veneno: borbulhar espectral — bolhas (ruído AM) + chiado + tom sinistro descendente + murk grave dissonante.
    static float[] GerarVeneno()
    {
        int dur = (int)(SR * 0.5f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float prog = Mathf.Clamp01(t / 0.5f);
            float master = Mathf.Clamp01(t * 35f);
            float env = Mathf.Exp(-t * 5f);
            float bolha = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 18f * t)) * Mathf.Exp(-t * 5f) * 0.4f;
            float sizzle = (Random.value * 2f - 1f) * Mathf.Exp(-t * 9f) * 0.2f;
            float tom = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(400f, 180f, prog) * t) * Mathf.Exp(-t * 6f) * 0.3f;
            float murk = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 4f) * 0.25f;
            float diss = Mathf.Sin(2f * Mathf.PI * 300f * TRITONE * t) * Mathf.Exp(-t * 7f) * 0.12f;
            float raw = (bolha + sizzle + tom + murk + diss) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.7f), -1f, 1f);
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
