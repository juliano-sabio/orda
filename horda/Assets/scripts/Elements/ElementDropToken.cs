using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ElementDropToken : MonoBehaviour
{
    [Header("Elemento")]
    public ElementType elementType = ElementType.Fogo;

    [Header("Animacao")]
    public float bobAmplitude = 0.12f;
    public float bobFrequency = 2.2f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Co-op: infusão é escolha do GRUPO (igual à escolha de skill) — os DOIS recebem o painel,
        // e quem escolher primeiro espera o outro. O token existe só no host (não é NetworkObject),
        // então aqui (no host) avisa cada player pra abrir a infusão no SEU cliente.
        if (NetSpawn.EmRede)
        {
            for (int i = 0; i < PlayerStats.All.Count; i++)
            {
                var pn = PlayerStats.All[i] != null ? PlayerStats.All[i].GetComponent<PlayerNet>() : null;
                if (pn != null) pn.AbrirInfusaoOwnerRpc((int)elementType);
            }
            Destroy(gameObject);
            return;
        }

        var ui = ElementApplicationUI.Instance;
        if (ui == null)
        {
            Debug.LogWarning("[ElementDropToken] ElementApplicationUI.Instance e null.");
            Destroy(gameObject);
            return;
        }

        ui.Abrir(elementType);
        Destroy(gameObject);
    }
}
