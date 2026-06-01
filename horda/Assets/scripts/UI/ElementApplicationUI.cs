using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ElementApplicationUI : MonoBehaviour
{
    static ElementApplicationUI _instance;
    public static ElementApplicationUI Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<ElementApplicationUI>();
            if (_instance == null)
            {
                var go = new GameObject("ElementApplicationUI");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ElementApplicationUI>();
            }
            return _instance;
        }
    }

    static readonly Color corFundo      = new Color(0.071f, 0.059f, 0.118f, 0.97f);
    static readonly Color corBorda      = new Color(0.784f, 0.659f, 0.251f, 1f);
    static readonly Color corTitulo     = new Color(0.95f,  0.80f,  0.40f,  1f);
    static readonly Color corTexto      = new Color(0.90f,  0.82f,  0.65f,  1f);
    static readonly Color corBotao      = new Color(0.14f,  0.11f,  0.22f,  1f);
    static readonly Color corBloqueado  = new Color(0.20f,  0.18f,  0.25f,  0.50f);

    Canvas      canvas;
    GameObject  painelEtapa1;
    GameObject  painelEtapa2;
    ElementType elementoAtual;
    SkillData   skillSelecionada;
    SkillManager skillManager;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        CriarCanvas();
    }

    void Start()
    {
        skillManager = FindFirstObjectByType<SkillManager>();
    }

    public void Abrir(ElementType tipo)
    {
        if (skillManager == null)
            skillManager = FindFirstObjectByType<SkillManager>();
        elementoAtual = tipo;
        skillSelecionada = null;
        Time.timeScale = 0f;
        canvas.gameObject.SetActive(true);
        MostrarEtapa1();
    }

    void MostrarEtapa1()
    {
        if (painelEtapa1 != null) Destroy(painelEtapa1);
        if (painelEtapa2 != null) Destroy(painelEtapa2);

        var reg = ElementRegistry.Instance;
        var def = reg?.Get(elementoAtual);
        string nomeElem = def != null ? def.nomeDisplay : elementoAtual.ToString();
        Color corElem   = def != null ? def.cor : Color.white;

        painelEtapa1 = CriarPainel("PainelEtapa1");
        var rt = painelEtapa1.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.25f, 0.12f);
        rt.anchorMax = new Vector2(0.75f, 0.92f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var titulo = CriarTexto(painelEtapa1, "Titulo",
            $"Elemento {nomeElem.ToUpper()} coletado!", corElem, 22f, FontStyles.Bold);
        Ancora(titulo, new Vector2(0f, 0.88f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-10f, 0f));

        var sub = CriarTexto(painelEtapa1, "Sub",
            "Escolha uma skill ativa para infundir:", corTexto, 13f);
        Ancora(sub, new Vector2(0f, 0.80f), new Vector2(1f, 0.89f), new Vector2(10f, 0f), new Vector2(-10f, 0f));

        var lista = CriarScrollArea(painelEtapa1, "ListaSkills",
            new Vector2(0f, 0.06f), new Vector2(1f, 0.80f));

        var skills = ObterSkillsDisponiveis();
        bool temDisponivel = false;
        foreach (var skill in skills)
        {
            bool pode = skill.appliedElement == ElementType.None;
            if (pode) temDisponivel = true;
            CriarLinhaSkill(lista, skill, pode, corElem);
        }

        if (!temDisponivel)
        {
            string msg = skills.Count == 0
                ? "Adquira skills primeiro!\nVoce ainda nao tem skills."
                : "Todas as skills ja possuem\num elemento infundido.";
            var aviso = CriarTexto(lista, "Aviso", msg, corTexto, 13f);
            Ancora(aviso, Vector2.zero, Vector2.one);

            var btnF = CriarBotao(painelEtapa1, "BtnFechar", "DESCARTAR",
                new Color(0.4f, 0.1f, 0.1f), Fechar);
            Ancora(btnF, new Vector2(0.2f, 0.01f), new Vector2(0.8f, 0.07f));
        }
    }

    void CriarLinhaSkill(GameObject pai, SkillData skill, bool disponivel, Color corElem)
    {
        var go = new GameObject($"Linha_{skill.skillName}");
        go.transform.SetParent(pai.transform, false);
        go.AddComponent<Image>().color = disponivel ? corBotao : corBloqueado;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 54f; le.flexibleWidth = 1f;

        if (skill.icon != null)
        {
            var ic = new GameObject("Icone"); ic.transform.SetParent(go.transform, false);
            var icRT = ic.AddComponent<RectTransform>();
            icRT.anchorMin = new Vector2(0f, 0f); icRT.anchorMax = new Vector2(0f, 1f);
            icRT.pivot = new Vector2(0f, 0.5f);
            icRT.anchoredPosition = new Vector2(6f, 0f); icRT.sizeDelta = new Vector2(44f, -8f);
            var icImg = ic.AddComponent<Image>();
            icImg.sprite = skill.icon; icImg.preserveAspect = true;
        }

        var nome = CriarTexto(go, "Nome", skill.skillName, disponivel ? corTexto : new Color(0.5f, 0.5f, 0.5f), 14f, FontStyles.Bold);
        Ancora(nome, new Vector2(0f, 0.5f), new Vector2(0.7f, 1f), new Vector2(58f, 0f), Vector2.zero);
        nome.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        var status = CriarTexto(go, "Status",
            disponivel ? "Sem elemento" : $"Ja tem: {ElementRegistry.Instance?.GetNome(skill.appliedElement) ?? skill.appliedElement.ToString()}",
            disponivel ? new Color(0.5f, 0.7f, 0.5f) : new Color(0.6f, 0.4f, 0.4f), 10f);
        Ancora(status, new Vector2(0f, 0f), new Vector2(0.7f, 0.5f), new Vector2(58f, 0f), Vector2.zero);
        status.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        if (disponivel)
        {
            var btn = new GameObject("BtnInfundir"); btn.transform.SetParent(go.transform, false);
            Ancora(btn, new Vector2(0.72f, 0.15f), new Vector2(0.97f, 0.85f));
            btn.AddComponent<Image>().color = corElem * 0.5f + corBotao * 0.5f;
            var b = btn.AddComponent<Button>();
            var cap = skill;
            b.onClick.AddListener(() => SelecionarSkill(cap));
            var lbl = CriarTexto(btn, "Txt", "INFUNDIR", corTexto, 11f, FontStyles.Bold);
            Ancora(lbl, Vector2.zero, Vector2.one);
        }
    }

    void SelecionarSkill(SkillData skill) { skillSelecionada = skill; MostrarEtapa2(); }

    void MostrarEtapa2()
    {
        if (painelEtapa1 != null) Destroy(painelEtapa1);
        if (painelEtapa2 != null) Destroy(painelEtapa2);

        var def = ElementRegistry.Instance?.Get(elementoAtual);
        string nomeElem = def != null ? def.nomeDisplay : elementoAtual.ToString();
        Color corElem   = def != null ? def.cor : Color.white;

        painelEtapa2 = CriarPainel("PainelEtapa2");
        var rt = painelEtapa2.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.25f, 0.20f);
        rt.anchorMax = new Vector2(0.75f, 0.85f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var titulo = CriarTexto(painelEtapa2, "Titulo",
            $"{skillSelecionada.skillName} + {nomeElem}", corElem, 20f, FontStyles.Bold);
        Ancora(titulo, new Vector2(0f, 0.88f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-10f, 0f));

        var sub = CriarTexto(painelEtapa2, "Sub", "Escolha o poder do elemento:", corTexto, 12f);
        Ancora(sub, new Vector2(0f, 0.81f), new Vector2(1f, 0.89f), new Vector2(10f, 0f), new Vector2(-10f, 0f));

        if (def?.caracteristicas != null)
        {
            float[] yMins  = { 0.50f, 0.18f };
            float[] yMaxes = { 0.80f, 0.48f };
            for (int i = 0; i < Mathf.Min(def.caracteristicas.Length, 2); i++)
            {
                var car = def.caracteristicas[i];
                if (car == null) continue;
                int idx = i;

                var card = new GameObject($"Card_{i}");
                card.transform.SetParent(painelEtapa2.transform, false);
                var cardRT = card.AddComponent<RectTransform>();
                cardRT.anchorMin = new Vector2(0.04f, yMins[i]);
                cardRT.anchorMax = new Vector2(0.96f, yMaxes[i]);
                cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;
                var cardImg = card.AddComponent<Image>(); cardImg.color = corBotao;

                var bord = new GameObject("Bord"); bord.transform.SetParent(card.transform, false);
                var bRT = bord.AddComponent<RectTransform>();
                bRT.anchorMin = Vector2.zero; bRT.anchorMax = new Vector2(0f, 1f);
                bRT.offsetMin = Vector2.zero; bRT.offsetMax = new Vector2(4f, 0f);
                bord.AddComponent<Image>().color = corElem;

                var letra = CriarTexto(card, "Letra", i == 0 ? "A" : "B", corElem, 22f, FontStyles.Bold);
                Ancora(letra, new Vector2(0f, 0f), new Vector2(0.1f, 1f), new Vector2(8f, 0f), Vector2.zero);

                var nomeC = CriarTexto(card, "Nome", car.nome, corTexto, 14f, FontStyles.Bold);
                Ancora(nomeC, new Vector2(0.1f, 0.55f), new Vector2(0.85f, 1f), new Vector2(8f, 0f), Vector2.zero);
                nomeC.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

                var desc = CriarTexto(card, "Desc", car.descricao, new Color(0.75f, 0.70f, 0.60f), 11f);
                Ancora(desc, new Vector2(0.1f, 0f), new Vector2(0.85f, 0.55f), new Vector2(8f, 2f), Vector2.zero);
                var dTxt = desc.GetComponent<TextMeshProUGUI>();
                dTxt.enableWordWrapping = true;
                dTxt.alignment = TextAlignmentOptions.TopLeft;

                var btn = card.AddComponent<Button>(); btn.targetGraphic = cardImg;
                btn.onClick.AddListener(() => ConfirmarEscolha(idx));
            }
        }

        var aviso = CriarTexto(painelEtapa2, "Aviso", "Esta escolha e permanente.", new Color(0.6f, 0.5f, 0.5f), 10f);
        Ancora(aviso, new Vector2(0f, 0.10f), new Vector2(1f, 0.18f), new Vector2(10f, 0f), new Vector2(-10f, 0f));

        var btnV = CriarBotao(painelEtapa2, "BtnVoltar", "< VOLTAR", new Color(0.2f, 0.15f, 0.3f), MostrarEtapa1);
        Ancora(btnV, new Vector2(0.04f, 0.01f), new Vector2(0.45f, 0.10f));
    }

    void ConfirmarEscolha(int indice)
    {
        if (skillSelecionada == null) return;
        skillSelecionada.appliedElement = elementoAtual;
        skillSelecionada.appliedCharacteristicIndex = indice;
        var def = ElementRegistry.Instance?.Get(elementoAtual);
        if (def != null) skillSelecionada.elementColor = def.cor;
        SkillIconsHUD.Instance?.AtualizarBadgeElemento(skillSelecionada);

        // Atualiza o UIManager principal (HUD de skills)
        var uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null) uiManager.AtualizarElementoAplicado();

        Debug.Log($"[ElementSystem] {elementoAtual} (opcao {indice}) -> {skillSelecionada.skillName}");
        Fechar();
    }

    void Fechar()
    {
        if (painelEtapa1 != null) { Destroy(painelEtapa1); painelEtapa1 = null; }
        if (painelEtapa2 != null) { Destroy(painelEtapa2); painelEtapa2 = null; }
        canvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    List<SkillData> ObterSkillsDisponiveis()
    {
        var lista = new List<SkillData>();
        if (skillManager == null) return lista;
        foreach (var s in skillManager.activeSkills)
        {
            if (s == null) continue;
            if (s.skillType == SkillType.Ultimate) continue;
            lista.Add(s);
        }
        return lista;
    }

    void CriarCanvas()
    {
        var go = new GameObject("ElementApplicationUI_Canvas");
        go.transform.SetParent(transform, false);
        DontDestroyOnLoad(go);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        var bd = new GameObject("Backdrop"); bd.transform.SetParent(go.transform, false);
        var bdRT = bd.AddComponent<RectTransform>();
        bdRT.anchorMin = Vector2.zero; bdRT.anchorMax = Vector2.one;
        bdRT.offsetMin = bdRT.offsetMax = Vector2.zero;
        bd.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        canvas.gameObject.SetActive(false);
    }

    GameObject CriarPainel(string nome)
    {
        var go = new GameObject(nome); go.transform.SetParent(canvas.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = corFundo;
        foreach (var (mn, mx, oMin, oMax) in new[]{
            (new Vector2(0,1), new Vector2(1,1), new Vector2(0,-2), new Vector2(0,0)),
            (new Vector2(0,0), new Vector2(1,0), new Vector2(0,0),  new Vector2(0,2)),
            (new Vector2(0,0), new Vector2(0,1), new Vector2(0,0),  new Vector2(2,0)),
            (new Vector2(1,0), new Vector2(1,1), new Vector2(-2,0), new Vector2(0,0)),
        })
        {
            var b = new GameObject("Borda"); b.transform.SetParent(go.transform, false);
            var bRT = b.AddComponent<RectTransform>();
            bRT.anchorMin = mn; bRT.anchorMax = mx; bRT.offsetMin = oMin; bRT.offsetMax = oMax;
            b.AddComponent<Image>().color = corBorda;
        }
        return go;
    }

    GameObject CriarScrollArea(GameObject pai, string nome, Vector2 anchorMin, Vector2 anchorMax)
    {
        var vp = new GameObject(nome + "_Viewport"); vp.transform.SetParent(pai.transform, false);
        var vpRT = vp.AddComponent<RectTransform>();
        vpRT.anchorMin = anchorMin; vpRT.anchorMax = anchorMax;
        vpRT.offsetMin = new Vector2(4f, 4f); vpRT.offsetMax = new Vector2(-4f, -4f);
        vp.AddComponent<Image>().color = new Color(0,0,0,0);
        vp.AddComponent<RectMask2D>();

        var ct = new GameObject(nome + "_Content"); ct.transform.SetParent(vp.transform, false);
        var ctRT = ct.AddComponent<RectTransform>();
        ctRT.anchorMin = new Vector2(0,1); ctRT.anchorMax = new Vector2(1,1);
        ctRT.pivot = new Vector2(0.5f,1f); ctRT.anchoredPosition = Vector2.zero; ctRT.sizeDelta = Vector2.zero;
        var vlg = ct.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5f; vlg.padding = new RectOffset(4,4,4,4);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        var csf = ct.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = vp.AddComponent<ScrollRect>();
        sr.content = ctRT; sr.viewport = vpRT;
        sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 20f;
        return ct;
    }

    static GameObject CriarTexto(GameObject pai, string nome, string texto, Color cor, float tam,
        FontStyles estilo = FontStyles.Normal)
    {
        var go = new GameObject(nome); go.transform.SetParent(pai.transform, false);
        go.AddComponent<RectTransform>();
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = texto; txt.fontSize = tam; txt.fontStyle = estilo;
        txt.color = cor; txt.alignment = TextAlignmentOptions.Center; txt.raycastTarget = false;
        return go;
    }

    static GameObject CriarBotao(GameObject pai, string nome, string label, Color cor,
        UnityEngine.Events.UnityAction cb)
    {
        var go = new GameObject(nome); go.transform.SetParent(pai.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = cor;
        go.AddComponent<Button>().onClick.AddListener(cb);
        var lbl = CriarTexto(go, "Label", label, new Color(0.95f, 0.85f, 0.65f), 13f, FontStyles.Bold);
        Ancora(lbl, Vector2.zero, Vector2.one);
        return go;
    }

    static void Ancora(GameObject go, Vector2 mn, Vector2 mx,
        Vector2 offMin = default, Vector2 offMax = default)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = mn; rt.anchorMax = mx; rt.offsetMin = offMin; rt.offsetMax = offMax;
    }
}
