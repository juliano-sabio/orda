#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para a ultimate Drenagem de Vida:
// início (sifão), loop (dreno pulsante) e fim (liberação).
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Drenagem)
public static class GerarSomDrenagemDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3 = 1.18921f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Drenagem)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("drenagem_inicio", Reverb(GerarInicio(), 0.45f, 7, 30f, 0.32f));
        GravarWav("drenagem_loop",   GerarLoop());           // SEM reverb (loop sem emenda)
        GravarWav("drenagem_fim",    Reverb(GerarFim(),    0.4f, 6, 26f, 0.3f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Drenagem gerada em " + PASTA + "/ (inicio, loop, fim)");
    }

    // Início: sucção sombria — tom descendente (puxando) + throb visceral + ruído molhado + grave.
    static float[] GerarInicio()
    {
        int dur = (int)(SR * 0.6f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 14f);
            float fSuck = Mathf.Lerp(500f, 90f, Mathf.Clamp01(t * 2.2f));
            float suck = Mathf.Sin(2f * Mathf.PI * fSuck * t) * Mathf.Exp(-t * 4f) * 0.4f;
            float throb = Mathf.Sin(2f * Mathf.PI * 70f * t) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 8f * t)) * Mathf.Clamp01(t / 0.3f) * 0.3f;
            float wet = (Random.value * 2f - 1f) * Mathf.Exp(-t * 5f) * 0.18f;
            float edge = Mathf.Sin(2f * Mathf.PI * 200f * MINOR3 * t) * Mathf.Exp(-t * 7f) * 0.1f;
            float sub = Mathf.Sin(2f * Mathf.PI * 48f * t) * Mathf.Clamp01(t / 0.25f) * 0.3f;
            float raw = (suck + throb + wet + edge + sub) * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.78f), -1f, 1f);
        }
        SuavizarRuido(s, 1);
        return Normalizar(s);
    }

    // Loop (2s): dreno contínuo — drone pulsante (AM) + sifão molhado + whine dissonante + sub.
    // Frequências/LFOs múltiplos de 0.5Hz -> loop sem emenda.
    static float[] GerarLoop()
    {
        int dur = (int)(SR * 2.0f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float throb = (Mathf.Sin(2f * Mathf.PI * 90f * t)
                        +  Mathf.Sin(2f * Mathf.PI * 107f * t) * 0.4f) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 3f * t)) * 0.25f;
            float wet = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 4.5f * t)) * 0.12f;
            float whine = Mathf.Sin(2f * Mathf.PI * 330f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 1f * t)) * 0.06f;
            float sub = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.18f;
            s[i] = Mathf.Clamp(throb + wet + whine + sub, -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Fim: liberação — whoosh ascendente + solta + resolve grave quente (curado).
    static float[] GerarFim()
    {
        int dur = (int)(SR * 0.4f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 5f);
            float fUp = Mathf.Lerp(100f, 400f, Mathf.Clamp01(t * 3f));
            float up = Mathf.Sin(2f * Mathf.PI * fUp * t) * Mathf.Exp(-t * 5f) * 0.3f;
            float solta = (Random.value * 2f - 1f) * Mathf.Exp(-t * 12f) * 0.2f;
            float low = Mathf.Sin(2f * Mathf.PI * 80f * t) * Mathf.Exp(-t * 6f) * 0.3f;
            s[i] = Mathf.Clamp((up + solta + low) * env, -1f, 1f);
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
