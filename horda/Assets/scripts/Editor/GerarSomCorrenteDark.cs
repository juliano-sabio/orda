#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para a Corrente Sombria.
// Salva em Assets/Resources/Sons/ (SomSkill carrega via Resources.Load).
// Menu: Tools/Sons/Gerar Sons Dark (Corrente)
public static class GerarSomCorrenteDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3 = 1.18921f; // terça menor — sonoridade sombria

    [MenuItem("Tools/Sons/Gerar Sons Dark (Corrente)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("corrente_canal_dark",    GerarCanalizacao());
        GravarWav("corrente_descarga_dark", GerarDescarga());
        GravarWav("corrente_tick_dark",     GerarTick());

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Corrente gerada em " + PASTA + "/ (corrente_canal_dark, corrente_descarga_dark, corrente_tick_dark)");
    }

    // Canalização: energia sombria se acumulando — tom e volume subindo, dissonante.
    static float[] GerarCanalizacao()
    {
        int dur = (int)(SR * 0.6f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env   = Mathf.Pow(Mathf.Clamp01(t / 0.6f), 1.3f);     // sobe
            float trem  = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 18f * t);
            float fRise = Mathf.Lerp(60f, 220f, Mathf.Clamp01(t * 1.6f));
            float main    = Mathf.Sin(2f * Mathf.PI * fRise * t);
            float shimmer = Mathf.Sin(2f * Mathf.PI * fRise * MINOR3 * t) * 0.4f;
            float sub     = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.5f;
            float sopro   = (Random.value * 2f - 1f) * 0.2f * Mathf.Clamp01(t * 2f);
            s[i] = Mathf.Clamp((main * 0.5f + shimmer * 0.3f + sub * 0.5f + sopro) * env * trem, -1f, 1f);
        }
        return Normalizar(s);
    }

    // Descarga: as correntes disparam — chicote/zap sombrio com estalo e boom grave.
    static float[] GerarDescarga()
    {
        int dur = (int)(SR * 0.45f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env     = Mathf.Exp(-t * 10f) * Mathf.Clamp01(t * 60f);
            float estalo  = (Random.value * 2f - 1f) * Mathf.Exp(-t * 14f);
            float fZap    = Mathf.Lerp(400f, 70f, Mathf.Clamp01(t * 3f));
            float zap     = Mathf.Sin(2f * Mathf.PI * fZap * t) * Mathf.Exp(-t * 8f);
            float boom    = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 9f) * 0.7f;
            float chicote = Mathf.Sin(2f * Mathf.PI * 180f * MINOR3 * t) * Mathf.Exp(-t * 12f) * 0.3f;
            float raw = (estalo * 0.6f + zap * 0.6f + boom * 0.6f + chicote) * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.8f), -1f, 1f);
        }
        return Normalizar(s);
    }

    // Tick: crepitar elétrico sombrio curto, sutil (dano por tick).
    static float[] GerarTick()
    {
        int dur = (int)(SR * 0.12f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env     = Mathf.Exp(-t * 40f);
            float estalo  = (Random.value * 2f - 1f) * Mathf.Exp(-t * 50f);
            float tom     = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Exp(-t * 45f) * 0.4f;
            s[i] = Mathf.Clamp((estalo * 0.7f + tom) * env, -1f, 1f);
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
