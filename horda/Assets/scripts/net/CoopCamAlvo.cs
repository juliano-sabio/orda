using UnityEngine;
using Unity.Cinemachine;

// Em co-op cada cliente carrega sua própria vcam de cena, mas o player nasce em
// runtime — então a Follow da vcam fica sem alvo. Este script (na vcab da cena)
// aponta a Follow pro player LOCAL (PlayerStats.Local). Funciona em single-player
// também (Local = o player). Reaplica se o Local mudar (ex.: respawn).
[RequireComponent(typeof(CinemachineCamera))]
public class CoopCamAlvo : MonoBehaviour
{
    CinemachineCamera vcam;

    void Awake() { vcam = GetComponent<CinemachineCamera>(); }

    void LateUpdate()
    {
        if (vcam == null) return;
        var local = PlayerStats.Local;
        if (local == null) return;
        if (vcam.Follow != local.transform) vcam.Follow = local.transform;
    }
}
