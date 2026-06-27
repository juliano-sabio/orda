#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

// Som procedural "dark fantasy" para SELECIONAR uma carta: confirmação mágica
// (shimmer ascendente + chime + pulso grave). Salva em Assets/Resources/Sons/.
// Menu: Tools/Sons/Gerar Sons Dark (Carta)
public static class GerarSomCartaDark
{
    const string PASTA = "Assets/Resources/Sons";
    const int SR = 44100;

    [MenuItem("Tools/Sons/Gerar Sons Dark (Carta)")]
    public static void Gerar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA))
            AssetDatabase.CreateFolder("Assets/Resources", "Sons");

        GravarWav("carta_select_dark", Reverb(GerarSelecao(), 0.5f, 8, 30f, 0.34f));

        AssetDatabase.Refresh();
        Debug.Log("[SonsDark] Carta gerada em " + PASTA + "/ (carta_select_dark)");
    }

    const float MINOR3  = 1.18921f;
    const float TRITONE = 1.41421f;

    // Seleção (dark fantasy): badalada grave de sino arcano + acorde menor + whoosh de sombra +
    // descida ominosa + sub profundo + leve inquietação (trítono). Atmosférico, não "alegre".
    static float[] GerarSelecao()
    {
        int dur = (int)(SR * 0.5f);
        var s = new float[dur];
        for (int i = 0; i < dur; i++)
        {
            float t = (float)i / SR;
            float master = Mathf.Clamp01(t * 28f); // ataque suave (sem clique)

            // Badalada de sino grave com overtone de terça menor + parcial inarmônica (sino/gongo sombrio)
            float toll = (Mathf.Sin(2f * Mathf.PI * 220f * t)
                       +  Mathf.Sin(2f * Mathf.PI * 220f * MINOR3 * t) * 0.5f
                       +  Mathf.Sin(2f * Mathf.PI * 220f * 2.76f * t) * 0.2f) * Mathf.Exp(-t * 4f) * 0.4f;

            // Whoosh de sombra (ruído com swell gaussiano — energia arcana se juntando)
            float whEnv = Mathf.Exp(-Mathf.Pow((t - 0.06f) / 0.06f, 2f));
            float whoosh = (Random.value * 2f - 1f) * whEnv * 0.35f;

            // Acorde menor grave ressoando (Lá menor: A, C, E) — sombrio
            float chord = (Mathf.Sin(2f * Mathf.PI * 220.00f * t)
                        +  Mathf.Sin(2f * Mathf.PI * 261.63f * t) * 0.6f
                        +  Mathf.Sin(2f * Mathf.PI * 329.63f * t) * 0.5f) * Mathf.Exp(-t * 3.5f) * 0.18f;

            // Descida ominosa (resolve pra baixo = mais sombrio que subir)
            float fDesc = Mathf.Lerp(500f, 130f, Mathf.Clamp01(t * 3f));
            float desc = Mathf.Sin(2f * Mathf.PI * fDesc * t) * Mathf.Exp(-t * 6f) * 0.25f;

            // Sub profundo
            float sub = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 4f) * 0.35f;

            // Inquietação: parcial alta com trítono, bem sutil
            float unease = Mathf.Sin(2f * Mathf.PI * 740f * TRITONE * t) * Mathf.Exp(-t * 10f) * 0.05f;

            float raw = (toll + whoosh + chord + desc + sub + unease) * master;
            float sign = raw >= 0f ? 1f : -1f;
            s[i] = sign * Mathf.Pow(Mathf.Abs(raw), 0.8f);
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
        int extra = spread * taps + SR / 6;
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
