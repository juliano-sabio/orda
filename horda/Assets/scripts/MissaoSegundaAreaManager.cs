using UnityEngine;

// Desbloqueio da segunda área (Abismo / segunda_fase): liberada ao matar a boss Maga Slime.
// Persistente em PlayerPrefs (meta-progressão local).
public static class MissaoSegundaAreaManager
{
    const string KEY_DESBLOQUEADO = "SegundaAreaDesbloqueada";

    public static bool Desbloqueada => PlayerPrefs.GetInt(KEY_DESBLOQUEADO, 0) == 1;

    // Chamado quando a boss Maga Slime morre.
    public static void MarcarBossMagaMorta()
    {
        if (Desbloqueada) return;
        PlayerPrefs.SetInt(KEY_DESBLOQUEADO, 1);
        PlayerPrefs.Save();
        UIManager.Instance?.ShowSkillAcquired("ABISMO DESBLOQUEADO!", "Nova área liberada!");
    }
}
