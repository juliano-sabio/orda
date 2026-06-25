using UnityEngine;
using UnityEngine.Rendering.Universal;

// Espírito de Luz: drop da segunda fase. Coletado por proximidade (ímã, igual outros
// drops) e recarrega a barra de luz do player.
public class EspiritoDeLuz : MonoBehaviour
{
    public float quantidadeLuz = 12f;
    public float moveSpeed     = 6f;

    Transform   player;
    PlayerStats playerStats;
    bool        isAttracted;
    bool        collected;

    public static GameObject Criar(Vector3 pos, float quantidadeLuz)
    {
        var go = new GameObject("EspiritoDeLuz");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarDisco(20, new Color(1f, 0.95f, 0.6f, 0.95f));
        sr.sortingOrder = 9;
        go.transform.localScale = Vector3.one * 0.4f;

        CriarLuz(go.transform, new Color(1f, 0.9f, 0.5f), 1.5f, 0.1f, 1f);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.25f;

        var espirito = go.AddComponent<EspiritoDeLuz>();
        espirito.quantidadeLuz = quantidadeLuz;

        return go;
    }

    void Start()
    {
        if (NetSpawn.EmRede)
        {
            // Co-op: drop host-local. Sem rede, FindGameObjectWithTag pegaria um player
            // arbitrário (frequentemente o player 1) → luz ia pro errado. Usa o mais próximo.
            var t = PlayerStats.MaisProximoTransform(transform.position);
            player = t; playerStats = t != null ? t.GetComponent<PlayerStats>() : null;
        }
        else
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform; // coop-local-ok: caminho SP (co-op usa MaisProximo)
            if (player != null) playerStats = player.GetComponent<PlayerStats>();
        }
    }

    void Update()
    {
        // Co-op: segue o player mais próximo (pode mudar enquanto se move).
        if (NetSpawn.EmRede)
        {
            var t = PlayerStats.MaisProximoTransform(transform.position);
            if (t != null) { player = t; playerStats = t.GetComponent<PlayerStats>(); }
        }

        if (!isAttracted)
            CheckDistance();
        else
            MoveToPlayer();
    }

    void CheckDistance()
    {
        if (playerStats == null || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= playerStats.GetItemCollectionRadius())
            isAttracted = true;
    }

    void MoveToPlayer()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        float speed    = moveSpeed * Mathf.Lerp(4f, 1f, distance / playerStats.GetItemCollectionRadius());

        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (distance < 0.5f)
            Collect();
    }

    void Collect()
    {
        if (collected || playerStats == null) return;
        collected = true;

        // Co-op: a luz vai pro DONO do player que coletou (não pra cópia local/fantoche).
        if (NetSpawn.EmRede)
        {
            var pn = playerStats.GetComponent<PlayerNet>();
            if (pn != null) pn.AdicionarLuzOwnerRpc(quantidadeLuz);
            else playerStats.AdicionarLuz(quantidadeLuz);
        }
        else
        {
            playerStats.AdicionarLuz(quantidadeLuz);
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Collect();
    }

    static void CriarLuz(Transform parent, Color cor, float intensidade, float raioInterno, float raioExterno)
    {
        var go = new GameObject("brilho");
        go.transform.SetParent(parent, false);
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = cor;
        light.intensity = intensidade;
        light.pointLightInnerRadius = raioInterno;
        light.pointLightOuterRadius = raioExterno;
        light.blendStyleIndex = 0;
    }

    static Sprite GerarDisco(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, d < cx ? cor.a : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
