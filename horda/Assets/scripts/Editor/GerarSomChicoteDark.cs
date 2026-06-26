#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Sons procedurais "dark fantasy" para o Chicote de Energia.
// Salva em Assets/Resources/Sons/ (SomSkill carrega via Resources.Load).
// Menu: Tools/Sons/Gerar Sons Dark (Chicote)
public static class GerarSomChicoteDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3 = 1.18921f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Chicote)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("chicote_estalo_dark",  GerarEstalo());
        GravarWav("chicote_impacto_dark", GerarImpacto());

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Chicote gerado em " + PASTA + "/ (chicote_estalo_dark, chicote_impacto_dark)");
    }

    // Estalo: chicotada sombria — swoosh que sobe e estala, com tom descendente e boom grave.
    static float[] GerarEstalo()
    {
        int dur = (int)(SR * 0.35f);
        var s = new float[dur];
        const float tPico = 0.1f; // momento do estalo
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            // swoosh: ruído com pico gaussiano em tPico (a "passada" do chicote)
            float swooshEnv = Mathf.Exp(-Mathf.Pow((t - tPico) / 0.06f, 2f));
            float swoosh    = (Random.value * 2f - 1f) * swooshEnv * 0.7f;
            // estalo: transiente agudo bem rápido no pico
            float crackEnv = Mathf.Exp(-Mathf.Abs(t - tPico) * 70f);
            float snap     = Mathf.Sin(2f * Mathf.PI * 600f * MINOR3 * t) * crackEnv * 0.4f;
            // tom sombrio descendente (a energia)
            float fTone = Mathf.Lerp(500f, 90f, Mathf.Clamp01((t - 0.05f) * 4f));
            float tone  = Mathf.Sin(2f * Mathf.PI * fTone * t) * Mathf.Exp(-t * 7f) * 0.5f;
            // boom grave
            float boom  = Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-t * 10f) * 0.5f;
            float env   = Mathf.Clamp01(t * 40f) * Mathf.Exp(-Mathf.Max(0f, t - tPico) * 6f);
            float raw   = (swoosh + snap + tone * 0.6f + boom * 0.6f) * env;
            float sign  = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.85f), -1f, 1f);
        }
        return Normalizar(s);
    }

    // Impacto: acento sombrio curto ao acertar (zap grave).
    static float[] GerarImpacto()
    {
        int dur = (int)(SR * 0.15f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env   = Mathf.Exp(-t * 30f);
            float crack = (Random.value * 2f - 1f) * Mathf.Exp(-t * 40f);
            float tom   = Mathf.Sin(2f * Mathf.PI * 150f * t) * Mathf.Exp(-t * 25f) * 0.5f;
            float sub   = Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-t * 20f) * 0.4f;
            float raw   = (crack * 0.6f + tom + sub) * env;
            float sign  = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.85f), -1f, 1f);
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
