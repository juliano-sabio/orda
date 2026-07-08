using UnityEngine;

// Missão de desbloqueio da passiva "Caçador": eliminar 200 slimes corrompidas
// (o inimigo slime_de_projetil, cujo nomeInimigo é "slime_corrupita").
// Progresso persistente em PlayerPrefs (meta-progressão local — conta no host/SP).
public static class MissaoCacadorManager
{
    const string KEY_KILLS        = "MissaoCacadorCorrompidas";
    const string KEY_DESBLOQUEADO = "CacadorDesbloqueada";

    public const int META = 200; // eliminar 200 slimes corrompidas

    public static int  Kills        => Mathf.Clamp(PlayerPrefs.GetInt(KEY_KILLS, 0), 0, META);
    public static bool Desbloqueada => PlayerPrefs.GetInt(KEY_DESBLOQUEADO, 0) == 1;

    // Conta o abate se o inimigo for a slime corrompida (nomeInimigo contém "corrup").
    public static void RegistrarSeCorrompida(InimigoController ic)
    {
        if (ic == null || Desbloqueada) return;
        if (ic.dadosInimigo == null) return;
        if (!ic.dadosInimigo.nomeInimigo.ToLowerInvariant().Contains("corrup")) return;

        int k = Mathf.Min(Kills + 1, META);
        PlayerPrefs.SetInt(KEY_KILLS, k);
        if (k >= META)
        {
            PlayerPrefs.SetInt(KEY_DESBLOQUEADO, 1);
            UIManager.Instance?.ShowSkillAcquired("CAÇADOR DESBLOQUEADO!", "Passiva liberada!");
        }
        PlayerPrefs.Save();
    }
}
