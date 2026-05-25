using System.Collections;
using UnityEngine;

public class SlimeColorida : MonoBehaviour
{
    [Header("Status")]
    public float vidaInicial  = 80f;
    public float danoPorContato = 4f;

    [Header("Animação — arraste o controller gerado pelo aseprite aqui")]
    public RuntimeAnimatorController controllerColorida;

    [Header("Movimento")]
    public float velocidade   = 1.8f;
    public float raioWander   = 5f;
    public float tempoMudanca = 2.5f;

    private InimigoController ic;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private Vector2 direcao;
    private float proximaMudanca;
    private bool notificou;

    void Awake()
    {
        ic = GetComponent<InimigoController>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (ic != null && ic.dadosInimigo == null)
        {
            ic.vidaAtual  = vidaInicial;
            ic.vidaMaxima = vidaInicial;
            ic.danoAtual  = danoPorContato;
        }
    }

    void Start()
    {
        if (controllerColorida != null)
        {
            var anim = GetComponent<Animator>();
            if (anim != null) anim.runtimeAnimatorController = controllerColorida;
        }
        InimigoController.OnPreMorte += OnPreMorte;
        EscolherNovaDirecao();
    }

    void OnDestroy()
    {
        InimigoController.OnPreMorte -= OnPreMorte;
    }

    void OnPreMorte(InimigoController morto)
    {
        if (morto != ic || notificou) return;
        notificou = true;
        GerenciadorEventos.Instance?.RegistrarSlimeColoridaEliminada();
        StartCoroutine(EfeitoMorte());
    }

    void Update()
    {
        if (ic != null && ic.estaMorrendo) return;

        if (Time.time >= proximaMudanca)
            EscolherNovaDirecao();

        if (sr != null)
        {
            if (direcao.x >  0.05f) sr.flipX = false;
            else if (direcao.x < -0.05f) sr.flipX = true;
        }
    }

    void FixedUpdate()
    {
        if (ic != null && ic.estaMorrendo) { rb.linearVelocity = Vector2.zero; return; }
        if (rb != null) rb.linearVelocity = direcao * velocidade;
    }

    void EscolherNovaDirecao()
    {
        var ge = GerenciadorEventos.Instance;
        for (int i = 0; i < 10; i++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Vector2 alvo = (Vector2)transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioWander;
            if (ge == null || ge.PosicaoValida(alvo))
            {
                direcao = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                proximaMudanca = Time.time + tempoMudanca + Random.Range(-0.5f, 0.5f);
                return;
            }
        }
        // fallback: inverte direção
        direcao = -direcao;
        proximaMudanca = Time.time + 1f;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.gameObject.CompareTag("Player"))
            EscolherNovaDirecao();
    }

    IEnumerator EfeitoMorte()
    {
        if (sr == null) yield break;

        const int   SEGS    = 32;
        const float DUR     = 0.5f;
        const float RAIO_MX = 1.8f;

        var go = new GameObject("SlimeColoridaEfeito");
        go.transform.position = transform.position;
        var lr        = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;

        Vector2 centro = transform.position;
        Color[] cores = { new Color(1f,0.2f,0.8f), new Color(0.2f,1f,0.6f),
                          new Color(0.2f,0.6f,1f), new Color(1f,0.9f,0.2f) };

        for (float t = 0f; t < DUR; t += Time.deltaTime)
        {
            float p    = t / DUR;
            float raio = Mathf.Lerp(0f, RAIO_MX, p);
            float a    = Mathf.Lerp(1f, 0f, p);
            Color c    = Color.Lerp(cores[Mathf.FloorToInt(p * cores.Length) % cores.Length],
                                    cores[(Mathf.FloorToInt(p * cores.Length) + 1) % cores.Length],
                                    (p * cores.Length) % 1f);
            c.a = a;
            lr.startColor = lr.endColor = c;
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.3f, 0.02f, p);
            for (int i = 0; i < SEGS; i++)
            {
                float ang = i / (float)SEGS * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
            }
            yield return null;
        }
        Destroy(go);
    }
}
