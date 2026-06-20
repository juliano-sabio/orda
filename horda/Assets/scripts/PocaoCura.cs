using UnityEngine;
using Unity.Netcode;

public class PocaoCura : MonoBehaviour
{
    [Header("Cura")]
    public float quantidadeCura = 30f;
    [Range(0f, 1f)]
    public float curaEmPorcentagem = 0f;

    [Header("Atração")]
    public float raioAtracao = 4f;
    public float velocidadeAtracao = 6f;
    public float aceleracao = 4f;

    [Header("Visual")]
    public float bobAmplitude = 0.15f;
    public float bobVelocidade = 2f;

    private Transform player;
    private bool atraindo = false;
    private bool coletada = false;
    private float velocidadeAtual = 0f;
    private Vector3 posicaoBase;

    // Em co-op é NetworkObject: host atrai/coleta (autoritativo), cliente é fantoche.
    bool EhClienteFantoche
    {
        get { var nm = NetworkManager.Singleton; return nm != null && nm.IsListening && !nm.IsServer; }
    }

    void Start()
    {
        posicaoBase = transform.position;

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go.transform;
    }

    void Update()
    {
        if (EhClienteFantoche) return; // cliente: NetworkTransform move

        if (NetSpawn.EmRede)
        {
            var t = PlayerStats.MaisProximoTransform(transform.position);
            if (t != null) player = t;
        }
        if (player == null) return;

        float distancia = Vector2.Distance(transform.position, player.position);

        if (!atraindo && distancia <= raioAtracao)
            atraindo = true;

        if (atraindo)
        {
            velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeAtracao, aceleracao * Time.deltaTime);
            transform.position = Vector2.MoveTowards(transform.position, player.position, velocidadeAtual * Time.deltaTime);
        }
        else
        {
            // bob enquanto espera
            posicaoBase.x = transform.position.x;
            float y = posicaoBase.y + Mathf.Sin(Time.time * bobVelocidade) * bobAmplitude;
            transform.position = new Vector3(posicaoBase.x, y, posicaoBase.z);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (coletada) return;
        if (EhClienteFantoche) return; // só o host coleta em co-op
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null) return;
        coletada = true;

        float cura = curaEmPorcentagem > 0f
            ? stats.maxHealth * curaEmPorcentagem
            : quantidadeCura;

        // Co-op: cura vai pro DONO do player que encostou.
        if (NetSpawn.EmRede)
        {
            var pn = stats.GetComponent<PlayerNet>();
            if (pn != null) pn.CurarOwnerRpc(cura);
            else stats.Heal(cura);
            SpawnEfeitoColeta(other.transform.position);
            NetSpawn.Despawnar(gameObject);
            return;
        }

        stats.Heal(cura);
        SpawnEfeitoColeta(other.transform.position);
        Destroy(gameObject);
    }

    void SpawnEfeitoColeta(Vector3 pos)
    {
        var go = new GameObject("EfeitoColetaCura");
        go.transform.position = pos;
        go.AddComponent<EfeitoColetaCuraFX>();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, raioAtracao);
    }
}
