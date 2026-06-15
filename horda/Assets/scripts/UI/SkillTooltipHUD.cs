using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Exibe tooltip ao passar o mouse sobre ícones de skill, ultimate e passiva na HUD.
// Inicialize chamando SkillTooltipHUD.ObterOuCriar() no Start() do UIManager.
public class SkillTooltipHUD : MonoBehaviour
{
    public static SkillTooltipHUD Instance { get; private set; }

    const int   SORT_ORDER = 110;
    const float WIDTH      = 230f;
    const float GAP        = 8f;

    Canvas     tooltipCanvas;
    GameObject tooltip;

    // ── Auto-criação ──────────────────────────────────────────────────────────

    public static SkillTooltipHUD ObterOuCriar()
    {
        if (Instance != null) return Instance;
        var existente = FindFirstObjectByType<SkillTooltipHUD>();
        if (existente != null) return existente;
        var uiManager = FindFirstObjectByType<UIManager>();
        var pai = uiManager != null ? uiManager.gameObject : new GameObject("SkillTooltipHUD_Host");
        return pai.AddComponent<SkillTooltipHUD>();
    }

    // ── API pública ───────────────────────────────────────────────────────────

    // Skill
    public static void Attach(Image icon, SkillData skill)
    {
        if (icon == null || skill == null) return;
        Color cor = CorPorRaridade(skill.rarity);
        AttachRaw(icon, skill.skillName, skill.description ?? "", cor, "SKILL", skill.specificType);
    }

    // Ultimate
    public static void AttachUltimate(Image icon, string nome, string desc,
        SpecificSkillType skillAlvo = SpecificSkillType.None)
    {
        if (icon == null) return;
        AttachRaw(icon, nome, desc ?? "", new Color(1f, 0.78f, 0.15f), "ULTIMATE", skillAlvo);
    }

    // Genérico (dash, etc.)
    public static void AttachRawPublic(Image icon, string nome, string desc, Color cor, string tipo)
        => AttachRaw(icon, nome, desc ?? "", cor, tipo);

    // Passiva
    public static void AttachPassiva(Image icon, string nome, string desc)
    {
        if (icon == null) return;
        AttachRaw(icon, nome, desc ?? "", new Color(0.35f, 0.90f, 0.45f), "PASSIVA");
    }

    static void AttachRaw(Image icon, string nome, string desc, Color cor, string tipo,
        SpecificSkillType skillAlvo = SpecificSkillType.None)
    {
        ObterOuCriar();
        var trigger = icon.GetComponent<SkillIconTooltipTrigger>()
                      ?? icon.gameObject.AddComponent<SkillIconTooltipTrigger>();
        icon.raycastTarget = true;
        trigger.Setup(nome, desc, cor, tipo, skillAlvo);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        CriarCanvas();
        CriarTooltip();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (tooltipCanvas != null) Destroy(tooltipCanvas.gameObject);
    }

    // ── Canvas ────────────────────────────────────────────────────────────────

    void CriarCanvas()
    {
        var go = new GameObject("SkillTooltipHUD_Canvas");
        tooltipCanvas = go.AddComponent<Canvas>();
        tooltipCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        tooltipCanvas.sortingOrder = SORT_ORDER;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    void CriarTooltip()
    {
        var go = new GameObject("Tooltip");
        go.transform.SetParent(tooltipCanvas.transform, false);
        tooltip = go;

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(WIDTH, 90f);
        rt.pivot     = new Vector2(0.5f, 0f); // cresce para cima
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        // Sombra
        var sombra = Filho(go, "Sombra");
        var sombraRT = sombra.AddComponent<RectTransform>();
        sombraRT.anchorMin = Vector2.zero; sombraRT.anchorMax = Vector2.one;
        sombraRT.offsetMin = new Vector2(3f, -4f); sombraRT.offsetMax = new Vector2(3f, -4f);
        sombra.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
        sombra.transform.SetAsFirstSibling();

        // Borda fina escura
        var borda = Filho(go, "Borda");
        var bordaRT = borda.AddComponent<RectTransform>();
        bordaRT.anchorMin = Vector2.zero; bordaRT.anchorMax = Vector2.one;
        bordaRT.offsetMin = new Vector2(-1f, -1f); bordaRT.offsetMax = new Vector2(1f, 1f);
        borda.AddComponent<Image>().color = new Color(0.15f, 0.03f, 0.03f, 1f);
        borda.transform.SetAsFirstSibling();

        // Fundo
        go.AddComponent<Image>().color = new Color(0.05f, 0.01f, 0.01f, 0.97f);

        // Strip de cor (esquerda, 4px)
        var strip = Filho(go, "Strip");
        var stripRT = strip.AddComponent<RectTransform>();
        stripRT.anchorMin = Vector2.zero; stripRT.anchorMax = new Vector2(0f, 1f);
        stripRT.pivot = new Vector2(0f, 0.5f);
        stripRT.offsetMin = Vector2.zero; stripRT.offsetMax = new Vector2(4f, 0f);
        strip.AddComponent<Image>().name = "Strip"; // cor atualizada por MostrarTooltip

        // Nome
        var nomeGO = Filho(go, "NomeTxt");
        var nomeRT = nomeGO.AddComponent<RectTransform>();
        nomeRT.anchorMin = new Vector2(0f, 1f); nomeRT.anchorMax = new Vector2(1f, 1f);
        nomeRT.pivot = new Vector2(0f, 1f);
        nomeRT.anchoredPosition = new Vector2(12f, -9f);
        nomeRT.sizeDelta = new Vector2(-16f, 22f);
        var nomeTxt = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTxt.fontSize = 13f; nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.alignment = TextAlignmentOptions.MidlineLeft;
        nomeTxt.overflowMode = TextOverflowModes.Truncate;
        nomeTxt.raycastTarget = false;

        // Tipo badge
        var tipoGO = Filho(go, "TipoTxt");
        var tipoRT = tipoGO.AddComponent<RectTransform>();
        tipoRT.anchorMin = new Vector2(0f, 1f); tipoRT.anchorMax = new Vector2(1f, 1f);
        tipoRT.pivot = new Vector2(0f, 1f);
        tipoRT.anchoredPosition = new Vector2(12f, -33f);
        tipoRT.sizeDelta = new Vector2(-16f, 16f);
        var tipoTxt = tipoGO.AddComponent<TextMeshProUGUI>();
        tipoTxt.fontSize = 9.5f; tipoTxt.fontStyle = FontStyles.Bold;
        tipoTxt.alignment = TextAlignmentOptions.MidlineLeft;
        tipoTxt.raycastTarget = false;

        // Divisor
        var div = Filho(go, "Div");
        var divRT = div.AddComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0f, 1f); divRT.anchorMax = new Vector2(1f, 1f);
        divRT.pivot = new Vector2(0.5f, 1f);
        divRT.anchoredPosition = new Vector2(0f, -52f);
        divRT.sizeDelta = new Vector2(-14f, 1f);
        div.AddComponent<Image>().color = new Color(0.35f, 0.07f, 0.07f, 0.7f);

        // Descrição
        var descGO = Filho(go, "DescTxt");
        var descRT = descGO.AddComponent<RectTransform>();
        descRT.anchorMin = Vector2.zero; descRT.anchorMax = Vector2.one;
        descRT.offsetMin = new Vector2(12f, 9f); descRT.offsetMax = new Vector2(-8f, -56f);
        var descTxt = descGO.AddComponent<TextMeshProUGUI>();
        descTxt.fontSize = 10f;
        descTxt.color = new Color(0.78f, 0.67f, 0.67f);
        descTxt.alignment = TextAlignmentOptions.TopLeft;
        descTxt.textWrappingMode = TextWrappingModes.Normal;
        descTxt.raycastTarget = false;

        go.SetActive(false);
    }

    // ── Tooltip API ───────────────────────────────────────────────────────────

    public void MostrarTooltip(string nome, string desc, Color cor, string tipo,
        RectTransform origem, List<SkillEvolutionData> evolucoes = null)
    {
        if (tooltip == null || tooltipCanvas == null) return;

        // Conteúdo da descrição: skill + evoluções
        string descFinal = desc ?? "";
        string evoBloco  = "";
        if (evolucoes != null && evolucoes.Count > 0)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var evo in evolucoes)
            {
                sb.Append($"<color=#{ColorToHex(evo.corDestaque)}><b>- {evo.nomeEvolucao}</b></color>");
                if (!string.IsNullOrEmpty(evo.descricao))
                    sb.Append($"\n{evo.descricao}");
                sb.Append("\n");
            }
            evoBloco = sb.ToString().TrimEnd('\n');
        }

        bool temDesc = !string.IsNullOrEmpty(descFinal);
        bool temEvo  = !string.IsNullOrEmpty(evoBloco);
        string textoDesc = temDesc && temEvo
            ? descFinal + "\n\n" + evoBloco
            : temDesc ? descFinal : evoBloco;

        // Altura dinâmica
        int linhas = string.IsNullOrEmpty(textoDesc) ? 0 : Mathf.Max(1, Mathf.CeilToInt(textoDesc.Length / 26f));
        float h = linhas == 0 ? 58f : 62f + linhas * 15f;
        var rt = tooltip.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(WIDTH, h);

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);

        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(null, origem.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)tooltipCanvas.transform, screenPt, null, out var local))
        {
            float iconHalf = Mathf.Max(origem.rect.height, origem.rect.width) * 0.5f
                           * origem.lossyScale.y / tooltipCanvas.scaleFactor;
            rt.anchoredPosition = new Vector2(local.x, local.y + iconHalf + GAP);
        }

        foreach (Transform child in tooltip.transform)
        {
            var img = child.GetComponent<Image>();
            var txt = child.GetComponent<TextMeshProUGUI>();
            switch (child.name)
            {
                case "Strip":   if (img) img.color = new Color(cor.r, cor.g, cor.b, 0.95f); break;
                case "NomeTxt": if (txt) { txt.text = nome; txt.color = new Color(cor.r, cor.g, cor.b, 1f); } break;
                case "TipoTxt": if (txt) { txt.text = tipo; txt.color = new Color(cor.r * 0.8f, cor.g * 0.8f, cor.b * 0.8f, 0.85f); } break;
                case "DescTxt": if (txt) txt.text = textoDesc; break;
                case "Div":     if (img) img.gameObject.SetActive(!string.IsNullOrEmpty(textoDesc)); break;
            }
        }

        tooltip.SetActive(true);
    }

    static string ColorToHex(Color c)
    {
        return $"{ToByte(c.r):X2}{ToByte(c.g):X2}{ToByte(c.b):X2}";
    }
    static int ToByte(float f) => Mathf.Clamp(Mathf.RoundToInt(f * 255), 0, 255);

    public void OcultarTooltip() { if (tooltip != null) tooltip.SetActive(false); }

    // ── Utilitários ───────────────────────────────────────────────────────────

    public static Color CorPorRaridade(SkillRarity r)
    {
        switch (r)
        {
            case SkillRarity.Common:    return new Color(0.70f, 0.70f, 0.70f);
            case SkillRarity.Uncommon:  return new Color(0.30f, 0.90f, 0.30f);
            case SkillRarity.Rare:      return new Color(0.35f, 0.55f, 1.00f);
            case SkillRarity.Epic:      return new Color(0.70f, 0.30f, 1.00f);
            case SkillRarity.Legendary: return new Color(1.00f, 0.80f, 0.20f);
            case SkillRarity.Mythic:    return new Color(1.00f, 0.30f, 0.50f);
            default:                    return Color.white;
        }
    }

    static GameObject Filho(GameObject pai, string nome)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai.transform, false);
        return go;
    }
}

// ── Trigger por ícone ─────────────────────────────────────────────────────────

public class SkillIconTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    string            nome, desc, tipo;
    Color             cor;
    SpecificSkillType skillAlvo;

    public void Setup(string n, string d, Color c, string t,
        SpecificSkillType alvo = SpecificSkillType.None)
    { nome = n; desc = d; cor = c; tipo = t; skillAlvo = alvo; }

    public void OnPointerEnter(PointerEventData _)
    {
        // Consulta evoluções em tempo real — sempre reflete o estado atual
        List<SkillEvolutionData> evos = null;
        var evoManager = SkillEvolutionManager.Instance;
        if (evoManager != null && skillAlvo != SpecificSkillType.None)
        {
            evos = new List<SkillEvolutionData>();
            foreach (var evo in evoManager.GetEvolucoesData())
                if (evo.skillAlvo == skillAlvo)
                    evos.Add(evo);
        }
        SkillTooltipHUD.Instance?.MostrarTooltip(nome, desc, cor, tipo,
            (RectTransform)transform, evos);
    }

    public void OnPointerExit(PointerEventData _)
        => SkillTooltipHUD.Instance?.OcultarTooltip();
}
