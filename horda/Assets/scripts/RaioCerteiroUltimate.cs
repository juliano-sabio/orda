using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaioCerteiroUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public int   maxRicochetes     = 5;
    public float danoPorRaio       = 60f;
    public float multiplicadorDano = 0.8f;
    public float raioMaxBounce     = 8f;
    public float cooldown          = 25f;
    public float delayEntreBounces = 0.1f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    float       cooldownRestante;
    bool        ativo;
    PlayerStats playerStats;

    static readonly Color COR_NUCLEO  = new Color(0.85f, 0.97f, 1f,  1f);
    static readonly Color COR_GLOW    = new Color(0.35f, 0.70f, 1f,  0.55f);
    static readonly Color COR_BRILHO  = new Color(1f,    1f,    1f,  1f);
    static readonly Color COR_IMPACT  = new Color(0.55f, 0.85f, 1f,  1f);

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            if (playerStats.ultimateSkill != null)
                playerStats.ultimateSkill.isActive = true;
            playerStats.ultimateCooldown   = cooldown;
            playerStats.ultimateChargeTime = 0f;
            playerStats.ultimateReady      = false;
        }
    }

    void Update()
    {
        if (cooldownRestante > 0f) cooldownRestante -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.R) && cooldownRestante <= 0f && !ativo)
            StartCoroutine(CadeiaDeRaios());
        SincronizarUI();
    }

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    // ─── LÓGICA PRINCIPAL ─────────────────────────────────────────────────────

    IEnumerator CadeiaDeRaios()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        yield return StartCoroutine(EfeitoCarga());

        CameraShaker.Tremer(0.25f, 0.3f);
        StartCoroutine(FlashTela());

        var    atingidos = new HashSet<GameObject>();
        Vector2 origem   = transform.position;
        float   dano     = danoPorRaio;

        // RaioEterno: +3 ricochetes extras
        int totalRicochetes = maxRicochetes;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.RaioEterno))
            totalRicochetes += 3;

        for (int i = 0; i < totalRicochetes; i++)
        {
            GameObject alvo = EncontrarAlvoMaisProximo(origem, atingidos);
            if (alvo == null) break;

            atingidos.Add(alvo);
            AplicarDano(alvo, dano);

            Vector2 destino = alvo.transform.position;
            StartCoroutine(AnimarRaio(origem, destino, i));
            StartCoroutine(EfeitoImpacto(destino, i));

            // RaioOvercarga: dano em área de 2u em cada bounce
            if (SkillEvolutionManager.Tem(SkillEvolutionType.RaioOvercarga))
            {
                foreach (var c in Physics2D.OverlapCircleAll(destino, 2f))
                {
                    var root = ResolverInimigo(c.gameObject);
                    if (root != null && root != alvo)
                    {
                        var ic2 = root.GetComponent<InimigoController>() ?? root.GetComponentInChildren<InimigoController>();
                        ic2?.ReceberDano(dano * 0.4f, false);
                    }
                }
            }

            origem = destino;
            dano  *= multiplicadorDano;

            yield return new WaitForSeconds(delayEntreBounces);
        }

        ativo = false;
    }

    void AplicarDano(GameObject alvo, float dano)
    {
        var ic = alvo.GetComponent<InimigoController>() ?? alvo.GetComponentInChildren<InimigoController>();
        ic?.ReceberDano(dano, false);
    }

    GameObject EncontrarAlvoMaisProximo(Vector2 origem, HashSet<GameObject> excluidos)
    {
        var cols      = Physics2D.OverlapCircleAll(origem, raioMaxBounce);
        GameObject melhor = null;
        float menorDist   = float.MaxValue;

        foreach (var c in cols)
        {
            var root = ResolverInimigo(c.gameObject);
            if (root == null || excluidos.Contains(root)) continue;
            float dist = Vector2.Distance(origem, root.transform.position);
            if (dist < menorDist) { menorDist = dist; melhor = root; }
        }
        return melhor;
    }

    // ─── EFEITO DE CARGA ANTES DE DISPARAR ───────────────────────────────────

    IEnumerator EfeitoCarga()
    {
        float dur = 0.4f;
        var sr = playerStats?.GetComponent<SpriteRenderer>();

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float prog  = t / dur;
            float pulso = Mathf.Sin(t * 30f) * 0.5f + 0.5f;

            if (sr != null)
                sr.color = Color.Lerp(Color.white, new Color(0.6f, 0.9f, 1f), pulso * prog);

            // Anel elétrico contraindo
            SpawnAnelEletrico((Vector2)transform.position, Mathf.Lerp(3f, 0.3f, prog), pulso);

            // Partículas sendo sugadas
            if (Time.frameCount % 2 == 0)
                SpawnParticulaCarga((Vector2)transform.position, Mathf.Lerp(3f, 0.5f, prog));

            yield return null;
        }

        if (sr != null) sr.color = Color.white;
    }

    void SpawnAnelEletrico(Vector2 centro, float raio, float pulso)
    {
        const int S = 36;
        var go = new GameObject("AnelE");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 15;
        lr.startWidth = lr.endWidth = 0.03f + pulso * 0.04f;
        lr.startColor = lr.endColor = new Color(0.5f, 0.85f, 1f, 0.5f + pulso * 0.4f);
        for (int i = 0; i < S; i++)
        {
            float ang = 360f / S * i * Mathf.Deg2Rad;
            float jitter = Random.Range(-0.05f, 0.05f);
            lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (raio + jitter));
        }
        Destroy(go, 0.05f);
    }

    void SpawnParticulaCarga(Vector2 centro, float raio)
    {
        float ang = Random.Range(0f, Mathf.PI * 2f);
        Vector2 pos = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio;
        var go = new GameObject("PC");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(5); sr.color = COR_GLOW; sr.sortingOrder = 16;
        go.transform.localScale = Vector3.one * Random.Range(0.07f, 0.16f);
        go.AddComponent<AutoDestroyFadeMove>().Iniciar((centro - pos).normalized * Random.Range(4f, 9f), 0.18f);
        Destroy(go, 0.35f);
    }

    // ─── VISUAL DO RAIO ───────────────────────────────────────────────────────

    IEnumerator AnimarRaio(Vector2 de, Vector2 para, int bounce)
    {
        float prog = bounce / (float)Mathf.Max(1, maxRicochetes - 1);
        float largGlow  = Mathf.Lerp(0.55f, 0.18f, prog);
        float largNucleo = Mathf.Lerp(0.18f, 0.05f, prog);
        float alpha      = Mathf.Lerp(1f, 0.55f, prog);
        float duracao    = Mathf.Lerp(0.35f, 0.2f, prog);

        // Camada 1: glow externo (mais largo, semi-transparente)
        var goGlow = CriarSegmentoRaio(de, para, largGlow, new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, alpha * 0.5f), 12);
        // Camada 2: núcleo brilhante
        var goNucleo = CriarSegmentoRaio(de, para, largNucleo, new Color(COR_NUCLEO.r, COR_NUCLEO.g, COR_NUCLEO.b, alpha), 14);
        // Camada 3: fio central branco fininho
        var goFio = CriarSegmentoRaio(de, para, largNucleo * 0.3f, COR_BRILHO, 16, jitter: 0.1f);

        // Ramificações
        SpawnRamificacoes(de, para, bounce);

        Destroy(goGlow,   0.5f);
        Destroy(goNucleo, 0.5f);
        Destroy(goFio,    0.5f);

        // Fade animado
        var lrG = goGlow?.GetComponent<LineRenderer>();
        var lrN = goNucleo?.GetComponent<LineRenderer>();
        var lrF = goFio?.GetComponent<LineRenderer>();

        // Flicker rápido
        for (int f = 0; f < 3; f++)
        {
            float on = f % 2 == 0 ? 1f : 0.3f;
            SetAlpha(lrG, alpha * 0.5f * on);
            SetAlpha(lrN, alpha * on);
            SetAlpha(lrF, on);
            yield return new WaitForSeconds(0.03f);
        }

        // Fade de saída
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float p = t / duracao;
            SetAlpha(lrG, Mathf.Lerp(alpha * 0.5f, 0f, p));
            SetAlpha(lrN, Mathf.Lerp(alpha,         0f, p));
            SetAlpha(lrF, Mathf.Lerp(1f,             0f, p));
            SetWidth(lrN, Mathf.Lerp(largNucleo, 0.01f,  p));
            yield return null;
        }

        if (goGlow   != null) Destroy(goGlow);
        if (goNucleo != null) Destroy(goNucleo);
        if (goFio    != null) Destroy(goFio);
    }

    GameObject CriarSegmentoRaio(Vector2 de, Vector2 para, float largura, Color cor, int ordem, float jitter = 0.4f)
    {
        const int SEGS = 12;
        var go = new GameObject("Raio");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = ordem; lr.numCapVertices = 4;
        lr.startWidth = lr.endWidth = largura;
        lr.startColor = lr.endColor = cor;

        for (int i = 0; i < SEGS; i++)
        {
            float t = i / (float)(SEGS - 1);
            Vector2 p = Vector2.Lerp(de, para, t);
            if (i > 0 && i < SEGS - 1) p += Random.insideUnitCircle * jitter;
            lr.SetPosition(i, p);
        }
        return go;
    }

    void SpawnRamificacoes(Vector2 de, Vector2 para, int bounce)
    {
        int qtd = Random.Range(2, 5 - bounce);
        Vector2 dir = (para - de);
        float comprimento = dir.magnitude;

        for (int r = 0; r < qtd; r++)
        {
            float t = Random.Range(0.2f, 0.8f);
            Vector2 origem = Vector2.Lerp(de, para, t);
            float angDesvio = Random.Range(-60f, 60f) * Mathf.Deg2Rad;
            Vector2 dirRam  = Quaternion.Euler(0, 0, angDesvio * Mathf.Rad2Deg) * dir.normalized;
            Vector2 fim     = origem + dirRam * Random.Range(0.5f, comprimento * 0.4f);

            var go = CriarSegmentoRaio(origem, fim, 0.04f, new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, 0.6f), 13, 0.2f);
            Destroy(go, Random.Range(0.08f, 0.2f));
        }
    }

    // ─── EFEITO DE IMPACTO NO ALVO ────────────────────────────────────────────

    IEnumerator EfeitoImpacto(Vector2 pos, int bounce)
    {
        // Flash no ponto de impacto
        var flash = new GameObject("Flash");
        flash.transform.position = pos;
        var fsr = flash.AddComponent<SpriteRenderer>();
        fsr.sprite = GerarDisco(32); fsr.color = COR_BRILHO; fsr.sortingOrder = 17;
        float escala = Mathf.Lerp(1.2f, 0.4f, bounce / (float)maxRicochetes);
        flash.transform.localScale = Vector3.one * escala;
        flash.AddComponent<AutoDestroyFade>().Iniciar(0.1f);
        Destroy(flash, 0.25f);

        // Partículas em rajada
        int qtdPartic = Mathf.Max(4, 10 - bounce * 2);
        for (int i = 0; i < qtdPartic; i++)
        {
            float ang = i / (float)qtdPartic * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            var p = new GameObject("PI");
            p.transform.position = pos;
            var psr = p.AddComponent<SpriteRenderer>();
            bool brilhante = i % 3 == 0;
            psr.sprite = GerarDisco(brilhante ? 8 : 5);
            psr.color  = brilhante ? COR_BRILHO : COR_IMPACT;
            psr.sortingOrder = 16;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(2.5f, 6f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(vel, Random.Range(0.2f, 0.45f));
            Destroy(p, 0.7f);
        }

        // Anel expansivo
        const int S = 40;
        var goAnel = new GameObject("AnelImpacto");
        goAnel.transform.position = pos;
        var lr = goAnel.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 14;
        Destroy(goAnel, 0.5f);

        float raioMax = Mathf.Lerp(2f, 0.8f, bounce / (float)maxRicochetes);
        float dur = 0.3f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (goAnel == null) yield break;
            float p = t / dur;
            float r = Mathf.Lerp(0.05f, raioMax, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.2f, 0.01f, p);
            lr.startColor = lr.endColor = new Color(COR_IMPACT.r, COR_IMPACT.g, COR_IMPACT.b, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < S; i++)
            {
                float a = 360f / S * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        if (goAnel != null) Destroy(goAnel);
    }

    // ─── FLASH DE TELA ────────────────────────────────────────────────────────

    IEnumerator FlashTela()
    {
        var sr = playerStats?.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        sr.color = COR_BRILHO;
        yield return new WaitForSeconds(0.06f);
        sr.color = Color.white;
    }

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    static void SetAlpha(LineRenderer lr, float a)
    {
        if (lr == null) return;
        Color c = lr.startColor; c.a = a;
        lr.startColor = lr.endColor = c;
    }

    static void SetWidth(LineRenderer lr, float w)
    {
        if (lr == null) return;
        lr.startWidth = lr.endWidth = w;
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, d < cx ? 1f : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    static GameObject ResolverInimigo(GameObject go)
    {
        var ic = go.GetComponent<InimigoController>() ?? go.GetComponentInParent<InimigoController>();
        if (ic != null) return ic.gameObject;
        var mi = go.GetComponent<movi_inimigo>() ?? go.GetComponentInParent<movi_inimigo>();
        if (mi != null) return mi.gameObject;
        if (go.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) return null;
        if (go.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) return null;
        return null;
    }
}
