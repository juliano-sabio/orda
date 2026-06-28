using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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
        if (_instance != null && _instance != this) { Destroy(this); return; }

        // Se compartilhando GO com outros scripts, isolar em GO dedicado
        if (GetComponents<MonoBehaviour>().Length > 1)
        {
            var root = new GameObject("ElementApplicationUI_Persistent");
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<ElementApplicationUI>();
            Destroy(this);
            return;
        }

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
        CoopPause.ReterEscolha();
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
            string.Format(Loc.T("ui.elem_collected_fmt"), nomeElem.ToUpper()), corElem, 22f, FontStyles.Bold);
        Ancora(titulo, new Vector2(0f, 0.88f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-10f, 0f));

        var sub = CriarTexto(painelEtapa1, "Sub",
            Loc.T("ui.choose_skill_infuse"), corTexto, 13f);
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
                ? Loc.T("ui.no_skills_yet")
                : Loc.T("ui.all_skills_infused");
            var aviso = CriarTexto(lista, "Aviso", msg, corTexto, 13f);
            Ancora(aviso, Vector2.zero, Vector2.one);

            var btnF = CriarBotao(painelEtapa1, "BtnFechar", Loc.T("ui.discard"),
                new Color(0.4f, 0.1f, 0.1f), Fechar);
            Ancora(btnF, new Vector2(0.2f, 0.01f), new Vector2(0.8f, 0.07f));
        }

        AnimarEntrada(painelEtapa1);
    }

    void CriarLinhaSkill(GameObject pai, SkillData skill, bool disponivel, Color corElem)
    {
        // root sem Image — irmãos controlam renderização
        var go = new GameObject($"Linha_{skill.skillName}");
        go.transform.SetParent(pai.transform, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 62f; le.flexibleWidth = 1f;

        // irmão 0: borda (dourada se disponível, apagada se bloqueada)
        var brd = new GameObject("Brd"); brd.transform.SetParent(go.transform, false);
        var brdRT = brd.AddComponent<RectTransform>();
        brdRT.anchorMin = Vector2.zero; brdRT.anchorMax = Vector2.one;
        brdRT.offsetMin = new Vector2(-1f,-1f); brdRT.offsetMax = new Vector2(1f,1f);
        brd.AddComponent<Image>().color = new Color(corBorda.r, corBorda.g, corBorda.b, disponivel ? 0.55f : 0.20f);

        // irmão 1: corpo
        var corpo = new GameObject("Corpo"); corpo.transform.SetParent(go.transform, false);
        var corpoRT = corpo.AddComponent<RectTransform>();
        corpoRT.anchorMin = Vector2.zero; corpoRT.anchorMax = Vector2.one; corpoRT.offsetMin = corpoRT.offsetMax = Vector2.zero;
        corpo.AddComponent<Image>().color = disponivel ? corBotao : corBloqueado;

        // irmão 2: acento esquerdo com cor do elemento
        var ac = new GameObject("Ac"); ac.transform.SetParent(go.transform, false);
        var acRT = ac.AddComponent<RectTransform>();
        acRT.anchorMin = Vector2.zero; acRT.anchorMax = new Vector2(0f,1f);
        acRT.offsetMin = Vector2.zero; acRT.offsetMax = new Vector2(5f,0f);
        ac.AddComponent<Image>().color = disponivel ? corElem : new Color(corElem.r,corElem.g,corElem.b,0.25f);

        // ícone
        if (skill.icon != null)
        {
            var ic = new GameObject("Icone"); ic.transform.SetParent(go.transform, false);
            var icRT = ic.AddComponent<RectTransform>();
            icRT.anchorMin = new Vector2(0f,0f); icRT.anchorMax = new Vector2(0f,1f);
            icRT.pivot = new Vector2(0f,0.5f);
            icRT.anchoredPosition = new Vector2(10f,0f); icRT.sizeDelta = new Vector2(44f,-10f);
            var icImg = ic.AddComponent<Image>(); icImg.sprite = skill.icon; icImg.preserveAspect = true;
        }

        var nome = CriarTexto(go, "Nome", skill.GetDisplayName(), disponivel ? corTexto : new Color(0.5f,0.5f,0.5f), 14f, FontStyles.Bold);
        Ancora(nome, new Vector2(0f,0.5f), new Vector2(0.7f,1f), new Vector2(60f,0f), Vector2.zero);
        nome.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        var statusStr = disponivel ? Loc.T("ui.no_element") : $"{Loc.T("ui.has_element")}: {ElementRegistry.Instance?.GetNome(skill.appliedElement) ?? skill.appliedElement.ToString()}";
        var status = CriarTexto(go, "Status", statusStr,
            disponivel ? new Color(0.5f,0.7f,0.5f) : new Color(0.6f,0.4f,0.4f), 10f);
        Ancora(status, new Vector2(0f,0f), new Vector2(0.7f,0.5f), new Vector2(60f,0f), Vector2.zero);
        status.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        if (disponivel)
        {
            var cap = skill;
            Color corBtn = corElem * 0.5f + corBotao * 0.5f;
            var btn = CriarBotao(go, "BtnInfundir", Loc.T("ui.infuse"), corBtn, () => SelecionarSkill(cap));
            Ancora(btn, new Vector2(0.72f,0.15f), new Vector2(0.97f,0.85f));
        }
    }

    void SelecionarSkill(SkillData skill) { skillSelecionada = skill; MostrarEtapa2(); }

    // Entrada do painel: fade + leve scale-up (unscaled, pois a escolha pausa o jogo).
    void AnimarEntrada(GameObject painel)
    {
        if (painel == null) return;
        var cg = painel.GetComponent<CanvasGroup>() ?? painel.AddComponent<CanvasGroup>();
        StartCoroutine(RotinaEntrada(painel.transform as RectTransform, cg));
    }
    IEnumerator RotinaEntrada(RectTransform rt, CanvasGroup cg)
    {
        if (rt == null || cg == null) yield break;
        Vector3 alvo = rt.localScale;
        const float dur = 0.24f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float e = 1f - Mathf.Pow(1f - t / dur, 3f); // ease-out cubic
            rt.localScale = alvo * Mathf.Lerp(0.85f, 1f, e);
            cg.alpha = e;
            yield return null;
        }
        rt.localScale = alvo; cg.alpha = 1f;
    }

    void MostrarEtapa2()
    {
        if (painelEtapa1 != null) Destroy(painelEtapa1);
        if (painelEtapa2 != null) Destroy(painelEtapa2);

        var def = ElementRegistry.Instance?.Get(elementoAtual);
        string nomeElem = def != null ? def.nomeDisplay : elementoAtual.ToString();
        Color corElem   = def != null ? def.cor : Color.white;

        // Painel mais largo — estilo SkillChoice
        painelEtapa2 = CriarPainel("PainelEtapa2");
        var rt = painelEtapa2.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.10f, 0.10f);
        rt.anchorMax = new Vector2(0.90f, 0.90f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var titulo = CriarTexto(painelEtapa2, "Titulo",
            $"{skillSelecionada.GetDisplayName()} + {nomeElem}", corElem, 22f, FontStyles.Bold);
        Ancora(titulo, new Vector2(0f,0.88f), new Vector2(1f,1f), new Vector2(12f,0f), new Vector2(-12f,0f));

        var sub = CriarTexto(painelEtapa2, "Sub", Loc.T("ui.choose_elem_power"), corTexto, 13f);
        Ancora(sub, new Vector2(0f,0.81f), new Vector2(1f,0.89f), new Vector2(12f,0f), new Vector2(-12f,0f));

        // Container horizontal dos cards
        var container = new GameObject("CardsContainer");
        container.transform.SetParent(painelEtapa2.transform, false);
        var cRT = container.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.02f,0.14f); cRT.anchorMax = new Vector2(0.98f,0.80f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.padding = new RectOffset(12,12,8,8);
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        bool ehDefensiva = skillSelecionada != null && !skillSelecionada.EhSkillDeAtaque();
        if (ehDefensiva && def?.caracteristicasDefensivas != null)
        {
            for (int i = 0; i < Mathf.Min(def.caracteristicasDefensivas.Length, 2); i++)
            {
                var car = def.caracteristicasDefensivas[i];
                if (car == null) continue;
                CriarCardPoderDefensivo(container, car, i, corElem);
            }
        }
        else if (def?.caracteristicas != null)
        {
            for (int i = 0; i < Mathf.Min(def.caracteristicas.Length, 2); i++)
            {
                var car = def.caracteristicas[i];
                if (car == null) continue;
                CriarCardPoder(container, car, i, corElem);
            }
        }

        var aviso = CriarTexto(painelEtapa2, "Aviso", Loc.T("ui.choice_permanent"), new Color(0.6f,0.5f,0.5f), 10f);
        Ancora(aviso, new Vector2(0f,0.07f), new Vector2(1f,0.14f), new Vector2(12f,0f), new Vector2(-12f,0f));

        var btnV = CriarBotao(painelEtapa2, "BtnVoltar", "< " + Loc.T("ui.back"), new Color(0.18f,0.12f,0.28f), MostrarEtapa1);
        Ancora(btnV, new Vector2(0.30f,0.01f), new Vector2(0.70f,0.08f));

        AnimarEntrada(painelEtapa2);
    }

    // Molduras de card iguais às das outras UIs de escolha (skill/status/evolução). Build-safe:
    // tenta Resources primeiro; no editor cai pro AssetDatabase. Cacheado.
    static bool _framesCarregados;
    static Sprite _cartaFrame, _slotFrame;
    static void CarregarFrames()
    {
        if (_framesCarregados) return;
        _framesCarregados = true;
        var res = Resources.LoadAll<Sprite>("UI/skill_card/cartaskill");
        if (res != null)
            foreach (var s in res)
            {
                if (s.name == "carta_frame") _cartaFrame = s;
                else if (s.name == "slot_frame") _slotFrame = s;
            }
#if UNITY_EDITOR
        if (_cartaFrame == null || _slotFrame == null)
        {
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/assets/UI/skill_card/cartaskill.png");
            foreach (var a in all)
                if (a is Sprite s)
                {
                    if (s.name == "carta_frame" && _cartaFrame == null) _cartaFrame = s;
                    else if (s.name == "slot_frame" && _slotFrame == null) _slotFrame = s;
                }
        }
#endif
    }
    static Sprite CartaFrame() { CarregarFrames(); return _cartaFrame; }
    static Sprite SlotFrame()  { CarregarFrames(); return _slotFrame;  }

    static Sprite[] _caricIcons;
    static Sprite GetCaracteristicaIcone(CharacteristicType tipo)
    {
        if (_caricIcons == null)
            _caricIcons = Resources.LoadAll<Sprite>("UI/caracteristicas_icons");
        if (_caricIcons == null || _caricIcons.Length == 0) return null;
        string nome = tipo.ToString();
        foreach (var s in _caricIcons)
            if (s.name == nome) return s;
        return null;
    }

    void CriarCardPoder(GameObject container, ElementCharacteristic car, int idx, Color corElem)
    {
        Sprite caricIcone = GetCaracteristicaIcone(car.tipo);
        CriarCardGenerico(container,
            Loc.T($"characteristic.name.{car.tipo.ToString().ToLower()}"),
            Loc.T($"characteristic.desc.{car.tipo.ToString().ToLower()}"),
            idx, corElem, caricIcone);
    }

    void CriarCardPoderDefensivo(GameObject container, DefensiveCharacteristic car, int idx, Color corElem)
    {
        CriarCardGenerico(container,
            Loc.T($"defchar.name.{car.tipo.ToString().ToLower()}"),
            Loc.T($"defchar.desc.{car.tipo.ToString().ToLower()}"),
            idx, corElem, null);
    }

    void CriarCardGenerico(GameObject container, string nomeStr, string descStr, int idx, Color corElem, Sprite icone)
    {
        // root sem Image — sibling pattern
        var go = new GameObject($"Card_{idx}");
        go.transform.SetParent(container.transform, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f; le.flexibleHeight = 1f;

        // Fundo = carta_frame (Simple), MESMA construção do card de skill; fallback escuro.
        Image bg = go.AddComponent<Image>();
        var frameSprite = CartaFrame();
        bool comFrame = frameSprite != null;
        if (comFrame) { bg.sprite = frameSprite; bg.type = Image.Type.Simple; bg.color = Color.white; }
        else bg.color = corFundo;

        // Área do ícone (topo), com o slot 9-slice (slot_frame) — igual ao card de skill.
        var iconArea = new GameObject("IconArea"); iconArea.transform.SetParent(go.transform, false);
        var iaRT = iconArea.AddComponent<RectTransform>();
        iaRT.anchorMin = new Vector2(0f, 0.66f); iaRT.anchorMax = new Vector2(1f, 0.96f);
        iaRT.offsetMin = iaRT.offsetMax = Vector2.zero;

        var slotGO = new GameObject("Slot"); slotGO.transform.SetParent(iconArea.transform, false);
        var slotRT = slotGO.AddComponent<RectTransform>();
        slotRT.anchorMin = new Vector2(0.32f, 0.05f); slotRT.anchorMax = new Vector2(0.68f, 0.95f);
        slotRT.offsetMin = slotRT.offsetMax = Vector2.zero;
        var slotImg = slotGO.AddComponent<Image>();
        var slotSp = SlotFrame();
        if (slotSp != null) { slotImg.sprite = slotSp; slotImg.type = Image.Type.Sliced; slotImg.fillCenter = false; slotImg.color = Color.white; }
        else slotImg.color = new Color(corElem.r, corElem.g, corElem.b, 0.25f);
        slotImg.raycastTarget = false;

        if (icone != null)
        {
            var inner = new GameObject("IconInner"); inner.transform.SetParent(slotGO.transform, false);
            var inRT = inner.AddComponent<RectTransform>();
            inRT.anchorMin = new Vector2(0.16f, 0.16f); inRT.anchorMax = new Vector2(0.84f, 0.84f);
            inRT.offsetMin = inRT.offsetMax = Vector2.zero;
            var inImg = inner.AddComponent<Image>(); inImg.sprite = icone; inImg.preserveAspect = true; inImg.raycastTarget = false;
        }
        else
        {
            var letra = CriarTexto(slotGO, "Letra", idx == 0 ? "A" : "B", corElem, 26f, FontStyles.Bold);
            Ancora(letra, Vector2.zero, Vector2.one);
        }

        // Nome
        var nomeGO = CriarTexto(go, "Nome", nomeStr, new Color(0.95f, 0.82f, 0.40f), 15f, FontStyles.Bold);
        Ancora(nomeGO, new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.66f));
        nomeGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Descrição
        var descGO = CriarTexto(go, "Desc", descStr, new Color(0.90f, 0.82f, 0.65f), 11f);
        Ancora(descGO, new Vector2(0.10f, 0.10f), new Vector2(0.90f, 0.50f));
        var dTxt = descGO.GetComponent<TextMeshProUGUI>();
        dTxt.textWrappingMode = TextWrappingModes.Normal;
        dTxt.alignment = TextAlignmentOptions.Top;

        // Button no root
        var btn = go.AddComponent<Button>(); btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = new ColorBlock{
            normalColor      = comFrame ? Color.white : corFundo,
            highlightedColor = comFrame ? new Color(1f,0.96f,0.85f) : new Color(corFundo.r+0.10f,corFundo.g+0.08f,corFundo.b+0.18f,1f),
            pressedColor     = comFrame ? new Color(0.85f,0.83f,0.78f) : new Color(0.03f,0.02f,0.05f,1f),
            selectedColor    = comFrame ? new Color(1f,0.96f,0.85f) : corFundo,
            disabledColor    = new Color(1f,1f,1f,0.5f),
            colorMultiplier  = 1f, fadeDuration = 0.1f
        };
        int capture = idx;
        btn.onClick.AddListener(() => ConfirmarEscolha(capture));
    }

    void ConfirmarEscolha(int indice)
    {
        if (skillSelecionada == null) return;
        skillSelecionada.appliedElement = elementoAtual;
        skillSelecionada.appliedCharacteristicIndex = indice;
        var def = ElementRegistry.Instance?.Get(elementoAtual);
        if (def != null) skillSelecionada.elementColor = def.cor;
        SkillIconsHUD.Instance?.AtualizarBadgeElemento(skillSelecionada);

        // Co-op: avisa o fantoche do colega pra recolorir a cópia cosmética desta skill.
        if (NetSpawn.EmRede)
        {
            var pl = PlayerStats.Local;
            var fx = pl != null ? pl.GetComponent<SkillFxNet>() : null;
            var pn = pl != null ? pl.GetComponent<PlayerNet>() : null;
            if (fx != null && pn != null)
            {
                int idxSkill = fx.IndiceSkill(skillSelecionada);
                if (idxSkill >= 0) pn.SincronizarInfusao(idxSkill, (int)elementoAtual);
            }
        }

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
        CoopPause.LiberarEscolha();
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
        bd.AddComponent<Image>().color = new Color(0.04f, 0.02f, 0.08f, 0.88f);
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
        // container sem Image — irmãos controlam renderização
        var go = new GameObject(nome); go.transform.SetParent(pai.transform, false);
        go.AddComponent<RectTransform>();

        // irmão 0: borda dourada
        var brd = new GameObject("Brd"); brd.transform.SetParent(go.transform, false);
        var brdRT = brd.AddComponent<RectTransform>();
        brdRT.anchorMin = Vector2.zero; brdRT.anchorMax = Vector2.one;
        brdRT.offsetMin = new Vector2(-1f,-1f); brdRT.offsetMax = new Vector2(1f,1f);
        brd.AddComponent<Image>().color = new Color(corBorda.r, corBorda.g, corBorda.b, 0.80f);

        // irmão 1: corpo
        var corpo = new GameObject("Corpo"); corpo.transform.SetParent(go.transform, false);
        var corpoRT = corpo.AddComponent<RectTransform>();
        corpoRT.anchorMin = Vector2.zero; corpoRT.anchorMax = Vector2.one;
        corpoRT.offsetMin = corpoRT.offsetMax = Vector2.zero;
        Image img = corpo.AddComponent<Image>(); img.color = cor;

        // irmão 2: bevel topo
        var hit = new GameObject("HiT"); hit.transform.SetParent(go.transform, false);
        var hitRT = hit.AddComponent<RectTransform>();
        hitRT.anchorMin = new Vector2(0f,1f); hitRT.anchorMax = new Vector2(1f,1f);
        hitRT.offsetMin = new Vector2(0f,-2f); hitRT.offsetMax = Vector2.zero;
        hit.AddComponent<Image>().color = new Color(1f,0.9f,0.6f,0.12f);

        // irmão 3: sombra base
        var shb = new GameObject("ShB"); shb.transform.SetParent(go.transform, false);
        var shbRT = shb.AddComponent<RectTransform>();
        shbRT.anchorMin = Vector2.zero; shbRT.anchorMax = new Vector2(1f,0f);
        shbRT.offsetMin = Vector2.zero; shbRT.offsetMax = new Vector2(0f,2f);
        shb.AddComponent<Image>().color = new Color(0f,0f,0f,0.50f);

        // irmão 4: acento lateral dourado
        var ac = new GameObject("Ac"); ac.transform.SetParent(go.transform, false);
        var acRT = ac.AddComponent<RectTransform>();
        acRT.anchorMin = Vector2.zero; acRT.anchorMax = new Vector2(0f,1f);
        acRT.offsetMin = Vector2.zero; acRT.offsetMax = new Vector2(4f,0f);
        ac.AddComponent<Image>().color = new Color(corBorda.r, corBorda.g, corBorda.b, 0.90f);

        // Button
        Button btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        Color hov = new Color(Mathf.Min(cor.r*1.4f,1f), Mathf.Min(cor.g*1.4f,1f), Mathf.Min(cor.b*1.4f,1f), cor.a);
        btn.colors = new ColorBlock{
            normalColor=cor, highlightedColor=hov,
            pressedColor=new Color(cor.r*0.6f, cor.g*0.6f, cor.b*0.6f, cor.a),
            selectedColor=cor, disabledColor=new Color(cor.r, cor.g, cor.b, 0.5f),
            colorMultiplier=1f, fadeDuration=0.1f
        };
        btn.onClick.AddListener(cb);

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
