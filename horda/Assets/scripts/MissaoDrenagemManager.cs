using UnityEngine;

// Missão de desbloqueio da ultimate "Drenagem de Vida": eliminar 500 slimes curandeiras.
// Progresso persistente em PlayerPrefs (meta-progressão local — conta no host/SP).
public static class MissaoDrenagemManager
{
    const string KEY_KILLS        = "MissaoDrenagemCurandeiras";
    const string KEY_DESBLOQUEADO = "DrenagemDeVidaDesbloqueada";

    public const int META = 500; // eliminar 500 slimes curandeiras

    public static int  Kills        => Mathf.Clamp(PlayerPrefs.GetInt(KEY_KILLS, 0), 0, META);
    public static bool Desbloqueada => PlayerPrefs.GetInt(KEY_DESBLOQUEADO, 0) == 1;

    // Conta o abate se o inimigo for uma slime curandeira (tem o SlimeCurativaOnda).
    public static void RegistrarSeCurandeira(InimigoController ic)
    {
        if (ic == null || Desbloqueada) return;
        if (ic.GetComponent<SlimeCurativaOnda>() == null) return;

        int k = Mathf.Min(Kills + 1, META);
        PlayerPrefs.SetInt(KEY_KILLS, k);
        if (k >= META)
        {
            PlayerPrefs.SetInt(KEY_DESBLOQUEADO, 1);
            UIManager.Instance?.ShowSkillAcquired("DRENAGEM DE VIDA DESBLOQUEADA!", "Ultimate liberada!");
        }
        PlayerPrefs.Save();
    }
}
