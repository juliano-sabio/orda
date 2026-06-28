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

        // Co-op: cada máquina detecta o SEU player local sobre o token e PEDE a coleta ao host.
        // (Antes era host-autoritativo via detecção do fantoche do outro no host — não confiável,
        // então o token não sumia pro P2.)
        if (NetSpawn.EmRede)
        {
            var ps = other.GetComponentInParent<PlayerStats>();
            if (ps == null || ps != PlayerStats.Local) return; // só o player local desta máquina
            var net = GetComponent<ElementTokenNet>();
            if (net != null)
            {
                coletado = true;        // trava local
                net.SolicitarColeta();  // host abre a infusão pro dono certo + despawna em todos
                return;
            }
            // sem ElementTokenNet → cai pro fluxo local abaixo
        }

        coletado = true;
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
