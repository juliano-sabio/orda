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

    // Boss drop: define o elemento + ícone do token. Chamado no host/SP direto, e no cliente
    // via ElementTokenNet (NetworkVariable) pra o ícone aparecer igual nos dois.
    public void Configurar(ElementType e)
    {
        elementType = e;
        var sr = GetComponent<SpriteRenderer>();
        var reg = ElementRegistry.Instance;
        if (sr != null && reg != null)
        {
            var def = reg.Get(e);
            if (def != null && def.icone != null) sr.sprite = def.icone;
            sr.color = Color.white;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (coletado || !other.CompareTag("Player")) return;
        // Co-op: coleta host-autoritativa (cada token = 1 coleta). O cliente não coleta; o host
        // detecta (player real ou fantoche) e despawna em todos.
        if (NetSpawn.EmRede && !NetSpawn.PodeSpawnar) return;
        coletado = true;

        // Co-op: um token = UMA infusão, só pro DONO do player que pegou (não pros dois).
        if (NetSpawn.EmRede)
        {
            var pn = other.GetComponentInParent<PlayerNet>();
            if (pn != null) pn.AbrirInfusaoOwnerRpc((int)elementType);
            NetSpawn.Despawnar(gameObject); // remove em todos
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
