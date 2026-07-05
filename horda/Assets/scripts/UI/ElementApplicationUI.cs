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

    // Etapa 2 no estilo "carta de evolução": cartas flutuando sobre o backdrop escuro.
    static readonly Vector2 _cardSizeEvo = new Vector2(300f, 450f);
    readonly List<GameObject> _cartasEtapa2 = new List<GameObject>();
    bool _selecionouEtapa2;

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

    // Etapa 1 recriada no MESMO visual da evolução/etapa 2: sem moldura de masmorra — uma carta por
    // skill flutua sobre o backdrop escuro. Disponíveis são clicáveis; já infundidas ficam esmaecidas.
    void MostrarEtapa1()
    {
        if (painelEtapa1 != null) { Destroy(painelEtapa1); painelEtapa1 = null; }
        if (painelEtapa2 != null) { Destroy(painelEtapa2); painelEtapa2 = null; }
        _selecionouEtapa2 = false;

        var reg = ElementRegistry.Instance;
        var def = reg?.Get(elementoAtual);
        string nomeElem = def != null ? def.nomeDisplay : elementoAtual.ToString();

        painelEtapa1 = new GameObject("PainelEtapa1", typeof(RectTransform));
        painelEtapa1.transform.SetParent(canvas.transform, false);
        var rt = painelEtapa1.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;

        var titulo = CriarTexto(painelEtapa1, "Titulo",
            string.Format(Loc.T("ui.elem_collected_fmt"), nomeElem.ToUpper()), corTitulo, 30f, FontStyles.Bold);
        Ancora(titulo, new Vector2(0.1f, 0.84f), new Vector2(0.9f, 0.94f));
        var sub = CriarTexto(painelEtapa1, "Sub", Loc.T("ui.choose_skill_infuse"), corTexto, 16f);
        Ancora(sub, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.84f));

        var skills = ObterSkillsDisponiveis();
        bool temDisponivel = skills.Exists(s => s != null && s.appliedElement == ElementType.None);

        // Nenhuma skill infundível → mensagem + descartar (não monta cards).
        if (!temDisponivel)
        {
            string msg = skills.Count == 0 ? Loc.T("ui.no_skills_yet") : Loc.T("ui.all_skills_infused");
            var aviso = CriarTexto(painelEtapa1, "Aviso", msg, corTexto, 18f);
            Ancora(aviso, new Vector2(0.2f, 0.45f), new Vector2(0.8f, 0.6f));
            var btnF = CriarBotao(painelEtapa1, "BtnFechar", Loc.T("ui.discard"), new Color(0.4f, 0.1f, 0.1f), Fechar);
            Ancora(btnF, new Vector2(0.35f, 0.12f), new Vector2(0.65f, 0.19f));
            return;
        }

        var containerGO = new GameObject("CardsRow", typeof(RectTransform));
        containerGO.transform.SetParent(painelEtapa1.transform, false);
        var contRT = containerGO.GetComponent<RectTransform>();
        contRT.anchorMin = contRT.anchorMax = new Vector2(0.5f, 0.5f);
        contRT.pivot     = new Vector2(0.5f, 0.5f);
        contRT.sizeDelta = new Vector2(1200f, 500f);
        contRT.anchoredPosition = new Vector2(0f, -10f);

        StartCoroutine(MontarCartasEtapa1(containerGO.transform, skills));
    }

    IEnumerator MontarCartasEtapa1(Transform container, List<SkillData> skills)
    {
        var cartas = new List<GameObject>();
        yield return null;
        foreach (var skill in skills)
        {
            if (skill == null) continue;
            bool disp = skill.appliedElement == ElementType.None;
            var card = CriarCartaSkillInfusao(container, skill, disp);
            if (card != null) cartas.Add(card);
        }
        yield return null;
        PosicionarEAnimarCartas(cartas);
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

    // Etapa 2 recriada no MESMO visual da tela de evolução: sem moldura de painel — as cartas
    // (com a moldura de evolução) flutuam sobre o backdrop escuro do canvas, entram animadas e,
    // ao escolher, disparam o efeito de fragmentos voando pro player (CartaSelecaoEfeito).
    void MostrarEtapa2()
    {
        if (painelEtapa1 != null) { Destroy(painelEtapa1); painelEtapa1 = null; }
        if (painelEtapa2 != null) { Destroy(painelEtapa2); painelEtapa2 = null; }
        foreach (var c in _cartasEtapa2) if (c != null) Destroy(c);
        _cartasEtapa2.Clear();
        _selecionouEtapa2 = false;

        var def = ElementRegistry.Instance?.Get(elementoAtual);
        string nomeElem = def != null ? def.nomeDisplay : elementoAtual.ToString();
        Color corElem   = def != null ? def.cor : Color.white;

        // Container transparente em tela cheia (sem CriarPainel — nada de moldura de masmorra aqui).
        painelEtapa2 = new GameObject("PainelEtapa2", typeof(RectTransform));
        painelEtapa2.transform.SetParent(canvas.transform, false);
        var rt = painelEtapa2.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Título enxuto no topo (contexto skill + elemento) — leve, sem banner pesado.
        var titulo = CriarTexto(painelEtapa2, "Titulo",
            $"{skillSelecionada.GetDisplayName()} + {nomeElem.ToUpper()}", corTitulo, 30f, FontStyles.Bold);
        Ancora(titulo, new Vector2(0.1f, 0.84f), new Vector2(0.9f, 0.94f));
        var sub = CriarTexto(painelEtapa2, "Sub", Loc.T("ui.choose_elem_power"), corTexto, 16f);
        Ancora(sub, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.84f));

        // Fileira central das cartas — mesma geometria da evolução (posicionamento manual, sem LayoutGroup).
        var containerGO = new GameObject("CardsRow", typeof(RectTransform));
        containerGO.transform.SetParent(painelEtapa2.transform, false);
        var contRT = containerGO.GetComponent<RectTransform>();
        contRT.anchorMin = contRT.anchorMax = new Vector2(0.5f, 0.5f);
        contRT.pivot     = new Vector2(0.5f, 0.5f);
        contRT.sizeDelta = new Vector2(1200f, 500f);
        contRT.anchoredPosition = new Vector2(0f, -10f);

        // Coleta as opções (nome/desc/ícone) NA MESMA ORDEM que ConfirmarEscolha(idx) espera.
        var opcoes = new List<(string nome, string desc, Sprite icone)>();
        bool ehDefensiva = skillSelecionada != null && !skillSelecionada.EhSkillDeAtaque();
        if (ehDefensiva && def?.caracteristicasDefensivas != null)
        {
            for (int i = 0; i < Mathf.Min(def.caracteristicasDefensivas.Length, 2); i++)
            {
                var car = def.caracteristicasDefensivas[i];
                if (car == null) { opcoes.Add((null, null, null)); continue; }
                opcoes.Add((
                    Loc.T($"defchar.name.{car.tipo.ToString().ToLower()}"),
                    Loc.T($"defchar.desc.{car.tipo.ToString().ToLower()}"),
                    GetCaracteristicaDefensivaIcone(car.tipo)));
            }
        }
        else if (def?.caracteristicas != null)
        {
            for (int i = 0; i < Mathf.Min(def.caracteristicas.Length, 2); i++)
            {
                var car = def.caracteristicas[i];
                if (car == null) { opcoes.Add((null, null, null)); continue; }
                opcoes.Add((
                    Loc.T($"characteristic.name.{car.tipo.ToString().ToLower()}"),
                    Loc.T($"characteristic.desc.{car.tipo.ToString().ToLower()}"),
                    GetCaracteristicaIcone(car.tipo)));
            }
        }

        StartCoroutine(MontarCartasEtapa2(containerGO.transform, opcoes, nomeElem, corElem));

        // Aviso de permanência + voltar (mantidos: infusão é permanente e tem etapa anterior).
        var aviso = CriarTexto(painelEtapa2, "Aviso", Loc.T("ui.choice_permanent"), new Color(0.6f,0.5f,0.5f), 12f);
        Ancora(aviso, new Vector2(0.2f, 0.10f), new Vector2(0.8f, 0.15f));
        var btnV = CriarBotao(painelEtapa2, "BtnVoltar", "< " + Loc.T("ui.back"), new Color(0.18f,0.12f,0.28f), MostrarEtapa1);
        Ancora(btnV, new Vector2(0.36f, 0.03f), new Vector2(0.64f, 0.09f));
    }

    IEnumerator MontarCartasEtapa2(Transform container,
        List<(string nome, string desc, Sprite icone)> opcoes, string nomeElem, Color corElem)
    {
        // 1) cria as cartas (sem animar ainda)
        yield return null;
        for (int i = 0; i < opcoes.Count; i++)
        {
            var o = opcoes[i];
            if (o.nome == null) continue;
            var card = CriarCartaCaracteristica(container, o.nome, o.desc, o.icone, i, nomeElem, corElem);
            if (card != null) _cartasEtapa2.Add(card);
        }

        // 2+3) posiciona centralizado + anima entrada/hover (compartilhado com a etapa 1)
        yield return null;
        PosicionarEAnimarCartas(_cartasEtapa2);
    }

    // Layout em fileira central (geometria da evolução) + animação de entrada escalonada. O animador
    // de hover/flutuação só entra em cartas clicáveis (uma skill já infundida fica esmaecida e parada).
    void PosicionarEAnimarCartas(List<GameObject> cartas)
    {
        int n = cartas.Count;
        float cw = _cardSizeEvo.x, esp = 40f;
        float largTotal = n * cw + Mathf.Max(0, n - 1) * esp;
        for (int i = 0; i < n; i++)
        {
            var crt = cartas[i] != null ? cartas[i].GetComponent<RectTransform>() : null;
            if (crt == null) continue;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(-largTotal * 0.5f + cw * 0.5f + i * (cw + esp), 0f);
        }
        for (int i = 0; i < n; i++)
        {
            if (cartas[i] == null) continue;
            var b = cartas[i].GetComponent<Button>() ?? cartas[i].GetComponentInChildren<Button>();
            if (b != null && b.interactable) cartas[i].AddComponent<EvoCardAnimador>();
            StartCoroutine(AnimarEntradaCard(cartas[i], i * 0.12f));
        }
    }

    IEnumerator AnimarEntradaCard(GameObject card, float delay)
    {
        yield return null;
        for (float t = 0f; t < delay; t += Time.unscaledDeltaTime) yield return null;
        if (card == null) yield break;

        var rt = card.GetComponent<RectTransform>();
        Vector2 posAlvo = rt != null ? rt.anchoredPosition : Vector2.zero;
        card.transform.localScale = Vector3.zero;
        if (rt != null) rt.anchoredPosition = posAlvo + new Vector2(0f, -120f);

        for (float e = 0f; e < 0.4f; e += Time.unscaledDeltaTime)
        {
            if (card == null || _selecionouEtapa2) yield break;
            float p = e / 0.4f;
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            float bounce = 1f + Mathf.Sin(p * Mathf.PI) * 0.18f;
            card.transform.localScale = Vector3.one * (ease * bounce);
            if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(posAlvo + new Vector2(0f, -120f), posAlvo, ease);
            yield return null;
        }
        if (card != null && !_selecionouEtapa2)
        {
            card.transform.localScale = Vector3.one;
            if (rt != null) rt.anchoredPosition = posAlvo;
            var anim = card.GetComponent<EvoCardAnimador>();
            if (anim != null) anim.EntradaConcluida(posAlvo);
        }
    }

    void SelecionarEtapa2(int idx, GameObject carta)
    {
        if (_selecionouEtapa2) return;
        _selecionouEtapa2 = true;
        foreach (var c in _cartasEtapa2)
        {
            if (c == null) continue;
            var b = c.GetComponent<Button>() ?? c.GetComponentInChildren<Button>();
            if (b != null) b.interactable = false;
            var a = c.GetComponent<EvoCardAnimador>();
            if (a != null) { a.StopAllCoroutines(); a.enabled = false; }
        }
        // Efeito "fragmentos voando pro player", idêntico à seleção de evolução; depois aplica.
        CartaSelecaoEfeito.Executar(carta, _cartasEtapa2, () => ConfirmarEscolha(idx));
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
    static Sprite IconePorNome(string nome)
    {
        if (_caricIcons == null)
            _caricIcons = Resources.LoadAll<Sprite>("UI/caracteristicas_icons");
        if (_caricIcons == null || _caricIcons.Length == 0) return null;
        foreach (var s in _caricIcons)
            if (s.name == nome) return s;
        return null;
    }

    static Sprite GetCaracteristicaIcone(CharacteristicType tipo) => IconePorNome(tipo.ToString());

    // As características DEFENSIVAS não têm sprite próprio em Resources → reusa o ícone ofensivo
    // temático mais próximo (mesma família de elemento), pra não ficarem sem ícone.
    static readonly System.Collections.Generic.Dictionary<DefensiveCharacteristicType, string> _defIconMap =
        new System.Collections.Generic.Dictionary<DefensiveCharacteristicType, string>
    {
        { DefensiveCharacteristicType.AuraIgnea,          "Queimadura"   },
        { DefensiveCharacteristicType.RetaliacaoChamas,   "Explosao"     },
        { DefensiveCharacteristicType.EsquivaVentosa,     "Rajada"       },
        { DefensiveCharacteristicType.SoproRepulsor,      "Recuo"        },
        { DefensiveCharacteristicType.PeleDePedra,        "EscudoPedra"  },
        { DefensiveCharacteristicType.FundacaoFirme,      "EscudoPedra"  },
        { DefensiveCharacteristicType.MareRestauradora,   "Cura"         },
        { DefensiveCharacteristicType.FluxoVital,         "Cura"         },
        { DefensiveCharacteristicType.DescargaReativa,    "Paralisia"    },
        { DefensiveCharacteristicType.CorrenteReflexiva,  "Cadeia"       },
        { DefensiveCharacteristicType.ArmaduraGelida,     "Congelamento" },
        { DefensiveCharacteristicType.ToqueCongelante,    "Lentidao"     },
        { DefensiveCharacteristicType.Espinhos,           "Enraizamento" },
        { DefensiveCharacteristicType.RaizesProtetoras,   "Enraizamento" },
        { DefensiveCharacteristicType.DrenagemSombria,    "RouboVida"    },
        { DefensiveCharacteristicType.MantoAmaldicoado,   "Maldicao"     },
        { DefensiveCharacteristicType.BencaoSagrada,      "Sagrado"      },
        { DefensiveCharacteristicType.LuzOfuscante,       "Cegamento"    },
        { DefensiveCharacteristicType.CaosDefensivo,      "Caos"         },
        { DefensiveCharacteristicType.PragaReativa,       "Infeccao"     },
    };

    static Sprite GetCaracteristicaDefensivaIcone(DefensiveCharacteristicType tipo)
        => _defIconMap.TryGetValue(tipo, out var nome) ? IconePorNome(nome) : null;

    // Construtor de carta no MESMO molde da evolução (usado pelas DUAS etapas da infusão): instancia
    // o prefab de card, inicializa via SkillCardRuntimeManager (nome/desc/ícone), troca a moldura pela
    // de evolução e usa a área de "raridade" pra um rótulo colorido. NÃO fia o clique — cada etapa
    // liga o próprio (etapa1 → escolher skill; etapa2 → confirmar com efeito). Deixa o Button pronto.
    GameObject ConstruirCartaEvo(Transform container, string cardName, string nomeStr, string descStr,
        Sprite icone, string rotulo, Color rotuloCor)
    {
        GameObject prefab = ObterCardPrefab();
        if (prefab == null) return ConstruirCartaFallbackEvo(container, cardName, nomeStr, descStr, icone, rotulo, rotuloCor);

        var card = Instantiate(prefab, container);
        card.name = cardName;
        card.SetActive(true);

        var rect = card.GetComponent<RectTransform>();
        if (rect != null) { rect.localScale = Vector3.one; rect.anchoredPosition = Vector2.zero; rect.sizeDelta = _cardSizeEvo; }
        var le = card.GetComponent<LayoutElement>(); if (le == null) le = card.AddComponent<LayoutElement>();
        le.preferredWidth = _cardSizeEvo.x; le.preferredHeight = _cardSizeEvo.y; le.flexibleWidth = 0; le.flexibleHeight = 0;

        bool temRotulo = !string.IsNullOrEmpty(rotulo);
        var rm = card.GetComponent<SkillCardRuntimeManager>();
        if (rm != null)
        {
            var temp = ScriptableObject.CreateInstance<SkillData>();
            temp.skillName   = nomeStr;
            temp.description  = descStr;
            temp.icon        = icone;
            temp.element     = PlayerStats.Element.None;
            rm.InitializeRuntime(temp);
            Destroy(temp);

            // aperta a descrição pra dentro da moldura estreita da evolução (margem + truncate)
            foreach (var t in card.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                string nd = t.name.ToLower();
                if (nd.Contains("desc") || nd.Contains("detail"))
                {
                    t.margin           = new Vector4(16f, t.margin.y, 16f, t.margin.w);
                    t.textWrappingMode = TMPro.TextWrappingModes.Normal;
                    t.overflowMode     = TMPro.TextOverflowModes.Truncate;
                    t.enableAutoSizing = true; t.fontSizeMin = 6f;
                    t.fontSizeMax      = Mathf.Min(t.fontSizeMax > 0f ? t.fontSizeMax : t.fontSize, 11f);
                }
            }
            // reaproveita a área de "raridade" pro rótulo colorido (elemento / status da skill)
            foreach (var t in card.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                string nm = t.name.ToLower();
                if (nm.Contains("rarity") || nm.Contains("rarid"))
                { t.gameObject.SetActive(temRotulo); if (temRotulo) { t.text = rotulo; t.color = rotuloCor; } }
                else if (nm.Contains("stat") || nm.Contains("bonus"))
                    t.gameObject.SetActive(false);
            }
            foreach (var img in card.GetComponentsInChildren<Image>(true))
                if (img.name == "RarityBorder") { img.gameObject.SetActive(temRotulo); if (temRotulo) img.color = rotuloCor; }
        }
        else
        {
            foreach (var t in card.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                string n = t.name.ToLower();
                if      (n.Contains("name") || n.Contains("nome") || n.Contains("title")) t.text = nomeStr;
                else if (n.Contains("desc") || n.Contains("detail")) t.text = descStr;
                else if (n.Contains("rarity") || n.Contains("rarid")) { t.gameObject.SetActive(temRotulo); if (temRotulo) { t.text = rotulo; t.color = rotuloCor; } }
            }
            var inner = card.transform.Find("IconArea/IconImageSlot/IconInner")?.GetComponent<Image>();
            if (inner != null && icone != null) { inner.sprite = icone; inner.color = Color.white; inner.preserveAspect = true; }
        }

        // troca a moldura/slot pela da evolução (editor); em build mantém a do prefab
        var cardBg = card.GetComponent<Image>();
        if (cardBg != null)
        {
            var spFundo = CarregarSpriteEvo("Assets/assets/UI/skill_card/cartaevolução.ase", "cartaevolução");
            if (spFundo != null) { cardBg.sprite = spFundo; cardBg.color = Color.white; cardBg.type = Image.Type.Simple; }
        }
        var slotT = card.transform.Find("IconArea/IconImageSlot");
        var slotImg = slotT != null ? slotT.GetComponent<Image>() : null;
        if (slotImg != null)
        {
            var spSlot = CarregarSpriteEvo("Assets/assets/UI/skill_card/slotevolução.ase", "slotevolução");
            if (spSlot != null) { slotImg.sprite = spSlot; slotImg.color = Color.white; slotImg.type = Image.Type.Simple; }
        }

        // Garante um Button pronto (listeners limpos); o clique é ligado pela etapa chamadora.
        var btn = card.GetComponent<Button>() ?? card.GetComponentInChildren<Button>();
        if (btn == null) { btn = card.AddComponent<Button>(); if (cardBg != null) btn.targetGraphic = cardBg; }
        btn.onClick.RemoveAllListeners();
        btn.interactable = true;
        return card;
    }

    // Etapa 2 — carta de característica: clique dispara os fragmentos voando pro player + confirma.
    GameObject CriarCartaCaracteristica(Transform container, string nomeStr, string descStr,
        Sprite icone, int idx, string nomeElem, Color corElem)
    {
        var card = ConstruirCartaEvo(container, $"CartaInfusao_{idx}", nomeStr, descStr, icone, nomeElem.ToUpper(), corElem);
        if (card == null) return null;
        var btn = card.GetComponent<Button>() ?? card.GetComponentInChildren<Button>();
        if (btn != null)
        {
            int capture = idx; var cap = card;
            btn.onClick.AddListener(() => SelecionarEtapa2(capture, cap));
        }
        return card;
    }

    // Etapa 1 — carta de skill a infundir: disponível → clique escolhe (vai pra etapa 2); já infundida
    // → carta esmaecida e não-clicável, com o elemento aplicado no rótulo.
    GameObject CriarCartaSkillInfusao(Transform container, SkillData skill, bool disponivel)
    {
        string rotulo = disponivel
            ? Loc.T("ui.no_element")
            : $"{Loc.T("ui.has_element")}: {(ElementRegistry.Instance != null ? ElementRegistry.Instance.GetNome(skill.appliedElement) : skill.appliedElement.ToString())}";
        Color rotuloCor = disponivel ? new Color(0.55f, 0.85f, 0.55f) : new Color(0.72f, 0.5f, 0.5f);

        var card = ConstruirCartaEvo(container, $"CartaSkill_{skill.skillName}",
            skill.GetDisplayName(), skill.GetDisplayDescription(), skill.icon, rotulo, rotuloCor);
        if (card == null) return null;

        var btn = card.GetComponent<Button>() ?? card.GetComponentInChildren<Button>();
        if (btn != null)
        {
            if (disponivel) { var cap = skill; btn.onClick.AddListener(() => SelecionarSkill(cap)); btn.interactable = true; }
            else btn.interactable = false;
        }
        if (!disponivel)
        {
            var cg = card.GetComponent<CanvasGroup>() ?? card.AddComponent<CanvasGroup>();
            cg.alpha = 0.45f; // esmaece a skill já infundida
        }
        return card;
    }

    // Carrega a moldura de evolução (só no editor; em build o prefab já traz sua própria).
    static Sprite CarregarSpriteEvo(string path, string spriteName)
    {
#if UNITY_EDITOR
        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        Sprite primeiro = null;
        foreach (var a in all)
            if (a is Sprite s)
            {
                if (s.name == spriteName) return s;
                if (primeiro == null) primeiro = s;
            }
        if (primeiro != null) return primeiro;
#endif
        return null;
    }

    // Fallback procedural (só se nenhum prefab de card existir) — dimensionado pro layout manual.
    // Retorna a carta com Button pronto (sem clique fiado); a etapa chamadora liga o próprio.
    GameObject ConstruirCartaFallbackEvo(Transform container, string cardName, string nomeStr, string descStr,
        Sprite icone, string rotulo, Color rotuloCor)
    {
        var go = new GameObject(cardName, typeof(RectTransform));
        go.transform.SetParent(container, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = _cardSizeEvo;

        var bg = go.AddComponent<Image>();
        var frame = CartaFrame();
        bool comFrame = frame != null;
        if (comFrame) { bg.sprite = frame; bg.type = Image.Type.Simple; bg.color = Color.white; }
        else bg.color = corFundo;

        if (icone != null)
        {
            var ic = new GameObject("IconInner", typeof(RectTransform)); ic.transform.SetParent(go.transform, false);
            Ancora(ic, new Vector2(0.32f, 0.66f), new Vector2(0.68f, 0.94f));
            var icImg = ic.AddComponent<Image>(); icImg.sprite = icone; icImg.preserveAspect = true; icImg.raycastTarget = false;
        }

        var nome = CriarTexto(go, "Nome", nomeStr, new Color(0.95f, 0.82f, 0.40f), 18f, FontStyles.Bold);
        Ancora(nome, new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.64f));
        var desc = CriarTexto(go, "Desc", descStr, new Color(0.90f, 0.82f, 0.65f), 13f);
        Ancora(desc, new Vector2(0.10f, 0.16f), new Vector2(0.90f, 0.50f));
        var dTxt = desc.GetComponent<TextMeshProUGUI>();
        dTxt.textWrappingMode = TextWrappingModes.Normal; dTxt.alignment = TextAlignmentOptions.Top;

        if (!string.IsNullOrEmpty(rotulo))
        {
            var rot = CriarTexto(go, "Rotulo", rotulo, rotuloCor, 11f, FontStyles.Bold);
            Ancora(rot, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.14f));
        }

        var btn = go.AddComponent<Button>(); btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = new ColorBlock{
            normalColor = comFrame ? Color.white : corFundo,
            highlightedColor = comFrame ? new Color(1f,0.96f,0.85f) : new Color(corFundo.r+0.1f,corFundo.g+0.08f,corFundo.b+0.18f,1f),
            pressedColor = new Color(0.85f,0.83f,0.78f), selectedColor = comFrame ? new Color(1f,0.96f,0.85f) : corFundo,
            disabledColor = new Color(1f,1f,1f,0.5f), colorMultiplier = 1f, fadeDuration = 0.1f
        };
        return go;
    }

    // Prefab de card: o da skill sendo infundida → qualquer skill ativa → Resources.
    GameObject ObterCardPrefab()
    {
        if (skillSelecionada != null && skillSelecionada.cardPrefab != null) return skillSelecionada.cardPrefab;
        if (skillManager != null && skillManager.activeSkills != null)
            foreach (var s in skillManager.activeSkills)
                if (s != null && s.cardPrefab != null) return s.cardPrefab;
        return Resources.Load<GameObject>("Cards/SkillCard_Auto");
    }

    void ConfirmarEscolha(int indice)
    {
        if (skillSelecionada == null) return;
        skillSelecionada.appliedElement = elementoAtual;
        skillSelecionada.appliedCharacteristicIndex = indice;
        var def = ElementRegistry.Instance?.Get(elementoAtual);
        if (def != null) skillSelecionada.elementColor = def.cor;
        SkillIconsHUD.Instance?.AtualizarBadgeElemento(skillSelecionada);

        // Feedback visual: partículas da cor do elemento convergindo no player (como as evoluções).
        var plFx = PlayerStats.Local;
        if (plFx != null && def != null)
            InfusaoFXParticulas.Disparar(plFx.transform.position, def.cor);

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

    // Fundo de painel (cena de masmorra) + banner de título — build-safe (Resources).
    static Sprite _fundoSp, _bannerSp; static bool _fundosCarregados;
    static void CarregarFundos()
    {
        if (_fundosCarregados) return;
        _fundosCarregados = true;
        _fundoSp  = Resources.Load<Sprite>("ui/fundo_painel");
        _bannerSp = Resources.Load<Sprite>("ui/banner_titulo");
    }

    // Título com banner crimson atrás (dark-fantasy), no tema do jogo.
    void TituloBanner(GameObject painel, string texto, Vector2 aMin, Vector2 aMax)
    {
        if (_bannerSp != null)
        {
            var bn = new GameObject("Banner"); bn.transform.SetParent(painel.transform, false);
            var bnRT = bn.AddComponent<RectTransform>();
            bnRT.anchorMin = aMin; bnRT.anchorMax = aMax;
            bnRT.offsetMin = new Vector2(-6f, -8f); bnRT.offsetMax = new Vector2(6f, 8f);
            var bnImg = bn.AddComponent<Image>(); bnImg.sprite = _bannerSp; bnImg.type = Image.Type.Simple; bnImg.raycastTarget = false;
        }
        var titulo = CriarTexto(painel, "Titulo", texto, corTitulo, 22f, FontStyles.Bold);
        Ancora(titulo, aMin, aMax, new Vector2(10f, 0f), new Vector2(-10f, 0f));
    }

    GameObject CriarPainel(string nome)
    {
        CarregarFundos();
        var go = new GameObject(nome); go.transform.SetParent(canvas.transform, false);
        go.AddComponent<RectTransform>();
        var bgImg = go.AddComponent<Image>();
        if (_fundoSp != null) { bgImg.sprite = _fundoSp; bgImg.type = Image.Type.Simple; bgImg.color = Color.white; }
        else bgImg.color = corFundo;

        // escurece a cena de fundo pra o conteúdo ficar legível
        var esc = new GameObject("Escurecer"); esc.transform.SetParent(go.transform, false);
        var escRT = esc.AddComponent<RectTransform>();
        escRT.anchorMin = Vector2.zero; escRT.anchorMax = Vector2.one; escRT.offsetMin = escRT.offsetMax = Vector2.zero;
        var escImg = esc.AddComponent<Image>(); escImg.color = new Color(0.04f, 0.02f, 0.06f, 0.5f); escImg.raycastTarget = false;
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
