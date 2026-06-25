using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalEvento : MonoBehaviour
{
    [HideInInspector] public float        raioFechar     = 2.5f;
    [HideInInspector] public float        tempoFechar    = 3.5f;
    [HideInInspector] public float        intervaloSpawn = 5f;
    [HideInInspector] public GameObject[] prefabsInimigos;

    public event Action          OnConcluido;
    public event Action<int,int> OnProgresso;

    PlayerStats player;
    readonly List<PortalData> portais = new List<PortalData>();
    int         fechados;
    float       elapsed;
    bool        indicadoresCriados;

    // Co-op: no cliente cada portal é um objeto cosmético próprio (só visual + indicador).
    [HideInInspector] public bool cosmetico;

    static readonly Color[] PALETA = {
        new Color(0.55f, 0.1f,  1f,    0.95f), // roxo
        new Color(1f,    0.45f, 0.05f, 0.95f), // laranja
        new Color(0.1f,  0.5f,  1f,    0.95f), // azul
        new Color(0.15f, 0.9f,  0.3f,  0.95f), // verde
        new Color(0.1f,  0.9f,  0.9f,  0.95f), // ciano
        new Color(1f,    0.1f,  0.7f,  0.95f), // magenta
    };
    static readonly string[] NOMES_COR = { "Roxo", "Laranja", "Azul", "Verde", "Ciano", "Magenta" };

    public void Iniciar(PlayerStats ps, Vector2 posA, Vector2 posB,
                        GameObject[] prefabs, float intervalo)
        => IniciarMultiplo(ps, new[] { posA, posB }, prefabs, intervalo);

    public void IniciarMultiplo(PlayerStats ps, Vector2[] posicoes,
                                GameObject[] prefabs, float intervalo)
    {
        player          = ps;
        prefabsInimigos = prefabs;
        intervaloSpawn  = intervalo;

        for (int i = 0; i < posicoes.Length; i++)
        {
            Color cor = PALETA[i % PALETA.Length];
            var p = CriarPortal(posicoes[i], cor);
            portais.Add(p);
            StartCoroutine(SpawnLoop(p));
            StartCoroutine(ParticulasLoop(p));
            StartCoroutine(TendrilLoop(p));
            StartCoroutine(PulsoLoop(p));

            // co-op: registra cada portal p/ o cliente reconstruir a cópia cosmética + indicador.
            if (NetSpawn.EmRede && NetSpawn.PodeSpawnar && CoopProgressao.Instance != null)
                p.objEvtId = CoopProgressao.Instance.RegistrarObjEvento(3, posicoes[i], 0f, 0f, cor);
        }
    }

    // Cliente co-op: um portal cosmético (só visual + indicador, sem fechamento/spawn).
    public void IniciarCosmeticoUmPortal(Vector2 pos, Color cor)
    {
        cosmetico = true;
        player    = PlayerStats.Local;
        var p = CriarPortal(pos, cor);
        portais.Add(p);
        StartCoroutine(ParticulasLoop(p));
        StartCoroutine(TendrilLoop(p));
        StartCoroutine(PulsoLoop(p));
        CriarIndicador(p, "Portal");
    }

    void Update()
    {
        if (player == null) player = PlayerStats.Local;
        if (player == null) return;
        elapsed += Time.deltaTime;

        Vector2 posPlayer = player.transform.position;
        foreach (var p in portais)
            AtualizarPortal(p, posPlayer);
    }

    public void MostrarIndicadores()
    {
        if (indicadoresCriados) return;
        indicadoresCriados = true;
        for (int i = 0; i < portais.Count; i++)
            CriarIndicador(portais[i], $"Portal {NOMES_COR[i % NOMES_COR.Length]}!");
    }

    void OnDestroy()
    {
        foreach (var p in portais)
        {
            if (p != null && p.objEvtId != 0 && CoopProgressao.Instance != null)
                CoopProgressao.Instance.RemoverObjEvento(p.objEvtId);
            p?.DestruirTudo();
        }
    }

    // ── Lógica ───────────────────────────────────────────────────────────────────

    // Co-op: true se QUALQUER player está dentro do raio de fechamento do portal.
    bool AlgumPlayerPerto(Vector2 pos)
    {
        for (int i = 0; i < PlayerStats.All.Count; i++)
        {
            var ps = PlayerStats.All[i];
            if (ps != null && Vector2.Distance(ps.transform.position, pos) <= raioFechar) return true;
        }
        return false;
    }

    void AtualizarPortal(PortalData p, Vector2 posPlayer)
    {
        if (p == null || p.fechado) return;

        // 3 anéis com velocidades e direções diferentes
        float a = elapsed * 80f;
        if (p.rootA != null) p.rootA.transform.rotation = Quaternion.Euler(0f, 0f,  a);
        if (p.rootB != null) p.rootB.transform.rotation = Quaternion.Euler(0f, 0f, -a * 0.55f);
        if (p.rootC != null) p.rootC.transform.rotation = Quaternion.Euler(0f, 0f,  a * 1.6f);

        bool  perto = AlgumPlayerPerto(p.posicao); // co-op: QUALQUER player perto carrega o portal
        p.progresso = Mathf.Clamp01(p.progresso + (perto ? 1f : -1f) * Time.deltaTime / tempoFechar);

        float pulso      = Mathf.Sin(elapsed * 3.5f) * 0.5f + 0.5f;
        float cargaExtra = p.progresso;

        // Anel externo
        if (p.lrAnelA != null)
        {
            p.lrAnelA.startColor = p.lrAnelA.endColor =
                new Color(p.cor.r, p.cor.g, p.cor.b, 0.6f + pulso * 0.35f + cargaExtra * 0.2f);
            p.lrAnelA.startWidth = p.lrAnelA.endWidth = 0.12f + pulso * 0.05f + cargaExtra * 0.06f;
        }
        // Anel médio
        if (p.lrAnelB != null)
            p.lrAnelB.startColor = p.lrAnelB.endColor =
                new Color(p.cor.r, p.cor.g, p.cor.b, 0.3f + pulso * 0.2f + cargaExtra * 0.15f);
        // Anel interno branco
        if (p.lrAnelC != null)
            p.lrAnelC.startColor = p.lrAnelC.endColor =
                new Color(1f, 1f, 1f, 0.2f + pulso * 0.25f + cargaExtra * 0.3f);

        // Fill
        if (p.srFill != null)
            p.srFill.color = new Color(p.cor.r, p.cor.g, p.cor.b, 0.05f + cargaExtra * 0.15f + pulso * 0.03f);

        // Core glow
        if (p.srCore != null && p.coreRoot != null)
        {
            p.srCore.color = new Color(p.cor.r, p.cor.g, p.cor.b, 0.35f + pulso * 0.35f + cargaExtra * 0.25f);
            float coreEscala = 0.35f + pulso * 0.15f + cargaExtra * 0.4f;
            p.coreRoot.transform.localScale = Vector3.one * coreEscala;
        }

        AtualizarArco(p);

        if (!cosmetico && p.progresso >= 1f) FecharPortal(p); // fechamento é host-autoritativo
    }

    void FecharPortal(PortalData p)
    {
        p.fechado = true;
        // co-op: remove a cópia cosmética desse portal no cliente (senão ela fica pra sempre).
        if (p.objEvtId != 0 && CoopProgressao.Instance != null)
        {
            CoopProgressao.Instance.RemoverObjEvento(p.objEvtId);
            p.objEvtId = 0;
        }
        if (p.indicador != null) { Destroy(p.indicador.gameObject); p.indicador = null; }
        fechados++;
        OnProgresso?.Invoke(fechados, portais.Count);
        StartCoroutine(AnimarFechamento(p));
        if (fechados >= portais.Count) OnConcluido?.Invoke();
    }

    void CriarIndicador(PortalData p, string nome)
    {
        if (p == null || p.fechado || p.rootA == null) return;
        var go  = new GameObject($"IndicadorPortal_{nome}");
        var ind = go.AddComponent<IndicadorSlime>();
        ind.alvo    = p.rootA.transform;
        ind.corSeta = p.cor;
        ind.label   = nome;
        p.indicador = ind;
    }

    // ── Criação do portal ─────────────────────────────────────────────────────────

    PortalData CriarPortal(Vector2 pos, Color cor)
    {
        var p = new PortalData { posicao = pos, cor = cor };

        // Root A — anel externo, gira rápido CW
        p.rootA = new GameObject("PortalRootA");
        p.rootA.transform.position = pos;
        p.lrAnelA = CriarAnelLocal(p.rootA, 48, 1.8f, cor, 0.13f, 8);

        // Root B — anel médio, gira lento CCW
        p.rootB = new GameObject("PortalRootB");
        p.rootB.transform.position = pos;
        p.lrAnelB = CriarAnelLocal(p.rootB, 32, 1.25f, new Color(cor.r, cor.g, cor.b, 0.4f), 0.06f, 7);

        // Root C — anel interno branco, gira mais rápido CW
        p.rootC = new GameObject("PortalRootC");
        p.rootC.transform.position = pos;
        p.lrAnelC = CriarAnelLocal(p.rootC, 20, 0.65f, new Color(1f, 1f, 1f, 0.25f), 0.04f, 9);

        // Fill
        p.fillRoot = new GameObject("PortalFill");
        p.fillRoot.transform.position = pos;
        p.srFill               = p.fillRoot.AddComponent<SpriteRenderer>();
        p.srFill.sprite        = GerarDisco(64);
        p.srFill.color         = new Color(cor.r, cor.g, cor.b, 0.06f);
        p.srFill.sortingOrder  = 6;
        p.fillRoot.transform.localScale = Vector3.one * 3.6f;

        // Core glow central
        p.coreRoot = new GameObject("PortalCore");
        p.coreRoot.transform.position = pos;
        p.srCore               = p.coreRoot.AddComponent<SpriteRenderer>();
        p.srCore.sprite        = GerarDisco(32);
        p.srCore.color         = new Color(cor.r, cor.g, cor.b, 0.45f);
        p.srCore.sortingOrder  = 10;
        p.coreRoot.transform.localScale = Vector3.one * 0.4f;

        // Arco de carregamento
        p.arcoRoot             = new GameObject("PortalArco");
        p.arcoRoot.transform.position = pos;
        p.lrArco               = p.arcoRoot.AddComponent<LineRenderer>();
        p.lrArco.useWorldSpace = true;
        p.lrArco.loop          = false;
        p.lrArco.material      = new Material(Shader.Find("Sprites/Default"));
        p.lrArco.sortingOrder  = 11;
        p.lrArco.positionCount = 0;

        // Círculo indicador de raio
        p.raioRoot = CriarCirculoRaio(pos, cor, raioFechar);

        return p;
    }

    LineRenderer CriarAnelLocal(GameObject parent, int segs, float raio, Color cor, float larg, int order)
    {
        var go = new GameObject("Anel");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = segs;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = order;
        lr.startWidth    = lr.endWidth = larg;
        lr.startColor    = lr.endColor = cor;
        for (int i = 0; i < segs; i++)
        {
            float ang = 360f / segs * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio, 0f));
        }
        return lr;
    }

    GameObject CriarCirculoRaio(Vector2 pos, Color cor, float raio)
    {
        const int SEGS = 32;
        var go = new GameObject("RaioIndicador");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 5;
        lr.startWidth    = lr.endWidth = 0.05f;
        lr.startColor    = lr.endColor = new Color(cor.r, cor.g, cor.b, 0.18f);
        for (int i = 0; i < SEGS; i++)
        {
            float ang = 360f / SEGS * i * Mathf.Deg2Rad;
            lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
        }
        return go;
    }

    void AtualizarArco(PortalData p)
    {
        if (p.lrArco == null) return;
        if (p.progresso <= 0f) { p.lrArco.positionCount = 0; return; }
        int segs = Mathf.Max(2, Mathf.RoundToInt(p.progresso * 48));
        p.lrArco.positionCount = segs;
        const float RAIO_ARCO  = 2.1f;
        for (int i = 0; i < segs; i++)
        {
            float ang = i / (float)Mathf.Max(1, segs - 1) * 360f * Mathf.Deg2Rad;
            p.lrArco.SetPosition(i, p.posicao + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * RAIO_ARCO);
        }
        Color c = Color.Lerp(p.cor, Color.white, p.progresso);
        p.lrArco.startColor = p.lrArco.endColor = c;
        p.lrArco.startWidth = p.lrArco.endWidth = Mathf.Lerp(0.1f, 0.26f, p.progresso);
    }

    // ── Fechamento ────────────────────────────────────────────────────────────────

    IEnumerator AnimarFechamento(PortalData p)
    {
        // Burst de partículas ao fechar
        for (int i = 0; i < 18; i++)
        {
            float ang = i / 18f * Mathf.PI * 2f;
            Vector2 bPos = p.posicao + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 1.8f;
            var go = new GameObject("BurstFechamento");
            go.transform.position = bPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = GerarDisco(8);
            sr.color        = Color.white;
            sr.sortingOrder = 13;
            go.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.15f, 0.38f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * UnityEngine.Random.Range(2.5f, 6f);
            go.AddComponent<ParticulaPortalFX>().Iniciar(vel, p.cor);
        }

        StartCoroutine(AnelExpansivoFX(p.posicao, p.cor, 4.5f, 0.6f));

        if (p.lrAnelA != null) p.lrAnelA.startColor = p.lrAnelA.endColor = Color.white;
        if (p.srFill  != null) p.srFill.color = new Color(1f, 1f, 1f, 0.5f);
        if (p.srCore  != null) p.srCore.color = Color.white;

        float dur = 0.55f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float prog  = t / dur;
            float scale = Mathf.Lerp(1f, 0f, prog * prog);

            if (p.rootA    != null) p.rootA.transform.localScale    = Vector3.one * scale;
            if (p.rootB    != null) p.rootB.transform.localScale    = Vector3.one * scale;
            if (p.rootC    != null) p.rootC.transform.localScale    = Vector3.one * scale;
            if (p.fillRoot != null) p.fillRoot.transform.localScale = Vector3.one * (3.6f * scale);
            if (p.coreRoot != null) p.coreRoot.transform.localScale = Vector3.one * (1.5f * (1f - prog));
            if (p.srFill   != null) p.srFill.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.5f, 0f, prog));
            if (p.srCore   != null) p.srCore.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, prog));
            if (p.lrArco   != null)
            {
                Color c = p.lrArco.startColor; c.a = Mathf.Lerp(1f, 0f, prog);
                p.lrArco.startColor = p.lrArco.endColor = c;
            }
            yield return null;
        }
        p.DestruirTudo();
    }

    // ── Spawn de inimigos ─────────────────────────────────────────────────────────

    IEnumerator SpawnLoop(PortalData p)
    {
        yield return new WaitForSeconds(2f);
        while (!p.fechado && this != null)
        {
            yield return new WaitForSeconds(intervaloSpawn);
            if (p.fechado || this == null) yield break;
            if (prefabsInimigos == null || prefabsInimigos.Length == 0) continue;
            var prefab = prefabsInimigos[UnityEngine.Random.Range(0, prefabsInimigos.Length)];
            if (prefab == null) continue;
            StartCoroutine(FlashPortal(p));
            NetSpawn.Spawnar(prefab, new Vector3(p.posicao.x, p.posicao.y, 0f)); // host spawna+replica
        }
    }

    IEnumerator FlashPortal(PortalData p)
    {
        if (p.lrAnelA != null) p.lrAnelA.startColor = p.lrAnelA.endColor = Color.white;
        if (p.srCore  != null) p.srCore.color = Color.white;
        StartCoroutine(AnelExpansivoFX(p.posicao, p.cor, 2.8f, 0.35f));
        yield return new WaitForSeconds(0.12f);
        // Cor restaurada automaticamente pelo Update
    }

    // ── Tendrils ──────────────────────────────────────────────────────────────────

    IEnumerator TendrilLoop(PortalData p)
    {
        while (!p.fechado && this != null)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.3f));
            if (p.fechado || this == null) yield break;
            int qtd = UnityEngine.Random.Range(1, 3);
            for (int i = 0; i < qtd; i++)
                StartCoroutine(SpawnTendril(p));
        }
    }

    IEnumerator SpawnTendril(PortalData p)
    {
        float   ang  = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector2 raiz = p.posicao + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 1.8f;
        Vector2 dir  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        float   len  = UnityEngine.Random.Range(0.5f, 1.8f);
        Vector2 ponta = raiz + dir * len;

        var go = new GameObject("Tendril");
        go.transform.position = raiz;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 4;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 12;
        lr.startWidth    = 0.07f;
        lr.endWidth      = 0.01f;

        for (int i = 0; i < 4; i++)
        {
            float t  = i / 3f;
            Vector2 pt = Vector2.Lerp(raiz, ponta, t);
            if (i > 0 && i < 3) pt += UnityEngine.Random.insideUnitCircle * 0.22f;
            lr.SetPosition(i, pt);
        }

        float dur = UnityEngine.Random.Range(0.15f, 0.35f);
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float alpha = Mathf.Lerp(1f, 0f, t / dur);
            lr.startColor = lr.endColor = new Color(p.cor.r, p.cor.g, p.cor.b, alpha);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ── Pulso periódico ───────────────────────────────────────────────────────────

    IEnumerator PulsoLoop(PortalData p)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.5f));
        while (!p.fechado && this != null)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(1.8f, 3.2f));
            if (p.fechado || this == null) yield break;
            StartCoroutine(AnelExpansivoFX(p.posicao, p.cor, 3.5f, 0.7f));
        }
    }

    IEnumerator AnelExpansivoFX(Vector2 pos, Color cor, float raioFinal, float dur)
    {
        const int SEGS = 40;
        var go = new GameObject("AnelPortalFX");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 7;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p     = t / dur;
            float raio  = Mathf.Lerp(0.3f, raioFinal, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.18f, 0.01f, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(0.7f, 0f, p));
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ── Partículas ────────────────────────────────────────────────────────────────

    IEnumerator ParticulasLoop(PortalData p)
    {
        while (!p.fechado && this != null)
        {
            yield return new WaitForSeconds(0.09f);
            if (p.fechado || this == null) yield break;

            float   ang  = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float   dist = UnityEngine.Random.Range(1.0f, 2.8f);
            Vector2 pos  = p.posicao + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

            var go = new GameObject("ParticulaPortal");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GerarDisco(6);
            // Mistura: 60% cor do portal, 40% branco
            Color c = UnityEngine.Random.value > 0.4f ? p.cor : new Color(1f, 1f, 1f, 0.9f);
            sr.color        = c;
            sr.sortingOrder = 10;
            go.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.07f, 0.2f);

            Vector2 vel = (p.posicao - pos).normalized * UnityEngine.Random.Range(1f, 3.5f);
            go.AddComponent<ParticulaPortalFX>().Iniciar(vel, c);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

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
}

// ── Dados de cada portal ──────────────────────────────────────────────────────────

class PortalData
{
    public Vector2        posicao;
    public bool           fechado;
    public int            objEvtId; // co-op: id do rebuild cosmético
    public float          progresso;
    public Color          cor;
    public GameObject     rootA, rootB, rootC;
    public GameObject     fillRoot, arcoRoot, raioRoot, coreRoot;
    public LineRenderer   lrAnelA, lrAnelB, lrAnelC;
    public LineRenderer   lrArco;
    public SpriteRenderer srFill, srCore;
    public IndicadorSlime indicador;

    public void DestruirTudo()
    {
        if (rootA     != null) UnityEngine.Object.Destroy(rootA);
        if (rootB     != null) UnityEngine.Object.Destroy(rootB);
        if (rootC     != null) UnityEngine.Object.Destroy(rootC);
        if (fillRoot  != null) UnityEngine.Object.Destroy(fillRoot);
        if (arcoRoot  != null) UnityEngine.Object.Destroy(arcoRoot);
        if (raioRoot  != null) UnityEngine.Object.Destroy(raioRoot);
        if (coreRoot  != null) UnityEngine.Object.Destroy(coreRoot);
        if (indicador != null) UnityEngine.Object.Destroy(indicador.gameObject);
        rootA = rootB = rootC = fillRoot = arcoRoot = raioRoot = coreRoot = null;
        indicador = null;
    }
}

// ── Partícula self-managed ────────────────────────────────────────────────────────

public class ParticulaPortalFX : MonoBehaviour
{
    public void Iniciar(Vector2 vel, Color cor) => StartCoroutine(Mover(vel, cor));

    System.Collections.IEnumerator Mover(Vector2 vel, Color cor)
    {
        var sr   = GetComponent<SpriteRenderer>();
        float vida = UnityEngine.Random.Range(0.35f, 0.8f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel *= Mathf.Pow(0.87f, Time.deltaTime * 60f);
            transform.position = (Vector2)transform.position + vel * Time.deltaTime;
            if (sr != null) sr.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, t / vida));
            yield return null;
        }
        Destroy(gameObject);
    }
}
