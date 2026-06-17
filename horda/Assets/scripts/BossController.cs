using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;

[RequireComponent(typeof(InimigoController))]
public class BossController : MonoBehaviour, IBoss
{
    // ──────────────────────────────────────────────
    // IDENTIDADE
    // ──────────────────────────────────────────────
    [Header("Identidade")]
    public string nomeBoss = "Maga Slime";

    // ──────────────────────────────────────────────
    // FASES
    // ──────────────────────────────────────────────
    [Header("Fases")]
    [Tooltip("Porcentagem de vida para entrar na Fase 2 (0‑1)")]
    public float gatilhoFase2 = 0.5f;
    [Tooltip("Redução de dano recebido na Fase 2 (0.3 = 30% menos dano)")]
    public float reducaoDanoFase2 = 0.3f;

    // ──────────────────────────────────────────────
    // TELEPORTE
    // ──────────────────────────────────────────────
    [Header("Teleporte")]
    public float intervalTeleporteFase1 = 4f;
    public float intervalTeleporteFase2 = 2.5f;
    public float distMinTeleporte = 4f;
    public float distMaxTeleporte = 9f;
    public float duracaoFade = 0.25f;

    // ──────────────────────────────────────────────
    // ATAQUES
    // ──────────────────────────────────────────────
    [Header("Ataques")]
    public GameObject prefabProjetil;
    [Tooltip("Projéteis disparados por salva na Fase 1")]
    public int projeteisFase1 = 5;
    [Tooltip("Ângulo total do leque de projéteis (graus)")]
    public float anguloLeque = 70f;
    public float intervalAtaqueFase1 = 2.5f;
    public float intervalAtaqueFase2 = 1.4f;
    public float velocidadeProjetil = 7f;
    public float danoProjetil = 15f;

    [Header("Olho (ponto de spawn do projétil)")]
    [Tooltip("Offset do olho em relação ao pivot do boss (world units). Ajuste pelo gizmo na cena.")]
    public Vector2 offsetOlho = new Vector2(0.03f, 0.13f);

    [Header("Projétil Especial (Pós-Teleporte)")]
    [Tooltip("Prefab do projétil especial. Se nulo, usa o prefabProjetil normal.")]
    public GameObject prefabProjetilEspecial;
    [Tooltip("Dano do projétil especial")]
    public float danoProjetilEspecial = 45f;

    [Header("Escudo (Fase 2)")]
    [Tooltip("Pontos de vida do escudo ativado na Fase 2")]
    public float vidaEscudo = 200f;

    // ──────────────────────────────────────────────
    // ATAQUE DE RAIO (FEITIÇO)
    // ──────────────────────────────────────────────
    [Header("Ataque de Raio")]
    [Tooltip("Tempo entre disparos do raio na Fase 1")]
    public float intervalRaioFase1 = 8f;
    [Tooltip("Tempo entre disparos do raio na Fase 2")]
    public float intervalRaioFase2 = 5f;
    [Tooltip("Duração da mira (telegraph) antes do raio disparar")]
    public float duracaoTelegraphRaio = 1f;
    [Tooltip("Duração do raio ativo causando dano")]
    public float duracaoRaio = 1.5f;
    [Tooltip("Largura do feixe (world units)")]
    public float larguraRaio = 0.6f;
    [Tooltip("Alcance máximo do feixe")]
    public float alcanceRaio = 14f;
    [Tooltip("Dano causado por tick enquanto o jogador estiver no feixe")]
    public float danoRaio = 8f;
    [Tooltip("Intervalo entre ticks de dano do feixe")]
    public float intervaloDanoRaio = 0.25f;
    [Tooltip("Ângulo total de varredura do feixe na Fase 2 (0 = sem varredura)")]
    public float anguloVarreduraRaio = 60f;

    // ──────────────────────────────────────────────
    // REFERÊNCIAS INTERNAS
    // ──────────────────────────────────────────────
    private InimigoController controller;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Transform player;
    private PlayerStats playerStats;

    private bool fase2Ativada = false;
    private int projeteis; // valor atual (muda na fase 2)
    private Transform sombraTransform;
    private GameObject bossMsgGO;
    private Vector3 escalaOriginal;
    private Dictionary<movi_inimigo_manter_distancia, float> intervaisOriginais = new Dictionary<movi_inimigo_manter_distancia, float>();

    // ──────────────────────────────────────────────
    // SPRITES PIXEL ART
    // ──────────────────────────────────────────────
    [Header("Sprites Pixel Art (arrastar no Inspector)")]
    public Sprite sprHpFrame;  // boss_hp_frame.png
    public Sprite sprHpFill;   // boss_hp_fill.png
    public Sprite sprHpBg;     // boss_hp_bg.png

    // ──────────────────────────────────────────────
    // UI
    // ──────────────────────────────────────────────
    private GameObject bossCanvasGO;
    private Image hpFill;
    private Image hpFillGhost; // barra "fantasma" que atrasa
    private TextMeshProUGUI faseText;
    private TextMeshProUGUI hpText;

    // ──────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────────────────────

    void Start()
    {
        controller    = GetComponent<InimigoController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator      = GetComponent<Animator>();
        player        = GameObject.FindGameObjectWithTag("Player")?.transform;
        playerStats   = player != null ? player.GetComponent<PlayerStats>() : null;

        // Escala de dano do boss por tempo (aplicada uma vez no spawn)
        float multDanoBoss = EnemyScaling.BossDanoMult();
        danoProjetil         *= multDanoBoss;
        danoProjetilEspecial *= multDanoBoss;
        danoRaio             *= multDanoBoss;

        projeteis = projeteisFase1;
        escalaOriginal = transform.localScale;

        CriarSombra();
        CriarBossUI();
        StartCoroutine(SequenciaEntrada());
        StartCoroutine(LoopTeleporte());
        StartCoroutine(LoopAtaque());
        StartCoroutine(LoopRaio());
    }

    void Update()
    {
        if (controller == null || controller.estaMorrendo) return;

        AtualizarUI();
        ChecarFase2();
        AtualizarSombra();
    }

    void OnDrawGizmosSelected()
    {
        // Mostra o ponto de spawn do projétil (olho) em amarelo no editor
        Gizmos.color = Color.yellow;
        float sinalX = transform.localScale.x >= 0 ? 1f : -1f;
        Vector3 pos  = transform.position + new Vector3(offsetOlho.x * sinalX, offsetOlho.y, 0f);
        Gizmos.DrawWireSphere(pos, 0.08f);
        Gizmos.DrawLine(transform.position, pos);
    }

    void OnDestroy()
    {
        if (bossCanvasGO != null)
            Destroy(bossCanvasGO);
        if (bossMsgGO != null)
            Destroy(bossMsgGO);
        RestaurarSlimeMagas();
    }

    // ──────────────────────────────────────────────────────────────
    // SOMBRA
    // ──────────────────────────────────────────────────────────────

    void CriarSombra()
    {
        // Textura de elipse (32x14 px) — alpha de 0 a 1, fadeout nas bordas
        int tw = 32, th = 14;
        Texture2D tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float cx = tw * 0.5f, cy = th * 0.5f;
        float rx = tw * 0.5f, ry = th * 0.5f;

        for (int y = 0; y < th; y++)
        {
            for (int x = 0; x < tw; x++)
            {
                float dx = (x + 0.5f - cx) / rx;
                float dy = (y + 0.5f - cy) / ry;
                float alpha = Mathf.Clamp01(1f - (dx * dx + dy * dy));
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }
        tex.Apply();

        Sprite spr = Sprite.Create(tex, new Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), 16f);

        GameObject sombraGO = new GameObject("Sombra");
        // Fora da hierarquia do boss para não interferir no collider
        sombraGO.transform.position   = transform.position + Vector3.down * 0.45f;
        sombraGO.transform.localScale = new Vector3(0.55f, 0.4f, 1f);

        SpriteRenderer sr = sombraGO.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.color  = new Color(1f, 1f, 1f, 0.5f);

        // Copia o sorting layer do boss para garantir que apareça na mesma camada
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
        // Segue o boss
        sombraTransform.position = transform.position + Vector3.down * 0.45f;
        SpriteRenderer sr = sombraTransform.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 1f, spriteRenderer.color.a * 0.5f);
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
        // Resistência a dano permanente
        controller.AplicarBuffDefesa(reducaoDanoFase2, 99999f);

        // Ativa escudo
        Escudo escudo = gameObject.AddComponent<Escudo>();
        escudo.Ativar(vidaEscudo);

        // Dobra os projéteis
        projeteis = projeteisFase1 * 2;

        // Muda animação (requer estado "Fase2" no AnimatorController)
        if (animator != null) animator.Play("Fase2");

        // Atualiza indicador de fase
        if (faseText != null)
        {
            faseText.text  = Loc.T("boss.phase2");
            faseText.color = new Color(1f, 0.4f, 0.1f);
        }

        // Flash de transição
        yield return StartCoroutine(FlashFase2());

        // Aviso de fase
        StartCoroutine(MostrarTextoTela("MODO FURIA ATIVADO!", new Color(1f, 0.3f, 0f), 2f));
    }

    IEnumerator FlashFase2()
    {
        Color corOriginal = spriteRenderer != null ? spriteRenderer.color : Color.white;
        for (int i = 0; i < 8; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.07f);
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.3f, 0f);
            yield return new WaitForSeconds(0.07f);
        }
        if (spriteRenderer != null) spriteRenderer.color = corOriginal;
    }

    // ──────────────────────────────────────────────────────────────
    // TELEPORTE
    // ──────────────────────────────────────────────────────────────

    IEnumerator LoopTeleporte()
    {
        // Aguarda o boss aparecer antes de começar a se teleportar
        yield return new WaitForSeconds(intervalTeleporteFase1);

        while (!controller.estaMorrendo)
        {
            yield return StartCoroutine(Teleportar());
            float proximo = fase2Ativada ? intervalTeleporteFase2 : intervalTeleporteFase1;
            yield return new WaitForSeconds(proximo);
        }
    }

    IEnumerator Teleportar()
    {
        if (player == null || spriteRenderer == null) yield break;

        // Efeito de carregamento visual enquanto pisca
        StartCoroutine(EfeitoCarregamento());
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = new Color(0.5f, 0f, 1f);
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
        }

        yield return StartCoroutine(FadeAlpha(1f, 0f, duracaoFade));
        transform.position = ObterPosicaoTeleporte();
        yield return null;
        yield return StartCoroutine(FadeAlpha(0f, 1f, duracaoFade));

        // Dispara o projétil especial logo após aparecer
        DispararEspecial();
    }

    IEnumerator EfeitoCarregamento()
    {
        // Textura de anel roxo para o efeito de carga
        int sz = 32;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
            float nt = d / c;
            float a  = Mathf.Clamp01(1f - Mathf.Abs(nt - 0.75f) / 0.25f);
            tex.SetPixel(x, y, new Color(0.6f, 0f, 1f, a));
        }
        tex.Apply();
        Sprite spr = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz * 0.5f);

        int sortL = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        int sortO = spriteRenderer != null ? spriteRenderer.sortingOrder    : 0;

        // 3 anéis em paralelo, um por piscada
        for (int i = 0; i < 3; i++)
        {
            StartCoroutine(LancarAnelCarga(spr, sortL, sortO));
            yield return new WaitForSeconds(0.16f);
        }
    }

    IEnumerator LancarAnelCarga(Sprite spr, int sortL, int sortO)
    {
        GameObject ring = new GameObject("AnelCarga");
        ring.transform.position = transform.position;
        SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingLayerID = sortL;
        sr.sortingOrder   = sortO + 1;

        float t = 0f, dur = 0.4f;
        while (t < dur)
        {
            if (ring == null) yield break;
            t += Time.deltaTime;
            float p = t / dur;
            ring.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 2.5f, p);
            sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.8f, 0f, p));
            yield return null;
        }
        Destroy(ring);
    }

    void DispararEspecial()
    {
        GameObject prefab = prefabProjetilEspecial != null ? prefabProjetilEspecial : prefabProjetil;
        if (prefab == null || player == null) return;

        int qtd = fase2Ativada ? 3 : 1;
        Vector3 spawnPos = PosicaoOlho();
        Vector2 dirBase  = ((Vector2)player.position - (Vector2)spawnPos).normalized;
        float anguloBase = Mathf.Atan2(dirBase.y, dirBase.x) * Mathf.Rad2Deg;
        float spread     = 25f;

        for (int i = 0; i < qtd; i++)
        {
            float offset = qtd > 1 ? Mathf.Lerp(-spread, spread, (float)i / (qtd - 1)) : 0f;
            float ang    = (anguloBase + offset) * Mathf.Deg2Rad;
            Vector2 dir  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            GameObject proj = Instantiate(prefab, spawnPos, Quaternion.identity);

            ProjetilEspecialBoss peb = proj.GetComponent<ProjetilEspecialBoss>();
            if (peb != null)
            {
                peb.dano = danoProjetilEspecial;
                peb.SetDirecao(dir);
                continue;
            }

            // Fallback: usa ProjetilInimigoDano se não tiver o script especial
            ProjetilInimigoDano pid = proj.GetComponent<ProjetilInimigoDano>();
            if (pid != null) { pid.dano = danoProjetilEspecial; pid.SetDirecao(dir); }
        }
    }

    Vector2 ObterPosicaoTeleporte()
    {
        if (player == null) return transform.position;

        // Raio do boss em world units (escala * raio do collider)
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

        // Fallback: mantém posição atual (não teleporta para dentro de parede)
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

    IEnumerator LoopAtaque()
    {
        yield return new WaitForSeconds(2.5f); // delay inicial

        while (!controller.estaMorrendo)
        {
            float intervalo = fase2Ativada ? intervalAtaqueFase2 : intervalAtaqueFase1;
            yield return new WaitForSeconds(intervalo);

            if (!controller.estaMorrendo && player != null)
                Disparar();
        }
    }

    Vector3 PosicaoOlho()
    {
        float sinalX = spriteRenderer != null ? Mathf.Sign(transform.localScale.x) : 1f;
        return transform.position + new Vector3(offsetOlho.x * sinalX, offsetOlho.y, 0f);
    }

    void Disparar()
    {
        if (prefabProjetil == null || player == null) return;

        Vector3 spawnPos  = PosicaoOlho();
        Vector2 dirBase   = ((Vector2)player.position - (Vector2)spawnPos).normalized;
        float anguloBase  = Mathf.Atan2(dirBase.y, dirBase.x) * Mathf.Rad2Deg;

        for (int i = 0; i < projeteis; i++)
        {
            float t      = projeteis > 1 ? (float)i / (projeteis - 1) : 0.5f;
            float offset = Mathf.Lerp(-anguloLeque / 2f, anguloLeque / 2f, t);
            float ang    = (anguloBase + offset) * Mathf.Deg2Rad;
            Vector2 dir  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            GameObject proj = Instantiate(prefabProjetil, spawnPos, Quaternion.identity);

            // Tenta ProjetilInimigoDano primeiro, depois projetil_inimigo como fallback
            ProjetilInimigoDano pid = proj.GetComponent<ProjetilInimigoDano>();
            if (pid != null)
            {
                pid.dano       = danoProjetil;
                pid.velocidade = velocidadeProjetil;
                pid.SetDirecao(dir);
            }
            else
            {
                projetil_inimigo pi = proj.GetComponent<projetil_inimigo>();
                if (pi != null)
                {
                    pi.velocidade = velocidadeProjetil;
                    pi.SetDirecao(dir);
                }
                else
                {
                    Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.linearVelocity = dir * velocidadeProjetil;
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // ATAQUE DE RAIO (FEITIÇO)
    // ──────────────────────────────────────────────────────────────

    IEnumerator LoopRaio()
    {
        // Começa decalado do ataque normal de projéteis
        yield return new WaitForSeconds(intervalRaioFase1 * 0.5f);

        while (!controller.estaMorrendo)
        {
            float intervalo = fase2Ativada ? intervalRaioFase2 : intervalRaioFase1;
            yield return new WaitForSeconds(intervalo);

            if (!controller.estaMorrendo && player != null)
                yield return StartCoroutine(AtaqueRaio());
        }
    }

    IEnumerator AtaqueRaio()
    {
        if (player == null) yield break;

        Color corNucleo = new Color(1f, 0.95f, 1f);
        Color corGlow   = new Color(0.7f, 0.25f, 1f);

        // ── Telegraph: feixe fraco acompanhando o jogador + retícula no alvo + orbe carregando ──
        GameObject telegraph = CriarFeixeVisual(corGlow, corNucleo);
        GameObject orbeCarga = CriarOrbeRaio(corGlow);
        GameObject reticula  = CriarReticulaRaio(corNucleo);

        Vector2 dir = Vector2.right;
        float t = 0f;
        while (t < duracaoTelegraphRaio)
        {
            if (telegraph == null || orbeCarga == null || reticula == null) yield break;
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracaoTelegraphRaio);

            Vector3 origem = PosicaoOlho();
            if (player != null)
                dir = ((Vector2)player.position - (Vector2)origem).normalized;

            PosicionarFeixe(telegraph, origem, dir, alcanceRaio, larguraRaio * 0.35f, 0f);

            float pulso = 0.12f + Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.18f;
            DefinirAlphaCamadas(telegraph, pulso, 0f);

            float escalaOrbe = Mathf.Lerp(0.15f, 0.6f, p) * (1f + Mathf.Sin(Time.time * 18f) * 0.08f);
            orbeCarga.transform.position   = origem;
            orbeCarga.transform.localScale = Vector3.one * escalaOrbe;

            if (player != null)
            {
                reticula.transform.position   = player.position;
                reticula.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 0.7f, p) * (1f + Mathf.Sin(Time.time * 14f) * 0.1f);
                SpriteRenderer srR = reticula.GetComponent<SpriteRenderer>();
                if (srR != null) { Color c = srR.color; c.a = Mathf.Lerp(0.15f, 0.9f, p); srR.color = c; }
            }

            yield return null;
        }
        Destroy(telegraph);
        Destroy(reticula);

        // Flash de disparo no orbe de carga
        if (orbeCarga != null)
        {
            orbeCarga.transform.localScale = Vector3.one * 0.95f;
            SpriteRenderer srO = orbeCarga.GetComponent<SpriteRenderer>();
            if (srO != null) { Color c = srO.color; c.a = 1f; srO.color = c; }
            StartCoroutine(FadeECrescerDestruir(orbeCarga, 0.2f));
        }

        CameraShaker.Tremer(0.05f, duracaoRaio);

        // ── Trava a mira final ──────────────────────────────────────────
        Vector3 origemFinal = PosicaoOlho();
        Vector2 direcaoFinal = player != null ? ((Vector2)player.position - (Vector2)origemFinal).normalized : dir;
        float anguloBase = Mathf.Atan2(direcaoFinal.y, direcaoFinal.x) * Mathf.Rad2Deg;
        float varredura  = fase2Ativada ? anguloVarreduraRaio : 0f;
        float anguloIni  = anguloBase - varredura * 0.5f;

        // ── Feixe real (núcleo brilhante + glow externo) ────────────────────
        GameObject feixe = CriarFeixeVisual(corGlow, corNucleo);
        CriarLuz(feixe.transform, corGlow, 1.8f, 0.05f, larguraRaio * 2.5f);

        // ── Disco de impacto pulsante na ponta do feixe ─────────────────────
        GameObject impacto = CriarOrbeRaio(corNucleo);

        float proxTick    = 0f;
        float proxFaisca  = 0f;
        t = 0f;
        while (t < duracaoRaio)
        {
            if (feixe == null) yield break;
            t += Time.deltaTime;
            float p = duracaoRaio > 0f ? Mathf.Clamp01(t / duracaoRaio) : 1f;

            float ang = varredura != 0f ? Mathf.Lerp(anguloIni, anguloIni + varredura, p) : anguloBase;
            float rad = ang * Mathf.Deg2Rad;
            Vector2 dirAtual = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector3 origemAtual = PosicaoOlho();
            Vector3 pontaFeixe  = origemAtual + (Vector3)(dirAtual * alcanceRaio);

            // Vibração de energia: o núcleo pulsa de intensidade/largura
            float vibra = 0.8f + Mathf.Sin(Time.time * 25f) * 0.2f;
            PosicionarFeixe(feixe, origemAtual, dirAtual, alcanceRaio, larguraRaio, vibra);
            DefinirAlphaCamadas(feixe, 0.55f, vibra);

            if (impacto != null)
            {
                impacto.transform.position   = pontaFeixe;
                float pulsoImpacto = 0.7f + Mathf.Abs(Mathf.Sin(Time.time * 16f)) * 0.3f;
                impacto.transform.localScale = Vector3.one * pulsoImpacto * (larguraRaio * 1.6f);
            }

            // Faíscas viajando pelo feixe
            proxFaisca -= Time.deltaTime;
            if (proxFaisca <= 0f)
            {
                proxFaisca = 0.05f;
                StartCoroutine(FaiscaRaio(origemAtual, dirAtual, alcanceRaio, corNucleo));
            }

            // Dano
            proxTick -= Time.deltaTime;
            if (proxTick <= 0f)
            {
                proxTick = intervaloDanoRaio;
                if (player != null && playerStats != null)
                {
                    float dist = DistanciaPontoSegmentoRaio(player.position, origemAtual, pontaFeixe);
                    if (dist <= larguraRaio * 0.5f)
                        playerStats.TakeDamage(danoRaio);
                }
            }

            yield return null;
        }

        Destroy(feixe);
        if (impacto != null) StartCoroutine(FadeECrescerDestruir(impacto, 0.2f));
    }

    // ── Feixe composto: camada "Glow" larga e suave + camada "Nucleo" fina e brilhante ──
    GameObject CriarFeixeVisual(Color corGlow, Color corNucleo)
    {
        GameObject root = new GameObject("RaioMaga");

        GameObject glow = CriarCamadaFeixe(ObterTexturaFeixe(0.6f), corGlow);
        glow.name = "Glow";
        glow.transform.SetParent(root.transform, false);

        GameObject nucleo = CriarCamadaFeixe(ObterTexturaFeixe(2.5f), corNucleo);
        nucleo.name = "Nucleo";
        nucleo.transform.SetParent(root.transform, false);
        nucleo.transform.localScale = new Vector3(1f, 0.3f, 1f);

        if (spriteRenderer != null)
        {
            SpriteRenderer srGlow   = glow.GetComponent<SpriteRenderer>();
            SpriteRenderer srNucleo = nucleo.GetComponent<SpriteRenderer>();
            srGlow.sortingLayerID   = srNucleo.sortingLayerID = spriteRenderer.sortingLayerID;
            srGlow.sortingOrder     = spriteRenderer.sortingOrder + 1;
            srNucleo.sortingOrder   = spriteRenderer.sortingOrder + 2;
        }

        DefinirAlphaCamadas(root, 0f, 0f);
        Destroy(root, 8f); // segurança
        return root;
    }

    static GameObject CriarCamadaFeixe(Sprite spr, Color cor)
    {
        GameObject go = new GameObject("Camada");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.color  = cor;
        return go;
    }

    // root.localScale.x = alcance, root.localScale.y = largura do glow.
    // intensidadeNucleo ajusta a largura relativa do núcleo (vibração de energia).
    void PosicionarFeixe(GameObject root, Vector3 origem, Vector2 dir, float alcance, float largura, float intensidadeNucleo)
    {
        root.transform.position = origem;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        root.transform.rotation   = Quaternion.Euler(0f, 0f, ang);
        root.transform.localScale = new Vector3(alcance, largura, 1f);

        Transform nucleo = root.transform.Find("Nucleo");
        if (nucleo != null)
            nucleo.localScale = new Vector3(1f, Mathf.Clamp(0.3f * Mathf.Max(intensidadeNucleo, 0.3f), 0.1f, 1f), 1f);
    }

    static void DefinirAlphaCamadas(GameObject root, float alphaGlow, float alphaNucleo)
    {
        SpriteRenderer glow   = root.transform.Find("Glow")?.GetComponent<SpriteRenderer>();
        SpriteRenderer nucleo = root.transform.Find("Nucleo")?.GetComponent<SpriteRenderer>();
        if (glow   != null) { Color c = glow.color;   c.a = alphaGlow;   glow.color = c; }
        if (nucleo != null) { Color c = nucleo.color; c.a = alphaNucleo; nucleo.color = c; }
    }

    // ── Orbe de luz (carga, impacto) ────────────────────────────────────
    GameObject CriarOrbeRaio(Color cor)
    {
        GameObject go = new GameObject("OrbeRaio");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ObterTexturaDisco();
        sr.color  = cor;
        if (spriteRenderer != null)
        {
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder   = spriteRenderer.sortingOrder + 3;
        }
        CriarLuz(go.transform, cor, 0.6f, 0.05f, 0.6f);
        Destroy(go, 5f); // segurança
        return go;
    }

    // ── Retícula de alvo (anel) ─────────────────────────────────────────
    GameObject CriarReticulaRaio(Color cor)
    {
        GameObject go = new GameObject("ReticulaRaio");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ObterTexturaAnel();
        sr.color  = new Color(cor.r, cor.g, cor.b, 0f);
        if (spriteRenderer != null)
        {
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder   = spriteRenderer.sortingOrder + 3;
        }
        Destroy(go, 5f); // segurança
        return go;
    }

    // ── Faísca que percorre o feixe enquanto ele está ativo ─────────────
    IEnumerator FaiscaRaio(Vector3 origem, Vector2 dir, float alcance, Color cor)
    {
        GameObject go = new GameObject("FaiscaRaio");
        go.transform.position = origem;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ObterTexturaDisco();
        sr.color  = cor;
        if (spriteRenderer != null)
        {
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder   = spriteRenderer.sortingOrder + 2;
        }
        go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
        Destroy(go, 1f); // segurança

        float dur = 0.3f;
        float t = 0f;
        Vector3 destino = origem + (Vector3)(dir * alcance);
        while (t < dur)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            float p = t / dur;
            go.transform.position = Vector3.Lerp(origem, destino, p);
            Color c = sr.color; c.a = Mathf.Lerp(0.9f, 0f, p); sr.color = c;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ── Fade + leve "explosão" de escala antes de destruir ───────────────
    IEnumerator FadeECrescerDestruir(GameObject go, float duracao)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        Color corBase = sr != null ? sr.color : Color.white;
        float t = 0f;
        while (t < duracao)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            float p = t / duracao;
            if (sr != null) { Color c = corBase; c.a = Mathf.Lerp(corBase.a, 0f, p); sr.color = c; }
            go.transform.localScale *= 1f + Time.deltaTime * 3f;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    static float DistanciaPontoSegmentoRaio(Vector2 ponto, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float comprimentoQuadrado = ab.sqrMagnitude;
        if (comprimentoQuadrado < 0.0001f) return Vector2.Distance(ponto, a);
        float tProj = Mathf.Clamp01(Vector2.Dot(ponto - a, ab) / comprimentoQuadrado);
        Vector2 projecao = a + ab * tProj;
        return Vector2.Distance(ponto, projecao);
    }

    // ── Texturas em cache (geradas uma única vez) ────────────────────────
    static Sprite s_texFeixeGlow, s_texFeixeNucleo, s_texDisco, s_texAnel;

    static Sprite ObterTexturaFeixe(float expoente)
    {
        if (expoente >= 1f)
        {
            if (s_texFeixeGlow == null) s_texFeixeGlow = GerarTexturaFeixe(expoente);
            return s_texFeixeGlow;
        }
        if (s_texFeixeNucleo == null) s_texFeixeNucleo = GerarTexturaFeixe(expoente);
        return s_texFeixeNucleo;
    }

    static Sprite ObterTexturaDisco()
    {
        if (s_texDisco == null) s_texDisco = GerarTexturaDisco();
        return s_texDisco;
    }

    static Sprite ObterTexturaAnel()
    {
        if (s_texAnel == null) s_texAnel = GerarTexturaAnel();
        return s_texAnel;
    }

    // Faixa horizontal com brilho suave nas bordas verticais; pivot na esquerda-centro.
    // expoente baixo -> faixa fina e concentrada (núcleo); alto -> faixa larga e difusa (glow).
    static Sprite GerarTexturaFeixe(float expoente)
    {
        const int sz = 64;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float dy = Mathf.Abs((y + 0.5f) / sz - 0.5f) * 2f; // 0 no centro, 1 na borda
            float a  = Mathf.Pow(Mathf.Clamp01(1f - dy), expoente);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0f, 0.5f), sz);
    }

    static Sprite GerarTexturaDisco()
    {
        const int sz = 32;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
            float a = Mathf.Clamp01(1f - d);
            a *= a;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    static Sprite GerarTexturaAnel()
    {
        const int sz = 32;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = sz * 0.5f;
        const float espessura = 0.18f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
            float borda = 1f - Mathf.Abs(d - (1f - espessura)) / espessura;
            float a = Mathf.Clamp01(borda);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    static void CriarLuz(Transform parent, Color cor, float intensidade, float raioInterno, float raioExterno)
    {
        GameObject go = new GameObject("brilho");
        go.transform.SetParent(parent, false);
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = cor;
        light.intensity = intensidade;
        light.pointLightInnerRadius = raioInterno;
        light.pointLightOuterRadius = raioExterno;
        light.blendStyleIndex = 0;
    }

    // ──────────────────────────────────────────────────────────────
    // ENTRADA
    // ──────────────────────────────────────────────────────────────

    IEnumerator SequenciaEntrada()
    {
        // Boss começa invisível
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color; c.a = 0f;
            spriteRenderer.color = c;
        }

        yield return StartCoroutine(MostrarAvisoBoss());

        // Treme a câmera e dispara explosão sombria juntos
        CameraShaker.Tremer(0.04f, 2.5f);
        StartCoroutine(ExplosaoSombria(2.5f));

        // Aparece com fade simples
        yield return StartCoroutine(FadeAlpha(0f, 1f, 0.6f));

        // Buffa todas as slime_maga presentes na cena
        BuffarSlimeMagas();
    }

    void BuffarSlimeMagas()
    {
        var slimes = FindObjectsByType<movi_inimigo_manter_distancia>(FindObjectsSortMode.None);
        foreach (var slime in slimes)
        {
            intervaisOriginais[slime] = slime.intervaloAtaque;
            slime.intervaloAtaque = Mathf.Max(0.5f, slime.intervaloAtaque * 0.45f);
            StartCoroutine(AnimacaoExcitacaoSlime(slime));
        }
    }

    void RestaurarSlimeMagas()
    {
        foreach (var par in intervaisOriginais)
        {
            if (par.Key != null)
                par.Key.intervaloAtaque = par.Value;
        }
        intervaisOriginais.Clear();
    }

    IEnumerator AnimacaoExcitacaoSlime(movi_inimigo_manter_distancia slime)
    {
        if (slime == null) yield break;
        SpriteRenderer sr = slime.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color corBase  = sr.color;
        Vector3 escBase = slime.transform.localScale;
        float dur = 2f, t = 0f;

        while (t < dur && slime != null)
        {
            t += Time.deltaTime;
            float pct  = t / dur;
            float ping = Mathf.PingPong(t * 10f, 1f);

            // Pulso roxo — cor que combina com o boss
            sr.color = Color.Lerp(corBase, new Color(0.85f, 0.4f, 1f, 1f), ping * (1f - pct) * 0.75f);

            // Vibração squish/stretch na escala
            float v = Mathf.Sin(t * 45f) * 0.09f * (1f - pct);
            slime.transform.localScale = escBase + new Vector3(v, -v * 0.6f, 0f);

            yield return null;
        }

        if (sr    != null) sr.color = corBase;
        if (slime != null) slime.transform.localScale = escBase;
    }

    IEnumerator ExplosaoSombria(float duracao)
    {
        int sortLayer = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        int sortOrder = spriteRenderer != null ? spriteRenderer.sortingOrder   : 0;
        Vector3 origem = transform.position;

        // ── Textura de anel ──────────────────────────────────────────
        int sz = 64;
        Texture2D texAnel = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        texAnel.filterMode = FilterMode.Bilinear;
        float cr = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cr, cr));
            float nt = d / cr;
            float a  = Mathf.Clamp01(1f - Mathf.Abs(nt - 0.72f) / 0.28f);
            texAnel.SetPixel(x, y, new Color(0.18f, 0.04f, 0.28f, a));
        }
        texAnel.Apply();
        Sprite sprAnel = Sprite.Create(texAnel, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz * 0.5f);

        // ── Anel de shockwave ─────────────────────────────────────────
        GameObject anelGO = new GameObject("ShockwaveSombrio");
        anelGO.transform.position = origem + Vector3.down * 0.3f;
        SpriteRenderer srAnel = anelGO.AddComponent<SpriteRenderer>();
        srAnel.sprite = sprAnel;
        srAnel.sortingLayerID = sortLayer;
        srAnel.sortingOrder   = sortOrder - 1;

        // ── Textura de partícula 4×4 ──────────────────────────────────
        Texture2D texP = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texP.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
        {
            float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f), new Vector2(2f,2f));
            texP.SetPixel(x, y, new Color(0f,0f,0f, Mathf.Clamp01(1f - d/2f)));
        }
        texP.Apply();
        Sprite sprP = Sprite.Create(texP, new Rect(0,0,4,4), new Vector2(0.5f,0.5f), 16f);

        // ── Partículas sombrias ───────────────────────────────────────
        int qtd = 18;
        var pGOs  = new GameObject[qtd];
        var pVels = new Vector2[qtd];

        for (int i = 0; i < qtd; i++)
        {
            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float spd = Random.Range(0.4f, 1.6f);
            pVels[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;

            GameObject p = new GameObject("PartSombria");
            p.transform.position   = origem + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f), 0f);
            float scl = Random.Range(0.08f, 0.22f);
            p.transform.localScale = new Vector3(scl, scl, 1f);

            SpriteRenderer srP = p.AddComponent<SpriteRenderer>();
            srP.sprite = sprP;
            // Cores: roxo escuro, preto, violeta apagado
            srP.color  = Color.Lerp(new Color(0.25f, 0.02f, 0.38f), new Color(0.05f, 0.0f, 0.08f), Random.value);
            srP.sortingLayerID = sortLayer;
            srP.sortingOrder   = sortOrder + 1;
            pGOs[i] = p;
        }

        // ── Animação ──────────────────────────────────────────────────
        float t = 0f;
        while (t < duracao)
        {
            t += Time.deltaTime;
            float p01 = t / duracao;

            // Anel expande devagar e some
            float sclAnel = Mathf.Lerp(0.05f, 3.2f, Mathf.Pow(p01, 0.5f));
            anelGO.transform.localScale = new Vector3(sclAnel, sclAnel * 0.28f, 1f);
            srAnel.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, 0f, p01));

            // Partículas derivam para cima levemente e somem
            for (int i = 0; i < qtd; i++)
            {
                if (pGOs[i] == null) continue;
                pGOs[i].transform.position += new Vector3(pVels[i].x, pVels[i].y + 0.3f, 0f) * Time.deltaTime;
                pVels[i] *= 0.97f; // desacelera gradualmente
                SpriteRenderer srP = pGOs[i].GetComponent<SpriteRenderer>();
                if (srP != null) { Color c = srP.color; c.a = Mathf.Lerp(0.9f, 0f, p01); srP.color = c; }
            }

            yield return null;
        }

        Destroy(anelGO);
        for (int i = 0; i < qtd; i++)
            if (pGOs[i] != null) Destroy(pGOs[i]);
    }

    IEnumerator MostrarAvisoBoss()
    {
        GameObject warnGO = new GameObject("BossWarning");
        Canvas cv = warnGO.AddComponent<Canvas>();
        cv.renderMode    = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder  = 200;
        warnGO.AddComponent<CanvasScaler>();

        // Fundo escuro
        GameObject bgGO = CriarUIGO("BG", warnGO.transform);
        ExpandirRect(bgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0f);

        // Texto principal
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
        txt.color     = new Color(1f, 0.15f, 0.15f, 0f);

        // Fade in
        yield return StartCoroutine(FadeCanvasElements(bgImg, txt, 0f, 1f, 0.4f, maxAlphaBG: 0.65f));

        // Pulsa 3 vezes
        for (int i = 0; i < 3; i++)
        {
            txt.color = new Color(1f, 0.9f, 0.1f);
            yield return new WaitForSeconds(0.15f);
            txt.color = new Color(1f, 0.15f, 0.15f);
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.5f);

        // Fade out
        yield return StartCoroutine(FadeCanvasElements(bgImg, txt, 1f, 0f, 0.4f, maxAlphaBG: 0.65f));

        Destroy(warnGO);
    }

    IEnumerator FadeCanvasElements(Image bg, TextMeshProUGUI txt, float de, float para, float dur, float maxAlphaBG = 0.65f)
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

    void CriarBossUI()
    {
        bossCanvasGO = new GameObject("BossCanvas");
        Canvas cv = bossCanvasGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 50;

        CanvasScaler cs = bossCanvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution    = new Vector2(1920, 1080);
        cs.matchWidthOrHeight     = 0.5f;
        bossCanvasGO.AddComponent<GraphicRaycaster>();

        // Painel de fundo — parte superior da tela (centralizado)
        GameObject painel = CriarUIGO("BossPanel", bossCanvasGO.transform);
        RectTransform pr = painel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.2f, 0.825f);
        pr.anchorMax = new Vector2(0.8f, 0.935f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        Image painelImg = painel.AddComponent<Image>();
        painelImg.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);

        // Linha decorativa no topo do painel
        GameObject linha = CriarUIGO("Linha", painel.transform);
        RectTransform lr = linha.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 0.92f);
        lr.anchorMax = new Vector2(1f, 1f);
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        Image linhaImg = linha.AddComponent<Image>();
        linhaImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        // Nome do boss
        GameObject nomeGO = CriarUIGO("NomeBoss", painel.transform);
        RectTransform nr = nomeGO.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0.01f, 0.6f);
        nr.anchorMax = new Vector2(0.99f, 0.92f);
        nr.offsetMin = nr.offsetMax = Vector2.zero;
        TextMeshProUGUI nomeTxt = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTxt.text      = nomeBoss.ToUpper();
        nomeTxt.fontSize  = 20;
        nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.color     = new Color(1f, 0.85f, 0.2f);
        nomeTxt.alignment = TextAlignmentOptions.Center;

        // Indicador de fase
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

        // Fundo da barra HP
        GameObject barBG = CriarUIGO("HPBarBG", painel.transform);
        RectTransform bbr = barBG.GetComponent<RectTransform>();
        bbr.anchorMin = new Vector2(0.01f, 0.08f);
        bbr.anchorMax = new Vector2(0.99f, 0.52f);
        bbr.offsetMin = bbr.offsetMax = Vector2.zero;
        Image bgImg = barBG.AddComponent<Image>();
        if (sprHpBg != null) { bgImg.sprite = sprHpBg; bgImg.type = Image.Type.Tiled; bgImg.color = Color.white; }
        else bgImg.color = new Color(0.1f, 0.1f, 0.12f);

        // Barra fantasma (amarela, desce devagar)
        GameObject ghostGO = CriarUIGO("HPGhost", barBG.transform);
        ExpandirRect(ghostGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpFillGhost            = ghostGO.AddComponent<Image>();
        if (sprHpFill != null) hpFillGhost.sprite = sprHpFill;
        hpFillGhost.type       = Image.Type.Filled;
        hpFillGhost.fillMethod = Image.FillMethod.Horizontal;
        hpFillGhost.fillAmount = 1f;
        hpFillGhost.color      = new Color(1f, 0.88f, 0.25f, 0.9f);

        // Barra HP principal (pixel art, animada)
        GameObject fillGO = CriarUIGO("HPFill", barBG.transform);
        ExpandirRect(fillGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpFill            = fillGO.AddComponent<Image>();
        if (sprHpFill != null) hpFill.sprite = sprHpFill;
        hpFill.type       = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillAmount = 1f;
        hpFill.color      = sprHpFill != null ? Color.white : new Color(0.1f, 0.85f, 0.2f);

        // Texto HP (ex: 450 / 500)
        GameObject hpTextGO = CriarUIGO("HPText", barBG.transform);
        ExpandirRect(hpTextGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpText           = hpTextGO.AddComponent<TextMeshProUGUI>();
        hpText.fontSize  = 11;
        hpText.color     = Color.white;
        hpText.fontStyle = FontStyles.Bold;
        hpText.alignment = TextAlignmentOptions.Center;

        // Moldura pixel art por cima (centro transparente, borda opaca)
        if (sprHpFrame != null)
        {
            GameObject frameGO = CriarUIGO("HPFrame", barBG.transform);
            ExpandirRect(frameGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Image frameImg = frameGO.AddComponent<Image>();
            frameImg.sprite       = sprHpFrame;
            frameImg.type         = Image.Type.Sliced;
            frameImg.raycastTarget = false;
            frameImg.color        = Color.white;
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

    void AtualizarUI()
    {
        if (hpFill == null || controller == null) return;

        float pct = controller.GetPorcentagemVida();

        // Barra principal consome visivelmente de forma suave
        hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, pct, Time.deltaTime * 2.5f);

        // Fase 1: branco (pixel art verde mostra natural); Fase 2: tint vermelho sobre pixel art
        hpFill.color = fase2Ativada
            ? new Color(1f, 0.18f, 0.12f)
            : (sprHpFill != null ? Color.white : new Color(0.1f, 0.85f, 0.2f));

        // Barra fantasma atrasa para dar efeito de "queima" da vida
        hpFillGhost.fillAmount = Mathf.MoveTowards(hpFillGhost.fillAmount, pct, Time.deltaTime * 0.35f);

        // Texto numérico de HP
        if (hpText != null)
            hpText.text = $"{Mathf.RoundToInt(controller.vidaAtual)} / {Mathf.RoundToInt(controller.vidaMaxima)}";
    }

    // ──────────────────────────────────────────────────────────────
    // TEXTO FLUTUANTE NA TELA
    // ──────────────────────────────────────────────────────────────

    IEnumerator MostrarTextoTela(string mensagem, Color cor, float duracao)
    {
        if (bossMsgGO != null) Destroy(bossMsgGO);
        GameObject go = new GameObject("BossMsg");
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

        // Fade in
        float t = 0f;
        while (t < 0.3f) { t += Time.deltaTime; txt.color = new Color(cor.r, cor.g, cor.b, t / 0.3f); yield return null; }
        txt.color = new Color(cor.r, cor.g, cor.b, 1f);

        yield return new WaitForSeconds(duracao);

        // Fade out + sobe
        t = 0f;
        RectTransform msgRect = txtGO.GetComponent<RectTransform>();
        Vector2 posBase = msgRect.anchoredPosition;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float a = 1f - (t / 0.5f);
            txt.color = new Color(cor.r, cor.g, cor.b, a);
            msgRect.anchoredPosition = posBase + Vector2.up * (t * 120f);
            yield return null;
        }

        Destroy(go);
        bossMsgGO = null;
    }

    // ──────────────────────────────────────────────────────────────
    // EFEITO DE MORTE
    // ──────────────────────────────────────────────────────────────

    public void IniciarEfeitoMorte() => StartCoroutine(EfeitoMorte());

    IEnumerator EfeitoMorte()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        CameraShaker.Tremer(0.12f, 3.5f);

        // Flash branco / roxo rápido
        for (int i = 0; i < 14; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = i % 2 == 0 ? Color.white : new Color(0.8f, 0.3f, 1f);
            yield return new WaitForSeconds(0.04f);
        }

        int sortL = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        int sortO = spriteRenderer != null ? spriteRenderer.sortingOrder   : 0;

        CriarAneisMorte(6, sortL, sortO);
        CriarParticulasMorte(30, sortL, sortO);

        // Boss cresce e desaparece
        Vector3 escBase = transform.localScale;
        float t = 0f, dur = 1.1f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            transform.localScale = escBase * Mathf.Lerp(1f, 3f, Mathf.Pow(p, 0.5f));
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(1f, 0.8f, 1f, Mathf.Lerp(1f, 0f, p * p));
            yield return null;
        }

        BossMorteUI.Exibir("BOSS DERROTADO!", new Color(1f, 0.9f, 0.2f));
        Destroy(gameObject);
    }

    void CriarAneisMorte(int qtd, int sortL, int sortO)
    {
        int sz = 32;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
            float nt = d / c;
            float a  = Mathf.Clamp01(1f - Mathf.Abs(nt - 0.75f) / 0.25f);
            tex.SetPixel(x, y, new Color(0.7f, 0.3f, 1f, a));
        }
        tex.Apply();
        Sprite spr = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz * 0.5f);

        for (int i = 0; i < qtd; i++)
        {
            GameObject ring = new GameObject("AnelMorteBoss");
            ring.transform.position = transform.position;
            SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
            sr.sprite         = spr;
            sr.sortingLayerID = sortL;
            sr.sortingOrder   = sortO - 1;

            AnelExpansaoAuto anim = ring.AddComponent<AnelExpansaoAuto>();
            anim.delay        = i * 0.13f;
            anim.duracaoTotal = Random.Range(0.6f, 1.0f);
            anim.escalaFinal  = Random.Range(5f, 13f);
        }
    }

    void CriarParticulasMorte(int qtd, int sortL, int sortO)
    {
        int sz = 6;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        float c2 = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c2, c2));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d / c2)));
        }
        tex.Apply();
        Sprite spr = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);

        Color[] cores = {
            new Color(0.8f, 0.4f, 1f), new Color(0.5f, 0.1f, 0.9f),
            new Color(1f, 0.85f, 0.2f), Color.white, new Color(0.3f, 0f, 0.7f)
        };

        for (int i = 0; i < qtd; i++)
        {
            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float spd = Random.Range(1.5f, 5.5f);

            GameObject p = new GameObject("PartMorteBoss");
            p.transform.position   = transform.position + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-0.4f, 0.4f), 0f);
            p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.4f);

            SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
            sr.sprite         = spr;
            sr.color          = cores[Random.Range(0, cores.Length)];
            sr.sortingLayerID = sortL;
            sr.sortingOrder   = sortO + 1;

            ParticlaMorteBoss anim = p.AddComponent<ParticlaMorteBoss>();
            anim.vel  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            anim.vida = Random.Range(0.5f, 1.4f);
        }
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

// ── Anel que se expande sozinho (independente do boss) ────────────
public class AnelExpansaoAuto : MonoBehaviour
{
    public float delay;
    public float duracaoTotal;
    public float escalaFinal;

    SpriteRenderer sr;
    float t;
    bool ativo;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        if (!ativo)
        {
            delay -= Time.deltaTime;
            if (delay <= 0f) ativo = true;
            return;
        }

        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / duracaoTotal);
        transform.localScale = Vector3.one * Mathf.Lerp(0.5f, escalaFinal, Mathf.Pow(p, 0.6f));
        if (sr) sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 0f, p));

        if (t >= duracaoTotal) Destroy(gameObject);
    }
}

// ── Partícula com física simples (independente do boss) ───────────
public class ParticlaMorteBoss : MonoBehaviour
{
    public Vector2 vel;
    public float vida;

    SpriteRenderer sr;
    float t;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        t += Time.deltaTime;
        vel.y -= 4f * Time.deltaTime;
        vel   *= Mathf.Pow(0.92f, Time.deltaTime * 60f);
        transform.position += (Vector3)(vel * Time.deltaTime);

        if (sr) { Color c = sr.color; c.a = 1f - (t / vida); sr.color = c; }
        if (t >= vida) Destroy(gameObject);
    }
}

// ── Mensagem "BOSS DERROTADO" exibida após a destruição ───────────
public class BossMorteUI : MonoBehaviour
{
    public static void Exibir(string msg, Color cor)
    {
        GameObject runner = new GameObject("BossMorteRunner");
        runner.AddComponent<BossMorteUI>().StartCoroutine(Mostrar(runner, msg, cor));
    }

    static IEnumerator Mostrar(GameObject runner, string msg, Color cor)
    {
        GameObject cvGO = new GameObject("BossMorteCanvas");
        Canvas cv = cvGO.AddComponent<Canvas>();
        cv.renderMode  = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 200;
        cvGO.AddComponent<CanvasScaler>();

        GameObject txtGO = new GameObject("MorteTxt");
        txtGO.transform.SetParent(cvGO.transform, false);
        RectTransform rt = txtGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.35f);
        rt.anchorMax = new Vector2(0.9f, 0.65f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = msg;
        txt.fontSize  = 64;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = new Color(cor.r, cor.g, cor.b, 0f);

        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            txt.color = new Color(cor.r, cor.g, cor.b, t / 0.35f);
            yield return null;
        }
        txt.color = new Color(cor.r, cor.g, cor.b, 1f);

        yield return new WaitForSeconds(2.5f);

        t = 0f;
        RectTransform msgRt = txt.GetComponent<RectTransform>();
        Vector2 posBase = msgRt.anchoredPosition;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            txt.color = new Color(cor.r, cor.g, cor.b, 1f - t / 0.6f);
            msgRt.anchoredPosition = posBase + Vector2.up * (t * 100f);
            yield return null;
        }

        Destroy(cvGO);
        Destroy(runner);
    }
}
