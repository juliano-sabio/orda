using UnityEngine;

// Missão de desbloqueio da ultimate "Domo Retardante": matar a boss Princesa Slime 2 vezes.
// Progresso persistente em PlayerPrefs (meta-progressão local — conta no host e no cliente,
// cada um na própria máquina, já que a morte replica pros dois lados em co-op).
public static class MissaoDomoManager
{
    const string KEY_KILLS        = "MissaoDomoKillsPrincesa";
    const string KEY_DESBLOQUEADO = "DomoRetardanteDesbloqueado";

    public const int META = 2; // matar a Princesa Slime 2x

    public static int  KillsPrincesa    => Mathf.Clamp(PlayerPrefs.GetInt(KEY_KILLS, 0), 0, META);
    public static bool DomoDesbloqueado => PlayerPrefs.GetInt(KEY_DESBLOQUEADO, 0) == 1;

    // Chamado quando a boss Princesa Slime morre.
    public static void RegistrarKillPrincesa()
    {
        if (DomoDesbloqueado) return;
        int k = Mathf.Min(KillsPrincesa + 1, META);
        PlayerPrefs.SetInt(KEY_KILLS, k);
        if (k >= META)
        {
            PlayerPrefs.SetInt(KEY_DESBLOQUEADO, 1);
            UIManager.Instance?.ShowSkillAcquired("DOMO RETARDANTE DESBLOQUEADO!", "Ultimate liberada!");
        }
        PlayerPrefs.Save();
    }
}
