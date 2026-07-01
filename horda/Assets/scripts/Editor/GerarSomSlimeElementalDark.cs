#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" (elementais) para o mob SlimeElemental:
// carga comum (antes de qualquer ataque) + disparo de gelo, vento e fogo.
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Slime Elemental)
public static class GerarSomSlimeElementalDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3  = 1.18921f;
    const float TRITONE = 1.41421f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Slime Elemental)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("slimeelem_carga", Reverb(GerarCarga(), 0.3f,  4, 18f, 0.18f));
        GravarWav("slimeelem_gelo",  Reverb(GerarGelo(),  0.4f,  6, 26f, 0.35f));
        GravarWav("slimeelem_vento", Reverb(GerarVento(), 0.4f,  6, 26f, 0.35f));
        GravarWav("slimeelem_fogo",  Reverb(GerarFogo(),  0.45f, 7, 30f, 0.4f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Slime Elemental gerado em " + PASTA + "/ (slimeelem_carga, slimeelem_gelo, slimeelem_vento, slimeelem_fogo)");
    }

    // Carga: energia elemental acumulando — whine subindo + hum + crepitar + shimmer + overtom menor.
    static float[] GerarCarga()
    {
        int dur = (int)(SR * 0.7f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 10f);
            float prog = Mathf.Clamp01(t / 0.7f);
            float intens = 0.25f + 0.75f * prog;
            float whine = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(150f, 650f, prog) * t) * 0.2f * intens;
            float hum = Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.25f * intens;
            float crackle = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 28f * t)) * 0.2f * intens;
            float shimmer = Mathf.Sin(2f * Mathf.PI * 900f * t) * 0.1f * intens * prog;
            float diss = Mathf.Sin(2f * Mathf.PI * 200f * MINOR3 * t) * 0.1f * intens;
            s[i] = Mathf.Clamp((whine + hum + crackle + shimmer + diss) * master, -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Gelo: zona congelante se formando — baque grave gélido + tilintar cristalino + sopro frio + grave.
    static float[] GerarGelo()
    {
        int dur = (int)(SR * 0.6f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 40f);
            float env = Mathf.Exp(-t * 4f);
            float whoomp = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 70f, Mathf.Clamp01(t * 4f)) * t) * Mathf.Exp(-t * 4f) * 0.5f;
            float tink = (Mathf.Sin(2f * Mathf.PI * 1300f * t) + Mathf.Sin(2f * Mathf.PI * 1950f * t) * 0.5f) * Mathf.Exp(-t * 9f) * 0.25f;
            float hiss = (Random.value * 2f - 1f) * Mathf.Exp(-t * 7f) * 0.25f;
            float low = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 4f) * 0.2f;
            float raw = (whoomp + tink + hiss + low) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
        }
        SuavizarRuido(s, 1);
        return Normalizar(s);
    }

    // Vento: vórtice de ar — sopro redemoinho (ruído modulado) + uivo aéreo subindo + assobio com vibrato.
    static float[] GerarVento()
    {
        int dur = (int)(SR * 0.7f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float prog = Mathf.Clamp01(t / 0.7f);
            float env = Mathf.Sin(prog * Mathf.PI);   // rajada: entra e sai
            float master = Mathf.Clamp01(t * 20f);
            float swirl = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 7f * t)) * 0.5f;
            float uivo = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(220f, 340f, prog) * t) * 0.2f;
            float assobio = Mathf.Sin(2f * Mathf.PI * (700f + Mathf.Sin(2f * Mathf.PI * 5f * t) * 100f) * t) * 0.12f;
            float raw = (swirl + uivo + assobio) * env * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
        }
        SuavizarRuido(s, 2);
        return Normalizar(s);
    }

    // Fogo: impacto/explosão do meteoro — boom grave + rajada ruidosa + crepitar + ronco + corpo dissonante.
    static float[] GerarFogo()
    {
        int dur = (int)(SR * 0.6f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 55f);
            float env = Mathf.Exp(-t * 4f);
            float boom = Mathf.Sin(2f * Mathf.PI * 50f * t) * Mathf.Exp(-t * 3f) * 0.6f;
            float burst = (Random.value * 2f - 1f) * Mathf.Exp(-t * 6f) * 0.5f;
            float crackle = (Random.value * 2f - 1f) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 42f * t)) * Mathf.Exp(-t * 4f) * 0.3f;
            float roar = Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-t * 4f) * 0.25f;
            float diss = Mathf.Sin(2f * Mathf.PI * 150f * TRITONE * t) * Mathf.Exp(-t * 5f) * 0.15f;
            float raw = (boom + burst + crackle + roar + diss) * env * master;
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
