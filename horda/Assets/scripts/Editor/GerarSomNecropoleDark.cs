#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para a ultimate Necrópole (necromancia):
// invocação (ritual), loop (ambiente de cripta) e fantasma (lamento ao invocar).
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Necropole)
public static class GerarSomNecropoleDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3 = 1.18921f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Necropole)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("necropole_inicio",   Reverb(GerarInicio(),   0.55f, 9, 36f, 0.42f));
        GravarWav("necropole_loop",     GerarLoop());            // SEM reverb (loop sem emenda)
        GravarWav("necropole_fantasma", Reverb(GerarFantasma(), 0.5f,  7, 30f, 0.38f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Necropole gerada em " + PASTA + "/ (inicio, loop, fantasma)");
    }

    // Invocação: ritual necromântico — coro grave menor subindo + swell + sussurros + badalada + sub.
    static float[] GerarInicio()
    {
        int dur = (int)(SR * 0.8f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 12f);
            float build = Mathf.Clamp01(t / 0.5f);
            float pad = (Mathf.Sin(2f * Mathf.PI * 110.00f * t)
                      +  Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.6f
                      +  Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.5f) * build * 0.25f;
            float swell = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(70f, 180f, build) * t) * build * 0.25f;
            float sussurro = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 7f * t)) * 0.12f * build;
            float p2 = Mathf.Max(0f, t - 0.45f);
            float ativo2 = t >= 0.45f ? 1f : 0f;
            float toll = (Mathf.Sin(2f * Mathf.PI * 65f * t)
                       +  Mathf.Sin(2f * Mathf.PI * 65f * MINOR3 * t) * 0.4f) * Mathf.Exp(-p2 * 3.5f) * 0.4f * ativo2;
            float sub = Mathf.Sin(2f * Mathf.PI * 45f * t) * build * 0.3f;
            float raw = (pad + swell + sussurro + toll + sub) * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Loop (2s): ambiente de cripta — drone grave + sussurros (AM) + pad dissonante tênue + sub.
    // Frequências/LFOs múltiplos de 0.5Hz -> loop sem emenda.
    static float[] GerarLoop()
    {
        int dur = (int)(SR * 2.0f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float drone = (Mathf.Sin(2f * Mathf.PI * 90f * t)
                        +  Mathf.Sin(2f * Mathf.PI * 107f * t) * 0.5f) * 0.25f;
            float sussurro = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 1.5f * t)) * 0.13f;
            float pad = Mathf.Sin(2f * Mathf.PI * 220f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.5f * t)) * 0.06f;
            float sub = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.18f;
            s[i] = Mathf.Clamp(drone + sussurro + pad + sub, -1f, 1f);
        }
        SuavizarRuido(s, 3);
        return Normalizar(s);
    }

    // Fantasma: lamento espectral — tom que sobe e cai (gemido) + parcial dissonante + ar + grave.
    static float[] GerarFantasma()
    {
        int dur = (int)(SR * 0.5f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Clamp01(t * 20f) * Mathf.Exp(-t * 4f);
            float fWail = t < 0.15f
                ? Mathf.Lerp(300f, 600f, t / 0.15f)
                : Mathf.Lerp(600f, 200f, Mathf.Clamp01((t - 0.15f) * 2.5f));
            float wail  = Mathf.Sin(2f * Mathf.PI * fWail * t) * 0.4f;
            float wailH = Mathf.Sin(2f * Mathf.PI * fWail * MINOR3 * t) * 0.2f;
            float ar    = (Random.value * 2f - 1f) * Mathf.Exp(-t * 5f) * 0.15f;
            float grave = Mathf.Sin(2f * Mathf.PI * 80f * t) * Mathf.Exp(-t * 7f) * 0.2f;
            float raw = (wail + wailH + ar + grave) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.85f), -1f, 1f);
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
