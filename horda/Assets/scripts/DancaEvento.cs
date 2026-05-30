using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

public class DancaEvento : MonoBehaviour
{
    [HideInInspector] public int     quantidade  = 8;
    [HideInInspector] public float   tempoZona   = 40f;
    [HideInInspector] public float   raioZona    = 2.5f;
    [HideInInspector] public Tilemap terrenoBase;

    public event Action<int,int> OnProgresso; // acertos, quantidade

    PlayerStats player;
    int   acertos;
    bool  encerrado;
    bool  primeiroErro;

    Canvas          uiCanvas;
    TextMeshProUGUI textoSequencia;
    CanvasGroup     cgUI;
    float           flashTimer;
    Color           flashCor;

    static readonly Color COR_OK     = new Color(0.15f, 1f,   0.35f, 0.95f);
    static readonly Color COR_AVISO  = new Color(1f,    0.85f,0.1f,  0.95f);
    static readonly Color COR_PERIGO = new Color(1f,    0.2f, 0.1f,  0.95f);

    public void Iniciar(PlayerStats ps, Tilemap terreno)
    {
        player      = ps;
        terrenoBase = terreno;
        CriarUI();
        StartCoroutine(SequenciaZonas());
    }

    public void Encerrar() => encerrado = true;

    void Update()
    {
        if (cgUI == null) return;
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            cgUI.alpha  = 1f;
        }
        else
        {
            cgUI.alpha = Mathf.MoveTowards(cgUI.alpha, 0.7f, Time.deltaTime * 3f);
        }
    }

    void OnDestroy()
    {
        if (uiCanvas != null) Destroy(uiCanvas.gameObject);
    }

    // ── Sequência contínua ────────────────────────────────────────────────────────

    IEnumerator SequenciaZonas()
    {
        yield return new WaitForSeconds(1.2f);

        while (!encerrado)
        {
            Vector2 pos = EscolherPosicao();
            yield return StartCoroutine(ExecutarZona(pos));

            if (!encerrado)
                yield return new WaitForSeconds(0.6f);
        }
    }

    IEnumerator ExecutarZona(Vector2 pos)
    {
        var zona   = CriarZonaGO(pos);
        var lrRing = zona.transform.Find("Ring")?.GetComponent<LineRenderer>();
        var lrArco = zona.transform.Find("Arco")?.GetComponent<LineRenderer>();
        var srFill = zona.transform.Find("Fill")?.GetComponent<SpriteRenderer>();
        var srCore = zona.transform.Find("Core")?.GetComponent<SpriteRenderer>();
        var coreT  = zona.transform.Find("Core");

        // Indicador aparece imediatamente
        var alvoGO  = new GameObject("AlvoDanca");
        alvoGO.transform.position = pos;
        var indGO   = new GameObject("IndDanca");
        var indComp = indGO.AddComponent<IndicadorSlime>();
        indComp.alvo    = alvoGO.transform;
        indComp.corSeta = COR_OK;
        indComp.label   = "Zona!";

        // Aguarda 15 segundos antes de mostrar o círculo
        const float DELAY_CIRCULO = 15f;
        for (float t = 0f; t < DELAY_CIRCULO; t += Time.deltaTime)
        {
            if (encerrado) { Destroy(alvoGO); Destroy(indGO); yield break; }
            yield return null;
        }

        // Círculo aparece após 15s — tempo restante para o player chegar
        float tempoRestante = tempoZona - DELAY_CIRCULO;

        for (float t = 0f; t < tempoRestante; t += Time.deltaTime)
        {
            if (encerrado || zona == null) { if (zona != null) Destroy(zona); Destroy(alvoGO); Destroy(indGO); yield break; }

            float prog  = t / tempoRestante;
            bool  dentro = player != null && Vector2.Distance(player.transform.position, pos) <= raioZona;

            Color cor;
            if      (prog < 0.45f) cor = Color.Lerp(COR_OK,   COR_AVISO,  prog / 0.45f);
            else if (prog < 0.75f) cor = Color.Lerp(COR_AVISO, COR_PERIGO, (prog - 0.45f) / 0.3f);
            else                   cor = COR_PERIGO;

            float pulso = Mathf.Sin(t * (5f + prog * 5f)) * 0.5f + 0.5f;

            if (lrRing != null)
            {
                lrRing.startColor = lrRing.endColor =
                    new Color(cor.r, cor.g, cor.b, dentro ? 0.95f : 0.5f + pulso * 0.3f);
                lrRing.startWidth = lrRing.endWidth = 0.1f + pulso * 0.05f + (dentro ? 0.07f : 0f);
            }

            if (lrArco != null)
            {
                int segs = Mathf.Max(2, Mathf.RoundToInt((1f - prog) * 48));
                lrArco.positionCount = segs;
                for (int i = 0; i < segs; i++)
                {
                    float ang = i / (float)Mathf.Max(1, segs - 1) * 360f * Mathf.Deg2Rad;
                    lrArco.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (raioZona - 0.15f));
                }
                lrArco.startColor = lrArco.endColor = new Color(cor.r, cor.g, cor.b, 0.85f);
                lrArco.startWidth = lrArco.endWidth = 0.18f;
            }

            if (srFill != null)
                srFill.color = new Color(cor.r, cor.g, cor.b, dentro ? 0.18f : 0.05f);

            if (srCore != null && coreT != null)
            {
                srCore.color = dentro
                    ? new Color(cor.r, cor.g, cor.b, 0.7f + pulso * 0.3f)
                    : new Color(cor.r, cor.g, cor.b, 0.2f + pulso * 0.12f);
                coreT.localScale = Vector3.one * (0.3f + pulso * 0.1f + (dentro ? 0.15f : 0f));
            }

            yield return null;
        }

        Destroy(alvoGO);
        Destroy(indGO);
        if (zona == null || encerrado) yield break;
        Destroy(zona);

        bool acertou = player != null && Vector2.Distance(player.transform.position, pos) <= raioZona;

        if (acertou)
        {
            acertos++;
            OnProgresso?.Invoke(acertos, quantidade);
            AtualizarUISequencia();
            StartCoroutine(BurstFX(pos, COR_OK));
        }
        else
        {
            acertos     = 0;
            primeiroErro = true;
            OnProgresso?.Invoke(acertos, quantidade);
            AtualizarUISequencia();
            StartCoroutine(BurstFX(pos, COR_PERIGO));
            flashTimer = 0.5f;
        }
    }

    // ── Posição — mapa todo ───────────────────────────────────────────────────────

    Vector2 EscolherPosicao()
    {
        var ge = GerenciadorEventos.Instance;

        Vector2 min, max;
        if (terrenoBase != null)
        {
            terrenoBase.CompressBounds();
            min = terrenoBase.transform.TransformPoint(terrenoBase.localBounds.min);
            max = terrenoBase.transform.TransformPoint(terrenoBase.localBounds.max);
        }
        else
        {
            var cam = Camera.main;
            if (cam == null) goto fallback;
            min = cam.ViewportToWorldPoint(new Vector3(0.05f, 0.1f, 0));
            max = cam.ViewportToWorldPoint(new Vector3(0.95f, 0.9f, 0));
        }

        for (int t = 0; t < 60; t++)
        {
            Vector2 c = new Vector2(
                UnityEngine.Random.Range(min.x, max.x),
                UnityEngine.Random.Range(min.y, max.y));
            if (ge != null && !ge.PosicaoValida(c)) continue;
            return c;
        }

        fallback:
        Vector2 orig = player != null ? (Vector2)player.transform.position : Vector2.zero;
        return orig + (Vector2)(UnityEngine.Random.insideUnitCircle.normalized * 6f);
    }

    // ── Visuais da zona ───────────────────────────────────────────────────────────

    GameObject CriarZonaGO(Vector2 pos)
    {
        var root = new GameObject("ZonaDanca");
        root.transform.position = pos;

        const int SEGS = 48;

        var ringGO = new GameObject("Ring");
        ringGO.transform.SetParent(root.transform, false);
        var lrRing = ringGO.AddComponent<LineRenderer>();
        lrRing.useWorldSpace = true;
        lrRing.loop          = true;
        lrRing.positionCount = SEGS;
        lrRing.material      = new Material(Shader.Find("Sprites/Default"));
        lrRing.sortingOrder  = 8;
        for (int i = 0; i < SEGS; i++)
        {
            float ang = 360f / SEGS * i * Mathf.Deg2Rad;
            lrRing.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioZona);
        }

        var arcoGO = new GameObject("Arco");
        arcoGO.transform.SetParent(root.transform, false);
        var lrArco = arcoGO.AddComponent<LineRenderer>();
        lrArco.useWorldSpace = true;
        lrArco.loop          = false;
        lrArco.material      = new Material(Shader.Find("Sprites/Default"));
        lrArco.sortingOrder  = 9;

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(root.transform, false);
        fillGO.transform.position = pos;
        var srFill = fillGO.AddComponent<SpriteRenderer>();
        srFill.sprite       = GerarDisco(64);
        srFill.color        = new Color(COR_OK.r, COR_OK.g, COR_OK.b, 0.05f);
        srFill.sortingOrder = 6;
        fillGO.transform.localScale = Vector3.one * (raioZona * 2f);

        var coreGO = new GameObject("Core");
        coreGO.transform.SetParent(root.transform, false);
        coreGO.transform.position = pos;
        var srCore = coreGO.AddComponent<SpriteRenderer>();
        srCore.sprite       = GerarDisco(32);
        srCore.color        = new Color(COR_OK.r, COR_OK.g, COR_OK.b, 0.25f);
        srCore.sortingOrder = 10;
        coreGO.transform.localScale = Vector3.one * 0.32f;

        return root;
    }

    IEnumerator BurstFX(Vector2 pos, Color cor)
    {
        const int SEGS = 32;
        var go = new GameObject("BurstDanca");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 11;

        for (int i = 0; i < 10; i++)
        {
            float ang = i / 10f * Mathf.PI * 2f;
            var p = new GameObject("P");
            p.transform.position = pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioZona;
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = GerarDisco(6); sr.color = cor; sr.sortingOrder = 12;
            p.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.1f, 0.25f);
            var vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * UnityEngine.Random.Range(2f, 5f);
            p.AddComponent<DancaParticulaFX>().Iniciar(vel, cor);
        }

        float dur = 0.5f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float prog = t / dur;
            float raio = Mathf.Lerp(0.2f, raioZona * 1.5f, prog);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.2f, 0.02f, prog);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, prog));
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }


    // ── UI ────────────────────────────────────────────────────────────────────────

    void CriarUI()
    {
        var canvasGO = new GameObject("DancaUI");
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 98;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        uiCanvas = canvas;

        cgUI               = canvasGO.AddComponent<CanvasGroup>();
        cgUI.interactable   = false;
        cgUI.blocksRaycasts = false;
        cgUI.alpha          = 0.7f;

        var go = new GameObject("TextoSequencia");
        go.transform.SetParent(canvasGO.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(300f, 55f);
        rt.anchoredPosition = new Vector2(0f, 30f);

        textoSequencia              = go.AddComponent<TextMeshProUGUI>();
        textoSequencia.fontSize     = 26f;
        textoSequencia.fontStyle    = FontStyles.Bold;
        textoSequencia.alignment    = TextAlignmentOptions.Center;
        textoSequencia.enableWordWrapping = false;
        AtualizarUISequencia();
    }

    void AtualizarUISequencia()
    {
        if (textoSequencia == null) return;
        if (acertos == 0 && primeiroErro)
            textoSequencia.text = "<color=#FF4444>Sequência zerada!</color>";
        else if (acertos > 0)
            textoSequencia.text = $"<color=#33FF88>Sequência: {acertos}</color>";
        else
            textoSequencia.text = "";
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

public class DancaParticulaFX : MonoBehaviour
{
    public void Iniciar(Vector2 vel, Color cor) => StartCoroutine(Mover(vel, cor));

    System.Collections.IEnumerator Mover(Vector2 vel, Color cor)
    {
        var sr   = GetComponent<SpriteRenderer>();
        float vida = UnityEngine.Random.Range(0.3f, 0.6f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel *= Mathf.Pow(0.85f, Time.deltaTime * 60f);
            transform.position = (Vector2)transform.position + vel * Time.deltaTime;
            if (sr != null) sr.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, t / vida));
            yield return null;
        }
        Destroy(gameObject);
    }
}
