using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SegundaChanceSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float porcentagemCura  = 0.3f;  // 30% do HP máximo
    float recarga          = 360f;  // 6 minutos
    float timerRecarga     = 0f;
    bool  emRecarga        = false;

    public bool  EmRecarga    => emRecarga;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => recarga;

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        if (data.specialValue > 0f)
            porcentagemCura = data.specialValue / 100f;
    }

    public override void ApplyEffect() { }

    void Update()
    {
        if (emRecarga)
        {
            timerRecarga -= Time.deltaTime;
            if (timerRecarga <= 0f)
                emRecarga = false;
        }
    }

    // Chamado pelo PlayerStats quando HP chega a 0
    public bool TentarReviver()
    {
        if (emRecarga || playerStats == null) return false;

        emRecarga    = true;
        timerRecarga = recarga;

        float cura = playerStats.maxHealth * porcentagemCura;
        playerStats.health = cura;

        StartCoroutine(EfeitoRessurreicao());
        return true;
    }

    IEnumerator EfeitoRessurreicao()
    {
        // Invulnerabilidade breve para não morrer logo em seguida
        playerStats.invulneravel = true;

        // Flash branco no player
        var sr = playerStats.GetComponent<SpriteRenderer>();

        // Tela flash dourado
        StartCoroutine(FlashTela());

        // Partículas douradas subindo
        StartCoroutine(ParticulasRessurreicao());

        // Texto na tela
        StartCoroutine(MostrarTexto("SEGUNDA CHANCE!", new Color(1f, 0.85f, 0.1f)));

        // Pisca o player 5 vezes
        for (int i = 0; i < 6; i++)
        {
            if (sr != null) sr.color = i % 2 == 0 ? new Color(1f, 0.9f, 0.1f) : Color.white;
            yield return new WaitForSecondsRealtime(0.12f);
        }
        if (sr != null) sr.color = Color.white;

        // Anel expansivo dourado no player
        StartCoroutine(AnelExpansivo());

        yield return new WaitForSecondsRealtime(1.5f);
        playerStats.invulneravel = false;
    }

    IEnumerator FlashTela()
    {
        var canvasGO = new GameObject("FlashSegundaChance");
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasGO.AddComponent<CanvasScaler>();

        var imgGO = new GameObject("Flash");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var rt = imgGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = imgGO.AddComponent<Image>();
        img.color = new Color(1f, 0.9f, 0.1f, 0f);
        img.raycastTarget = false;

        // Fade in rápido
        for (float t = 0f; t < 0.15f; t += Time.unscaledDeltaTime)
        {
            img.color = new Color(1f, 0.9f, 0.1f, Mathf.Lerp(0f, 0.7f, t / 0.15f));
            yield return null;
        }
        // Fade out
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            img.color = new Color(1f, 0.9f, 0.1f, Mathf.Lerp(0.7f, 0f, t / 0.5f));
            yield return null;
        }
        Destroy(canvasGO);
    }

    IEnumerator ParticulasRessurreicao()
    {
        for (int i = 0; i < 16; i++)
        {
            var go = new GameObject("PartRessur");
            go.transform.position = (Vector2)playerStats.transform.position
                + Random.insideUnitCircle * 0.8f;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = GerarDisco(8);
            sr2.color  = new Color(1f, 0.85f + Random.value * 0.15f, 0.1f);
            sr2.sortingOrder = 20;
            go.transform.localScale = Vector3.one * Random.Range(0.12f, 0.28f);
            Vector2 vel = (Vector2.up * Random.Range(1f, 3f)) + Random.insideUnitCircle * 1.5f;
            StartCoroutine(AnimarParticula(sr2, vel));
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator AnimarParticula(SpriteRenderer sr2, Vector2 vel)
    {
        Color cor = sr2.color;
        float vida = Random.Range(0.6f, 1.2f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel   += Vector2.up * Time.deltaTime * 1.5f;
            vel   *= Mathf.Pow(0.92f, Time.deltaTime * 60f);
            if (sr2 != null)
            {
                sr2.transform.position += (Vector3)(vel * Time.deltaTime);
                sr2.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, t / vida));
            }
            yield return null;
        }
        if (sr2 != null) Destroy(sr2.gameObject);
    }

    IEnumerator AnelExpansivo()
    {
        const int SEGS = 48;
        var go = new GameObject("AnelSegundaChance");
        go.transform.position = playerStats.transform.position;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 15;

        float dur = 0.6f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p    = t / dur;
            float raio = Mathf.Lerp(0.3f, 5f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.3f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(1f, 0.85f, 0.1f, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lr.SetPosition(i, (Vector2)playerStats.transform.position
                    + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator MostrarTexto(string msg, Color cor)
    {
        var go = new GameObject("TextoSegundaChance");
        DontDestroyOnLoad(go);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 201;
        go.AddComponent<CanvasScaler>();

        var txtGO = new GameObject("Texto");
        txtGO.transform.SetParent(go.transform, false);
        var rt = txtGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.5f); rt.anchorMax = new Vector2(0.9f, 0.7f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = msg;
        txt.fontSize  = 52;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = new Color(cor.r, cor.g, cor.b, 0f);

        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        { txt.color = new Color(cor.r, cor.g, cor.b, t / 0.3f); yield return null; }
        txt.color = new Color(cor.r, cor.g, cor.b, 1f);

        yield return new WaitForSecondsRealtime(1.5f);

        var r = txtGO.GetComponent<RectTransform>();
        Vector2 posBase = r.anchoredPosition;
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            txt.color = new Color(cor.r, cor.g, cor.b, 1f - t / 0.5f);
            r.anchoredPosition = posBase + Vector2.up * (t * 80f);
            yield return null;
        }
        Destroy(go);
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
