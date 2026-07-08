using UnityEngine;

// Missão de desbloqueio da ultimate "Tempestade Elétrica": concluir 3 eventos de Tempestade.
// Progresso persistente em PlayerPrefs (meta-progressão local — conta no host e no cliente).
public static class MissaoTempestadeManager
{
    const string KEY_COMPLETAS    = "MissaoTempestadeCompletas";
    const string KEY_DESBLOQUEADO = "TempestadeEletricaDesbloqueada";

    public const int META = 3; // concluir 3 eventos de Tempestade

    public static int  Completas    => Mathf.Clamp(PlayerPrefs.GetInt(KEY_COMPLETAS, 0), 0, META);
    public static bool Desbloqueada => PlayerPrefs.GetInt(KEY_DESBLOQUEADO, 0) == 1;

    // Chamado quando um evento de Tempestade Elétrica é concluído com sucesso.
    public static void RegistrarEventoTempestade()
    {
        if (Desbloqueada) return;
        int k = Mathf.Min(Completas + 1, META);
        PlayerPrefs.SetInt(KEY_COMPLETAS, k);
        if (k >= META)
        {
            PlayerPrefs.SetInt(KEY_DESBLOQUEADO, 1);
            UIManager.Instance?.ShowSkillAcquired("TEMPESTADE ELÉTRICA DESBLOQUEADA!", "Ultimate liberada!");
        }
        PlayerPrefs.Save();
    }
}
