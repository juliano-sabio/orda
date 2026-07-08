using UnityEngine;

// Missão de desbloqueio da passiva "Coração Robusto": eliminar 150 inimigos (qualquer tipo).
// Progresso persistente em PlayerPrefs (meta-progressão local — conta no host/SP).
public static class MissaoCoracaoManager
{
    const string KEY_KILLS        = "MissaoCoracaoKills";
    const string KEY_DESBLOQUEADO = "CoracaoRobustoDesbloqueada";

    public const int META = 150; // eliminar 150 inimigos

    public static int  Kills        => Mathf.Clamp(PlayerPrefs.GetInt(KEY_KILLS, 0), 0, META);
    public static bool Desbloqueada => PlayerPrefs.GetInt(KEY_DESBLOQUEADO, 0) == 1;

    // Chamado a cada inimigo eliminado (qualquer tipo).
    public static void RegistrarKill()
    {
        if (Desbloqueada) return;
        int k = Mathf.Min(Kills + 1, META);
        PlayerPrefs.SetInt(KEY_KILLS, k);
        if (k >= META)
        {
            PlayerPrefs.SetInt(KEY_DESBLOQUEADO, 1);
            UIManager.Instance?.ShowSkillAcquired("CORAÇÃO ROBUSTO DESBLOQUEADO!", "Passiva liberada!");
        }
        PlayerPrefs.Save();
    }
}
