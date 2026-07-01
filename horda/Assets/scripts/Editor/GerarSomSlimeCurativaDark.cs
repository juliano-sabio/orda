#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Som procedural "dark fantasy" (cura etérea/arcana) para o mob slime_curativa:
// pulso de cura em área e onda de cura ao morrer usam o mesmo som.
// Salva em Assets/Resources/Sons/. Menu: Tools/Sons/Gerar Sons Dark (Slime Curativa)
public static class GerarSomSlimeCurativaDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Slime Curativa)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("slimecurativa_cura", Reverb(GerarCura(), 0.45f, 8, 40f, 0.45f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Slime Curativa gerado em " + PASTA + "/ (slimecurativa_cura)");
    }

    // Cura: pulso etéreo suave — acorde quente (raiz + quinta + oitava) que incha e some,
    // com um brilho ascendente e um sopro aéreo. Restaurador, mas com timbre arcano/sombrio.
    static float[] GerarCura()
    {
        int dur = (int)(SR * 0.7f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float prog = Mathf.Clamp01(t / 0.7f);
            float env = Mathf.Sin(prog * Mathf.PI);           // swell 0→1→0 (entra e sai suave)
            float f0 = 330f;
            float pad = (Mathf.Sin(2f * Mathf.PI * f0 * t)
                      +  Mathf.Sin(2f * Mathf.PI * f0 * 1.5f * t) * 0.6f
                      +  Mathf.Sin(2f * Mathf.PI * f0 * 2f * t) * 0.4f) * 0.3f;
            float brilho = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(700f, 1050f, prog) * t) * 0.12f * prog;
            float ar = (Random.value * 2f - 1f) * 0.06f;
            float raw = (pad + brilho) * env + ar * env;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = Mathf.Clamp(sign * Mathf.Pow(Mathf.Abs(raw), 0.9f), -1f, 1f);
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
