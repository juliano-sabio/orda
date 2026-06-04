#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class GerarSomSkillAtaque
{
    const string PASTA = "Assets/Sons";
    const int SAMPLE_RATE = 44100;

    [MenuItem("Tools/Sons/Gerar Sons de Skill (Teste)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets", "Sons");

        GravarWav("som_impacto",  GerarImpacto());
        GravarWav("som_disparo",  GerarDisparo());
        GravarWav("som_explosao", GerarExplosao());

        AssetDatabase.Refresh();

        var imp = AssetImporter.GetAtPath(PASTA + "/som_impacto.wav");
        var dis = AssetImporter.GetAtPath(PASTA + "/som_disparo.wav");
        var exp = AssetImporter.GetAtPath(PASTA + "/som_explosao.wav");

        Debug.Log("Sons gerados em " + PASTA + "/");
    }

    static float[] GerarImpacto()
    {
        int dur = SAMPLE_RATE / 3;
        float[] s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float env = Mathf.Exp(-t * 18f);
            float body = Mathf.Sin(2f * Mathf.PI * 180f * t);
            float punch = Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-t * 30f);
            float noise = (UnityEngine.Random.value * 2f - 1f) * 0.15f * Mathf.Exp(-t * 40f);
            s[i] = Mathf.Clamp((body * 0.5f + punch * 0.8f + noise) * env, -1f, 1f);
        }
        return Normalizar(s);
    }

    static float[] GerarDisparo()
    {
        int dur = (int)(SAMPLE_RATE * 0.4f);
        float[] s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float env = Mathf.Exp(-t * 8f) * Mathf.Clamp01(t * 40f);
            float freq = Mathf.Lerp(800f, 200f, t * 2.5f);
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = (UnityEngine.Random.value * 2f - 1f) * 0.3f * Mathf.Exp(-t * 15f);
            s[i] = Mathf.Clamp((wave * 0.7f + noise) * env, -1f, 1f);
        }
        return Normalizar(s);
    }

    static float[] GerarExplosao()
    {
        int dur = (int)(SAMPLE_RATE * 0.6f);
        float[] s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float env = Mathf.Exp(-t * 6f);
            float sub = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 4f);
            float mid = Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 8f);
            float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 5f);
            float raw = (sub * 0.6f + mid * 0.3f + noise * 0.8f) * env;
            float sign = raw >= 0f ? 1f : -1f;
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

        FileStream fs = new FileStream(fullPath, FileMode.Create);
        BinaryWriter bw = new BinaryWriter(fs);

        int dataLen = samples.Length * 2;

        bw.Write(new byte[] { 82, 73, 70, 70 });         // "RIFF"
        bw.Write(36 + dataLen);
        bw.Write(new byte[] { 87, 65, 86, 69 });         // "WAVE"
        bw.Write(new byte[] { 102, 109, 116, 32 });      // "fmt "
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(SAMPLE_RATE);
        bw.Write(SAMPLE_RATE * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(new byte[] { 100, 97, 116, 97 });       // "data"
        bw.Write(dataLen);

        for (int i = 0; i < samples.Length; i++)
            bw.Write((short)(samples[i] * 32767f));

        bw.Close();
        fs.Close();
    }
}
#endif
