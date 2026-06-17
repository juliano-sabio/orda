using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunicaoDivinaUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float raioDeteccao   = 25f;
    public float danoImpacto    = 80f;
    public float danoSecundario = 32f;
    public float raioExplosao   = 4f;
    public float cooldown       = 27f;
    public int   numSecundarios = 3;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    float       cooldownRestante;
    bool        ativo;
    PlayerStats playerStats;

    static readonly Color COR_OURO   = new Color(1f, 0.88f, 0.15f);
    static readonly Color COR_OURO2  = new Color(1f, 0.62f, 0.04f);
    static readonly Color COR_BRANCA = Color.white;

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
        if (InputBindings.UltimateDown() && cooldownRestante <= 0f && !ativo)
            StartCoroutine(CorotinaAtivacao());
        SincronizarUI();
    }

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    // ─── COROUTINE PRINCIPAL ─────────────────────────────────────────────────

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        var alvoGO = EncontrarAlvoPrincipal();
        if (alvoGO == null) { ativo = false; yield break; }

        Vector2 posAlvo = alvoGO.transform.position;

        // Telegráfico: marcador + partículas de aviso
        var marcador = CriarMarcador(posAlvo);
        StartCoroutine(ParticulasAviso(posAlvo, 0.7f));
        yield return StartCoroutine(AnimarMarcador(marcador, 0.7f));
        Destroy(marcador);

        // Flash de tela branco instantâneo
        StartCoroutine(FlashTela());

        // Raio principal (jagged + multicamada)
        yield return StartCoroutine(AnimarRaioPrincipal(posAlvo));

        // Dano
        float danoEfetivo = danoImpacto;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.PunicaoDivina2))
            danoEfetivo *= 1.6f;
        AplicarDano(posAlvo, danoEfetivo, true);

        // Impacto visual completo
        StartCoroutine(AnimarImpacto(posAlvo));

        yield return new WaitForSeconds(0.12f);

        // Raios secundários
        int numSecEfetivo = numSecundarios;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.PunicaoJulgamento))
            numSecEfetivo += 2;
        float danoSecEfetivo = danoSecundario;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.PunicaoJulgamento))
            danoSecEfetivo *= 1.5f;

        var secundarios = EncontrarSecundarios(posAlvo, alvoGO, numSecEfetivo);
        foreach (var sec in secundarios)
        {
            if (sec == null || sec.gameObject == null) continue;
            Vector2 posSec = sec.transform.position;
            var marcSec = CriarMarcadorPequeno(posSec);
            yield return new WaitForSeconds(0.18f);
            Destroy(marcSec);
            StartCoroutine(AnimarRaioSecundario(posSec));
            AplicarDano(posSec, danoSecEfetivo, false);
            StartCoroutine(AnimarExplosaoSecundaria(posSec));
        }

        yield return new WaitForSeconds(0.5f);
        ativo = false;
    }

    // ─── LÓGICA ──────────────────────────────────────────────────────────────

    GameObject EncontrarAlvoPrincipal()
    {
        float distMin = float.MaxValue;
        GameObject melhor = null;
        foreach (var c in Physics2D.OverlapCircleAll(transform.position, raioDeteccao))
        {
            if (c.gameObject == gameObject) continue;
            var root = ResolverInimigo(c.gameObject);
            if (root == null) continue;
            float d = Vector2.Distance(transform.position, root.transform.position);
            if (d < distMin) { distMin = d; melhor = root; }
        }
        return melhor;
    }

    List<GameObject> EncontrarSecundarios(Vector2 centro, GameObject excluir, int maxSec = -1)
    {
        if (maxSec < 0) maxSec = numSecundarios;
        var lista = new List<GameObject>();
        var vistos = new HashSet<GameObject> { excluir };
        foreach (var c in Physics2D.OverlapCircleAll(centro, raioDeteccao))
        {
            if (lista.Count >= maxSec) break;
            var root = ResolverInimigo(c.gameObject);
            if (root == null || vistos.Contains(root)) continue;
            vistos.Add(root);
            lista.Add(root);
        }
        return lista;
    }

    void AplicarDano(Vector2 centro, float dano, bool podeCrit)
    {
        float raioEfetivo = raioExplosao;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.PunicaoDivina2))
            raioEfetivo *= 1.5f;
        foreach (var c in Physics2D.OverlapCircleAll(centro, raioEfetivo))
        {
            var ic = c.GetComponent<InimigoController>() ?? c.GetComponentInParent<InimigoController>();
            if (ic != null)
            {
                bool crit = podeCrit && Random.value < 0.3f;
                ic.ReceberDano(crit ? dano * 1.5f : dano, crit);
            }
        }
    }

    static GameObject ResolverInimigo(GameObject go)
    {
        GameObject root = null;
        var ic = go.GetComponent<InimigoController>() ?? go.GetComponentInParent<InimigoController>();
        if (ic != null) root = ic.gameObject;
        if (root == null) { var mi = go.GetComponent<movi_inimigo>() ?? go.GetComponentInParent<movi_inimigo>(); if (mi != null) root = mi.gameObject; }
        if (root == null) { var bc = go.GetComponent<BossController>() ?? go.GetComponentInParent<BossController>(); if (bc != null) root = bc.gameObject; }
        if (root == null) { var bp = go.GetComponent<BossPrincesa>() ?? go.GetComponentInParent<BossPrincesa>(); if (bp != null) root = bp.gameObject; }
        if (root == null) return null;
        if (go.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) return null;
        if (go.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) return null;
        return root;
    }

    // ─── VISUAIS — MARCADOR ──────────────────────────────────────────────────

    GameObject CriarMarcador(Vector2 pos)
    {
        var root = new GameObject("MarcadorDivino");
        root.transform.position = pos;

        // Cruz dourada (4 raios)
        for (int i = 0; i < 4; i++)
        {
            float ang = i * 90f * Mathf.Deg2Rad;
            var linha = new GameObject($"Linha{i}");
            linha.transform.SetParent(root.transform, false);
            var lr = linha.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.positionCount = 2;
            lr.material     = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 15;
            lr.startWidth = lr.endWidth = 0.08f;
            lr.startColor = lr.endColor = new Color(1f, 0.92f, 0.2f, 0.9f);
            lr.SetPosition(0, pos);
            lr.SetPosition(1, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioExplosao);
        }

        // Anel exterior que se contrai (animado em AnimarMarcador)
        const int S = 36;
        var anel = new GameObject("Anel");
        anel.transform.SetParent(root.transform, false);
        var lrA = anel.AddComponent<LineRenderer>();
        lrA.useWorldSpace = true; lrA.loop = true; lrA.positionCount = S;
        lrA.material     = new Material(Shader.Find("Sprites/Default"));
        lrA.sortingOrder = 14;
        lrA.startWidth   = lrA.endWidth = 0.1f;
        lrA.startColor   = lrA.endColor = new Color(1f, 0.85f, 0.1f, 0.8f);
        // Posição inicial definida em AnimarMarcador

        // Disco central brilhante
        var centro = new GameObject("Centro");
        centro.transform.SetParent(root.transform, false);
        centro.transform.position = pos;
        var srC = centro.AddComponent<SpriteRenderer>();
        srC.sprite = GerarDisco(12);
        srC.color  = new Color(1f, 0.92f, 0.2f, 0.85f);
        srC.sortingOrder = 16;
        centro.transform.localScale = Vector3.one * 0.35f;

        return root;
    }

    IEnumerator AnimarMarcador(GameObject root, float dur)
    {
        var pos  = (Vector2)root.transform.position;
        var lrs  = root.GetComponentsInChildren<LineRenderer>();
        var srs  = root.GetComponentsInChildren<SpriteRenderer>();
        const int S = 36;

        // Identifica o anel (único com loop = true)
        LineRenderer anelLR = null;
        foreach (var lr in lrs) if (lr.loop) { anelLR = lr; break; }

        float raioInicial = raioExplosao * 2.8f;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p     = t / dur;
            float pulso = Mathf.Sin(p * Mathf.PI * 9f) * 0.5f + 0.5f;

            // Anel contrai do raio externo até o raio de explosão
            if (anelLR != null)
            {
                float r = Mathf.Lerp(raioInicial, raioExplosao, p);
                anelLR.positionCount = S;
                for (int i = 0; i < S; i++)
                {
                    float a = (360f / S) * i * Mathf.Deg2Rad;
                    anelLR.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
                float a2 = Mathf.Lerp(0.4f, 1f, p);
                anelLR.startWidth = anelLR.endWidth = Mathf.Lerp(0.07f, 0.13f, p);
                anelLR.startColor = anelLR.endColor = new Color(1f, 0.85f, 0.1f, a2);
            }

            // Cruz pulsa
            foreach (var lr in lrs)
            {
                if (lr == anelLR) continue;
                Color c = lr.startColor; c.a = 0.45f + pulso * 0.55f;
                lr.startColor = lr.endColor = c;
                lr.startWidth = lr.endWidth = 0.06f + pulso * 0.07f;
            }

            // Disco central pulsa
            foreach (var sr in srs)
            {
                Color c = sr.color; c.a = 0.6f + pulso * 0.4f;
                sr.color = c;
                sr.transform.localScale = Vector3.one * (0.3f + pulso * 0.08f);
            }

            yield return null;
        }
    }

    // Partículas douradas subindo do alvo durante o telegráfico
    IEnumerator ParticulasAviso(Vector2 pos, float dur)
    {
        int frame = 0;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            frame++;
            if (frame % 4 == 0)
            {
                var p = new GameObject("AvisoParticula");
                p.transform.position = pos + Random.insideUnitCircle * 0.6f;
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = GerarDisco(8);
                sr.color  = new Color(1f, 0.88f, 0.15f, 0.9f);
                sr.sortingOrder = 14;
                float esc = Random.Range(0.07f, 0.17f);
                p.transform.localScale = Vector3.one * esc;
                p.AddComponent<AutoDestroyFadeMove>().Iniciar(
                    Vector2.up * Random.Range(1.2f, 2.8f) + Random.insideUnitCircle * 0.5f,
                    Random.Range(0.35f, 0.65f));
                Destroy(p, 0.9f);
            }
            yield return null;
        }
    }

    // ─── VISUAIS — RAIO PRINCIPAL ────────────────────────────────────────────

    IEnumerator AnimarRaioPrincipal(Vector2 alvo)
    {
        Vector2 origem = alvo + Vector2.up * 17f;
        const int N = 14;

        // 3 camadas: glow externo, core, centro brilhante
        (float sw, float ew, float alpha, int order)[] camadas = {
            (4.0f, 1.0f, 0.22f, 14),
            (1.5f, 0.38f, 0.88f, 16),
            (0.38f, 0.08f, 1.0f, 18),
        };

        var gos = new GameObject[3];
        var lrs = new LineRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            gos[i] = new GameObject($"RaioLayer{i}");
            lrs[i] = gos[i].AddComponent<LineRenderer>();
            lrs[i].useWorldSpace = true; lrs[i].positionCount = N;
            lrs[i].material = new Material(Shader.Find("Sprites/Default"));
            lrs[i].sortingOrder = camadas[i].order;
        }

        // 2 frames: flash branco puro
        AtualizarBolt(lrs, origem, alvo, N, 0f);
        for (int i = 0; i < 3; i++)
        {
            lrs[i].startWidth = camadas[i].sw; lrs[i].endWidth = camadas[i].ew;
            lrs[i].startColor = lrs[i].endColor = new Color(1f, 1f, 1f, camadas[i].alpha);
        }
        yield return null; yield return null;

        // 8 frames: flicker com jitter (raio tremendo/piscando)
        for (int f = 0; f < 8; f++)
        {
            AtualizarBolt(lrs, origem, alvo, N, 0.25f);
            float brilho = (f % 2 == 0) ? 1f : 0.6f;
            Color base_ = Color.Lerp(COR_BRANCA, COR_OURO, f * 0.12f);
            for (int i = 0; i < 3; i++)
            {
                lrs[i].startWidth = camadas[i].sw * brilho;
                lrs[i].endWidth   = camadas[i].ew * brilho;
                lrs[i].startColor = lrs[i].endColor = new Color(base_.r, base_.g, base_.b, camadas[i].alpha * brilho);
            }
            yield return null;
        }

        // Fade dourado
        float dur = 0.38f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            AtualizarBolt(lrs, origem, alvo, N, 0.1f * (1f - p));
            for (int i = 0; i < 3; i++)
            {
                lrs[i].startWidth = Mathf.Lerp(camadas[i].sw, 0f, p);
                lrs[i].endWidth   = Mathf.Lerp(camadas[i].ew, 0f, p);
                lrs[i].startColor = lrs[i].endColor = new Color(COR_OURO.r, COR_OURO.g, COR_OURO.b, camadas[i].alpha * (1f - p));
            }
            yield return null;
        }
        for (int i = 0; i < 3; i++) Destroy(gos[i]);
    }

    void AtualizarBolt(LineRenderer[] lrs, Vector2 orig, Vector2 dest, int n, float jitter)
    {
        var pts = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            pts[i] = Vector2.Lerp(orig, dest, t);
            if (i > 0 && i < n - 1)
                pts[i] += Random.insideUnitCircle * jitter * Mathf.Sin(t * Mathf.PI);
        }
        foreach (var lr in lrs)
        {
            lr.positionCount = n;
            for (int i = 0; i < n; i++) lr.SetPosition(i, pts[i]);
        }
    }

    // ─── VISUAIS — IMPACTO ───────────────────────────────────────────────────

    IEnumerator AnimarImpacto(Vector2 pos)
    {
        // Disco flash central (branco → dourado)
        StartCoroutine(DiscoCentral(pos));

        // Pilar de luz subindo
        StartCoroutine(PilarDeLuz(pos));

        // 3 anéis com velocidades e delays diferentes
        for (int r = 0; r < 3; r++)
            StartCoroutine(AnelExpansao(pos, raioExplosao * (1.2f + r * 0.5f), 0.42f + r * 0.07f, r * 0.05f));

        // 18 faíscas douradas voando
        SpawnFaiscas(pos, 18, true);

        // Marca escura no chão
        StartCoroutine(MarcaChao(pos));

        yield return null;
    }

    IEnumerator DiscoCentral(Vector2 pos)
    {
        var go = new GameObject("DiscoCentral");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(32);
        sr.sortingOrder = 21;
        float dur = 0.32f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p      = t / dur;
            float escala = Mathf.Lerp(0.2f, raioExplosao * 2.2f, Mathf.Sqrt(p));
            float alpha  = Mathf.Lerp(1f, 0f, p * p);
            Color c      = Color.Lerp(COR_BRANCA, COR_OURO, p);
            sr.color = new Color(c.r, c.g, c.b, alpha);
            go.transform.localScale = Vector3.one * escala;
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator PilarDeLuz(Vector2 pos)
    {
        var go = new GameObject("PilarLuz");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarRetangulo(8, 64);
        sr.sortingOrder = 20;
        float dur = 0.65f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p      = t / dur;
            float larg   = Mathf.Lerp(1.5f, 0.05f, p);
            float alt    = Mathf.Lerp(2.5f, 11f, Mathf.Sqrt(p * 0.7f));
            float alpha  = Mathf.Lerp(0.92f, 0f, p * p);
            Color c      = Color.Lerp(COR_BRANCA, COR_OURO, p * 2f);
            sr.color = new Color(c.r, c.g, c.b, alpha);
            go.transform.localScale = new Vector3(larg, alt, 1f);
            go.transform.position   = pos + Vector2.up * Mathf.Lerp(0f, 4f, p);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator AnelExpansao(Vector2 pos, float raioMax, float dur, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        const int S = 44;
        var go = new GameObject("AnelImpacto");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 17;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p  = t / dur;
            float pe = 1f - Mathf.Pow(1f - p, 2.2f); // ease-out
            float r  = Mathf.Lerp(0.15f, raioMax, pe);
            float w  = Mathf.Lerp(0.6f, 0.02f, p);
            Color c  = Color.Lerp(COR_BRANCA, COR_OURO2, p);
            lr.startWidth = lr.endWidth = w;
            lr.startColor = lr.endColor = new Color(c.r, c.g, c.b, 1f - p);
            for (int i = 0; i < S; i++)
            {
                float a = (360f / S) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        Destroy(go);
    }

    void SpawnFaiscas(Vector2 pos, int qtd, bool grande)
    {
        for (int i = 0; i < qtd; i++)
        {
            float ang   = i / (float)qtd * Mathf.PI * 2f + Random.Range(-0.18f, 0.18f);
            float speed = grande ? Random.Range(5f, 13f) : Random.Range(3f, 7f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;
            float esc   = grande ? Random.Range(0.13f, 0.3f) : Random.Range(0.07f, 0.17f);

            // Faísca principal
            var go = new GameObject("FaiscaDivina");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * esc;
            go.transform.rotation = Quaternion.Euler(0f, 0f, ang * Mathf.Rad2Deg);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = i % 3 == 0 ? GerarDiamante() : GerarDisco(8);
            sr.color  = i % 4 == 0 ? COR_BRANCA : COR_OURO;
            sr.sortingOrder = 19;
            go.AddComponent<AutoDestroyFadeMove>().Iniciar(vel, Random.Range(0.3f, 0.55f));
            Destroy(go, 0.7f);

            // Rastro menor
            var tr = new GameObject("FaiscaTrail");
            tr.transform.position = pos;
            tr.transform.localScale = Vector3.one * esc * 0.45f;
            var trsr = tr.AddComponent<SpriteRenderer>();
            trsr.sprite = GerarDisco(6);
            trsr.color  = new Color(COR_OURO.r, COR_OURO.g, COR_OURO.b, 0.55f);
            trsr.sortingOrder = 18;
            tr.AddComponent<AutoDestroyFadeMove>().Iniciar(vel * 0.4f, 0.2f);
            Destroy(tr, 0.4f);
        }
    }

    IEnumerator MarcaChao(Vector2 pos)
    {
        var go = new GameObject("MarcaChao");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(32);
        sr.sortingOrder = 9;
        go.transform.localScale = Vector3.one * raioExplosao * 1.9f;
        float dur = 1.8f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            float a = p < 0.08f
                ? Mathf.Lerp(0f, 0.45f, p / 0.08f)
                : Mathf.Lerp(0.45f, 0f, (p - 0.08f) / 0.92f);
            sr.color = new Color(0.45f, 0.3f, 0.04f, a);
            yield return null;
        }
        Destroy(go);
    }

    // ─── VISUAIS — FLASH TELA ────────────────────────────────────────────────

    IEnumerator FlashTela()
    {
        var go = new GameObject("FlashTela");
        go.transform.position = transform.position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(32);
        sr.sortingOrder = 50;
        go.transform.localScale = Vector3.one * 80f;
        sr.color = new Color(1f, 0.97f, 0.82f, 0.65f);
        yield return null;
        sr.color = new Color(1f, 0.97f, 0.82f, 0.32f);
        yield return null;
        sr.color = new Color(1f, 0.97f, 0.82f, 0.1f);
        yield return null;
        Destroy(go);
    }

    // ─── VISUAIS — RAIOS SECUNDÁRIOS ─────────────────────────────────────────

    GameObject CriarMarcadorPequeno(Vector2 pos)
    {
        const int S = 20;
        var go = new GameObject("MarcadorSec");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 13;
        lr.startWidth = lr.endWidth = 0.06f;
        lr.startColor = lr.endColor = new Color(1f, 0.8f, 0.1f, 0.8f);
        float r = raioExplosao * 0.6f;
        for (int i = 0; i < S; i++)
        {
            float a = (360f / S) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
        }
        return go;
    }

    IEnumerator AnimarRaioSecundario(Vector2 alvo)
    {
        Vector2 origem = alvo + Vector2.up * 12f;
        const int N = 10;

        (float sw, float ew, float alpha, int order)[] camadas = {
            (2.0f, 0.5f, 0.28f, 14),
            (0.7f, 0.18f, 0.92f, 16),
        };
        var gos = new GameObject[2];
        var lrs = new LineRenderer[2];
        for (int i = 0; i < 2; i++)
        {
            gos[i] = new GameObject($"RaioSecLayer{i}");
            lrs[i] = gos[i].AddComponent<LineRenderer>();
            lrs[i].useWorldSpace = true; lrs[i].positionCount = N;
            lrs[i].material = new Material(Shader.Find("Sprites/Default"));
            lrs[i].sortingOrder = camadas[i].order;
        }

        AtualizarBolt(lrs, origem, alvo, N, 0f);
        for (int i = 0; i < 2; i++)
        {
            lrs[i].startWidth = camadas[i].sw; lrs[i].endWidth = camadas[i].ew;
            lrs[i].startColor = lrs[i].endColor = new Color(1f, 1f, 1f, camadas[i].alpha);
        }
        yield return null; yield return null;

        for (int f = 0; f < 6; f++)
        {
            AtualizarBolt(lrs, origem, alvo, N, 0.19f);
            float b = (f % 2 == 0) ? 1f : 0.58f;
            Color base_ = Color.Lerp(COR_BRANCA, COR_OURO, f * 0.16f);
            for (int i = 0; i < 2; i++)
            {
                lrs[i].startWidth = camadas[i].sw * b; lrs[i].endWidth = camadas[i].ew * b;
                lrs[i].startColor = lrs[i].endColor = new Color(base_.r, base_.g, base_.b, camadas[i].alpha * b);
            }
            yield return null;
        }

        float dur = 0.27f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            AtualizarBolt(lrs, origem, alvo, N, 0.08f * (1f - p));
            for (int i = 0; i < 2; i++)
            {
                lrs[i].startWidth = Mathf.Lerp(camadas[i].sw, 0f, p);
                lrs[i].endWidth   = Mathf.Lerp(camadas[i].ew, 0f, p);
                lrs[i].startColor = lrs[i].endColor = new Color(COR_OURO.r, COR_OURO.g, COR_OURO.b, camadas[i].alpha * (1f - p));
            }
            yield return null;
        }
        for (int i = 0; i < 2; i++) Destroy(gos[i]);
    }

    IEnumerator AnimarExplosaoSecundaria(Vector2 pos)
    {
        SpawnFaiscas(pos, 8, false);
        StartCoroutine(AnelExpansao(pos, raioExplosao * 0.9f, 0.3f, 0f));
        StartCoroutine(AnelExpansao(pos, raioExplosao * 0.65f, 0.23f, 0.04f));
        yield return null;
    }

    // ─── SPRITES PROCEDURAIS ─────────────────────────────────────────────────

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, d < cx ? 1f : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    static Sprite GerarDiamante()
    {
        const int SZ = 8;
        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        float cx = SZ * 0.5f;
        for (int y = 0; y < SZ; y++) for (int x = 0; x < SZ; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx;
            float ny = Mathf.Abs(y + 0.5f - cx) / cx;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, nx + ny < 1.05f ? 1f : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ);
    }

    static Sprite GerarRetangulo(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        float cx = w * 0.5f;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - nx * nx * 2f)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
    }
}
