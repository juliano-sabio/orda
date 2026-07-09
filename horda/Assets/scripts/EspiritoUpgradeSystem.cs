using UnityEngine;

// Sistema de progressão por personagem: Espíritos de Evolução ganhos em missões
// podem ser gastos para subir o nível dos 8 status do painel de seleção.
public static class EspiritoUpgradeSystem
{
    public const int NIVEL_MAXIMO = 20;
    const float INCREMENTO_POR_NIVEL = 0.08f;

    // Índices de status cujo "menor é melhor" (cooldowns)
    static bool EhCooldown(int statIndex) => statIndex == 3 || statIndex == 7; // Vel.Atq, Escudo

    public static int GetEspiritos(int charIndex)
    {
        return PlayerPrefs.GetInt($"Espiritos_{charIndex}", 0);
    }

    public static void AdicionarEspirito(int charIndex, int qtd = 1)
    {
        int atual = GetEspiritos(charIndex);
        PlayerPrefs.SetInt($"Espiritos_{charIndex}", atual + qtd);
        PlayerPrefs.Save();
    }

    public static int GetNivel(int charIndex, int statIndex)
    {
        return PlayerPrefs.GetInt($"SpiritLv_{charIndex}_{statIndex}", 0);
    }

    public static bool TryUpgrade(int charIndex, int statIndex)
    {
        int espiritos = GetEspiritos(charIndex);
        int nivel = GetNivel(charIndex, statIndex);

        if (espiritos <= 0 || nivel >= NIVEL_MAXIMO) return false;

        PlayerPrefs.SetInt($"Espiritos_{charIndex}", espiritos - 1);
        PlayerPrefs.SetInt($"SpiritLv_{charIndex}_{statIndex}", nivel + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static float GetMultiplicador(int charIndex, int statIndex)
    {
        int nivel = GetNivel(charIndex, statIndex);
        if (EhCooldown(statIndex))
            return Mathf.Max(0.4f, 1f - nivel * INCREMENTO_POR_NIVEL);

        return 1f + nivel * INCREMENTO_POR_NIVEL;
    }
}
