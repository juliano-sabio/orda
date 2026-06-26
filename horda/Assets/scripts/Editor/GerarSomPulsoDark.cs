#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Som procedural "dark fantasy" para o Pulso Rítmico: batida grave/cavernosa curta.
// Salva em Assets/Resources/Sons/ (SomSkill carrega via Resources.Load).
// Menu: Tools/Sons/Gerar Sons Dark (Pulso)
public static class GerarSomPulsoDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;
    const float MINOR3 = 1.18921f;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Pulso)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("pulso_dark", GerarPulso());

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Pulso gerado em " + PASTA + "/ (pulso_dark)");
    }

    // Pulso: batida grave com sub-cavernoso, punch e ressonância dissonante curta.
    static float[] GerarPulso()
    {
        int dur = (int)(SR * 0.38f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float env   = Mathf.Exp(-t * 9f);
            float sub   = Mathf.Sin(2f * Mathf.PI * 50f * t) * Mathf.Exp(-t * 5f);   // sub cavernoso (cauda)
            float punch = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 16f) * 0.6f;
            float ring  = Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 10f) * 0.4f
                        + Mathf.Sin(2f * Mathf.PI * 120f * MINOR3 * t) * Mathf.Exp(-t * 11f) * 0.25f; // dissonante
            float click = (Random.value * 2f - 1f) * 0.15f * Mathf.Exp(-t * 30f);
            float raw   = (sub * 0.8f + punch * 0.6f + ring + click) * env;
            float sign  = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.7f), -1f, 1f);
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
