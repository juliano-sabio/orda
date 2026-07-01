#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" (suporte arcano) para o mob SlimeProtetoraInimiga:
// carga comum + escudo (ward), buff (empoderar) e projétil anti-ultimate (negação).
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Slime Protetora)
public static class GerarSomSlimeProtetoraDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3  = 1.18921f;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Slime Protetora)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("slimeprot_carga",    Reverb(GerarCarga(),    0.3f,  4, 18f, 0.18f));
        GravarWav("slimeprot_escudo",   Reverb(GerarEscudo(),   0.45f, 8, 34f, 0.4f));
        GravarWav("slimeprot_buff",     Reverb(GerarBuff(),     0.4f,  7, 30f, 0.38f));
        GravarWav("slimeprot_projetil", Reverb(GerarProjetil(), 0.4f,  6, 26f, 0.35f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Slime Protetora gerado em " + PASTA + "/ (slimeprot_carga, slimeprot_escudo, slimeprot_buff, slimeprot_projetil)");
    }

    // Carga: energia arcana de suporte acumulando — whine subindo + hum + shimmer + overtom místico.
    static float[] GerarCarga()
    {
        int dur = (int)(SR * 0.6f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 10f);
            float prog = Mathf.Clamp01(t / 0.6f);
            float intens = 0.25f + 0.75f * prog;
            float whine = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(180f, 600f, prog) * t) * 0.18f * intens;
            float hum = Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.22f * intens;
            float shimmer = Mathf.Sin(2f * Mathf.PI * 1000f * t) * 0.1f * intens * prog;
            float crackle = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 25f * t)) * 0.15f * intens;
            float mistico = Mathf.Sin(2f * Mathf.PI * 300f * MINOR3 * t) * 0.1f * intens;
            s[i] = Mathf.Clamp((whine + hum + shimmer + crackle + mistico) * master, -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Escudo (ward): domo protetor ressonante — acorde quente (raiz+quinta+oitava) + wub do domo + brilho.
    static float[] GerarEscudo()
    {
        int dur = (int)(SR * 0.6f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 30f);
            float env = Mathf.Exp(-t * 3.5f);
            float domo = (Mathf.Sin(2f * Mathf.PI * 220f * t)
                       +  Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.6f
                       +  Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.4f) * 0.3f;
            float wub = Mathf.Sin(2f * Mathf.PI * 130f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 6f * t)) * 0.3f;
            float brilho = Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 5f) * 0.12f;
            float raw = (domo + wub + brilho) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.9f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Buff (empoderar): tom ascendente empoderador — sweep pra cima + brilho cintilante + calor grave.
    static float[] GerarBuff()
    {
        int dur = (int)(SR * 0.5f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float prog = Mathf.Clamp01(t / 0.5f);
            float env = Mathf.Sin(prog * Mathf.PI);
            float master = Mathf.Clamp01(t * 30f);
            float sweep = (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(300f, 600f, prog) * t)
                        +  Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(450f, 900f, prog) * t) * 0.5f) * 0.3f;
            float sparkle = Mathf.Sin(2f * Mathf.PI * 1300f * t) * Mathf.Exp(-t * 4f) * 0.12f * prog;
            float calor = Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.2f;
            float raw = (sweep + sparkle + calor) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.9f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Projétil anti-ultimate (negação): disparo opressivo — baque descendente + par dissonante + sub + ruído.
    static float[] GerarProjetil()
    {
        int dur = (int)(SR * 0.5f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 40f);
            float env = Mathf.Exp(-t * 5f);
            float whump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(200f, 80f, Mathf.Clamp01(t * 4f)) * t) * Mathf.Exp(-t * 5f) * 0.5f;
            float diss = (Mathf.Sin(2f * Mathf.PI * 400f * t) + Mathf.Sin(2f * Mathf.PI * 400f * TRITONE * t) * 0.6f) * Mathf.Exp(-t * 7f) * 0.25f;
            float sub = Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-t * 4f) * 0.3f;
            float ruido = (Random.value * 2f - 1f) * Mathf.Exp(-t * 9f) * 0.2f;
            float raw = (whump + diss + sub + ruido) * env * master;
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
