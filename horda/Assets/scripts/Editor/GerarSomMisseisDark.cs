#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Gera sons procedurais no estilo "dark fantasy" (graves, dissonantes, com cauda
// cavernosa) para os Mísseis Teleguiados. Salva em Assets/Resources/Sons/ para o
// SomSkill carregar via Resources.Load.
// Menu: Tools/Sons/Gerar Sons Dark (Misseis)
public static class GerarSomMisseisDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Misseis)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("missil_disparo_dark",  GerarDisparoDark());
        GravarWav("missil_impacto_dark",  GerarImpactoDark());
        GravarWav("missil_explosao_dark", GerarExplosaoDark());

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Gerados em " + PASTA + "/ (missil_disparo_dark, missil_impacto_dark, missil_explosao_dark)");
    }

    const float MINOR3 = 1.18921f; // razão de terça menor (2^(3/12)) — sonoridade sombria

    // Conjuro sombrio: tom descendente + drone grave + shimmer dissonante + sopro etéreo.
    static float[] GerarDisparoDark()
    {
        int dur = (int)(SR * 0.55f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 5f) * Mathf.Clamp01(t * 30f);
            float fMain   = Mathf.Lerp(330f, 90f, Mathf.Clamp01(t * 2.2f));
            float main    = Mathf.Sin(2f * Mathf.PI * fMain * t);
            float shimmer = Mathf.Sin(2f * Mathf.PI * fMain * MINOR3 * t) * 0.5f;
            float drone   = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.6f;
            float sopro   = (Random.value * 2f - 1f) * 0.25f * (1f - Mathf.Exp(-t * 8f)) * Mathf.Exp(-t * 3f);
            s[i] = Mathf.Clamp((main * 0.55f + shimmer * 0.3f + drone * 0.5f + sopro) * env, -1f, 1f);
        }
        return Normalizar(s);
    }

    // Impacto sombrio: golpe grave + corpo dissonante + estalo curto + queda de tom.
    static float[] GerarImpactoDark()
    {
        int dur = SR / 3;
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env   = Mathf.Exp(-t * 14f);
            float golpe = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 22f);
            float corpo = Mathf.Sin(2f * Mathf.PI * 130f * t) * 0.5f;
            float estalo = (Random.value * 2f - 1f) * 0.3f * Mathf.Exp(-t * 35f);
            float fSweep = Mathf.Lerp(200f, 80f, Mathf.Clamp01(t * 4f));
            float sweep = Mathf.Sin(2f * Mathf.PI * fSweep * t) * 0.4f * Mathf.Exp(-t * 20f);
            float raw = (golpe * 0.9f + corpo * 0.4f + estalo + sweep) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
        }
        return Normalizar(s);
    }

    // Explosão cavernosa: sub-grave longo + ronco + rosnado descendente + grit.
    static float[] GerarExplosaoDark()
    {
        int dur = (int)(SR * 0.8f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env    = Mathf.Exp(-t * 4f);
            float sub    = Mathf.Sin(2f * Mathf.PI * 40f * t) * Mathf.Exp(-t * 2.5f);
            float ronco  = (Random.value * 2f - 1f) * Mathf.Exp(-t * 3.5f);
            float mid    = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 6f) * 0.4f;
            float fGrowl = Mathf.Lerp(140f, 50f, Mathf.Clamp01(t * 1.5f));
            float rosnado = Mathf.Sin(2f * Mathf.PI * fGrowl * t) * 0.5f * Mathf.Exp(-t * 4f);
            float raw = (sub * 0.7f + ronco * 0.7f + mid * 0.3f + rosnado * 0.4f) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.65f), -1f, 1f);
        }
        return Normalizar(s);
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
