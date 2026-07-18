using UnityEngine;

// Atalho de DEBUG: tecla F10 desbloqueia todas as ultimates que são destravadas por missão,
// gravando direto o PlayerPrefs de desbloqueio de cada uma (as mesmas chaves usadas pelos
// Missao*Manager). Utilitário de teste "pra mim" — não afeta o resto do jogo.
//
// O desbloqueio é lido pela tela de seleção/lobby quando ela é montada, então após apertar F10
// as ultimates ficam disponíveis na próxima vez que a seleção abrir.
public class DebugDesbloquearUltimates : MonoBehaviour
{
    const KeyCode TECLA = KeyCode.F10;

    // Chaves de PlayerPrefs dos desbloqueios das ULTIMATES (ver Missao*Manager).
    static readonly string[] ULTIMATES =
    {
        "DomoRetardanteDesbloqueado",     // Domo Retardante   (matar Princesa Slime 2x)
        "TempestadeEletricaDesbloqueada", // Tempestade Elétrica (3 eventos de tempestade)
        "NecropoleDesbloqueada",          // Necrópole         (500 fantasmas)
        "DrenagemDeVidaDesbloqueada",     // Drenagem de Vida  (500 slimes curandeiras)
    };

    static DebugDesbloquearUltimates _i;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_i != null) return;
        var go = new GameObject("DebugDesbloquearUltimates");
        DontDestroyOnLoad(go);
        _i = go.AddComponent<DebugDesbloquearUltimates>();
    }

    void Update()
    {
        if (!Input.GetKeyDown(TECLA)) return;

        foreach (var chave in ULTIMATES)
            PlayerPrefs.SetInt(chave, 1);
        PlayerPrefs.Save();

        Debug.Log("[DEBUG] Todas as ultimates desbloqueadas (F10).");
        UIManager.Instance?.ShowSkillAcquired("ULTIMATES DESBLOQUEADAS!", "Todas liberadas (debug)");
    }
}
