using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventoColapso : MonoBehaviour
{
    [Header("Zona")]
    public float raioInicial    = 22f;
    public float raioFinal      = 3.5f;
    public float danoForaDaZona = 20f;

    PlayerStats  player;
    float        duracao;
    float        elapsed;
    float        proxDano;
    float        raioAtual;

    LineRenderer anelPrincipal;
    LineRenderer anelInterno;
    GameObject   anelGO;
    GameObject   anelInternoGO;
    TextMeshProUGUI textoPerigo;
    Image           vignetaPerigo;
    float           tempoPerigo;

    static readonly Color COR_SEGURO  = new Color(0.15f, 1f,   0.3f,  0.9f);
    static readonly Color COR_PERIGO  = new Color(1f,    0.2f, 0.08f, 0.95f);
    static readonly Color COR_AVISO   = new Color(1f,    0.75f,0.05f, 0.9f);

    public void Iniciar(PlayerStats ps, Vector2 centro, float dur, Bounds bounds)
    {
        player    = ps;
        duracao   = dur;
        transform.position = new Vector3(centro.x, centro.y, 0f);

        // Começa cobrindo o mapa inteiro
        raioInicial = bounds.extents.magnitude * 1.1f;
        raioAtual   = raioInicial;

        CriarAneis();
        CriarTextoPerigo();
        StartCoroutine(SpawnPerigosExteriores());
        StartCoroutine(PulsarBorda());
        StartCoroutine(FaiscasBorda());
    }

    void Update()
    {
        if (player == null) return;

        elapsed += Time.deltaTime;

        // sqrt(t): fecha rápido no início e desacelera no fim
        float t = Mathf.Clamp01(elapsed / Mathf.Max(1f, duracao));
        raioAtual = Mathf.Lerp(raioInicial, raioFinal, Mathf.Sqrt(t));

        AtualizarAneis(t);

        float dist       = Vector2.Distance(player.transform.position, transform.position);
        bool  foraZona   = dist > raioAtual;

        if (foraZona)
        {
            proxDano -= Time.deltaTime;
            if (proxDano <= 0f)
            {
                player.TakeDamage(danoForaDaZona);
                proxDano = 1f;
                StartCoroutine(FlashPlayerPerigo());
            }
        }
        else
        {
            proxDano = 0f;
        }

        AtualizarTextoPerigo(foraZona);
    }

    public Vector2 Centro    => transform.position;
    public float   RaioAtual => raioAtual;
    public float   Progresso => Mathf.Sqrt(Mathf.Clamp01(elapsed / Mathf.Max(1f, duracao)));

    // ── Texto PERIGO ──────────────────────────────────────────────────────────

    void CriarTextoPerigo()
    {
        var canvasGO = new GameObject("PerigoCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas          = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>();

        // Vinheta vermelha nas bordas da tela
        var vigGO = new GameObject("Vinheta");
        vigGO.transform.SetParent(canvasGO.transform, false);
        var vigRT      = vigGO.AddComponent<RectTransform>();
        vigRT.anchorMin = Vector2.zero;
        vigRT.anchorMax = Vector2.one;
        vigRT.offsetMin = vigRT.offsetMax = Vector2.zero;
        vignetaPerigo       = vigGO.AddComponent<Image>();
        vignetaPerigo.sprite = GerarSpriteVinheta(128);
        vignetaPerigo.color  = new Color(1f, 0.05f, 0.02f, 0f);

        var go = new GameObject("TextoPerigo");
        go.transform.SetParent(canvasGO.transform, false);
        var rt             = go.AddComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0.5f, 0.7f);
        rt.anchorMax       = new Vector2(0.5f, 0.7f);
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = new Vector2(400f, 80f);
        rt.anchoredPosition = Vector2.zero;

        textoPerigo            = go.AddComponent<TextMeshProUGUI>();
        textoPerigo.text = Loc.T("event.danger");
        textoPerigo.fontSize   = 42f;
        textoPerigo.fontStyle  = FontStyles.Bold;
        textoPerigo.alignment  = TextAlignmentOptions.Center;
        textoPerigo.color      = new Color(1f, 0.15f, 0.05f, 0f);
    }

    void AtualizarTextoPerigo(bool foraZona)
    {
        if (!foraZona)
        {
            // Some rápido
            if (textoPerigo != null)
            {
                Color c = textoPerigo.color;
                c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 3f);
                textoPerigo.color = c;
            }
            if (vignetaPerigo != null)
            {
                Color c = vignetaPerigo.color;
                c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 3f);
                vignetaPerigo.color = c;
            }
            return;
        }

        // Pisca texto e vinheta
        tempoPerigo += Time.deltaTime * 4f;
        float alpha = Mathf.Abs(Mathf.Sin(tempoPerigo)) * 0.95f + 0.05f;

        if (textoPerigo != null)
        {
            textoPerigo.color = new Color(1f, 0.12f, 0.04f, alpha);
            float escala = 1f + Mathf.Sin(tempoPerigo * 1.5f) * 0.05f;
            textoPerigo.transform.localScale = Vector3.one * escala;
        }

        if (vignetaPerigo != null)
            vignetaPerigo.color = new Color(1f, 0.05f, 0.02f, alpha * 0.65f);
    }

    // ── Visuais ───────────────────────────────────────────────────────────────

    void CriarAneis()
    {
        anelGO = new GameObject("AnelColapso");
        anelGO.transform.SetParent(transform, false);
        anelPrincipal = CriarLR(anelGO, 64, COR_SEGURO, 0.22f, 10);

        anelInternoGO = new GameObject("AnelColapsоInt");
        anelInternoGO.transform.SetParent(transform, false);
        anelInterno = CriarLR(anelInternoGO, 48, new Color(0.15f, 1f, 0.3f, 0.25f), 0.06f, 9);

        AtualizarAneis(0f);
    }

    void AtualizarAneis(float t)
    {
        Color cor = t < 0.5f
            ? Color.Lerp(COR_SEGURO, COR_AVISO, t * 2f)
            : Color.Lerp(COR_AVISO,  COR_PERIGO, (t - 0.5f) * 2f);

        float larg = Mathf.Lerp(0.22f, 0.45f, t);
        SetAnel(anelPrincipal, raioAtual, cor, larg);

        Color corInt = cor; corInt.a = 0.18f + t * 0.12f;
        SetAnel(anelInterno, raioAtual * 0.97f, corInt, 0.06f);
    }

    void SetAnel(LineRenderer lr, float raio, Color cor, float larg)
    {
        if (lr == null) return;
        lr.startColor = lr.endColor = cor;
        lr.startWidth = lr.endWidth = larg;
        Vector2 centro = transform.position;
        for (int i = 0; i < lr.positionCount; i++)
        {
            float ang = i / (float)lr.positionCount * Mathf.PI * 2f;
            lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
        }
    }

    IEnumerator FaiscasBorda()
    {
        while (gameObject != null)
        {
            float t = Mathf.Clamp01(Mathf.Max(0f, elapsed - 5f) / Mathf.Max(1f, duracao - 5f));

            // Mais faíscas conforme a zona fecha
            float intervalo = Mathf.Lerp(0.12f, 0.03f, t);
            yield return new WaitForSeconds(intervalo);
            if (gameObject == null) yield break;

            Color cor = t < 0.5f
                ? Color.Lerp(COR_SEGURO, COR_AVISO,  t * 2f)
                : Color.Lerp(COR_AVISO,  COR_PERIGO, (t - 0.5f) * 2f);

            // Spawna 2-5 faíscas distribuídas pela borda
            int qtd = Mathf.RoundToInt(Mathf.Lerp(2f, 5f, t));
            for (int i = 0; i < qtd; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                Vector2 pos = (Vector2)transform.position
                    + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioAtual;

                var go  = new GameObject("F");
                go.transform.position = pos;
                var sr2 = go.AddComponent<SpriteRenderer>();
                sr2.sprite       = GerarDiscoSimples(5);
                sr2.color        = cor;
                sr2.sortingOrder = 11;
                go.transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f);

                // Dispara para dentro (em direção ao centro)
                Vector2 dirEntrada = ((Vector2)transform.position - pos).normalized;
                Vector2 lateral    = new Vector2(-dirEntrada.y, dirEntrada.x);
                Vector2 vel = dirEntrada * Random.Range(1.5f, 4f)
                            + lateral   * Random.Range(-1f, 1f);

                go.AddComponent<FaiscaFX>().Iniciar(vel, cor);
            }
        }
    }

    IEnumerator PulsarBorda()
    {
        while (gameObject != null)
        {
            yield return new WaitForSeconds(3.5f);
            if (gameObject == null) yield break;

            float t = Mathf.Clamp01(elapsed / duracao);
            Color cor = t < 0.5f
                ? Color.Lerp(COR_SEGURO, COR_AVISO, t * 2f)
                : Color.Lerp(COR_AVISO, COR_PERIGO, (t - 0.5f) * 2f);

            // Cria pulse GO independente para sobreviver ao Destroy desta component
            var go = new GameObject("ColapsoP");
            go.transform.position = transform.position;
            var lr = CriarLR(go, 48, cor, 0.35f, 11);
            go.AddComponent<ColapsoPulseFX>().Iniciar(raioAtual, cor, lr);
        }
    }

    IEnumerator SpawnPerigosExteriores()
    {
        while (gameObject != null)
        {
            yield return new WaitForSeconds(0.22f);
            if (gameObject == null) yield break;

            // Partícula na borda exterior
            float ang  = Random.Range(0f, Mathf.PI * 2f);
            float dist = raioAtual + Random.Range(0.5f, 2.5f);
            Vector2 pos = (Vector2)transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

            var go  = new GameObject("P");
            go.transform.position = pos;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite       = GerarDiscoSimples(6);
            sr2.color        = new Color(1f, 0.25f, 0.05f, 0.8f);
            sr2.sortingOrder = 7;
            go.transform.localScale = Vector3.one * Random.Range(0.12f, 0.28f);

            Vector2 dirInicio = ((Vector2)transform.position - pos).normalized;
            go.AddComponent<PerigoPFX>().Iniciar(dirInicio * Random.Range(0.8f, 2f));
        }
    }

    IEnumerator FlashPlayerPerigo()
    {
        var sr = player?.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color orig = sr.color;
        sr.color = new Color(1f, 0.2f, 0.1f);
        yield return new WaitForSeconds(0.12f);
        if (sr != null) sr.color = orig;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    LineRenderer CriarLR(GameObject go, int segs, Color cor, float larg, int order)
    {
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = segs;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = order;
        lr.startWidth    = lr.endWidth = larg;
        lr.startColor    = lr.endColor = cor;
        return lr;
    }

    // Gradiente radial: bordas opacas, centro transparente — para vinheta de tela
    static Sprite GerarSpriteVinheta(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float nx = (x - cx) / cx; // -1..1
            float ny = (y - cx) / cx;
            // Distância normalizada ao centro (0 = centro, 1 = canto)
            float d  = Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny));
            // Borda forte acima de 0.55, transparente abaixo de 0.4
            float a  = Mathf.SmoothStep(0.4f, 0.85f, d);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
    }

    static Sprite GerarDiscoSimples(int sz)
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

// Pulso visual independente — sobrevive à destruição do EventoColapso
public class ColapsoPulseFX : MonoBehaviour
{
    public void Iniciar(float raio, Color cor, LineRenderer lr) =>
        StartCoroutine(Animar(raio, cor, lr));

    IEnumerator Animar(float raio, Color cor, LineRenderer lr)
    {
        if (lr == null) { Destroy(gameObject); yield break; }

        Vector2 centro = transform.position;
        float   dur    = 0.75f;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (lr == null) { Destroy(gameObject); yield break; }
            float p = t / dur;
            float r = raio * (1f + p * 0.25f);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(0.85f, 0f, p));
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.4f, 0.04f, p);
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

// Faísca da borda da zona — self-managed
public class FaiscaFX : MonoBehaviour
{
    public void Iniciar(Vector2 vel, Color cor) => StartCoroutine(Mover(vel, cor));

    System.Collections.IEnumerator Mover(Vector2 vel, Color cor)
    {
        var sr   = GetComponent<SpriteRenderer>();
        float vida = Random.Range(0.25f, 0.55f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel   *= Mathf.Pow(0.80f, Time.deltaTime * 60f);
            transform.position = (Vector2)transform.position + vel * Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / vida);
            if (sr != null) sr.color = new Color(cor.r, cor.g, cor.b, alpha);
            yield return null;
        }
        Destroy(gameObject);
    }
}

// Partícula de perigo exterior — self-managed
public class PerigoPFX : MonoBehaviour
{
    public void Iniciar(Vector2 vel) => StartCoroutine(Mover(vel));

    IEnumerator Mover(Vector2 vel)
    {
        var sr   = GetComponent<SpriteRenderer>();
        float vida = Random.Range(0.6f, 1.2f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel   *= Mathf.Pow(0.85f, Time.deltaTime * 60f);
            transform.position = (Vector2)transform.position + vel * Time.deltaTime;
            if (sr != null) sr.color = new Color(1f, 0.25f, 0.05f, Mathf.Lerp(0.8f, 0f, t / vida));
            yield return null;
        }
        Destroy(gameObject);
    }
}
