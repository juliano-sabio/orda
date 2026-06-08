using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Coloque num GameObject vazio na cena "loading_screen".
// A cena anterior salva o destino em PlayerPrefs["ProximaCena"].
public class LoadingScreenUI : MonoBehaviour
{
    [Header("Tempo mínimo na tela (segundos)")]
    public float tempoMinimo = 2.0f;

    [Header("Assets")]
    public Sprite spriteFundo;

    static readonly Color corFundo  = new Color(0.03f, 0.01f, 0.01f);
    static readonly Color corAcento = new Color(0.55f, 0.08f, 0.08f);
    static readonly Color corClaro  = new Color(0.90f, 0.45f, 0.45f);

    RectTransform   barraFillRT;
    TextMeshProUGUI txtPorcentagem;
    TextMeshProUGUI txtDica;
    TextMeshProUGUI txtCarregando;
    RectTransform   spinnerRT;
    GameObject      canvasGO;

    static readonly string[] dicas =
    {
        "Desvie dos inimigos para sobreviver mais tempo.",
        "Colete XP para subir de nível e ganhar habilidades.",
        "Use a ultimate no momento certo para virar o jogo.",
        "Inimigos mais fortes aparecem em ondas posteriores.",
        "Combine skills para potencializar seus ataques.",
        "O dash tem invencibilidade nos primeiros frames.",
        "Personagens de Fogo causam efeito de queimadura.",
        "Observe os padrões de ataque dos chefes.",
    };

    // ─────────────────────────────────────────────────────────────────
    void Start()
    {
        canvasGO = CriarCanvas();
#if UNITY_EDITOR
        if (spriteFundo == null)
            spriteFundo = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/assets/UI/charselection/bg_charselection.png");
#endif
        CriarFundo();
        CriarConteudo();

        string proxima = PlayerPrefs.GetString("ProximaCena", "primeira_fase");
        if (txtCarregando != null)
            txtCarregando.text = NomeFase(proxima).ToUpper();

        StartCoroutine(AnimarPontos());
        StartCoroutine(AnimarSpinner());
        StartCoroutine(AnimarDicas());
        StartCoroutine(Carregar(proxima));
    }

    // ── Canvas ────────────────────────────────────────────────────────
    GameObject CriarCanvas()
    {
        var go = new GameObject("Canvas_Loading");
        var c  = go.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 99;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    // ── Fundo limpo ───────────────────────────────────────────────────
    void CriarFundo()
    {
        // imagem de fundo
        var fundoGO  = Img("Fundo", Color.white);
        Esticar(fundoGO);
        var fundoImg = fundoGO.GetComponent<Image>();
        if (spriteFundo != null)
        {
            fundoImg.sprite = spriteFundo;
            fundoImg.type   = Image.Type.Simple;
            fundoImg.preserveAspect = false;
        }
        else
        {
            fundoImg.color = corFundo;
        }

        // overlay escuro para legibilidade
        var ov = Img("Overlay", new Color(0f, 0f, 0f, 0.55f));
        Esticar(ov);

        // vinheta nas bordas (gradiente simulado com imagem escura nas extremidades)
        var vig = Img("Vinheta", new Color(0f, 0f, 0f, 0.50f));
        var rVig = vig.GetComponent<RectTransform>();
        rVig.anchorMin = Vector2.zero; rVig.anchorMax = Vector2.one;
        rVig.offsetMin = new Vector2(-60f, -60f); rVig.offsetMax = new Vector2(60f, 60f);

        // faixa topo com accent
        var topo = Img("Topo", new Color(corAcento.r, corAcento.g, corAcento.b, 0.20f));
        var rt   = topo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.90f); rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // linha decorativa topo
        var lnTopo = Img("LnTopo", new Color(corAcento.r, corAcento.g, corAcento.b, 0.70f));
        var rlT    = lnTopo.GetComponent<RectTransform>();
        rlT.anchorMin = new Vector2(0f, 0.90f); rlT.anchorMax = new Vector2(1f, 0.90f);
        rlT.offsetMin = Vector2.zero; rlT.offsetMax = new Vector2(0f, 2f);

        // faixa rodapé
        var bot = Img("Bot", new Color(0f, 0f, 0f, 0.65f));
        var rb  = bot.GetComponent<RectTransform>();
        rb.anchorMin = Vector2.zero; rb.anchorMax = new Vector2(1f, 0.10f);
        rb.offsetMin = rb.offsetMax = Vector2.zero;

        // linha decorativa rodapé
        var lnBot = Img("LnBot", new Color(corAcento.r, corAcento.g, corAcento.b, 0.70f));
        var rlB   = lnBot.GetComponent<RectTransform>();
        rlB.anchorMin = new Vector2(0f, 0.10f); rlB.anchorMax = new Vector2(1f, 0.10f);
        rlB.offsetMin = Vector2.zero; rlB.offsetMax = new Vector2(0f, 2f);
    }

    // ── Conteúdo central ─────────────────────────────────────────────
    void CriarConteudo()
    {
        // nome da fase no topo
        txtCarregando = Texto("TxtFase",
            new Vector2(0f, 0.90f), Vector2.one,
            "", 30f, FontStyles.Bold, Color.white);
        txtCarregando.alignment = TextAlignmentOptions.Center;

        // spinner — 12 pontos em arco
        var spinGO = new GameObject("Spinner");
        spinGO.transform.SetParent(canvasGO.transform, false);
        spinnerRT = spinGO.AddComponent<RectTransform>();
        spinnerRT.anchorMin = spinnerRT.anchorMax = new Vector2(0.5f, 0.56f);
        spinnerRT.pivot     = new Vector2(0.5f, 0.5f);
        spinnerRT.sizeDelta = new Vector2(70f, 70f);

        for (int i = 0; i < 12; i++)
        {
            float ang   = i * 30f;
            float alpha = Mathf.Lerp(0.08f, 1.00f, i / 11f);
            float rad   = ang * Mathf.Deg2Rad;
            float sz    = Mathf.Lerp(5f, 10f, i / 11f);

            var dot = new GameObject($"D{i}");
            dot.transform.SetParent(spinGO.transform, false);
            var rd = dot.AddComponent<RectTransform>();
            rd.anchorMin = rd.anchorMax = new Vector2(0.5f, 0.5f);
            rd.pivot     = new Vector2(0.5f, 0.5f);
            rd.anchoredPosition = new Vector2(Mathf.Sin(rad) * 28f, Mathf.Cos(rad) * 28f);
            rd.sizeDelta = new Vector2(sz, sz);
            dot.AddComponent<Image>().color = new Color(corClaro.r, corClaro.g, corClaro.b, alpha);
        }

        // label "CARREGANDO" abaixo do spinner
        Texto("LblCarregando",
            new Vector2(0.30f, 0.46f), new Vector2(0.70f, 0.52f),
            "CARREGANDO", 13f, FontStyles.Bold,
            new Color(corClaro.r, corClaro.g, corClaro.b, 0.80f))
            .alignment = TextAlignmentOptions.Center;

        // linha decorativa acima da barra
        var lnAcima = Img("LnBarra", new Color(corAcento.r, corAcento.g, corAcento.b, 0.40f));
        var rlA = lnAcima.GetComponent<RectTransform>();
        rlA.anchorMin = new Vector2(0.15f, 0.425f); rlA.anchorMax = new Vector2(0.85f, 0.425f);
        rlA.offsetMin = Vector2.zero; rlA.offsetMax = new Vector2(0f, 1f);

        // barra de progresso — fundo
        var fundoBarra = Img("BarraFundo", new Color(0.08f, 0.03f, 0.03f, 0.90f));
        var rfb = fundoBarra.GetComponent<RectTransform>();
        rfb.anchorMin = new Vector2(0.15f, 0.38f); rfb.anchorMax = new Vector2(0.85f, 0.42f);
        rfb.offsetMin = rfb.offsetMax = Vector2.zero;

        // barra brilho interno
        var barraGlow = Img("BarraGlow", new Color(corAcento.r * 0.5f, corAcento.g * 0.5f, corAcento.b * 0.5f, 0.30f));
        var rfg = barraGlow.GetComponent<RectTransform>();
        rfg.anchorMin = new Vector2(0.15f, 0.375f); rfg.anchorMax = new Vector2(0.85f, 0.425f);
        rfg.offsetMin = rfg.offsetMax = Vector2.zero;

        // fill
        var fill = Img("BarraFill", corAcento);
        barraFillRT = fill.GetComponent<RectTransform>();
        barraFillRT.anchorMin = new Vector2(0.15f, 0.38f);
        barraFillRT.anchorMax = new Vector2(0.15f, 0.42f);
        barraFillRT.offsetMin = barraFillRT.offsetMax = Vector2.zero;

        // brilho topo do fill
        var fillSheen = Img("FillSheen", new Color(1f, 0.7f, 0.7f, 0.25f));
        var rfs = fillSheen.GetComponent<RectTransform>();
        rfs.anchorMin = new Vector2(0.15f, 0.41f);
        rfs.anchorMax = new Vector2(0.15f, 0.42f);
        rfs.offsetMin = rfs.offsetMax = Vector2.zero;

        // porcentagem
        txtPorcentagem = Texto("TxtPct",
            new Vector2(0.35f, 0.32f), new Vector2(0.65f, 0.38f),
            "0%", 16f, FontStyles.Bold, new Color(0.95f, 0.75f, 0.75f));
        txtPorcentagem.alignment = TextAlignmentOptions.Center;

        // separador
        var sep = Img("Sep", new Color(corAcento.r, corAcento.g, corAcento.b, 0.35f));
        var rs  = sep.GetComponent<RectTransform>();
        rs.anchorMin = new Vector2(0.25f, 0.22f); rs.anchorMax = new Vector2(0.75f, 0.22f);
        rs.offsetMin = Vector2.zero; rs.offsetMax = new Vector2(0f, 1f);

        // label DICA
        Texto("LblDica",
            new Vector2(0.15f, 0.17f), new Vector2(0.85f, 0.22f),
            "— DICA —", 11f, FontStyles.Bold,
            new Color(corAcento.r + 0.2f, corAcento.g + 0.1f, corAcento.b + 0.1f, 0.80f))
            .alignment = TextAlignmentOptions.Center;

        // texto da dica
        txtDica = Texto("TxtDica",
            new Vector2(0.15f, 0.10f), new Vector2(0.85f, 0.17f),
            dicas[Random.Range(0, dicas.Length)],
            13f, FontStyles.Italic, new Color(0.85f, 0.72f, 0.72f));
        txtDica.textWrappingMode = TMPro.TextWrappingModes.Normal;
        txtDica.alignment = TextAlignmentOptions.Center;
    }

    // ── Carregamento ──────────────────────────────────────────────────
    IEnumerator Carregar(string cena)
    {
        float inicio = Time.time;
        AsyncOperation op = SceneManager.LoadSceneAsync(cena);

        // cena não está no Build Profiles
        if (op == null)
        {
            Debug.LogError($"[LoadingScreen] Cena '{cena}' nao encontrada. Adicione em File > Build Profiles.");
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene(0);
            yield break;
        }

        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float progresso = Mathf.Clamp01(op.progress / 0.9f);
            float tempoProg = Mathf.Clamp01((Time.time - inicio) / tempoMinimo);
            float p         = Mathf.Min(progresso, tempoProg);

            AtualizarBarra(p);

            if (op.progress >= 0.9f && (Time.time - inicio) >= tempoMinimo)
            {
                AtualizarBarra(1f);
                yield return new WaitForSeconds(0.25f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }


    void AtualizarBarra(float p)
    {
        if (barraFillRT != null)
            barraFillRT.anchorMax = new Vector2(0.15f + 0.70f * p, 0.43f);

        if (txtPorcentagem != null)
            txtPorcentagem.text = $"{Mathf.RoundToInt(p * 100f)}%";
    }

    // ── Animações ─────────────────────────────────────────────────────
    IEnumerator AnimarSpinner()
    {
        float ang = 0f;
        while (spinnerRT != null)
        {
            ang += Time.deltaTime * 200f;
            spinnerRT.localRotation = Quaternion.Euler(0f, 0f, -ang);
            yield return null;
        }
    }

    IEnumerator AnimarPontos()
    {
        int i = 0;
        while (txtCarregando != null)
        {
            string base2 = NomeFase(PlayerPrefs.GetString("ProximaCena", "")).ToUpper();
            txtCarregando.text = base2 + new string('.', i % 4);
            i++;
            yield return new WaitForSeconds(0.35f);
        }
    }

    IEnumerator AnimarDicas()
    {
        int idx = Random.Range(0, dicas.Length);
        yield return new WaitForSeconds(3f);

        while (txtDica != null)
        {
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                if (txtDica == null) yield break;
                txtDica.alpha = 1f - t / 0.35f;
                yield return null;
            }

            idx = (idx + 1) % dicas.Length;
            if (txtDica != null) txtDica.text = dicas[idx];

            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                if (txtDica == null) yield break;
                txtDica.alpha = t / 0.35f;
                yield return null;
            }
            if (txtDica != null) txtDica.alpha = 1f;

            yield return new WaitForSeconds(3.5f);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────
    string NomeFase(string cena)
    {
        switch (cena)
        {
            case "primeira_fase":      return "Primeira Fase";
            case "segunda_fase":       return "Segunda Fase";
            case "terceira_fase":      return "Terceira Fase";
            case "Modo_sobrevivencia": return "Modo Sobrevivência";
            default:                   return cena.Replace("_", " ");
        }
    }

    GameObject Img(string nome, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(canvasGO.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = cor;
        return go;
    }

    TextMeshProUGUI Texto(string nome, Vector2 mn, Vector2 mx,
        string texto, float size, FontStyles style, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(canvasGO.transform, false);
        var r  = go.AddComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var t  = go.AddComponent<TextMeshProUGUI>();
        t.text = texto; t.fontSize = size;
        t.fontStyle = style; t.color = cor;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    void Esticar(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
