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
    bool coletado; // trava: o trigger pode disparar 2x (player do host + fantoche do P2) antes do Destroy

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
        if (coletado || !other.CompareTag("Player")) return;
        coletado = true;

        // Co-op: um token = UMA infusão, só pro DONO do player que pegou (não pros dois).
        // O token existe só no host; roteia pro dono daquele player abrir a infusão na tela dele.
        if (NetSpawn.EmRede)
        {
            var pn = other.GetComponentInParent<PlayerNet>();
            if (pn != null) pn.AbrirInfusaoOwnerRpc((int)elementType);
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
