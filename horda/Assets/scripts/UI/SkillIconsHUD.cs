using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Exibe os ícones das skills adquiridas na HUD em tempo real.
// Adicione este componente ao mesmo GameObject que contém o UIManager.
public class SkillIconsHUD : MonoBehaviour
{
    [Header("Layout")]
    public Vector2 posicaoBase      = new Vector2(10f, 10f); // offset do canto inferior-esquerdo
    public float   tamanhoIcone     = 52f;
    public float   espacamento      = 8f;
    public int     sortingOrder     = 95;

    [Header("Visual")]
    public Color   corFundo         = new Color(0.05f, 0.05f, 0.08f, 0.88f);
    public Color   corBorda         = new Color(0.4f,  0.6f,  1f,   0.55f);
    public Color   corSemIcone      = new Color(0.25f, 0.45f, 0.8f, 1f);

    // ── Internos ─────────────────────────────────────────────────────────────

    Canvas          canvas;
    Transform       container;
    SkillManager    skillManager;
    List<GameObject> slots = new List<GameObject>();
    public static SkillIconsHUD Instance { get; private set; }
    Dictionary<string, GameObject> slotPorNome = new Dictionary<string, GameObject>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    // Auto-cria uma instância se não existir na cena
    public static SkillIconsHUD ObterOuCriar()
    {
        var existente = FindFirstObjectByType<SkillIconsHUD>();
        if (existente != null) return existente;

        var uiManager = FindFirstObjectByType<UIManager>();
        GameObject pai = uiManager != null ? uiManager.gameObject : new GameObject("SkillIconsHUD_Host");
        return pai.AddComponent<SkillIconsHUD>();
    }

    void Start()
    {
        Instance = this;
        skillManager = FindFirstObjectByType<SkillManager>();
        if (skillManager == null) return;

        CriarCanvas();
        skillManager.OnSkillAcquired += OnSkillAdquirida;

        // Mostra skills já adquiridas (caso o componente seja adicionado tarde)
        foreach (var skill in skillManager.activeSkills)
            AdicionarIcone(skill, animar: false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (skillManager != null)
            skillManager.OnSkillAcquired -= OnSkillAdquirida;
        if (canvas != null)
            Destroy(canvas.gameObject);
    }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    void OnSkillAdquirida(SkillData skill)
    {
        // Mostra a partir da 2ª skill, até o máximo de 3 slots (skills 2, 3 e 4)
        int total = skillManager.activeSkills.Count;
        if (total < 2) return;       // ignora a 1ª skill
        if (slots.Count >= 3) return; // máximo de 3 ícones na HUD

        AdicionarIcone(skill, animar: true);
    }

    // ── Criação da UI ─────────────────────────────────────────────────────────

    void CriarCanvas()
    {
        var go = new GameObject("SkillIconsHUD_Canvas");
        DontDestroyOnLoad(go);

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        // Container com layout horizontal
        var contGO = new GameObject("SkillsContainer");
        contGO.transform.SetParent(go.transform, false);
        var contRT = contGO.AddComponent<RectTransform>();
        contRT.anchorMin        = new Vector2(0f, 0f);
        contRT.anchorMax        = new Vector2(0f, 0f);
        contRT.pivot            = new Vector2(0f, 0f);
        contRT.anchoredPosition = posicaoBase;

        var layout = contGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing           = espacamento;
        layout.childAlignment    = TextAnchor.LowerLeft;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;

        var fitter = contGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        container = contGO.transform;
    }

    void AdicionarIcone(SkillData skill, bool animar)
    {
        if (container == null) return;

        var slot = CriarSlot(skill);
        slots.Add(slot);

        if (animar)
            StartCoroutine(AnimarEntrada(slot.transform));
    }

    GameObject CriarSlot(SkillData skill)
    {
        float sz = tamanhoIcone;

        // Slot raiz
        var slotGO = new GameObject($"Slot_{skill.skillName}");
        slotGO.transform.SetParent(container, false);
        var slotRT = slotGO.AddComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(sz, sz);

        var layoutEl = slotGO.AddComponent<LayoutElement>();
        layoutEl.preferredWidth  = sz;
        layoutEl.preferredHeight = sz;

        // Fundo circular
        var fundo = CriarImagem(slotGO, "Fundo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fundo.color  = corFundo;
        fundo.sprite = GerarDisco(64);

        // Borda
        var borda = CriarImagem(slotGO, "Borda", Vector2.zero, Vector2.one,
            new Vector2(-2f, -2f), new Vector2(2f, 2f));
        borda.color  = skill.elementColor != Color.white && skill.elementColor != Color.clear
                        ? skill.elementColor
                        : corBorda;
        borda.sprite = GerarAnel(64, 4);
        borda.type   = Image.Type.Simple;

        // Ícone da skill
        if (skill.icon != null)
        {
            var icone = CriarImagem(slotGO, "Icone", Vector2.zero, Vector2.one,
                new Vector2(6f, 6f), new Vector2(-6f, -6f));
            icone.sprite              = skill.icon;
            icone.preserveAspect = true;
            icone.color               = Color.white;
        }
        else
        {
            // Sem ícone: mostra inicial do nome
            var icone = CriarImagem(slotGO, "FundoSemIcone", Vector2.zero, Vector2.one,
                new Vector2(6f, 6f), new Vector2(-6f, -6f));
            icone.color  = new Color(corSemIcone.r, corSemIcone.g, corSemIcone.b, 0.5f);
            icone.sprite = GerarDisco(32);

            var txtGO = new GameObject("Inicial");
            txtGO.transform.SetParent(slotGO.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.text      = skill.skillName.Length > 0 ? skill.skillName[0].ToString().ToUpper() : "?";
            txt.fontSize  = sz * 0.45f;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color     = Color.white;
        }

        // Tooltip com nome (texto pequeno abaixo)
        var nomeGO = new GameObject("Nome");
        nomeGO.transform.SetParent(slotGO.transform, false);
        var nomeRT = nomeGO.AddComponent<RectTransform>();
        nomeRT.anchorMin        = new Vector2(0f, 0f);
        nomeRT.anchorMax        = new Vector2(1f, 0f);
        nomeRT.pivot            = new Vector2(0.5f, 1f);
        nomeRT.anchoredPosition = new Vector2(0f, -4f);
        nomeRT.sizeDelta        = new Vector2(sz + 20f, 16f);
        var nomeTxt = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTxt.text      = skill.skillName;
        nomeTxt.fontSize  = 9f;
        nomeTxt.alignment = TextAlignmentOptions.Center;
        nomeTxt.color     = new Color(1f, 1f, 1f, 0.75f);
        nomeTxt.enableWordWrapping = false;
        nomeTxt.overflowMode = TextOverflowModes.Truncate;

        // Registrar slot para lookup posterior
        slotPorNome[skill.skillName] = slotGO;

        // Badge de elemento (oculto por padrão)
        var badgeGO = new GameObject("BadgeElemento");
        badgeGO.transform.SetParent(slotGO.transform, false);
        var badgeRT = badgeGO.AddComponent<RectTransform>();
        badgeRT.anchorMin = badgeRT.anchorMax = new Vector2(0.5f, 0f);
        badgeRT.pivot = new Vector2(0.5f, 1f);
        badgeRT.anchoredPosition = new Vector2(0f, -4f);
        badgeRT.sizeDelta = new Vector2(18f, 18f);
        var badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.sprite = GerarDisco(32);
        badgeImg.color = Color.clear;
        badgeGO.SetActive(false);

        if (skill.appliedElement != ElementType.None)
            AtualizarBadgeGO(badgeGO, skill);

        return slotGO;
    }

    public void AtualizarBadgeElemento(SkillData skill)
    {
        if (skill == null || !slotPorNome.TryGetValue(skill.skillName, out var slotGO)) return;

        // Atualiza badge
        var badge = slotGO.transform.Find("BadgeElemento")?.gameObject;
        if (badge != null) AtualizarBadgeGO(badge, skill);

        // Atualiza borda com a cor do elemento
        var borda = slotGO.transform.Find("Borda")?.GetComponent<Image>();
        if (borda != null && skill.appliedElement != ElementType.None)
            borda.color = ElementRegistry.Instance?.GetCor(skill.appliedElement) ?? corBorda;
    }

    static void AtualizarBadgeGO(GameObject badge, SkillData skill)
    {
        if (skill.appliedElement == ElementType.None) { badge.SetActive(false); return; }
        var cor = ElementRegistry.Instance?.GetCor(skill.appliedElement) ?? Color.white;
        badge.GetComponent<Image>().color = cor;
        badge.SetActive(true);
    }

    // ── Animação de entrada ───────────────────────────────────────────────────

    IEnumerator AnimarEntrada(Transform t)
    {
        float dur = 0.35f;
        t.localScale = Vector3.zero;
        for (float e = 0f; e < dur; e += Time.unscaledDeltaTime)
        {
            float p = e / dur;
            float ease = 1f - Mathf.Pow(1f - p, 3f); // ease-out cubic
            t.localScale = Vector3.one * (ease * 1.1f);
            yield return null;
        }
        // Pequeno overshoot
        for (float e = 0f; e < 0.1f; e += Time.unscaledDeltaTime)
        {
            t.localScale = Vector3.one * Mathf.Lerp(1.1f, 1f, e / 0.1f);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ── Helpers de UI ─────────────────────────────────────────────────────────

    static Image CriarImagem(GameObject pai, string nome,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go  = new GameObject(nome);
        go.transform.SetParent(pai.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    // ── Sprites procedurais ───────────────────────────────────────────────────

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

    public void FlashEvolucao()
    {
        foreach (var slot in slots)
            StartCoroutine(FlashSlot(slot));
    }

    IEnumerator FlashSlot(GameObject slot)
    {
        if (slot == null) yield break;
        var imgs = slot.GetComponentsInChildren<Image>();
        float t = 0f;
        while (t < 0.5f)
        {
            float alpha = Mathf.PingPong(t * 6f, 1f);
            foreach (var img in imgs)
            {
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, alpha);
            }
            t += Time.deltaTime;
            yield return null;
        }
        foreach (var img in imgs)
        {
            var c = img.color;
            img.color = new Color(c.r, c.g, c.b, 1f);
        }
    }

    static Sprite GerarAnel(int sz, int espessura)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        float rExt = cx - 0.5f;
        float rInt = cx - espessura - 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            float a = (d <= rExt && d >= rInt) ? 1f : 0f;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
