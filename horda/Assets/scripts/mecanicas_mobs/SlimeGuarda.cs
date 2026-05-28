using System.Collections;
using UnityEngine;

public class SlimeGuarda : MonoBehaviour
{
    [Header("Detecção")]
    public float raioDeteccao = 8f;

    [Header("Movimento")]
    public float velocidadePatrulha = 2.5f;

    [Header("Dash")]
    public float velocidadeDash = 28f;
    public float duracaoWindup  = 0.45f;
    public float duracaoDash    = 0.32f;
    public float cooldownDash   = 3.5f;

    [Header("Dano")]
    public float danoContato    = 8f;
    public float danoDash       = 20f;
    public float intervaloContato = 1f;

    Rigidbody2D       rb;
    SpriteRenderer    sr;
    InimigoController inimigoCtrl;
    PlayerStats       player;

    bool    emDash;
    bool    sequenciaAtiva;
    float   proxDash;
    float   proxDano;

    Vector3 escalaBase;

    void Start()
    {
        rb          = GetComponent<Rigidbody2D>();
        sr          = GetComponent<SpriteRenderer>();
        inimigoCtrl = GetComponent<InimigoController>();
        escalaBase  = transform.localScale;

        player = FindFirstObjectByType<PlayerStats>();

        InimigoController.OnPreMorte += OnPreMorteHandler;
    }

    void OnDestroy()
    {
        InimigoController.OnPreMorte -= OnPreMorteHandler;
    }

    void OnPreMorteHandler(InimigoController ic)
    {
        if (ic != inimigoCtrl) return;
        StartCoroutine(EfeitoMorte());
    }

    void Update()
    {
        if (Morto()) return;
        if (player == null) { player = FindFirstObjectByType<PlayerStats>(); return; }

        if (!sequenciaAtiva)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist <= raioDeteccao && Time.time >= proxDash)
                StartCoroutine(SequenciaDash());
        }
    }

    void FixedUpdate()
    {
        if (Morto()) { rb.linearVelocity = Vector2.zero; return; }

        if (!sequenciaAtiva)
        {
            if (player != null)
            {
                Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                rb.linearVelocity = dir * velocidadePatrulha;
                if      (dir.x >  0.05f) sr.flipX = false;
                else if (dir.x < -0.05f) sr.flipX = true;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    // ── Sequência de Dash ─────────────────────────────────────────────────────

    IEnumerator SequenciaDash()
    {
        if (player == null) yield break;

        sequenciaAtiva = true;

        // Captura a direção antes do windup — evita race condition com FixedUpdate
        Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        sr.flipX = dir.x < 0f;
        rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(TelegrafiarDash());
        if (Morto()) { sequenciaAtiva = false; yield break; }

        // Dash em sincronismo com a física
        emDash = true;
        float elapsed = 0f;
        while (elapsed < duracaoDash && !Morto())
        {
            rb.linearVelocity = dir * velocidadeDash;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        emDash = false;

        yield return new WaitForSeconds(0.3f);

        proxDash       = Time.time + cooldownDash;
        sequenciaAtiva = false;
    }

    IEnumerator TelegrafiarDash()
    {
        float t = 0f;
        while (t < duracaoWindup && !Morto())
        {
            t += Time.deltaTime;
            float p     = t / duracaoWindup;
            float flash = (Mathf.Sin(t * Mathf.PI * 2f / 0.18f) + 1f) * 0.5f;

            sr.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0f), flash * 0.85f);

            float shake = Mathf.Sin(t * 65f) * Mathf.Lerp(0f, 0.12f, p);
            transform.localScale = escalaBase * (1f + shake);

            yield return null;
        }
        sr.color         = Color.white;
        transform.localScale = escalaBase;
    }

    // ── Dano de Contato ───────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Morto() || !other.CompareTag("Player")) return;
        AplicarDano(other.GetComponent<PlayerStats>());
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (Morto() || !other.CompareTag("Player") || Time.time < proxDano) return;
        AplicarDano(other.GetComponent<PlayerStats>());
    }

    void AplicarDano(PlayerStats ps)
    {
        if (ps == null) return;
        ps.TakeDamage(emDash ? danoDash : danoContato);
        proxDano = Time.time + intervaloContato;
    }

    // ── VFX de Morte ──────────────────────────────────────────────────────────

    IEnumerator EfeitoMorte()
    {
        for (int i = 0; i < 10; i++)
        {
            var go  = new GameObject("SplatGuarda");
            go.transform.position = transform.position;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite       = GerarSprite(Random.Range(6, 14), new Color(0.15f, 0.55f, 0.15f));
            sr2.sortingOrder = 14;
            go.transform.localScale = Vector3.one * Random.Range(0.4f, 1.1f);
            float ang = i * 36f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            // Corre no próprio GO do splat — não depende do SlimeGuarda continuar vivo
            go.AddComponent<SplatParticula>().Iniciar(dir, Random.Range(3f, 7f), Random.Range(0.5f, 1f));
        }
        yield break;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    bool Morto() => inimigoCtrl != null && inimigoCtrl.estaMorrendo;

    static Sprite GerarSprite(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, d < cx ? 1f : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}

// Roda no próprio GO do splat para sobreviver à destruição do SlimeGuarda
public class SplatParticula : MonoBehaviour
{
    public void Iniciar(Vector2 dir, float vel, float vida) =>
        StartCoroutine(Mover(dir, vel, vida));

    System.Collections.IEnumerator Mover(Vector2 dir, float vel, float vida)
    {
        float   t   = 0f;
        var     sr  = GetComponent<SpriteRenderer>();
        Color   cor = new Color(0.15f, 0.55f, 0.15f);
        while (t < vida)
        {
            t   += Time.deltaTime;
            vel *= Mathf.Pow(0.88f, Time.deltaTime * 60f);
            transform.position = (Vector2)transform.position + dir * vel * Time.deltaTime;
            if (sr != null) sr.color = new Color(cor.r, cor.g, cor.b, 1f - t / vida);
            yield return null;
        }
        Destroy(gameObject);
    }
}
