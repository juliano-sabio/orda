using UnityEngine;

// Na cena de teste de rede: a câmera segue o player LOCAL (PlayerStats.Local).
// Cada cliente tem sua própria câmera seguindo o próprio player.
public class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] float zOffset = -10f;

    void LateUpdate()
    {
        var local = PlayerStats.Local;
        if (local == null) return;
        var p = local.transform.position;
        transform.position = new Vector3(p.x, p.y, zOffset);
    }
}
