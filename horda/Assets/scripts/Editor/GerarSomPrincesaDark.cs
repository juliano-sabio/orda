#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para a Princesa Slime (regal + sombrio + slime):
// canalização, disparo de projéteis, transição de fase 2 e morte.
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Princesa)
public static class GerarSomPrincesaDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3  = 1.18921f;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Princesa)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("princesa_canalizar", Reverb(GerarCanalizar(), 0.4f, 5, 30f, 0.2f));
        GravarWav("princesa_disparo",   Reverb(GerarDisparo(),   0.42f, 6, 24f, 0.3f));
        GravarWav("princesa_fase2",     Reverb(GerarFase2(),     0.5f, 9, 32f, 0.42f));
        GravarWav("princesa_morte",     Reverb(GerarMorte(),     0.5f, 9, 34f, 0.4f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Princesa gerada em " + PASTA + "/ (canalizar, disparo, fase2, morte)");
    }

    // Canalização (melhorada): drone arcano sombrio que CRESCE em tensão ao longo de ~3s —
    // pra durar a canalização inteira (o áudio é cortado no lançamento dos projéteis).
    static float[] GerarCanalizar()
    {
        int dur = (int)(SR * 3.0f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float prog = t / (dur / (float)SR);          // 0..1 ao longo do clipe
            float master = Mathf.Clamp01(t * 8f);        // fade-in rápido
            float intens = 0.4f + 0.6f * prog;           // intensifica conforme acumula

            // Drone arcano menor, subindo de tom devagar (tensão crescente)
            float fBase = Mathf.Lerp(80f, 130f, prog);
            float drone = (Mathf.Sin(2f * Mathf.PI * fBase * t)
                        +  Mathf.Sin(2f * Mathf.PI * fBase * MINOR3 * t) * 0.5f
                        +  Mathf.Sin(2f * Mathf.PI * fBase * 1.5f * t) * 0.4f) * 0.3f;

            // Tremolo de canalização (pulsa mais rápido conforme cresce)
            float trem = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * (4f + 4f * prog) * t);

            // Borbulhar de slime (ruído AM)
            float bolha = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 11f * t)) * 0.12f * intens;

            // Shimmer dissonante subindo (pico no fim, energia prestes a explodir)
            float fSh = Mathf.Lerp(300f, 700f, prog);
            float shimmer = (Mathf.Sin(2f * Mathf.PI * fSh * t)
                          +  Mathf.Sin(2f * Mathf.PI * fSh * TRITONE * t) * 0.5f) * 0.1f * intens;

            float sub = Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.25f;

            float raw = (drone * trem + bolha + shimmer + sub) * intens * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = sign * Mathf.Pow(Mathf.Abs(raw), 0.85f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Disparo: barragem de projéteis lançada — fwoom mágico descendente + whoosh + zing magenta dissonante.
    static float[] GerarDisparo()
    {
        int dur = (int)(SR * 0.35f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Clamp01(t * 45f) * Mathf.Exp(-t * 7f);
            float fTone = Mathf.Lerp(700f, 150f, Mathf.Clamp01(t * 3f));
            float tone  = Mathf.Sin(2f * Mathf.PI * fTone * t) * Mathf.Exp(-t * 6f) * 0.5f;
            float whoosh = (Random.value * 2f - 1f) * Mathf.Exp(-t * 8f) * 0.45f;
            float zing = (Mathf.Sin(2f * Mathf.PI * 1100f * t)
                       +  Mathf.Sin(2f * Mathf.PI * 1100f * MINOR3 * t) * 0.6f) * Mathf.Exp(-t * 12f) * 0.12f;
            float sub = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 9f) * 0.4f;
            float raw = (tone + whoosh + zing + sub) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
        }
        SuavizarRuido(s, 1);
        return Normalizar(s);
    }

    // Fase 2: enfurecer — swell dissonante ascendente -> impacto + rosnado + acorde menor sinistro + textura de rugido.
    static float[] GerarFase2()
    {
        int dur = (int)(SR * 0.9f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 20f);
            float subir = Mathf.Clamp01(t / 0.35f);
            float decA = t < 0.35f ? 1f : Mathf.Exp(-(t - 0.35f) * 5f);
            float fSwell = Mathf.Lerp(80f, 260f, subir);
            float swell = Mathf.Sin(2f * Mathf.PI * fSwell * t) * subir * decA * 0.35f;
            float shriek = (Mathf.Sin(2f * Mathf.PI * 500f * t) * 0.4f
                         +  Mathf.Sin(2f * Mathf.PI * 500f * TRITONE * t) * 0.3f) * subir * decA * 0.2f;

            float p2 = Mathf.Max(0f, t - 0.35f);
            float ativo2 = t >= 0.35f ? 1f : 0f;
            float boom  = Mathf.Sin(2f * Mathf.PI * 45f * t) * Mathf.Exp(-p2 * 4f) * 0.6f * ativo2;
            float fGrowl = Mathf.Lerp(120f, 55f, Mathf.Clamp01(p2 * 2f));
            float growl = Mathf.Sin(2f * Mathf.PI * fGrowl * t) * Mathf.Exp(-p2 * 4f) * 0.4f * ativo2;
            float chord = (Mathf.Sin(2f * Mathf.PI * 98.00f * t)
                        +  Mathf.Sin(2f * Mathf.PI * 116.54f * t) * 0.7f
                        +  Mathf.Sin(2f * Mathf.PI * 146.83f * t) * 0.6f) * Mathf.Exp(-p2 * 3f) * 0.18f * ativo2;
            float rugido = (Random.value * 2f - 1f) * Mathf.Exp(-Mathf.Abs(t - 0.35f) * 8f) * 0.2f;

            float raw = (swell + shriek + boom + growl + chord + rugido) * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.7f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Morte: lamento descendente + splat de slime + shimmer dissolvendo, com cauda longa.
    static float[] GerarMorte()
    {
        int dur = (int)(SR * 0.9f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 15f);
            float env = Mathf.Exp(-t * 2.5f);
            float fWail = Mathf.Lerp(600f, 80f, Mathf.Clamp01(t * 1.3f));
            float wail  = Mathf.Sin(2f * Mathf.PI * fWail * t) * 0.4f;
            float wailH = Mathf.Sin(2f * Mathf.PI * fWail * MINOR3 * t) * 0.2f;
            float splat = (Random.value * 2f - 1f) * Mathf.Exp(-t * 9f) * 0.4f * (t < 0.3f ? 1f : 0.2f);
            float shimmer = (Mathf.Sin(2f * Mathf.PI * 900f * t)
                          +  Mathf.Sin(2f * Mathf.PI * 1300f * t) * 0.5f) * Mathf.Exp(-t * 3f) * 0.1f;
            float sub = Mathf.Sin(2f * Mathf.PI * 50f * t) * Mathf.Exp(-t * 2f) * 0.3f;
            s[i] = Mathf.Clamp((wail + wailH + splat + shimmer + sub) * env * master, -1f, 1f);
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
