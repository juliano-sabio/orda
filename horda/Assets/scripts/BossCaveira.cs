using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(InimigoController))]
public class BossCaveira : MonoBehaviour, IBoss, IBossHud
{
    // ──────────────────────────────────────────────
    // IDENTIDADE
    // ──────────────────────────────────────────────
    [Header("Identidade")]
    public string nomeBoss = "Criatura da Noite";

    // ──────────────────────────────────────────────
    // FASES
    // ──────────────────────────────────────────────
    [Header("Fases")]
    [Tooltip("Porcentagem de vida para entrar na Fase 2 (0-1)")]
    public float gatilhoFase2 = 0.5f;
    [Tooltip("Redução de dano recebido na Fase 2 (0.25 = 25% menos dano)")]
    public float reducaoDanoFase2 = 0.25f;
    [Tooltip("Multiplicador aplicado aos cooldowns dos ataques especiais na Fase 2 (menor = mais frequente)")]
    [Range(0.1f, 1f)] public float multiplicadorCooldownFase2 = 0.6f;
    [Tooltip("Bônus somado à chance de Investida na Fase 2")]
    [Range(0f, 1f)] public float bonusChanceInvestidaFase2 = 0.25f;
    [Tooltip("Cor que a criatura assume permanentemente ao entrar na Fase 2")]
    public Color corFase2 = new Color(0.75f, 0.45f, 1f);
    [Tooltip("Intervalo entre rajadas de partículas sombrias (aura) na Fase 2")]
    public float intervaloAuraFase2 = 1.2f;

    // ──────────────────────────────────────────────
    // FURTIVIDADE / EMBOSCADA
    // ──────────────────────────────────────────────
    [Header("Furtividade / Emboscada")]
    public float intervalEmboscadaFase1 = 5.5f;
    public float intervalEmboscadaFase2 = 3.5f;
    [Tooltip("Tempo que a criatura passa invisível, espreitando, antes de reaparecer")]
    public float duracaoInvisivel = 1f;
    [Tooltip("Transparência da criatura enquanto está nas sombras")]
    [Range(0f, 1f)] public float alphaSombra = 0.1f;
    public float distMinTeleporte = 3f;
    public float distMaxTeleporte = 6f;
    public float duracaoFade = 0.25f;

    // ──────────────────────────────────────────────
    // ATAQUES
    // ──────────────────────────────────────────────
    [Header("Ataques (à distância)")]
    public GameObject prefabProjetil;
    public int projeteisFase1 = 4;
    [Tooltip("Ângulo total do leque de projéteis (graus)")]
    public float anguloLeque = 50f;
    public float velocidadeProjetil = 6f;
    public float danoProjetil = 12f;

    [Header("Rajada de Emboscada (pós-teleporte)")]
    public int projeteisEmboscada = 6;
    public float danoProjetilEmboscada = 22f;
    public float velocidadeProjetilEmboscada = 8f;

    [Header("Boca (ponto de spawn do projétil)")]
    public Vector2 offsetBoca = new Vector2(0f, 0f);

    [Header("Investida (Ferroada)")]
    [Tooltip("Chance de fazer uma investida em vez de atirar quando para no ar")]
    [Range(0f, 1f)] public float chanceInvestida = 0.4f;
    public float velocidadeInvestida = 24f;
    [Tooltip("Tempo \"avisando\" (piscando) antes de avançar")]
    public float duracaoTelegraphInvestida = 0.4f;
    [Tooltip("Margem fora da câmera onde a investida começa/termina")]
    public float margemForaCamera = 1.5f;
    public float danoInvestida = 18f;
    public float cooldownInvestida = 4f;

    [Header("Fumaça de Veneno (trilha da investida)")]
    public float intervaloSpawnNuvemVeneno = 0.15f;
    public float danoNuvemVeneno = 15f;
    public float intervaloTickVeneno = 0.5f;
    public float duracaoNuvemVeneno = 3f;
    public float raioNuvemVeneno = 2.6f;

    [Header("Grito Sônico")]
    [Tooltip("Distância em que o jogador \"chega perto demais\" e sofre o grito")]
    public float raioGritoSonico = 1.8f;
    public float danoGritoSonico = 10f;
    public float forcaEmpurraoGrito = 28f;
    public float cooldownGritoSonico = 5f;

    [Header("Garras das Sombras")]
    [Tooltip("Raio da área atingida pelas garras que emergem do chão")]
    public float raioGarras = 1.6f;
    public float danoGarras = 16f;
    [Tooltip("Tempo de aviso (marcador no chão) antes das garras emergirem")]
    public float duracaoTelegraphGarras = 0.9f;
    [Tooltip("Tempo que o jogador fica preso, sem se mover, se atingido")]
    public float duracaoImobilizacao = 1f;
    public float cooldownGarras = 7f;

    // ──────────────────────────────────────────────
    // VOO (estilo abelha) / FLUTUAÇÃO
    // ──────────────────────────────────────────────
    [Header("Voo (estilo abelha)")]
    public float velocidadeVoo = 3f;
    [Tooltip("Raio ao redor do jogador onde a criatura fica zanzando")]
    public float raioVooAoRedorJogador = 4.5f;
    [Tooltip("Duração mínima/máxima de cada trecho de voo antes de parar para atacar")]
    public float tempoMinVoo = 1f;
    public float tempoMaxVoo = 2.2f;
    [Tooltip("Tempo parado (hover) enquanto ataca")]
    public float tempoMinParado = 0.6f;
    public float tempoMaxParado = 1.2f;
    [Tooltip("Frequência de mudança brusca de direção durante o voo (zigue-zague)")]
    public float intervalZigueZague = 0.2f;
    [Tooltip("Multiplicador de velocidade de voo na Fase 2")]
    public float multiplicadorVooFase2 = 1.4f;

    [Header("Flutuação (hover)")]
    public float amplitudeFlutuacao = 0.12f;
    public float velocidadeFlutuacao = 1.6f;

    // ──────────────────────────────────────────────
    // SPRITES PIXEL ART (HP bar)
    // ──────────────────────────────────────────────
    [Header("Sprites Pixel Art (arrastar no Inspector)")]
    public Sprite sprHpFrame;
    public Sprite sprHpFill;
    public Sprite sprHpBg;

    // ──────────────────────────────────────────────
    // REFERÊNCIAS INTERNAS
    // ──────────────────────────────────────────────
    private InimigoController controller;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Transform player;

    private bool fase2Ativada = false;
    private int projeteis;
    private Vector3 posBase;
    private float tempoFlutuacao;
    private Transform sombraTransform;
    private GameObject bossMsgGO;

    // Voo (estilo abelha)
    private Vector2 direcaoVoo = Vector2.zero;
    private float timerZigueZague;
    private bool emEmboscada = false;

    // Investida
    private DanoInimigo danoContato;
    private float proximaInvestidaPermitida;

    // Grito Sônico
    private float proximoGritoPermitido;

    // Garras das Sombras
    private float proximaGarraPermitida;

    // UI
    private GameObject bossCanvasGO;
    private Image hpFill;
    private Image hpFillGhost;
    private TextMeshProUGUI faseText;
    private TextMeshProUGUI hpText;
    private float hpGhostDisplay = 1f;
    private float damageTimer;

    // ──────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────────────────────

    // Co-op: re-mira o player mais próximo periodicamente (em SP = o único player).
    void AtualizarAlvoCoop()
    {
        var t = PlayerStats.MaisProximoTransform(transform.position);
        if (t != null) player = t;
    }

    void Start()
    {
        controller    = GetComponent<InimigoController>();
        // Escala de dano do boss por tempo (uma vez no spawn)
        float md = EnemyScaling.BossDanoMult();
        danoProjetil          *= md;
        danoProjetilEmboscada *= md;
        danoInvestida         *= md;
        danoNuvemVeneno       *= md;
        danoGritoSonico       *= md;
        danoGarras            *= md;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator      = GetComponent<Animator>();
        AtualizarAlvoCoop();
        InvokeRepeating(nameof(AtualizarAlvoCoop), 0.5f, 0.5f);

        projeteis = projeteisFase1;
        posBase   = transform.position;

        danoContato = GetComponent<DanoInimigo>();
        if (danoContato != null)
        {
            danoContato.dano    = danoInvestida;
            danoContato.enabled = false;
        }

        CriarSombra();
        CriarBossUI();
        StartCoroutine(SequenciaEntrada());
        StartCoroutine(LoopEmboscada());
        StartCoroutine(LoopVoo());
        StartCoroutine(LoopGarras());
    }

    void Update()
    {
        if (controller == null || controller.estaMorrendo) return;

        AtualizarUI();
        ChecarFase2();
        AtualizarFlutuacao();
        AtualizarSombra();
        ChecarGritoSonico();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        float sinalX = transform.localScale.x >= 0 ? 1f : -1f;
        Vector3 pos  = transform.position + new Vector3(offsetBoca.x * sinalX, offsetBoca.y, 0f);
        Gizmos.DrawWireSphere(pos, 0.08f);
        Gizmos.DrawLine(transform.position, pos);
    }

    void OnDestroy()
    {
        if (bossCanvasGO != null) Destroy(bossCanvasGO);
        if (bossMsgGO   != null) Destroy(bossMsgGO);
    }

    // ──────────────────────────────────────────────────────────────
    // FLUTUAÇÃO / SOMBRA
    // ──────────────────────────────────────────────────────────────

    void AtualizarFlutuacao()
    {
        tempoFlutuacao += Time.deltaTime * velocidadeFlutuacao;
        transform.position = posBase + Vector3.up * (Mathf.Sin(tempoFlutuacao) * amplitudeFlutuacao);

        if (spriteRenderer != null && Mathf.Abs(direcaoVoo.x) > 0.05f)
            spriteRenderer.flipX = direcaoVoo.x < 0f;
    }

    void CriarSombra()
    {
        int tw = 32, th = 14;
        Texture2D tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float cx = tw * 0.5f, cy = th * 0.5f;
        float rx = tw * 0.5f, ry = th * 0.5f;

        for (int y = 0; y < th; y++)
        for (int x = 0; x < tw; x++)
        {
            float dx = (x + 0.5f - cx) / rx;
            float dy = (y + 0.5f - cy) / ry;
            float alpha = Mathf.Clamp01(1f - (dx * dx + dy * dy));
            tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
        }
        tex.Apply();

        Sprite spr = Sprite.Create(tex, new Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), 16f);

        GameObject sombraGO = new GameObject("Sombra");
        sombraGO.transform.position   = transform.position + Vector3.down * 0.45f;
        sombraGO.transform.localScale = new Vector3(0.55f, 0.4f, 1f);

        SpriteRenderer sr = sombraGO.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.color  = new Color(1f, 1f, 1f, 0.5f);

        if (spriteRenderer != null)
        {
            sr.sortingLayerID   = spriteRenderer.sortingLayerID;
            sr.sortingLayerName = spriteRenderer.sortingLayerName;
            sr.sortingOrder     = spriteRenderer.sortingOrder - 1;
        }

        sombraTransform = sombraGO.transform;
    }

    void AtualizarSombra()
    {
        if (sombraTransform == null || spriteRenderer == null) return;
        sombraTransform.position = transform.position + Vector3.down * 0.45f;
        SpriteRenderer sr = sombraTransform.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 1f, Mathf.Max(0f, spriteRenderer.color.a) * 0.5f);
    }

    // ──────────────────────────────────────────────────────────────
    // VERIFICAÇÃO DE FASE
    // ──────────────────────────────────────────────────────────────

    void ChecarFase2()
    {
        if (fase2Ativada) return;
        if (controller.GetPorcentagemVida() <= gatilhoFase2)
        {
            fase2Ativada = true;
            StartCoroutine(TransicaoFase2());
        }
    }

    IEnumerator TransicaoFase2()
    {
        controller.AplicarBuffDefesa(reducaoDanoFase2, 99999f);
        projeteis = projeteisFase1 * 2;

        if (faseText != null)
        {
            faseText.text  = Loc.T("boss.phase2");
            faseText.color = new Color(0.6f, 0.3f, 1f);
        }

        yield return StartCoroutine(FlashFase2());
        StartCoroutine(MostrarTextoTela("A NOITE DESPERTOU!", new Color(0.6f, 0.3f, 1f), 2f));
        StartCoroutine(LoopAuraFase2());
    }

    IEnumerator FlashFase2()
    {
        for (int i = 0; i < 8; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.07f);
            if (spriteRenderer != null) spriteRenderer.color = new Color(0.5f, 0.1f, 0.7f);
            yield return new WaitForSeconds(0.07f);
        }
        // A criatura mantém uma cor mais sombria/roxa pelo resto da luta, marcando a Fase 2
        if (spriteRenderer != null) spriteRenderer.color = corFase2;
    }

    IEnumerator LoopAuraFase2()
    {
        while (!controller.estaMorrendo)
        {
            if (!emEmboscada) StartCoroutine(ParticulasSombrias(3));
            yield return new WaitForSeconds(intervaloAuraFase2);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // FURTIVIDADE / EMBOSCADA
    // ──────────────────────────────────────────────────────────────

    IEnumerator LoopEmboscada()
    {
        yield return new WaitForSeconds(intervalEmboscadaFase1 * 0.6f);

        while (!controller.estaMorrendo)
        {
            yield return StartCoroutine(Emboscar());
            float proximo = fase2Ativada ? intervalEmboscadaFase2 : intervalEmboscadaFase1;
            yield return new WaitForSeconds(proximo);
        }
    }

    IEnumerator Emboscar()
    {
        if (spriteRenderer == null || player == null) yield break;

        emEmboscada = true;
        direcaoVoo  = Vector2.zero;

        // Dissolve nas sombras
        StartCoroutine(ParticulasSombrias(8));
        yield return StartCoroutine(FadeAlpha(spriteRenderer.color.a, alphaSombra, duracaoFade));

        // Espreita invisível
        yield return new WaitForSeconds(duracaoInvisivel);

        // Teleporta para perto do jogador
        posBase = ObterPosicaoTeleporte();
        transform.position = posBase;
        if (sombraTransform != null) sombraTransform.position = posBase + Vector3.down * 0.45f;

        // Olhos brilham — telegraph antes de atacar
        yield return StartCoroutine(PiscarOlhos());

        // Reaparece
        yield return StartCoroutine(FadeAlpha(alphaSombra, 1f, duracaoFade));
        CameraShaker.Tremer(0.05f, 1.5f);
        StartCoroutine(ParticulasSombrias(10));

        DispararEmboscada();
        emEmboscada = false;
    }

    IEnumerator PiscarOlhos()
    {
        if (spriteRenderer == null) yield break;
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = new Color(0.7f, 0.2f, 1f, 0.45f);
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = new Color(1f, 1f, 1f, alphaSombra);
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator ParticulasSombrias(int qtd)
    {
        int sortL = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        int sortO = spriteRenderer != null ? spriteRenderer.sortingOrder   : 0;
        Vector3 origem = transform.position;

        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(2f, 2f));
            tex.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Clamp01(1f - d / 2f)));
        }
        tex.Apply();
        Sprite spr = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);

        var pGOs  = new GameObject[qtd];
        var pVels = new Vector2[qtd];

        for (int i = 0; i < qtd; i++)
        {
            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float spd = Random.Range(0.6f, 1.8f);
            pVels[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;

            GameObject p = new GameObject("PartSombria");
            p.transform.position   = origem + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0f);
            float scl = Random.Range(0.06f, 0.18f);
            p.transform.localScale = new Vector3(scl, scl, 1f);

            SpriteRenderer srP = p.AddComponent<SpriteRenderer>();
            srP.sprite = spr;
            srP.color  = Color.Lerp(new Color(0.25f, 0.05f, 0.4f), Color.black, Random.value);
            srP.sortingLayerID = sortL;
            srP.sortingOrder   = sortO + 1;
            pGOs[i] = p;
        }

        float t = 0f, dur = 0.5f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p01 = t / dur;
            for (int i = 0; i < qtd; i++)
            {
                if (pGOs[i] == null) continue;
                pGOs[i].transform.position += (Vector3)(pVels[i] * Time.deltaTime);
                pVels[i] *= 0.95f;
                SpriteRenderer srP = pGOs[i].GetComponent<SpriteRenderer>();
                if (srP != null) { Color c = srP.color; c.a = Mathf.Lerp(0.9f, 0f, p01); srP.color = c; }
            }
            yield return null;
        }

        for (int i = 0; i < qtd; i++)
            if (pGOs[i] != null) Destroy(pGOs[i]);
    }

    Vector2 ObterPosicaoTeleporte()
    {
        if (player == null) return transform.position;

        float raioBoss = Mathf.Abs(transform.localScale.x) * 0.22f + 0.15f;
        int mask = LayerMask.GetMask("obstacles");

        for (int tentativa = 0; tentativa < 30; tentativa++)
        {
            float angulo = Random.Range(0f, 360f);
            float dist   = Random.Range(distMinTeleporte, distMaxTeleporte);
            float rad    = angulo * Mathf.Deg2Rad;
            Vector2 alvo = (Vector2)player.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * dist;

            if (!Physics2D.OverlapCircle(alvo, raioBoss, mask))
                return alvo;
        }

        return transform.position;
    }

    IEnumerator FadeAlpha(float de, float para, float duracao)
    {
        if (spriteRenderer == null) yield break;
        float t = 0f;
        Color c = spriteRenderer.color;
        while (t < duracao)
        {
            t   += Time.deltaTime;
            c.a  = Mathf.Lerp(de, para, t / duracao);
            spriteRenderer.color = c;
            yield return null;
        }
        c.a = para;
        spriteRenderer.color = c;
    }

    // ──────────────────────────────────────────────────────────────
    // ATAQUES
    // ──────────────────────────────────────────────────────────────

    // ──────────────────────────────────────────────────────────────
    // VOO ERRÁTICO (estilo abelha) + PARADA PARA ATACAR
    // ──────────────────────────────────────────────────────────────

    IEnumerator LoopVoo()
    {
        yield return new WaitForSeconds(2.5f);

        while (!controller.estaMorrendo)
        {
            // ── Voando: zigue-zague ao redor do jogador, igual abelha ──
            EscolherDirecaoVoo();
            float velFase = fase2Ativada ? multiplicadorVooFase2 : 1f;
            float duracaoVoo = Random.Range(tempoMinVoo, tempoMaxVoo) / velFase;
            float t = 0f;

            while (t < duracaoVoo && !controller.estaMorrendo)
            {
                if (emEmboscada) { yield return null; continue; }

                t += Time.deltaTime;
                timerZigueZague -= Time.deltaTime;
                if (timerZigueZague <= 0f) EscolherDirecaoVoo();

                // Atravessa obstáculos livremente — é uma criatura voadora
                posBase += (Vector3)(direcaoVoo * velocidadeVoo * velFase * Time.deltaTime);

                yield return null;
            }

            if (controller.estaMorrendo) yield break;

            // ── Para no ar e ataca ──
            direcaoVoo = Vector2.zero;
            if (!emEmboscada && EstaVisivel() && player != null)
            {
                if (Time.time >= proximaInvestidaPermitida && Random.value < ChanceInvestidaAtual())
                    yield return StartCoroutine(Investida());
                else
                    Disparar();
            }

            yield return new WaitForSeconds(Random.Range(tempoMinParado, tempoMaxParado));
        }
    }

    void EscolherDirecaoVoo()
    {
        timerZigueZague = intervalZigueZague;

        float anguloAleatorio = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 direcaoAleatoria = new Vector2(Mathf.Cos(anguloAleatorio), Mathf.Sin(anguloAleatorio));

        if (player == null) { direcaoVoo = direcaoAleatoria; return; }

        Vector2 paraJogador = (Vector2)player.position - (Vector2)posBase;
        float dist = paraJogador.magnitude;

        // Fica zanzando perto do jogador, sem se afastar demais — como uma abelha
        direcaoVoo = dist > raioVooAoRedorJogador
            ? (direcaoAleatoria * 0.4f + paraJogador.normalized * 0.6f).normalized
            : direcaoAleatoria;
    }

    IEnumerator Investida()
    {
        proximaInvestidaPermitida = Time.time + CooldownFase2(cooldownInvestida);

        Vector2 direcao = (Vector2)player.position - (Vector2)posBase;
        if (direcao.sqrMagnitude < 0.001f) direcao = Vector2.right;
        direcao.Normalize();

        // Telegraph: pisca em vermelho avisando o golpe
        Color corOriginal = spriteRenderer != null ? spriteRenderer.color : Color.white;
        float t = 0f;
        while (t < duracaoTelegraphInvestida)
        {
            t += Time.deltaTime;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(corOriginal, new Color(1f, 0.2f, 0.2f), Mathf.PingPong(t * 10f, 1f));
            yield return null;
        }
        if (spriteRenderer != null) spriteRenderer.color = corOriginal;

        // Calcula a reta de um lado da câmera ao outro, passando pelo jogador
        Vector2 origem, destino;
        Camera cam = Camera.main;
        if (cam != null)
        {
            float distCam   = Mathf.Abs(cam.transform.position.z);
            Vector2 bl       = cam.ViewportToWorldPoint(new Vector3(0f, 0f, distCam));
            Vector2 tr       = cam.ViewportToWorldPoint(new Vector3(1f, 1f, distCam));
            Vector2 centro   = (bl + tr) * 0.5f;
            Vector2 meiaTela = (tr - bl) * 0.5f + Vector2.one * margemForaCamera;

            float tx = Mathf.Abs(direcao.x) > 0.0001f ? meiaTela.x / Mathf.Abs(direcao.x) : float.MaxValue;
            float ty = Mathf.Abs(direcao.y) > 0.0001f ? meiaTela.y / Mathf.Abs(direcao.y) : float.MaxValue;
            float alcance = Mathf.Min(tx, ty);

            destino = centro + direcao * alcance;
            origem  = centro - direcao * alcance;
        }
        else
        {
            origem  = posBase;
            destino = (Vector2)posBase + direcao * 20f;
        }

        // Salta para o lado da câmera de onde vai surgir
        posBase = origem;
        transform.position = posBase;
        if (sombraTransform != null) sombraTransform.position = posBase + Vector3.down * 0.45f;
        StartCoroutine(ParticulasSombrias(8));

        // Atravessa a tela inteira deixando uma trilha de fumaça venenosa
        if (danoContato != null) danoContato.enabled = true;
        CameraShaker.Tremer(0.08f, 0.4f);
        direcaoVoo = direcao;

        float distanciaTotal   = Vector2.Distance(origem, destino);
        float duracaoTravessia = distanciaTotal / velocidadeInvestida;
        float timerNuvem = 0f;
        t = 0f;
        while (t < duracaoTravessia)
        {
            t += Time.deltaTime;
            posBase += (Vector3)(direcao * velocidadeInvestida * Time.deltaTime);

            timerNuvem += Time.deltaTime;
            if (timerNuvem >= intervaloSpawnNuvemVeneno)
            {
                timerNuvem = 0f;
                CriarNuvemVeneno(posBase);
            }

            yield return null;
        }

        if (danoContato != null) danoContato.enabled = false;
        direcaoVoo = Vector2.zero;
    }

    void CriarNuvemVeneno(Vector3 pos)
    {
        GameObject nuvem = new GameObject("NuvemVenenoCaveira");
        nuvem.transform.position = pos;
        nuvem.AddComponent<NuvemVenenoCaveira>()
             .Inicializar(danoNuvemVeneno, intervaloTickVeneno, duracaoNuvemVeneno, raioNuvemVeneno);
    }

    // ──────────────────────────────────────────────────────────────
    // GRITO SÔNICO — empurra e machuca quem chega perto demais
    // ──────────────────────────────────────────────────────────────

    void ChecarGritoSonico()
    {
        if (emEmboscada || player == null) return;
        if (Time.time < proximoGritoPermitido) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= raioGritoSonico)
            StartCoroutine(GritoSonico());
    }

    IEnumerator GritoSonico()
    {
        proximoGritoPermitido = Time.time + CooldownFase2(cooldownGritoSonico);

        // Telegraph: a criatura "incha" rapidamente antes de gritar
        Vector3 escalaOriginal = transform.localScale;
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            transform.localScale = escalaOriginal * Mathf.Lerp(1f, 1.2f, t / 0.15f);
            yield return null;
        }

        CameraShaker.Tremer(0.08f, 0.3f);
        StartCoroutine(EfeitoOndaGrito());

        if (player != null && Vector2.Distance(transform.position, player.position) <= raioGritoSonico * 1.4f)
        {
            PlayerStats ps = player.GetComponent<PlayerStats>();
            if (ps != null) ps.TakeDamage(danoGritoSonico);

            Rigidbody2D rbPlayer = player.GetComponent<Rigidbody2D>();
            if (rbPlayer != null)
            {
                Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                rbPlayer.AddForce(dir * forcaEmpurraoGrito, ForceMode2D.Impulse);
            }
        }

        t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaOriginal * 1.2f, escalaOriginal, t / 0.15f);
            yield return null;
        }
        transform.localScale = escalaOriginal;
    }

    IEnumerator EfeitoOndaGrito()
    {
        const int segmentos = 28;
        GameObject go = new GameObject("OndaGritoSonico");
        go.transform.position = transform.position;

        LineRenderer lr  = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = segmentos;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 145;

        for (int i = 0; i < segmentos; i++)
        {
            float a = (360f / segmentos) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a)));
        }

        float t = 0f, dur = 0.4f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p  = t / dur;
            float r  = Mathf.Lerp(0.1f, raioGritoSonico * 1.4f, p);
            float a  = Mathf.Lerp(0.8f, 0f, p);
            go.transform.localScale = Vector3.one * r;
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.15f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(0.7f, 0.3f, 1f, a);
            yield return null;
        }

        Destroy(go);
    }

    // ──────────────────────────────────────────────────────────────
    // GARRAS DAS SOMBRAS — raízes escuras emergem sob o jogador, prendendo-o
    // ──────────────────────────────────────────────────────────────

    IEnumerator LoopGarras()
    {
        yield return new WaitForSeconds(4f);
        while (!controller.estaMorrendo)
        {
            if (!emEmboscada && player != null && EstaVisivel() && Time.time >= proximaGarraPermitida)
                yield return StartCoroutine(GarrasDasSombras());

            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator GarrasDasSombras()
    {
        proximaGarraPermitida = Time.time + CooldownFase2(cooldownGarras);

        Vector3 alvo = player.position;
        GameObject marcador = CriarMarcadorGarras(alvo);

        // Acompanha o jogador só no começo do aviso — depois trava no lugar,
        // dando tempo dele se afastar e desviar antes da erupção.
        float duracaoPerseguicao = duracaoTelegraphGarras * 0.4f;
        float t = 0f;
        while (t < duracaoTelegraphGarras)
        {
            t += Time.deltaTime;
            float p = t / duracaoTelegraphGarras;

            if (player != null && t < duracaoPerseguicao) alvo = player.position;
            marcador.transform.position   = alvo;
            marcador.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, p);

            SpriteRenderer srM = marcador.GetComponent<SpriteRenderer>();
            if (srM != null)
            {
                Color c = srM.color;
                c.a = Mathf.Lerp(0.25f, 0.85f, Mathf.PingPong(p * 6f, 1f));
                srM.color = c;
            }

            yield return null;
        }

        Destroy(marcador);

        CameraShaker.Tremer(0.12f, 0.25f);
        StartCoroutine(EfeitoGarrasVisual(alvo));

        Collider2D[] alvos = Physics2D.OverlapCircleAll(alvo, raioGarras);
        foreach (Collider2D col in alvos)
        {
            if (!col.CompareTag("Player")) continue;

            if (col.TryGetComponent(out PlayerStats ps))
                ps.TakeDamage(danoGarras);

            if (col.TryGetComponent(out moviment_player2 mov))
                mov.Imobilizar(duracaoImobilizacao);
        }
    }

    GameObject CriarMarcadorGarras(Vector3 pos)
    {
        const int tam = 64;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        float c = tam * 0.5f;
        for (int y = 0; y < tam; y++)
        for (int x = 0; x < tam; x++)
        {
            float d     = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
            float anel  = Mathf.SmoothStep(1f, 0f, Mathf.Abs(d - 0.85f) * 6f);
            float miolo = Mathf.Clamp01(1f - d) * 0.35f;
            float alpha = Mathf.Clamp01(anel + miolo);
            tex.SetPixel(x, y, new Color(0.6f, 0.05f, 0.15f, alpha));
        }
        tex.Apply();

        GameObject go = new GameObject("MarcadorGarras");
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * 0.3f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), tam / (raioGarras * 2f));
        sr.sortingOrder = 5;
        sr.color        = new Color(0.6f, 0.05f, 0.15f, 0.25f);

        return go;
    }

    IEnumerator EfeitoGarrasVisual(Vector3 pos)
    {
        const int qtdGarras = 5;
        var gos = new GameObject[qtdGarras];
        var lrs = new LineRenderer[qtdGarras];

        for (int i = 0; i < qtdGarras; i++)
        {
            float ang   = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            GameObject go = new GameObject("GarraSombria");
            go.transform.position = pos + (Vector3)(dir * Random.Range(0f, raioGarras * 0.3f));

            LineRenderer lr  = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            // Sorting order alto para a garra aparecer rasgando por cima do jogador
            lr.sortingOrder  = 250;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, Vector3.up * 0.1f);
            lr.startColor = lr.endColor = new Color(0.05f, 0f, 0.1f, 0.9f);

            gos[i] = go;
            lrs[i] = lr;
        }

        // Subida
        float t = 0f, durSubida = 0.25f;
        while (t < durSubida)
        {
            t += Time.deltaTime;
            float p       = t / durSubida;
            float altura  = Mathf.Lerp(0.1f, 1.9f, Mathf.Sin(p * Mathf.PI * 0.6f));
            float largura = Mathf.Lerp(0.25f, 0.05f, p);

            for (int i = 0; i < qtdGarras; i++)
            {
                lrs[i].SetPosition(1, Vector3.up * altura);
                lrs[i].startWidth = largura * 1.4f;
                lrs[i].endWidth   = largura * 0.2f;
            }

            yield return null;
        }

        // Mantém as garras erguidas, totalmente visíveis
        yield return new WaitForSeconds(0.5f);

        // Desaparecimento
        t = 0f;
        float durFade = 0.4f;
        while (t < durFade)
        {
            t += Time.deltaTime;
            float p = t / durFade;

            for (int i = 0; i < qtdGarras; i++)
            {
                Color cor = lrs[i].startColor;
                cor.a = Mathf.Lerp(0.9f, 0f, p);
                lrs[i].startColor = lrs[i].endColor = cor;
            }

            yield return null;
        }

        for (int i = 0; i < qtdGarras; i++)
            Destroy(gos[i]);
    }

    // Fase 2: ataques especiais ficam mais frequentes e a Investida mais provável
    float CooldownFase2(float cooldownBase) => fase2Ativada ? cooldownBase * multiplicadorCooldownFase2 : cooldownBase;
    float ChanceInvestidaAtual() => fase2Ativada ? Mathf.Clamp01(chanceInvestida + bonusChanceInvestidaFase2) : chanceInvestida;

    bool EstaVisivel() => spriteRenderer == null || spriteRenderer.color.a > 0.5f;

    Vector3 PosicaoBoca()
    {
        float sinalX = spriteRenderer != null ? Mathf.Sign(transform.localScale.x) : 1f;
        return transform.position + new Vector3(offsetBoca.x * sinalX, offsetBoca.y, 0f);
    }

    void Disparar()
    {
        if (prefabProjetil == null || player == null) return;

        Vector3 spawnPos  = PosicaoBoca();
        Vector2 dirBase   = ((Vector2)player.position - (Vector2)spawnPos).normalized;
        float anguloBase  = Mathf.Atan2(dirBase.y, dirBase.x) * Mathf.Rad2Deg;

        for (int i = 0; i < projeteis; i++)
        {
            float t      = projeteis > 1 ? (float)i / (projeteis - 1) : 0.5f;
            float offset = Mathf.Lerp(-anguloLeque / 2f, anguloLeque / 2f, t);
            float ang    = (anguloBase + offset) * Mathf.Deg2Rad;
            Vector2 dir  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            GameObject proj = NetSpawn.Spawnar(prefabProjetil, spawnPos);
            if (proj == null) continue;
            ProjetilInimigoDano pid = proj.GetComponent<ProjetilInimigoDano>();
            if (pid != null)
            {
                pid.dano       = danoProjetil;
                pid.velocidade = velocidadeProjetil;
                pid.SetDirecao(dir);
            }
            else
            {
                Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = dir * velocidadeProjetil;
            }
        }
    }

    void DispararEmboscada()
    {
        if (prefabProjetil == null || player == null) return;

        Vector3 spawnPos = PosicaoBoca();
        Vector2 dirBase  = ((Vector2)player.position - (Vector2)spawnPos).normalized;
        float anguloBase = Mathf.Atan2(dirBase.y, dirBase.x) * Mathf.Rad2Deg;

        for (int i = 0; i < projeteisEmboscada; i++)
        {
            float t      = projeteisEmboscada > 1 ? (float)i / (projeteisEmboscada - 1) : 0.5f;
            float offset = Mathf.Lerp(-180f, 180f, t);
            float ang    = (anguloBase + offset) * Mathf.Deg2Rad;
            Vector2 dir  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            GameObject proj = NetSpawn.Spawnar(prefabProjetil, spawnPos);
            if (proj == null) continue;
            ProjetilInimigoDano pid = proj.GetComponent<ProjetilInimigoDano>();
            if (pid != null)
            {
                pid.dano       = danoProjetilEmboscada;
                pid.velocidade = velocidadeProjetilEmboscada;
                pid.SetDirecao(dir);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // ENTRADA
    // ──────────────────────────────────────────────────────────────

    IEnumerator SequenciaEntrada()
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color; c.a = 0f;
            spriteRenderer.color = c;
        }

        yield return StartCoroutine(MostrarAvisoBoss());

        CameraShaker.Tremer(0.04f, 2.5f);
        StartCoroutine(ParticulasSombrias(18));

        yield return StartCoroutine(FadeAlpha(0f, 1f, 0.6f));
    }

    IEnumerator MostrarAvisoBoss()
    {
        GameObject warnGO = new GameObject("BossWarning");
        Canvas cv = warnGO.AddComponent<Canvas>();
        cv.renderMode    = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder  = 200;
        warnGO.AddComponent<CanvasScaler>();

        GameObject bgGO = CriarUIGO("BG", warnGO.transform);
        ExpandirRect(bgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0f);

        GameObject txtGO = CriarUIGO("WarnText", warnGO.transform);
        RectTransform tr = txtGO.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.05f, 0.35f);
        tr.anchorMax = new Vector2(0.95f, 0.65f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = Loc.T("boss.appeared") + "\n<size=60%>" + nomeBoss.ToUpper() + "</size>";
        txt.fontSize  = 52;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = new Color(0.7f, 0.2f, 1f, 0f);

        yield return StartCoroutine(FadeCanvasElements(bgImg, txt, 0f, 1f, 0.4f, 0.65f));

        for (int i = 0; i < 3; i++)
        {
            txt.color = new Color(1f, 0.9f, 0.1f);
            yield return new WaitForSeconds(0.15f);
            txt.color = new Color(0.7f, 0.2f, 1f);
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(FadeCanvasElements(bgImg, txt, 1f, 0f, 0.4f, 0.65f));

        Destroy(warnGO);
    }

    IEnumerator FadeCanvasElements(Image bg, TextMeshProUGUI txt, float de, float para, float dur, float maxAlphaBG)
    {
        float t = 0f;
        while (t < dur)
        {
            t  += Time.deltaTime;
            float a = Mathf.Lerp(de, para, t / dur);
            bg.color  = new Color(0f, 0f, 0f, a * maxAlphaBG);
            txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, a);
            yield return null;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // BOSS HEALTH BAR UI
    // ──────────────────────────────────────────────────────────────

    // Um Image do tipo Filled SEM sprite ignora o fillAmount (renderiza quad cheio),
    // então a barra nunca desce. Garante um sprite branco como fallback.
    static Sprite s_spriteBranco;
    static Sprite SpriteBranco()
    {
        if (s_spriteBranco == null)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var px = new Color[4]; for (int i = 0; i < 4; i++) px[i] = Color.white;
            t.SetPixels(px); t.Apply();
            s_spriteBranco = Sprite.Create(t, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }
        return s_spriteBranco;
    }

    public void CriarBossUI()
    {
        if (controller == null)     controller     = GetComponent<InimigoController>(); // co-op: no cliente o Start não roda
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();    // efeitos cosméticos (flash/aura/morte)
        bossCanvasGO = new GameObject("BossCaveiraCanvas");
        Canvas cv = bossCanvasGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 50;

        CanvasScaler cs = bossCanvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        bossCanvasGO.AddComponent<GraphicRaycaster>();

        GameObject painel = CriarUIGO("BossPanel", bossCanvasGO.transform);
        RectTransform pr = painel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.28f, 0.845f);
        pr.anchorMax = new Vector2(0.72f, 0.978f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        Image painelImg = painel.AddComponent<Image>();
        painelImg.color = new Color(0.04f, 0.03f, 0.06f, 0.9f);

        // Linha decorativa roxa (tema noturno)
        GameObject linha = CriarUIGO("Linha", painel.transform);
        RectTransform lr = linha.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 0.92f);
        lr.anchorMax = new Vector2(1f, 1f);
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        Image linhaImg = linha.AddComponent<Image>();
        linhaImg.color = new Color(0.5f, 0.2f, 0.8f, 1f);

        // Nome
        GameObject nomeGO = CriarUIGO("NomeBoss", painel.transform);
        RectTransform nr = nomeGO.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0.01f, 0.6f);
        nr.anchorMax = new Vector2(0.99f, 0.92f);
        nr.offsetMin = nr.offsetMax = Vector2.zero;
        TextMeshProUGUI nomeTxt = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTxt.text      = nomeBoss.ToUpper();
        nomeTxt.fontSize  = 20;
        nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.color     = new Color(0.75f, 0.5f, 1f);
        nomeTxt.alignment = TextAlignmentOptions.Center;

        // Fase
        GameObject faseGO = CriarUIGO("FaseBoss", painel.transform);
        RectTransform fr = faseGO.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0.75f, 0.92f);
        fr.anchorMax = new Vector2(0.99f, 1f);
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        faseText           = faseGO.AddComponent<TextMeshProUGUI>();
        faseText.text      = Loc.T("boss.phase1");
        faseText.fontSize  = 14;
        faseText.color     = new Color(0.75f, 0.75f, 0.75f);
        faseText.alignment = TextAlignmentOptions.MidlineRight;

        // Barra HP
        GameObject barBG = CriarUIGO("HPBarBG", painel.transform);
        RectTransform bbr = barBG.GetComponent<RectTransform>();
        bbr.anchorMin = new Vector2(0.01f, 0.08f);
        bbr.anchorMax = new Vector2(0.99f, 0.52f);
        bbr.offsetMin = bbr.offsetMax = Vector2.zero;
        Image bgImg = barBG.AddComponent<Image>();
        if (sprHpBg != null) { bgImg.sprite = sprHpBg; bgImg.type = Image.Type.Tiled; bgImg.color = Color.white; }
        else bgImg.color = new Color(0.1f, 0.1f, 0.12f);

        GameObject ghostGO = CriarUIGO("HPGhost", barBG.transform);
        ExpandirRect(ghostGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpFillGhost            = ghostGO.AddComponent<Image>();
        hpFillGhost.sprite     = sprHpFill != null ? sprHpFill : SpriteBranco();
        hpFillGhost.type       = Image.Type.Filled;
        hpFillGhost.fillMethod = Image.FillMethod.Horizontal;
        hpFillGhost.fillAmount = 1f;
        hpFillGhost.color      = new Color(0.85f, 0.55f, 1f, 0.9f);

        GameObject fillGO = CriarUIGO("HPFill", barBG.transform);
        ExpandirRect(fillGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpFill            = fillGO.AddComponent<Image>();
        hpFill.sprite     = sprHpFill != null ? sprHpFill : SpriteBranco();
        hpFill.type       = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillAmount = 1f;
        hpFill.color      = sprHpFill != null ? Color.white : new Color(0.45f, 0.15f, 0.7f);

        GameObject hpTextGO = CriarUIGO("HPText", barBG.transform);
        ExpandirRect(hpTextGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpText           = hpTextGO.AddComponent<TextMeshProUGUI>();
        hpText.fontSize  = 11;
        hpText.color     = Color.white;
        hpText.fontStyle = FontStyles.Bold;
        hpText.alignment = TextAlignmentOptions.Center;

        if (sprHpFrame != null)
        {
            GameObject frameGO = CriarUIGO("HPFrame", barBG.transform);
            ExpandirRect(frameGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Image frameImg = frameGO.AddComponent<Image>();
            frameImg.sprite        = sprHpFrame;
            frameImg.type          = Image.Type.Sliced;
            frameImg.raycastTarget = false;
            frameImg.color         = Color.white;
        }

        StartCoroutine(EntradaUI(painel));
    }

    IEnumerator EntradaUI(GameObject painel)
    {
        CanvasGroup cg = painel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime * 1.5f; cg.alpha = Mathf.Lerp(0f, 1f, t); yield return null; }
        cg.alpha = 1f;
    }

    // IBossHud: o BossHudNet (cliente) chama isto com a vida sincronizada no controller.
    public void AtualizarBarraUI() => AtualizarUI();

    public int FaseUI
    {
        get => fase2Ativada ? 1 : 0;
        set
        {
            bool era = fase2Ativada;
            fase2Ativada = value >= 1;
            if (faseText != null)
            {
                faseText.text = Loc.T(value >= 1 ? "boss.phase2" : "boss.phase1");
                if (value >= 1) faseText.color = new Color(0.6f, 0.3f, 1f);
            }
            // Cliente co-op: reproduz a APARÊNCIA de fase 2 (no host vem de TransicaoFase2).
            // Corrotinas rodam mesmo com o script desligado (GameObject ativo).
            if (value >= 1 && !era)
            {
                if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
                if (controller == null)     controller     = GetComponent<InimigoController>();
                StartCoroutine(FlashFase2());
                StartCoroutine(LoopAuraFase2());
            }
        }
    }

    void AtualizarUI()
    {
        if (hpFill == null || controller == null) return;

        float pct = controller.GetPorcentagemVida();

        hpFill.fillAmount = pct;

        hpFill.color = fase2Ativada
            ? new Color(0.6f, 0.2f, 1f)
            : (sprHpFill != null ? Color.white : new Color(0.45f, 0.15f, 0.7f));

        if (pct < hpGhostDisplay - 0.005f) damageTimer = 0.6f;
        if (damageTimer > 0f) damageTimer -= Time.deltaTime;
        else hpGhostDisplay = Mathf.Lerp(hpGhostDisplay, pct, Time.deltaTime * 2f);
        hpFillGhost.fillAmount = hpGhostDisplay;

        if (hpText != null)
            hpText.text = $"{Mathf.RoundToInt(controller.vidaAtual)} / {Mathf.RoundToInt(controller.vidaMaxima)}";
    }

    // ──────────────────────────────────────────────────────────────
    // TEXTO FLUTUANTE NA TELA
    // ──────────────────────────────────────────────────────────────

    IEnumerator MostrarTextoTela(string mensagem, Color cor, float duracao)
    {
        GetComponent<BossHudNet>()?.BroadcastMensagem(mensagem, cor, duracao); // co-op: mesmo banner nos clientes
        if (bossMsgGO != null) Destroy(bossMsgGO);
        GameObject go = new GameObject("BossCaveiraMsg");
        bossMsgGO = go;
        Canvas cv = go.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 150;
        go.AddComponent<CanvasScaler>();

        GameObject txtGO = CriarUIGO("Msg", go.transform);
        RectTransform tr = txtGO.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.1f, 0.55f);
        tr.anchorMax = new Vector2(0.9f, 0.75f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = mensagem;
        txt.fontSize  = 38;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = new Color(cor.r, cor.g, cor.b, 0f);

        float t = 0f;
        while (t < 0.3f) { t += Time.deltaTime; txt.color = new Color(cor.r, cor.g, cor.b, t / 0.3f); yield return null; }
        txt.color = new Color(cor.r, cor.g, cor.b, 1f);

        yield return new WaitForSeconds(duracao);

        t = 0f;
        RectTransform msgRect = txtGO.GetComponent<RectTransform>();
        Vector2 posBaseTxt = msgRect.anchoredPosition;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float a = 1f - (t / 0.5f);
            txt.color = new Color(cor.r, cor.g, cor.b, a);
            msgRect.anchoredPosition = posBaseTxt + Vector2.up * (t * 120f);
            yield return null;
        }

        Destroy(go);
        bossMsgGO = null;
    }

    // ──────────────────────────────────────────────────────────────
    // EFEITO DE MORTE
    // ──────────────────────────────────────────────────────────────

    bool morteCosmetica = false;

    public void IniciarEfeitoMorte()
    {
        GetComponent<BossHudNet>()?.BroadcastMorte("CRIATURA DERROTADA!", new Color(0.8f, 0.5f, 1f)); // co-op
        StartCoroutine(EfeitoMorte());
    }

    // Cliente co-op: mesmo efeito de morte sem destruir o NetworkObject (host despawna).
    public void MorteCosmetica()
    {
        morteCosmetica = true;
        StartCoroutine(EfeitoMorte());
    }

    IEnumerator EfeitoMorte()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        CameraShaker.Tremer(0.12f, 3.5f);

        for (int i = 0; i < 14; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = i % 2 == 0 ? Color.white : new Color(0.5f, 0.1f, 0.8f);
            yield return new WaitForSeconds(0.04f);
        }

        StartCoroutine(ParticulasSombrias(30));

        Vector3 escBase = transform.localScale;
        float t = 0f, dur = 1.1f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            transform.localScale = escBase * Mathf.Lerp(1f, 3f, Mathf.Pow(p, 0.5f));
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(0.7f, 0.4f, 1f, Mathf.Lerp(1f, 0f, p * p));
            yield return null;
        }

        if (!morteCosmetica) BossMorteUI.Exibir("CRIATURA DERROTADA!", new Color(0.8f, 0.5f, 1f)); // cliente recebe via RPC
        if (!morteCosmetica) Destroy(gameObject); // cliente não destrói NetworkObject (host despawna)
    }

    // ──────────────────────────────────────────────────────────────
    // HELPERS DE UI
    // ──────────────────────────────────────────────────────────────

    static GameObject CriarUIGO(string nome, Transform pai)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void ExpandirRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}

// ──────────────────────────────────────────────────────────────
// NUVEM DE VENENO — trilha deixada pela investida do BossCaveira
// ──────────────────────────────────────────────────────────────
public class NuvemVenenoCaveira : MonoBehaviour
{
    private float dano;
    private float intervaloTick;
    private float duracao;
    private float raio;

    private float timerTick;
    private float timerVida;
    private SpriteRenderer sr;

    public void Inicializar(float _dano, float _intervalo, float _duracao, float _raio)
    {
        dano          = _dano;
        intervaloTick = _intervalo;
        duracao       = _duracao;
        raio          = _raio;

        const int tam = 64;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        float c = tam * 0.5f;
        for (int y = 0; y < tam; y++)
        for (int x = 0; x < tam; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
            float ruido = Mathf.PerlinNoise(x * 0.25f + Random.value * 100f, y * 0.25f + Random.value * 100f);
            Color cor = Color.Lerp(new Color(0.55f, 0.85f, 0.15f), new Color(0.15f, 0.35f, 0.05f), ruido);
            cor.a = Mathf.Clamp01(1f - d) * 0.7f;
            tex.SetPixel(x, y, cor);
        }
        tex.Apply();

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), tam / (raio * 2f));
        sr.sortingOrder = 140;
        Color corInicial = sr.color; corInicial.a = 0f; sr.color = corInicial;

        StartCoroutine(Aparecer());
    }

    IEnumerator Aparecer()
    {
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            Color c = sr.color;
            c.a = Mathf.Lerp(0f, 1f, t / 0.2f);
            sr.color = c;
            yield return null;
        }
    }

    void Update()
    {
        timerVida += Time.deltaTime;
        timerTick += Time.deltaTime;

        if (timerTick >= intervaloTick)
        {
            timerTick = 0f;

            Collider2D[] alvos = Physics2D.OverlapCircleAll(transform.position, raio);
            foreach (Collider2D col in alvos)
            {
                if (col.CompareTag("Player") && col.TryGetComponent(out PlayerStats ps))
                    ps.AplicarVenenoPlayer(dano, intervaloTick, intervaloTick + 0.5f);
            }
        }

        float restante = duracao - timerVida;
        if (restante <= 0.4f)
        {
            Color c = sr.color;
            c.a = Mathf.Max(0f, c.a - Time.deltaTime * 2.5f);
            sr.color = c;
        }

        if (timerVida >= duracao) Destroy(gameObject);
    }
}
