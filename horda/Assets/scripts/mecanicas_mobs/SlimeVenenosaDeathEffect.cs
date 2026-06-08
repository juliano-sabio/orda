using System.Collections;
using UnityEngine;

// Adicione este componente ao prefab da slime_venenosa.
// Ao morrer, spawna uma nuvem de fumaca venenosa que envenena o player ao toque.
public class SlimeVenenosaDeathEffect : MonoBehaviour
{
    [Header("Fumaca")]
    public float raioFumaca    = 2.5f;
    public float duracaoFumaca = 4f;

    [Header("Veneno no Player")]
    public float danoPorTick   = 4f;
    public float intervalTick  = 0.8f;
    public float duracaoVeneno = 5f;

    InimigoController controller;
    bool jaSpawnou = false;

    void Start()
    {
        controller = GetComponent<InimigoController>();
    }

    void OnDestroy()
    {
        if (controller != null && controller.vidaAtual <= 0 && !jaSpawnou && gameObject.scene.isLoaded)
            SpawnarFumaca();
    }

    void SpawnarFumaca()
    {
        jaSpawnou = true;
        var go = new GameObject("FumacaVenenosa");
        go.transform.position = transform.position;
        go.AddComponent<FumacaVenenosaCloud>().Inicializar(
            raioFumaca, duracaoFumaca, danoPorTick, intervalTick, duracaoVeneno);
    }
}

// ── Nuvem de fumaca ────────────────────────────────────────────────────────────

public class FumacaVenenosaCloud : MonoBehaviour
{
    float raio, duracao, danoPorTick, intervalTick, duracaoVeneno;

    const int NUM_PARTICULAS = 10;

    public void Inicializar(float raio, float duracao, float danoPorTick,
        float intervalTick, float duracaoVeneno)
    {
        this.raio          = raio;
        this.duracao       = duracao;
        this.danoPorTick   = danoPorTick;
        this.intervalTick  = intervalTick;
        this.duracaoVeneno = duracaoVeneno;

        // Collider trigger para detectar o player
        var col    = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = raio;

        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CriarVisual();
        StartCoroutine(Vida());
    }

    void OnTriggerEnter2D(Collider2D other) => TentarEnvenenar(other);
    void OnTriggerStay2D(Collider2D other)  => TentarEnvenenar(other);

    void TentarEnvenenar(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var ps = other.GetComponent<PlayerStats>();
        if (ps != null)
            ps.AplicarVenenoPlayer(danoPorTick, intervalTick, duracaoVeneno);
    }

    IEnumerator Vida()
    {
        yield return new WaitForSeconds(duracao);
        Destroy(gameObject);
    }

    // ── Visual ─────────────────────────────────────────────────────────────────

    void CriarVisual()
    {
        for (int i = 0; i < NUM_PARTICULAS; i++)
        {
            float angulo = i * (360f / NUM_PARTICULAS) * Mathf.Deg2Rad;
            float dist   = Random.Range(raio * 0.1f, raio * 0.8f);
            var   offset = new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo)) * dist;

            var part = new GameObject($"Fumaca_{i}");
            part.transform.position = transform.position + offset;

            var sr = part.AddComponent<SpriteRenderer>();
            sr.sprite       = GerarDisco();
            sr.color        = i % 3 == 0
                ? new Color(0.20f, 0.70f, 0.15f, 0.80f)
                : new Color(0.30f, 0.85f, 0.20f, 0.65f);
            sr.sortingOrder = 15;

            float escala = Random.Range(raio * 0.55f, raio * 1.0f);
            part.transform.localScale = Vector3.one * escala;

            part.AddComponent<FumacaParticula>().Iniciar(duracao);
        }
    }

    static Texture2D _discoTex;
    static Sprite GerarDisco()
    {
        if (_discoTex == null)
        {
            int sz = 64;
            _discoTex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            _discoTex.filterMode = FilterMode.Bilinear;
            float cx = sz * 0.5f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
                float a = d < cx ? Mathf.Pow(1f - d / cx, 0.5f) : 0f;
                _discoTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            _discoTex.Apply();
        }
        return Sprite.Create(_discoTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
    }
}

// ── Particula individual ───────────────────────────────────────────────────────

public class FumacaParticula : MonoBehaviour
{
    public void Iniciar(float duracao) => StartCoroutine(Animar(duracao));

    IEnumerator Animar(float duracao)
    {
        var    sr      = GetComponent<SpriteRenderer>();
        Color  corBase = sr != null ? sr.color : Color.white;
        float  sway    = Random.Range(0.06f, 0.14f);
        float  subida  = Random.Range(0.15f, 0.40f);
        float  freq    = Random.Range(1.2f, 2.5f);
        Vector3 posBase = transform.position;

        float t = 0f;
        while (t < duracao)
        {
            t += Time.deltaTime;
            float progresso = t / duracao;

            // Curva de alpha: sobe rápido, fica, desce no final
            float alpha = progresso < 0.15f
                ? progresso / 0.15f
                : progresso > 0.75f
                    ? (1f - progresso) / 0.25f
                    : 1f;

            if (sr != null)
                sr.color = new Color(corBase.r, corBase.g, corBase.b, corBase.a * alpha);

            // Flutua para cima com balanço lateral
            float dx = Mathf.Sin(t * freq + GetInstanceID()) * sway;
            float dy = subida * t;
            transform.position = posBase + new Vector3(dx, dy, 0f);

            yield return null;
        }

        Destroy(gameObject);
    }
}
