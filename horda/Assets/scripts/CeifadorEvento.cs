using System.Collections;
using UnityEngine;

public class CeifadorEvento : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidadeWander = 1.8f;
    public float raioWander       = 5f;
    public float tempoMudanca     = 3f;

    [Header("Detecção e Dash")]
    public float raioDeteccao  = 6f;
    public float velocidadeDash = 14f;
    public float duracaoDash    = 0.35f;
    public float cooldownDash   = 1.8f;

    [Header("Animação")]
    public RuntimeAnimatorController controllerCeifador;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform player;

    private Vector2 direcaoWander;
    private float   proximaMudanca;
    private float   proximoDash;
    private bool    isDashing;
    private Vector2 dashDir;

    private enum Estado { Wander, Perseguindo, Dashing }
    private Estado estado = Estado.Wander;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

void Start()
{
    if (controllerCeifador != null)
    {
        var anim = GetComponent<Animator>();
        if (anim != null) anim.runtimeAnimatorController = controllerCeifador;
    }

    var ps = FindFirstObjectByType<PlayerStats>();
    if (ps != null) player = ps.transform;

    AdicionarBrilhoVermelho();
    EscolherNovaDirecao();
}

void AdicionarBrilhoVermelho()
{
    var luzGO = new GameObject("BrilhoVermelho");
    luzGO.transform.SetParent(transform, false);
    luzGO.transform.localPosition = Vector3.zero;

    var luz = luzGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
    luz.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
    luz.color = new Color(1f, 0.05f, 0.05f, 1f);
    luz.intensity = 2.5f;
    luz.pointLightOuterRadius = 2.5f;
    luz.pointLightInnerRadius = 0.4f;
    luz.shadowsEnabled = false;
}

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        switch (estado)
        {
            case Estado.Wander:
                if (dist <= raioDeteccao)
                    estado = Estado.Perseguindo;
                else if (Time.time >= proximaMudanca)
                    EscolherNovaDirecao();
                break;

            case Estado.Perseguindo:
                if (dist > raioDeteccao * 1.6f)
                {
                    estado = Estado.Wander;
                    EscolherNovaDirecao();
                }
                else if (Time.time >= proximoDash)
                {
                    StartCoroutine(Dash());
                }
                break;

            case Estado.Dashing:
                break;
        }

        // Flip sprite pela velocidade
        Vector2 vel = rb.linearVelocity;
        if (sr != null)
        {
            if (vel.x > 0.05f)       sr.flipX = false;
            else if (vel.x < -0.05f) sr.flipX = true;
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = dashDir * velocidadeDash;
            return;
        }

        switch (estado)
        {
            case Estado.Wander:
                rb.linearVelocity = direcaoWander * velocidadeWander;
                break;

            case Estado.Perseguindo:
                if (player != null)
                {
                    Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                    rb.linearVelocity = dir * velocidadeWander * 1.4f;
                }
                break;
        }
    }

    IEnumerator Dash()
    {
        if (player == null) yield break;

        estado    = Estado.Dashing;
        isDashing = true;
        dashDir   = ((Vector2)player.position - (Vector2)transform.position).normalized;

        yield return new WaitForSeconds(duracaoDash);

        isDashing  = false;
        proximoDash = Time.time + cooldownDash;
        estado     = Estado.Perseguindo;
    }

    void EscolherNovaDirecao()
    {
        var ge = GerenciadorEventos.Instance;
        for (int i = 0; i < 10; i++)
        {
            float   ang  = Random.Range(0f, Mathf.PI * 2f);
            Vector2 alvo = (Vector2)transform.position
                         + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioWander;

            if (ge == null || ge.PosicaoValida(alvo))
            {
                direcaoWander   = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                proximaMudanca  = Time.time + tempoMudanca + Random.Range(-0.5f, 0.5f);
                return;
            }
        }
        direcaoWander  = -direcaoWander;
        proximaMudanca = Time.time + 1f;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // Cancela dash ao bater em obstáculo
        if (isDashing && !col.gameObject.CompareTag("Player"))
        {
            StopAllCoroutines();
            isDashing   = false;
            proximoDash = Time.time + cooldownDash;
            estado      = Estado.Perseguindo;
        }
        else if (!isDashing && estado == Estado.Wander && !col.gameObject.CompareTag("Player"))
        {
            EscolherNovaDirecao();
        }
    }
}
