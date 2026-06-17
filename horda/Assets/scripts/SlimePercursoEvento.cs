using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

public class SlimePercursoEvento : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade         = 1.5f;
    public float toleranciaWaypoint = 0.6f;
    public RuntimeAnimatorController controllerSlime;

    [Header("Onda de Dano")]
    public float danoOnda      = 5f;
    public float intervaloOnda = 7f;

    [Header("Onda de Cura")]
    public float curaOnda        = 15f;
    public float intervaloCura   = 7f;
    public float raioOndaCura    = 8f;
    public float forcaRepulsao   = 12f;

    // A* grid
    const float PASSO    = 1.5f;
    const float RAIO_OBS = 2.2f;
    const int   MAX_ITER = 8000;

    List<Vector2> caminho = new List<Vector2>();
    int wpIdx;

    Rigidbody2D    rb;
    SpriteRenderer sr;
    LineRenderer   linha;
    GameObject     marcadorGO;
    Light2D        marcadorLuz;
    SpriteRenderer marcadorSR;

    // Barra de vida HUD
    GameObject        barraVidaCanvasGO;
    Image             barraFillImg;
    TextMeshProUGUI   textoVida;
    InimigoController inimigoCtrl;

    public bool Chegou { get; private set; }
    bool morreu;
    public event System.Action OnChegou;
    public event System.Action OnMorreu;

    float proximoContatoInimigo;
    const float INTERVALO_CONTATO_INIMIGO = 1.2f;
    const float DANO_POR_INIMIGO          = 80f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (controllerSlime != null)
        {
            var anim = GetComponent<Animator>();
            if (anim != null) anim.runtimeAnimatorController = controllerSlime;
        }
        inimigoCtrl = GetComponent<InimigoController>();
        if (inimigoCtrl != null) inimigoCtrl.imuneAoDano = true;
        AdicionarBrilho();
        CriarBarraVida();
        StartCoroutine(OndaDano());
        StartCoroutine(OndaCura());
    }

    public void IniciarPercurso(Vector2 destino)
    {
        caminho = SuavizarCaminho(CalcularAStar((Vector2)transform.position, destino));
        wpIdx   = 0;
        CriarLinha();
        CriarMarcador(destino);
        AtualizarLinha();
        StartCoroutine(PulsarMarcador());
    }

    // ─── Movimento ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (Chegou || morreu || caminho.Count == 0) return;

        if (wpIdx < caminho.Count &&
            Vector2.Distance(transform.position, caminho[wpIdx]) < toleranciaWaypoint)
            wpIdx++;

        if (wpIdx >= caminho.Count)
        {
            Chegou = true;
            rb.linearVelocity = Vector2.zero;
            DestruirVisuais();
            OnChegou?.Invoke();
            return;
        }
        AtualizarLinha();
        AtualizarBarraVida();
    }

    void FixedUpdate()
    {
        if (Chegou || morreu || wpIdx >= caminho.Count) return;
        Vector2 dir = ((Vector2)caminho[wpIdx] - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * velocidade;
        if (sr != null)
        {
            if      (dir.x >  0.05f) sr.flipX = false;
            else if (dir.x < -0.05f) sr.flipX = true;
        }
    }

    void OnDestroy()
    {
        // Não dispara OnMorreu se o unload da cena está destruindo o objeto
        if (!Chegou && !morreu && gameObject.scene.isLoaded)
        {
            morreu = true;
            OnMorreu?.Invoke();
        }
        DestruirVisuais();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (Chegou || morreu) return;
        if (!other.CompareTag("Enemy") || other.gameObject == gameObject) return;
        if (Time.time < proximoContatoInimigo) return;

        proximoContatoInimigo = Time.time + INTERVALO_CONTATO_INIMIGO;

        if (inimigoCtrl == null) return;
        inimigoCtrl.vidaAtual = Mathf.Max(0f, inimigoCtrl.vidaAtual - DANO_POR_INIMIGO);
        AtualizarBarraVida();

        // Flash vermelho
        if (sr != null) StartCoroutine(FlashDano());

        if (inimigoCtrl.vidaAtual <= 0f)
        {
            morreu = true;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            DestruirVisuais();
            OnMorreu?.Invoke();
            Destroy(gameObject);
        }
    }

    IEnumerator FlashDano()
    {
        Color orig = sr.color;
        sr.color = Color.red;
        yield return new WaitForSeconds(0.12f);
        if (sr != null) sr.color = orig;
    }

    void DestruirVisuais()
    {
        if (linha            != null && linha.gameObject != null) Destroy(linha.gameObject);
        if (marcadorGO       != null)                             Destroy(marcadorGO);
        if (barraVidaCanvasGO != null)                            Destroy(barraVidaCanvasGO);
        linha = null; marcadorGO = null; barraVidaCanvasGO = null;
    }

    // ─── A* ────────────────────────────────────────────────────────────────────

    List<Vector2> CalcularAStar(Vector2 inicio, Vector2 fim)
    {
        var startC = V2I(inicio);
        var endC   = V2I(fim);
        var open   = new List<No>();
        var closed = new HashSet<Vector2Int>();
        var map    = new Dictionary<Vector2Int, No>();

        open.Add(new No(startC, 0f, H(startC, endC), null));
        map[startC] = open[0];

        int[] dx = { 1,-1, 0, 0, 1, 1,-1,-1 };
        int[] dy = { 0, 0, 1,-1, 1,-1, 1,-1 };

        int iter = 0;
        while (open.Count > 0 && iter++ < MAX_ITER)
        {
            int mi = 0;
            for (int i = 1; i < open.Count; i++)
                if (open[i].F < open[mi].F) mi = i;

            var cur = open[mi];
            open.RemoveAt(mi);
            if (cur.P == endC) return Reconstruir(cur);
            closed.Add(cur.P);

            for (int i = 0; i < 8; i++)
            {
                var nb = new Vector2Int(cur.P.x + dx[i], cur.P.y + dy[i]);
                if (closed.Contains(nb)) continue;
                if (!Livre(V2W(nb)))     continue;
                if (i >= 4 && (!Livre(V2W(new Vector2Int(cur.P.x + dx[i], cur.P.y))) ||
                               !Livre(V2W(new Vector2Int(cur.P.x, cur.P.y + dy[i]))))) continue;

                float g = cur.G + (i < 4 ? PASSO : PASSO * 1.41f);
                if (map.TryGetValue(nb, out var ex))
                {
                    if (g < ex.G) { ex.G = g; ex.Parent = cur; }
                }
                else
                {
                    var n = new No(nb, g, H(nb, endC), cur);
                    open.Add(n); map[nb] = n;
                }
            }
        }
        Debug.LogWarning($"[SlimePercurso] A* atingiu MAX_ITER={MAX_ITER}. Usando rota direta ({inicio}→{fim}) — pode atravessar paredes.");
        return new List<Vector2> { inicio, fim };
    }

    List<Vector2> Reconstruir(No n)
    {
        var p = new List<Vector2>();
        while (n != null) { p.Add(V2W(n.P)); n = n.Parent; }
        p.Reverse(); return p;
    }

    List<Vector2> SuavizarCaminho(List<Vector2> path)
    {
        if (path.Count <= 2) return path;
        var s = new List<Vector2> { path[0] };
        int i = 0;
        while (i < path.Count - 1)
        {
            int next = i + 1;
            for (int j = path.Count - 1; j > i + 1; j--)
            {
                var hit = Physics2D.CircleCast(path[i], RAIO_OBS * 0.5f,
                    (path[j] - path[i]).normalized,
                    Vector2.Distance(path[i], path[j]), MascaraObs());
                if (hit.collider == null) { next = j; break; }
            }
            i = next;
            s.Add(path[i]);
        }
        return s;
    }

    bool Livre(Vector2 pos)
    {
        if (Physics2D.OverlapCircle(pos, RAIO_OBS, MascaraObs()) != null) return false;
        var ge = GerenciadorEventos.Instance;
        return ge == null || ge.PosicaoValida(pos);
    }

    int MascaraObs()
    {
        var ge = GerenciadorEventos.Instance;
        return (ge != null && ge.camadasObstaculo != 0) ? (int)ge.camadasObstaculo : (1 << 3);
    }

    float      H(Vector2Int a, Vector2Int b)  => Vector2Int.Distance(a, b) * PASSO;
    Vector2Int V2I(Vector2 w)                 => new Vector2Int(Mathf.RoundToInt(w.x / PASSO), Mathf.RoundToInt(w.y / PASSO));
    Vector2    V2W(Vector2Int c)              => new Vector2(c.x * PASSO, c.y * PASSO);

    class No
    {
        public Vector2Int P; public float G, H; public No Parent;
        public float F => G + H;
        public No(Vector2Int p, float g, float h, No par) { P=p; G=g; H=h; Parent=par; }
    }

    // ─── Visual ────────────────────────────────────────────────────────────────

    void AdicionarBrilho()
    {
        var go  = new GameObject("BrilhoSlimePercurso");
        go.transform.SetParent(transform, false);
        var luz = go.AddComponent<Light2D>();
        luz.lightType             = Light2D.LightType.Point;
        luz.color                 = new Color(0.2f, 1f, 0.5f);
        luz.intensity             = 2f;
        luz.pointLightOuterRadius = 3f;
        luz.pointLightInnerRadius = 0.5f;
        luz.shadowsEnabled        = false;
    }

    void CriarLinha()
    {
        var go = new GameObject("CaminhoSlimePercurso");
        linha  = go.AddComponent<LineRenderer>();
        linha.material       = new Material(Shader.Find("Sprites/Default"));
        linha.startColor     = new Color(0.2f, 1f, 0.45f, 0.9f);
        linha.endColor       = new Color(0.2f, 1f, 0.45f, 0.15f);
        linha.startWidth     = 0.4f;
        linha.endWidth       = 0.1f;
        linha.sortingOrder   = 10;
        linha.useWorldSpace  = true;
        linha.numCapVertices = 4;
    }

    void AtualizarLinha()
    {
        if (linha == null || caminho.Count == 0) return;
        int rest = Mathf.Max(0, caminho.Count - wpIdx);
        if (rest == 0) { linha.positionCount = 0; return; }
        linha.positionCount = rest + 1;
        linha.SetPosition(0, (Vector3)transform.position + Vector3.back * 0.1f);
        for (int i = 0; i < rest; i++)
            linha.SetPosition(i + 1, new Vector3(caminho[wpIdx + i].x, caminho[wpIdx + i].y, -0.1f));
    }

    void CriarMarcador(Vector2 pos)
    {
        marcadorGO = new GameObject("MarcadorDestinoSlime");
        marcadorGO.transform.position = new Vector3(pos.x, pos.y, 0f);

        marcadorSR               = marcadorGO.AddComponent<SpriteRenderer>();
        marcadorSR.sprite        = CriarSpriteAnel();
        marcadorSR.color         = new Color(0.2f, 1f, 0.45f, 0.8f);
        marcadorSR.sortingOrder  = 5;
        marcadorGO.transform.localScale = Vector3.one * 4f;

        var luzGO   = new GameObject("LuzDestino");
        luzGO.transform.SetParent(marcadorGO.transform, false);
        marcadorLuz = luzGO.AddComponent<Light2D>();
        marcadorLuz.lightType             = Light2D.LightType.Point;
        marcadorLuz.color                 = new Color(0.2f, 1f, 0.45f);
        marcadorLuz.intensity             = 3.5f;
        marcadorLuz.pointLightOuterRadius = 6f;
        marcadorLuz.pointLightInnerRadius = 1f;
        marcadorLuz.shadowsEnabled        = false;
    }

    Sprite CriarSpriteAnel()
    {
        const int S = 128;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;
        float c = S / 2f;
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float d  = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            float nr = d / c;
            float a  = Mathf.SmoothStep(0.55f, 0.72f, nr) * (1f - Mathf.SmoothStep(0.85f, 1f, nr));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
    }

    IEnumerator PulsarMarcador()
    {
        float t = 0f;
        while (marcadorGO != null && !Chegou && !morreu)
        {
            t += Time.deltaTime * 2.5f;
            float p = Mathf.Sin(t);
            if (marcadorLuz != null) marcadorLuz.intensity               = 3f + p * 1.2f;
            if (marcadorGO  != null) marcadorGO.transform.localScale     = Vector3.one * (4f + p * 0.4f);
            if (marcadorSR  != null) marcadorSR.color = new Color(0.2f, 1f, 0.45f, 0.65f + p * 0.15f);
            yield return null;
        }
    }

    // ─── Barra de Vida ─────────────────────────────────────────────────────────

    void CriarBarraVida()
    {
        barraVidaCanvasGO = new GameObject("BarraVidaSlimePercurso");
        var canvas = barraVidaCanvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 85;
        var scaler = barraVidaCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        barraVidaCanvasGO.AddComponent<GraphicRaycaster>();

        // Container posicionado na base central da tela
        var cGO = new GameObject("Container");
        cGO.transform.SetParent(barraVidaCanvasGO.transform, false);
        var cRT = cGO.AddComponent<RectTransform>();
        cRT.anchorMin        = new Vector2(0.5f, 1f);
        cRT.anchorMax        = new Vector2(0.5f, 1f);
        cRT.pivot            = new Vector2(0.5f, 1f);
        cRT.sizeDelta        = new Vector2(440f, 58f);
        cRT.anchoredPosition = new Vector2(0f, -20f);

        // Fundo escuro
        var bg = CriarImagem(cGO, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.04f, 0.06f, 0.04f, 0.88f));

        // Borda verde fina
        CriarImagem(cGO, "Borda", Vector2.zero, Vector2.one,
            new Vector2(-2f, -2f), new Vector2(2f, 2f),
            new Color(0.2f, 1f, 0.45f, 0.5f));

        // Label com nome
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(cGO.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin        = new Vector2(0f, 0.54f);
        labelRT.anchorMax        = new Vector2(1f, 1f);
        labelRT.offsetMin        = new Vector2(8f, 0f);
        labelRT.offsetMax        = new Vector2(-8f, -3f);
        textoVida                = labelGO.AddComponent<TextMeshProUGUI>();
        textoVida.text           = Loc.T("mob.slime_protetora");
        textoVida.fontSize       = 15f;
        textoVida.fontStyle      = FontStyles.Bold;
        textoVida.color          = new Color(0.2f, 1f, 0.45f);
        textoVida.alignment      = TextAlignmentOptions.Center;
        textoVida.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        // Track (fundo da barra)
        var trackGO = new GameObject("Track");
        trackGO.transform.SetParent(cGO.transform, false);
        var trackRT = trackGO.AddComponent<RectTransform>();
        trackRT.anchorMin = new Vector2(0f, 0f);
        trackRT.anchorMax = new Vector2(1f, 0.5f);
        trackRT.offsetMin = new Vector2(8f, 5f);
        trackRT.offsetMax = new Vector2(-8f, 0f);
        var trackImg = trackGO.AddComponent<Image>();
        trackImg.color = new Color(0f, 0.12f, 0.04f, 1f);

        // Fill (barra de HP)
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(trackGO.transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(1f, 1f);
        fillRT.offsetMax = new Vector2(-1f, -1f);
        barraFillImg             = fillGO.AddComponent<Image>();
        barraFillImg.color       = new Color(0.2f, 1f, 0.45f);
        barraFillImg.type        = Image.Type.Filled;
        barraFillImg.fillMethod  = Image.FillMethod.Horizontal;
        barraFillImg.fillAmount  = 1f;
    }

    Image CriarImagem(GameObject pai, string nome, Vector2 anchorMin, Vector2 anchorMax,
                      Vector2 offsetMin, Vector2 offsetMax, Color cor)
    {
        var go  = new GameObject(nome);
        go.transform.SetParent(pai.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        var img = go.AddComponent<Image>();
        img.color = cor;
        return img;
    }

    void AtualizarBarraVida()
    {
        if (inimigoCtrl == null || barraFillImg == null) return;
        float pct = inimigoCtrl.vidaMaxima > 0
            ? Mathf.Clamp01(inimigoCtrl.vidaAtual / inimigoCtrl.vidaMaxima)
            : 1f;
        barraFillImg.fillAmount = pct;
        barraFillImg.color      = Color.Lerp(new Color(1f, 0.3f, 0.1f), new Color(0.2f, 1f, 0.45f), pct);
        if (textoVida != null)
            textoVida.text = $"{Loc.T("mob.slime_protetora")}   {inimigoCtrl.vidaAtual:F0} / {inimigoCtrl.vidaMaxima:F0}";
    }

    // ─── Dano ──────────────────────────────────────────────────────────────────

    IEnumerator OndaDano()
    {
        yield return new WaitForSeconds(intervaloOnda);
        while (!Chegou && !morreu)
        {
            var ps = FindFirstObjectByType<PlayerStats>();
            if (ps != null) ps.TakeDamage(danoOnda);
            yield return new WaitForSeconds(intervaloOnda);
        }
    }

    IEnumerator OndaCura()
    {
        yield return new WaitForSeconds(intervaloCura);
        while (!Chegou && !morreu)
        {
            EmitirOndaCura();
            yield return new WaitForSeconds(intervaloCura);
        }
    }

    void EmitirOndaCura()
    {
        Vector2 centro = transform.position;

        // Cura o player se estiver no raio
        var ps = FindFirstObjectByType<PlayerStats>();
        if (ps != null && Vector2.Distance(centro, ps.transform.position) <= raioOndaCura)
            ps.Heal(curaOnda);

        // Cura a própria slime
        if (inimigoCtrl != null)
        {
            inimigoCtrl.vidaAtual = Mathf.Min(inimigoCtrl.vidaMaxima, inimigoCtrl.vidaAtual + curaOnda * 2f);
            AtualizarBarraVida();
        }

        // Repele inimigos no raio
        var hits = Physics2D.OverlapCircleAll(centro, raioOndaCura, LayerMask.GetMask("Enemy"));
        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;
            var rbInimigo = col.GetComponent<Rigidbody2D>();
            if (rbInimigo == null) continue;
            Vector2 dir = ((Vector2)col.transform.position - centro);
            if (dir.sqrMagnitude < 0.01f) dir = Random.insideUnitCircle.normalized;
            else dir.Normalize();
            rbInimigo.AddForce(dir * forcaRepulsao, ForceMode2D.Impulse);
        }

        // Onda visual — roda no próprio GO para sobreviver à destruição da slime
        var ondaGO = new GameObject("OndaCuraPercurso");
        ondaGO.transform.position = centro;
        var lr = ondaGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = 48;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 12;
        ondaGO.AddComponent<OndaCuraFX>().Iniciar(raioOndaCura);
    }
}

public class OndaCuraFX : MonoBehaviour
{
    public void Iniciar(float raio) => StartCoroutine(Animar(raio));

    System.Collections.IEnumerator Animar(float raio)
    {
        var lr     = GetComponent<LineRenderer>();
        var centro = (Vector2)transform.position;
        float dur  = 0.7f;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (lr == null) { Destroy(gameObject); yield break; }
            float p = t / dur;
            float r = Mathf.Lerp(0.3f, raio, p);
            lr.startColor = lr.endColor = new Color(0.2f, 1f, 0.5f, Mathf.Lerp(0.9f, 0f, p));
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.5f, 0.05f, p);
            for (int i = 0; i < 48; i++)
            {
                float ang = i / 48f * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
