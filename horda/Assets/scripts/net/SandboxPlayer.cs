using Unity.Netcode;
using UnityEngine;

// Player de teste do sandbox. Só o dono lê input e move; o NetworkTransform
// (configurado como Owner authority no prefab) replica a posição pros demais.
// A câmera de cada cliente segue o player de que ele é dono.
[RequireComponent(typeof(NetworkObject))]
public class SandboxPlayer : NetworkBehaviour
{
    [SerializeField] float speed = 6f;
    Camera cam;

    public override void OnNetworkSpawn()
    {
        if (IsOwner) cam = Camera.main;
    }

    void Update()
    {
        if (!IsOwner) return;
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(x, y, 0f);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        transform.position += dir * (speed * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (!IsOwner || cam == null) return;
        var p = transform.position;
        cam.transform.position = new Vector3(p.x, p.y, cam.transform.position.z);
    }
}
