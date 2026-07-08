using UnityEngine;

// Missão de desbloqueio da ultimate "Necrópole": eliminar 500 inimigos fantasma.
// Progresso persistente em PlayerPrefs (meta-progressão local — conta no host/SP, igual às
// outras missões de abate).
public static class MissaoNecropoleManager
{
    const string KEY_KILLS        = "MissaoNecropoleFantasmas";
    const string KEY_DESBLOQUEADO = "NecropoleDesbloqueada";

    public const int META = 500; // eliminar 500 fantasmas

    public static int  Kills        => Mathf.Clamp(PlayerPrefs.GetInt(KEY_KILLS, 0), 0, META);
    public static bool Desbloqueada => PlayerPrefs.GetInt(KEY_DESBLOQUEADO, 0) == 1;

    // Conta o abate se o inimigo for um fantasma (qualquer um dos 5 tipos).
    public static void RegistrarSeFantasma(InimigoController ic)
    {
        if (ic == null || Desbloqueada) return;
        if (ic.GetComponent<FantasmaFogo>()          == null &&
            ic.GetComponent<FantasmaEletrico>()       == null &&
            ic.GetComponent<FantasmaGelo>()           == null &&
            ic.GetComponent<FantasmaVeneno>()         == null &&
            ic.GetComponent<FantasmaVenenoAtirador>() == null)
            return;

        int k = Mathf.Min(Kills + 1, META);
        PlayerPrefs.SetInt(KEY_KILLS, k);
        if (k >= META)
        {
            PlayerPrefs.SetInt(KEY_DESBLOQUEADO, 1);
            UIManager.Instance?.ShowSkillAcquired("NECRÓPOLE DESBLOQUEADA!", "Ultimate liberada!");
        }
        PlayerPrefs.Save();
    }
}
