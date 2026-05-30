using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class TempestadeEletricaEvento : MonoBehaviour
{
    [HideInInspector] public float   danoJogador = 20f;
    [HideInInspector] public float   danoInimigo = 50f;
    [HideInInspector] public float   raioImpacto = 1.5f;
    [HideInInspector] public Tilemap terrenoBase;

    static readonly Color COR_AVISO_1 = new Color(1f, 0.85f, 0.1f,  0.55f);
    static readonly Color COR_AVISO_2 = new Color(1f, 0.25f, 0.05f, 0.65f);

    PlayerStats player;
    float duracao;
    float elapsed;

    CanvasGroup bordaCG;
    float tempoOscilacao;
    bool  flashAtivo;

    public void Iniciar(PlayerStats ps, float dur)
    {
        player  = ps;
        duracao = dur;
        CriarBordaEletrica();
        StartCoroutine(CicloRaios());
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (bordaCG == null || flashAtivo) return;
        tempoOscilacao += Time.deltaTime * 1.8f;
        bordaCG.alpha = 0.12f + Mathf.Sin(tempoOscilacao) * 0.06f;
    }

    // Intervalo varia aleatoriamente dentro de uma faixa que estreita com o tempo
    float IntervaloAtual()
    {
        float t   = Mathf.Clamp01(elapsed / duracao);
        float min = Mathf.Lerp(1.0f, 0.4f, t);
        float max = Mathf.Lerp(2.5f, 1.0f, t);
        return Random.Range(min, max);
    }

    // ── Ciclo principal ───────────────────────────────────────────────────────

    IEnumerator CicloRaios()
    {
        yield return new WaitForSeconds(2f);
        while (gameObject != null)
        {
            yield return new WaitForSeconds(IntervaloAtual());
            if (gameObject == null) yield break;

            // Quantidade de raios simultâneos aumenta com o tempo
            float t   = Mathf.Clamp01(elapsed / duracao);
            int   qtd = Mathf.RoundToInt(Mathf.Lerp(3f, 6f, t));
            qtd += Random.Range(-1, 2); // variação de ±1
            qtd  = Mathf.Max(2, qtd);
            for (int i = 0; i < qtd; i++)
            {
                Vector2? pos = EscolherPosicao();
                if (pos.HasValue)
                    StartCoroutine(SpawnRaio(pos.Value));
            }
        }
    }

    Vector2? EscolherPosicao()
    {
        var ge       = GerenciadorEventos.Instance;
        Vector2 centro = player != null ? (Vector2)player.transform.position : Vector2.zero;

        // Tenta dentro de um raio ao redor do player, com validação
        for (int t = 0; t < 60; t++)
        {
            Vector2 c = centro + Random.insideUnitCircle * 12f;
            if (ge != null && !ge.PosicaoValida(c)) continue;
            return c;
        }

        // Fallback sem validação
        return centro + Random.insideUnitCircle * 10f;
    }

    // ── Raio ──────────────────────────────────────────────────────────────────

    IEnumerator SpawnRaio(Vector2 pos)
    {
        // Círculo de aviso que pulsa e muda de cor antes do impacto
        var avisoGO = CriarCirculoAviso(pos, 2.5f);
        var avisoLR = avisoGO.GetComponent<LineRenderer>();
        float duracaoAviso = 1.1f;

        for (float t = 0f; t < duracaoAviso; t += Time.deltaTime)
        {
            if (avisoGO == null) yield break;
            float prog  = t / duracaoAviso;
            float pulso = Mathf.Sin(t * 15f) * 0.5f + 0.5f;
            Color cor   = Color.Lerp(
                new Color(COR_AVISO_1.r, COR_AVISO_1.g, COR_AVISO_1.b, 0.45f + pulso * 0.45f),
                new Color(COR_AVISO_2.r, COR_AVISO_2.g, COR_AVISO_2.b, 0.55f + pulso * 0.35f),
                prog);
            if (avisoLR != null) avisoLR.startColor = avisoLR.endColor = cor;
            yield return null;
        }

        if (avisoGO != null) Destroy(avisoGO);
        if (gameObject == null) yield break;

        AplicarDano(pos);
        StartCoroutine(AnimarRaio(pos));
        StartCoroutine(FlashBorda());
    }

    void AplicarDano(Vector2 pos)
    {
        if (player != null && Vector2.Distance(player.transform.position, pos) <= raioImpacto)
            player.TakeDamage(danoJogador);

        foreach (var c in Physics2D.OverlapCircleAll(pos, raioImpacto))
        {
            var ic = c.GetComponent<InimigoController>()
                  ?? c.GetComponentInParent<InimigoController>();
            if (ic != null) ic.ReceberDano(danoInimigo, false);
        }
    }

    IEnumerator AnimarRaio(Vector2 alvo)
    {
        Vector2 origem = alvo + Vector2.up * 10f;

        // Raio principal
        var pontos = GerarPontosRaio(origem, alvo, 8, 0.8f);
        var go     = CriarLineRenderer("RaioEvento", pontos, 0.25f, Color.white, 15);

        // Ramificações a partir de pontos aleatórios do raio principal
        for (int b = 0; b < 3; b++)
        {
            int   seg     = Random.Range(1, pontos.Length - 2);
            Vector2 raiz  = pontos[seg];
            Vector2 dir   = (Random.insideUnitCircle + Vector2.down * 0.5f).normalized;
            Vector2 ponta = raiz + dir * Random.Range(1.5f, 3.5f);
            var ptsBranch = GerarPontosRaio(raiz, ponta, 4, 0.4f);
            StartCoroutine(AnimarRamificacao(ptsBranch));
        }

        yield return null;

        // Fade do raio principal
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / 0.3f;
            var lr = go.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.startColor = lr.endColor =
                    Color.Lerp(new Color(1f, 0.95f, 0.3f, 1f), new Color(1f, 0.85f, 0.1f, 0f), p);
                lr.startWidth = lr.endWidth = Mathf.Lerp(0.22f, 0.02f, p);
            }
            yield return null;
        }
        if (go != null) Destroy(go);

        SpawnImpacto(alvo);
    }

    IEnumerator AnimarRamificacao(Vector2[] pontos)
    {
        var go = CriarLineRenderer("Ramo", pontos, 0.08f, new Color(0.85f, 0.95f, 1f, 0.9f), 14);
        yield return null;
        for (float t = 0f; t < 0.18f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            var lr = go.GetComponent<LineRenderer>();
            if (lr != null)
            {
                float p = t / 0.18f;
                lr.startColor = lr.endColor = new Color(1f, 0.9f, 0.2f, Mathf.Lerp(0.9f, 0f, p));
                lr.startWidth = lr.endWidth = Mathf.Lerp(0.07f, 0.01f, p);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    void SpawnImpacto(Vector2 pos)
    {
        // Faíscas em duas cores
        for (int i = 0; i < 12; i++)
        {
            Color cor = i < 6 ? new Color(1f, 0.95f, 0.3f) : Color.white;
            float vel = Random.Range(2f, 6f);
            var go    = new GameObject("SparkTempestade");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = GerarDisco(8);
            sr.color        = cor;
            sr.sortingOrder = 14;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);
            go.AddComponent<SparkTempestadeFX>().Iniciar(Random.insideUnitCircle * vel, cor);
        }

        // Anel expansivo no ponto de impacto
        StartCoroutine(AnelImpacto(pos));

        // Disco de brilho central
        StartCoroutine(GlowImpacto(pos));
    }

    IEnumerator AnelImpacto(Vector2 pos)
    {
        const int SEGS = 32;
        var go = new GameObject("AnelImpacto");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 13;

        float dur = 0.45f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p     = t / dur;
            float raio  = Mathf.Lerp(0.1f, 3.5f, p);
            float alpha = Mathf.Lerp(1f, 0f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.2f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(1f, 0.9f, 0.2f, alpha);
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator GlowImpacto(Vector2 pos)
    {
        var go = new GameObject("GlowImpacto");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarDisco(32);
        sr.color        = new Color(1f, 0.95f, 0.4f, 0.9f);
        sr.sortingOrder = 12;

        float dur = 0.35f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            float escala = Mathf.Lerp(0.2f, 2.5f, p);
            go.transform.localScale = Vector3.one * escala;
            if (sr != null) sr.color = new Color(1f, 0.95f, 0.4f, Mathf.Lerp(0.9f, 0f, p));
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ── Helpers de raio ───────────────────────────────────────────────────────

    Vector2[] GerarPontosRaio(Vector2 origem, Vector2 destino, int segs, float desvio)
    {
        var pts = new Vector2[segs];
        pts[0]        = origem;
        pts[segs - 1] = destino;
        for (int i = 1; i < segs - 1; i++)
        {
            float t = i / (float)(segs - 1);
            pts[i] = Vector2.Lerp(origem, destino, t) + Random.insideUnitCircle * desvio;
        }
        return pts;
    }

    GameObject CriarLineRenderer(string nome, Vector2[] pontos, float largura, Color cor, int order)
    {
        var go = new GameObject(nome);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = order;
        lr.positionCount = pontos.Length;
        lr.startWidth    = lr.endWidth = largura;
        lr.startColor    = lr.endColor = cor;
        for (int i = 0; i < pontos.Length; i++)
            lr.SetPosition(i, pontos[i]);
        return go;
    }

    // ── Visuais ───────────────────────────────────────────────────────────────

    IEnumerator FlashBorda()
    {
        if (bordaCG == null) yield break;
        flashAtivo    = true;
        bordaCG.alpha = 0.75f;
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            if (bordaCG == null) { flashAtivo = false; yield break; }
            bordaCG.alpha = Mathf.Lerp(0.75f, 0.12f, t / 0.35f);
            yield return null;
        }
        flashAtivo = false;
    }

    GameObject CriarCirculoAviso(Vector2 pos, float raio)
    {
        const int SEGS = 32;
        var root = new GameObject("AvisoRaio");
        root.transform.position = pos;

        // Borda
        var lr = root.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 10;
        lr.startWidth    = lr.endWidth = 0.1f;
        lr.startColor    = lr.endColor = COR_AVISO_1;
        for (int i = 0; i < SEGS; i++)
        {
            float ang = 360f / SEGS * i * Mathf.Deg2Rad;
            lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
        }

        // Fill semitransparente
        var fill = new GameObject("Fill");
        fill.transform.SetParent(root.transform, false);
        var sr = fill.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarDisco(64);
        sr.color        = new Color(1f, 0.85f, 0.1f, 0.08f);
        sr.sortingOrder = 9;
        float escala    = raio * 2f / (64f / 100f);
        fill.transform.localScale = Vector3.one * escala;

        return root;
    }

    void CriarBordaEletrica()
    {
        var canvasGO = new GameObject("TempestadeCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas        = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        canvasGO.AddComponent<CanvasScaler>();

        bordaCG                = canvasGO.AddComponent<CanvasGroup>();
        bordaCG.alpha          = 0f;
        bordaCG.interactable   = false;
        bordaCG.blocksRaycasts = false;

        var imgGO = new GameObject("BordaEletrica");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var rt       = imgGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img          = imgGO.AddComponent<Image>();
        img.sprite       = GerarSpriteVinheta(128, new Color(0.9f, 0.75f, 0.05f));
        img.raycastTarget = false;
    }

    // ── Helpers de textura ────────────────────────────────────────────────────

    static Sprite GerarSpriteVinheta(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float nx = Mathf.Abs((x - cx) / cx);
            float ny = Mathf.Abs((y - cx) / cx);
            float d  = Mathf.Max(nx, ny);
            float a  = Mathf.SmoothStep(0.38f, 0.88f, d) * 0.72f;
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
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
}

// Partícula de faísca do impacto — self-managed
public class SparkTempestadeFX : MonoBehaviour
{
    public void Iniciar(Vector2 vel, Color cor) => StartCoroutine(Mover(vel, cor));

    System.Collections.IEnumerator Mover(Vector2 vel, Color cor)
    {
        var sr   = GetComponent<SpriteRenderer>();
        float vida = Random.Range(0.2f, 0.45f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel *= Mathf.Pow(0.82f, Time.deltaTime * 60f);
            transform.position = (Vector2)transform.position + vel * Time.deltaTime;
            if (sr != null) sr.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, t / vida));
            yield return null;
        }
        Destroy(gameObject);
    }
}
