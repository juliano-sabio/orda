using UnityEngine;

// Missão de desbloqueio da passiva "Asceta": concluir a primeira área.
// Missão binária (concluiu ou não). Persistente em PlayerPrefs (meta-progressão local).
public static class MissaoAscetaManager
{
    const string KEY_DESBLOQUEADO = "AscetaDesbloqueada";

    public static bool Desbloqueada => PlayerPrefs.GetInt(KEY_DESBLOQUEADO, 0) == 1;

    // Chamado quando a primeira área é concluída (boss final derrotado na primeira fase).
    public static void MarcarPrimeiraAreaConcluida()
    {
        if (Desbloqueada) return;
        PlayerPrefs.SetInt(KEY_DESBLOQUEADO, 1);
        PlayerPrefs.Save();
        UIManager.Instance?.ShowSkillAcquired("ASCETA DESBLOQUEADO!", "Passiva liberada!");
    }
}
