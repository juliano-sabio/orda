# Sistema de Elementos — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar o sistema de elementos onde bosses dropam tokens que o player infunde em skills ativas, dando características especiais e visuais distintos.

**Architecture:** Dados em um ScriptableObject `ElementRegistry` com os 10 elementos. `ElementDropToken` detecta colisão do player e abre `ElementApplicationUI` (2 etapas: escolher skill → escolher característica). Aplicação modifica os campos `appliedElement`/`appliedCharacteristicIndex` do `SkillData`. `SkillIconsHUD` exibe badge colorido. `SkillElementEffect` aplica o efeito no hit.

**Tech Stack:** Unity 2D C#, uGUI procedural (sem prefabs de UI), Unity MCP para criar scripts e assets, TextMeshPro

---

## Arquivos a criar/modificar

**Criar:**
- `horda/Assets/scripts/Elements/ElementType.cs`
- `horda/Assets/scripts/Elements/ElementDefinition.cs`
- `horda/Assets/scripts/Elements/ElementRegistry.cs`
- `horda/Assets/scripts/Elements/ElementDropToken.cs`
- `horda/Assets/scripts/Elements/SkillElementEffect.cs`
- `horda/Assets/scripts/UI/ElementApplicationUI.cs`

**Modificar:**
- `horda/Assets/scripts/skilldata.cs` — adicionar 2 campos
- `horda/Assets/scripts/UI/SkillIconsHUD.cs` — badge de elemento + Instance

---

### Task 1: Tipos de dados — enums e classes serializáveis

**Files:**
- Create: `horda/Assets/scripts/Elements/ElementType.cs`
- Create: `horda/Assets/scripts/Elements/ElementDefinition.cs`

- [ ] **Step 1: Criar ElementType.cs com os dois enums**

```csharp
// horda/Assets/scripts/Elements/ElementType.cs
public enum ElementType
{
    None, Fogo, Ar, Terra, Agua, Raio, Gelo, Planta, Trevas, Luz, Corrompido
}

public enum CharacteristicType
{
    // Fogo
    Queimadura, Explosao,
    // Ar
    Recuo, Rajada,
    // Terra
    Atordoamento, EscudoPedra,
    // Agua
    Lentidao, Cura,
    // Raio
    Cadeia, Paralisia,
    // Gelo
    Congelamento, Fragilidade,
    // Planta
    Veneno, Enraizamento,
    // Trevas
    Maldicao, RouboVida,
    // Luz
    Sagrado, Cegamento,
    // Corrompido
    Caos, Infeccao
}
```

Usar Unity MCP create_script:
```
path: "Assets/scripts/Elements/ElementType.cs"
```

- [ ] **Step 2: Criar ElementDefinition.cs com classes serializáveis**

```csharp
// horda/Assets/scripts/Elements/ElementDefinition.cs
using UnityEngine;

[System.Serializable]
public class ElementCharacteristic
{
    public string nome;
    [TextArea(2, 3)]
    public string descricao;
    public CharacteristicType tipo;
    public float valor1;
    public float valor2;
}

[System.Serializable]
public class ElementDefinition
{
    public ElementType tipo;
    public string nomeDisplay;
    public Color cor;
    public Sprite icone;
    [HideInInspector] public string emoji;
    public ElementCharacteristic[] caracteristicas = new ElementCharacteristic[2];
}
```

- [ ] **Step 3: Criar ElementRegistry.cs (ScriptableObject)**

```csharp
// horda/Assets/scripts/Elements/ElementRegistry.cs
using UnityEngine;

[CreateAssetMenu(fileName = "ElementRegistry", menuName = "Horda/ElementRegistry")]
public class ElementRegistry : ScriptableObject
{
    public ElementDefinition[] elementos = new ElementDefinition[10];

    static ElementRegistry _instance;
    public static ElementRegistry Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<ElementRegistry>("Elements/ElementRegistry");
            return _instance;
        }
    }

    public ElementDefinition Get(ElementType tipo)
    {
        if (elementos == null) return null;
        foreach (var el in elementos)
            if (el != null && el.tipo == tipo) return el;
        return null;
    }

    public Color GetCor(ElementType tipo)
    {
        var def = Get(tipo);
        return def != null ? def.cor : Color.white;
    }

    public string GetNome(ElementType tipo)
    {
        var def = Get(tipo);
        return def != null ? def.nomeDisplay : tipo.ToString();
    }
}
```

- [ ] **Step 4: Verificar compilação**

Usar `read_console` e checar que não há erros de compilação. Expected: sem erros nos 3 novos arquivos.

---

### Task 2: Adicionar campos a SkillData

**Files:**
- Modify: `horda/Assets/scripts/skilldata.cs`

- [ ] **Step 1: Adicionar os 2 campos de elemento aplicado**

Localizar o header `[Header("⚡ Sistema de Elementos")]` em skilldata.cs (linha ~26) e adicionar logo após os campos existentes do sistema elemental:

```csharp
[Header("💎 Elemento Infundido (runtime)")]
public ElementType appliedElement = ElementType.None;
public int appliedCharacteristicIndex = -1; // -1 = sem característica
```

Inserir após a linha `public float elementalEffectDuration = 3f;` (linha ~31).

- [ ] **Step 2: Verificar compilação**

Usar `validate_script` em skilldata.cs. Expected: sem erros.

---

### Task 3: ElementDropToken — pickup no chão

**Files:**
- Create: `horda/Assets/scripts/Elements/ElementDropToken.cs`

- [ ] **Step 1: Criar o script**

```csharp
// horda/Assets/scripts/Elements/ElementDropToken.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ElementDropToken : MonoBehaviour
{
    [Header("Elemento")]
    public ElementType elementType = ElementType.Fogo;

    [Header("Animação")]
    public float bobAmplitude = 0.12f;
    public float bobFrequency = 2.2f;
    public float rotationSpeed = 45f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var ui = ElementApplicationUI.Instance;
        if (ui == null)
        {
            Debug.LogWarning("[ElementDropToken] ElementApplicationUI.Instance é null — verifique se o componente está na cena.");
            Destroy(gameObject);
            return;
        }

        ui.Abrir(elementType);
        Destroy(gameObject);
    }
}
```

- [ ] **Step 2: Verificar compilação**

Usar `read_console`. Expected: sem erros.

---

### Task 4: ElementApplicationUI — painel de infusão em 2 etapas

**Files:**
- Create: `horda/Assets/scripts/UI/ElementApplicationUI.cs`

- [ ] **Step 1: Criar o script completo**

```csharp
// horda/Assets/scripts/UI/ElementApplicationUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ElementApplicationUI : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
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

    // ── Paleta dark fantasy ───────────────────────────────────────────────────
    static readonly Color corFundo    = new Color(0.071f, 0.059f, 0.118f, 0.97f);
    static readonly Color corBorda    = new Color(0.784f, 0.659f, 0.251f, 1f);
    static readonly Color corTitulo   = new Color(0.95f,  0.80f,  0.40f,  1f);
    static readonly Color corTexto    = new Color(0.90f,  0.82f,  0.65f,  1f);
    static readonly Color corBotao    = new Color(0.14f,  0.11f,  0.22f,  1f);
    static readonly Color corSelecionado = new Color(0.22f, 0.17f, 0.35f, 1f);
    static readonly Color corBloqueado   = new Color(0.20f, 0.18f, 0.25f, 0.50f);

    // ── Estado ───────────────────────────────────────────────────────────────
    Canvas      canvas;
    GameObject  painelEtapa1;
    GameObject  painelEtapa2;
    ElementType elementoAtual;
    SkillData   skillSelecionada;
    SkillManager skillManager;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
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

    // ── API pública ───────────────────────────────────────────────────────────
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

    // ── Etapa 1 — escolha da skill ────────────────────────────────────────────
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

        // Título
        var titulo = CriarTexto(painelEtapa1, "Titulo",
            $"Elemento {nomeElem.ToUpper()} coletado!",
            corTitulo, 22f, FontStyles.Bold);
        var tRT = titulo.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.88f); tRT.anchorMax = new Vector2(1f, 1f);
        tRT.offsetMin = new Vector2(10f, 0f); tRT.offsetMax = new Vector2(-10f, 0f);
        titulo.GetComponent<TextMeshProUGUI>().color = corElem;

        // Subtítulo
        var sub = CriarTexto(painelEtapa1, "Sub",
            "Escolha uma skill ativa para infundir o elemento:", corTexto, 13f);
        var sRT = sub.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0.80f); sRT.anchorMax = new Vector2(1f, 0.89f);
        sRT.offsetMin = new Vector2(10f, 0f); sRT.offsetMax = new Vector2(-10f, 0f);

        // Lista de skills ativas
        var lista = CriarScrollArea(painelEtapa1, "ListaSkills",
            new Vector2(0f, 0.06f), new Vector2(1f, 0.80f));

        var skills = ObterSkillsDisponiveis();
        bool temSkillDisponivel = false;

        foreach (var skill in skills)
        {
            bool podeReceber = skill.appliedElement == ElementType.None;
            if (podeReceber) temSkillDisponivel = true;
            CriarLinhaSkill(lista, skill, podeReceber, corElem);
        }

        if (!temSkillDisponivel)
        {
            // Sem skills disponíveis — fechar com mensagem
            var aviso = CriarTexto(lista, "Aviso",
                "Nenhuma skill ativa disponível.\nTodas já possuem um elemento.", corTexto, 13f);
            aviso.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            aviso.GetComponent<RectTransform>().anchorMax = Vector2.one;

            var btnFechar = CriarBotao(painelEtapa1, "BtnFechar", "DESCARTAR ELEMENTO",
                new Color(0.4f, 0.1f, 0.1f), () => Fechar());
            var bRT = btnFechar.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.2f, 0.01f); bRT.anchorMax = new Vector2(0.8f, 0.07f);
            bRT.offsetMin = bRT.offsetMax = Vector2.zero;
        }
    }

    void CriarLinhaSkill(GameObject pai, SkillData skill, bool disponivel, Color corElem)
    {
        var goLinha = new GameObject($"Linha_{skill.skillName}");
        goLinha.transform.SetParent(pai.transform, false);

        var img = goLinha.AddComponent<Image>();
        img.color = disponivel ? corBotao : corBloqueado;

        var le = goLinha.AddComponent<LayoutElement>();
        le.preferredHeight = 54f;
        le.flexibleWidth = 1f;

        var rt = goLinha.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 54f);

        // Ícone da skill
        if (skill.icon != null)
        {
            var icGO = new GameObject("Icone");
            icGO.transform.SetParent(goLinha.transform, false);
            var icRT = icGO.AddComponent<RectTransform>();
            icRT.anchorMin = new Vector2(0f, 0f); icRT.anchorMax = new Vector2(0f, 1f);
            icRT.pivot = new Vector2(0f, 0.5f);
            icRT.anchoredPosition = new Vector2(6f, 0f);
            icRT.sizeDelta = new Vector2(44f, -8f);
            var icImg = icGO.AddComponent<Image>();
            icImg.sprite = skill.icon;
            icImg.preserveAspect = true;
        }

        // Nome
        var nomeGO = new GameObject("Nome");
        nomeGO.transform.SetParent(goLinha.transform, false);
        var nomeRT = nomeGO.AddComponent<RectTransform>();
        nomeRT.anchorMin = new Vector2(0f, 0.5f); nomeRT.anchorMax = new Vector2(0.7f, 1f);
        nomeRT.offsetMin = new Vector2(58f, 0f); nomeRT.offsetMax = new Vector2(0f, 0f);
        var nomeTxt = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTxt.text = skill.skillName;
        nomeTxt.fontSize = 14f;
        nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.color = disponivel ? corTexto : new Color(0.5f, 0.5f, 0.5f);
        nomeTxt.alignment = TextAlignmentOptions.MidlineLeft;

        // Status
        var statusGO = new GameObject("Status");
        statusGO.transform.SetParent(goLinha.transform, false);
        var statusRT = statusGO.AddComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0f, 0f); statusRT.anchorMax = new Vector2(0.7f, 0.5f);
        statusRT.offsetMin = new Vector2(58f, 0f); statusRT.offsetMax = new Vector2(0f, 0f);
        var statusTxt = statusGO.AddComponent<TextMeshProUGUI>();
        statusTxt.fontSize = 10f;
        statusTxt.alignment = TextAlignmentOptions.MidlineLeft;

        if (!disponivel)
        {
            string nomeElemAtual = ElementRegistry.Instance?.GetNome(skill.appliedElement) ?? skill.appliedElement.ToString();
            statusTxt.text = $"Ja possui elemento: {nomeElemAtual}";
            statusTxt.color = new Color(0.6f, 0.4f, 0.4f);
        }
        else
        {
            statusTxt.text = "Sem elemento";
            statusTxt.color = new Color(0.5f, 0.7f, 0.5f);
        }

        // Botão de selecionar (só se disponível)
        if (disponivel)
        {
            var btnGO = new GameObject("BtnSelecionar");
            btnGO.transform.SetParent(goLinha.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.72f, 0.15f); btnRT.anchorMax = new Vector2(0.97f, 0.85f);
            btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = corElem * 0.5f + corBotao * 0.5f;
            var btn = btnGO.AddComponent<Button>();
            var skillCapturada = skill;
            btn.onClick.AddListener(() => SelecionarSkill(skillCapturada));
            var btnTxt = CriarTexto(btnGO, "Txt", "INFUNDIR", corTexto, 11f, FontStyles.Bold);
            var btRT = btnTxt.GetComponent<RectTransform>();
            btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
            btRT.offsetMin = btRT.offsetMax = Vector2.zero;
        }
    }

    void SelecionarSkill(SkillData skill)
    {
        skillSelecionada = skill;
        MostrarEtapa2();
    }

    // ── Etapa 2 — escolha da característica ──────────────────────────────────
    void MostrarEtapa2()
    {
        if (painelEtapa1 != null) Destroy(painelEtapa1);
        if (painelEtapa2 != null) Destroy(painelEtapa2);

        var reg = ElementRegistry.Instance;
        var def = reg?.Get(elementoAtual);
        string nomeElem = def != null ? def.nomeDisplay : elementoAtual.ToString();
        Color corElem   = def != null ? def.cor : Color.white;

        painelEtapa2 = CriarPainel("PainelEtapa2");
        var rt = painelEtapa2.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.25f, 0.20f);
        rt.anchorMax = new Vector2(0.75f, 0.85f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Título
        var titulo = CriarTexto(painelEtapa2, "Titulo",
            $"{skillSelecionada.skillName} + {nomeElem}",
            corElem, 20f, FontStyles.Bold);
        var tRT = titulo.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.88f); tRT.anchorMax = new Vector2(1f, 1f);
        tRT.offsetMin = new Vector2(10f, 0f); tRT.offsetMax = new Vector2(-10f, 0f);

        // Sub
        var sub = CriarTexto(painelEtapa2, "Sub", "Escolha o poder do elemento:", corTexto, 12f);
        var sRT = sub.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0.81f); sRT.anchorMax = new Vector2(1f, 0.89f);
        sRT.offsetMin = new Vector2(10f, 0f); sRT.offsetMax = new Vector2(-10f, 0f);

        // Opções de característica
        if (def != null && def.caracteristicas != null)
        {
            float[] yMins  = { 0.50f, 0.18f };
            float[] yMaxes = { 0.80f, 0.48f };

            for (int i = 0; i < Mathf.Min(def.caracteristicas.Length, 2); i++)
            {
                var car = def.caracteristicas[i];
                if (car == null) continue;
                int idx = i;

                var cardGO = new GameObject($"Card_{i}");
                cardGO.transform.SetParent(painelEtapa2.transform, false);
                var cardRT = cardGO.AddComponent<RectTransform>();
                cardRT.anchorMin = new Vector2(0.04f, yMins[i]);
                cardRT.anchorMax = new Vector2(0.96f, yMaxes[i]);
                cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;
                var cardImg = cardGO.AddComponent<Image>();
                cardImg.color = corBotao;

                // Borda esquerda colorida
                var bord = new GameObject("Bord");
                bord.transform.SetParent(cardGO.transform, false);
                var bordRT = bord.AddComponent<RectTransform>();
                bordRT.anchorMin = Vector2.zero; bordRT.anchorMax = new Vector2(0f, 1f);
                bordRT.offsetMin = Vector2.zero; bordRT.offsetMax = new Vector2(4f, 0f);
                var bordImg = bord.AddComponent<Image>();
                bordImg.color = corElem;

                // Letra da opção
                var letraGO = CriarTexto(cardGO, "Letra", i == 0 ? "A" : "B", corElem, 22f, FontStyles.Bold);
                var lRT = letraGO.GetComponent<RectTransform>();
                lRT.anchorMin = new Vector2(0f, 0f); lRT.anchorMax = new Vector2(0.1f, 1f);
                lRT.offsetMin = new Vector2(8f, 0f); lRT.offsetMax = Vector2.zero;

                // Nome da característica
                var nomeGO = CriarTexto(cardGO, "NomeCar", car.nome, corTexto, 14f, FontStyles.Bold);
                var nRT = nomeGO.GetComponent<RectTransform>();
                nRT.anchorMin = new Vector2(0.1f, 0.55f); nRT.anchorMax = new Vector2(0.85f, 1f);
                nRT.offsetMin = new Vector2(8f, 0f); nRT.offsetMax = Vector2.zero;

                // Descrição
                var descGO = CriarTexto(cardGO, "Desc", car.descricao, new Color(0.75f, 0.70f, 0.60f), 11f);
                var dRT = descGO.GetComponent<RectTransform>();
                dRT.anchorMin = new Vector2(0.1f, 0f); dRT.anchorMax = new Vector2(0.85f, 0.55f);
                dRT.offsetMin = new Vector2(8f, 2f); dRT.offsetMax = Vector2.zero;
                descGO.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

                // Botão escolher
                var btn = cardGO.AddComponent<Button>();
                btn.targetGraphic = cardImg;
                btn.onClick.AddListener(() => ConfirmarEscolha(idx));
            }
        }

        // Aviso permanência
        var avisoGO = CriarTexto(painelEtapa2, "Aviso", "Esta escolha e permanente — nao pode ser removida.", 
            new Color(0.6f, 0.5f, 0.5f), 10f);
        var aRT = avisoGO.GetComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0f, 0.10f); aRT.anchorMax = new Vector2(1f, 0.18f);
        aRT.offsetMin = new Vector2(10f, 0f); aRT.offsetMax = new Vector2(-10f, 0f);

        // Botão voltar
        var btnVoltar = CriarBotao(painelEtapa2, "BtnVoltar", "< VOLTAR",
            new Color(0.2f, 0.15f, 0.3f), () => MostrarEtapa1());
        var bvRT = btnVoltar.GetComponent<RectTransform>();
        bvRT.anchorMin = new Vector2(0.04f, 0.01f); bvRT.anchorMax = new Vector2(0.45f, 0.10f);
        bvRT.offsetMin = bvRT.offsetMax = Vector2.zero;
    }

    void ConfirmarEscolha(int indice)
    {
        if (skillSelecionada == null) return;

        // Aplicar elemento na SkillData
        skillSelecionada.appliedElement = elementoAtual;
        skillSelecionada.appliedCharacteristicIndex = indice;

        // Atualizar cor do elemento na skill para o HUD usar
        var def = ElementRegistry.Instance?.Get(elementoAtual);
        if (def != null) skillSelecionada.elementColor = def.cor;

        // Notificar HUD
        SkillIconsHUD.Instance?.AtualizarBadgeElemento(skillSelecionada);

        Debug.Log($"[ElementSystem] Elemento {elementoAtual} (opção {indice}) infundido em {skillSelecionada.skillName}");
        Fechar();
    }

    void Fechar()
    {
        if (painelEtapa1 != null) { Destroy(painelEtapa1); painelEtapa1 = null; }
        if (painelEtapa2 != null) { Destroy(painelEtapa2); painelEtapa2 = null; }
        canvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    List<SkillData> ObterSkillsDisponiveis()
    {
        var lista = new List<SkillData>();
        if (skillManager == null) return lista;
        foreach (var skill in skillManager.activeSkills)
        {
            if (skill == null) continue;
            if (skill.skillType == SkillType.Passive || skill.skillType == SkillType.Ultimate) continue;
            lista.Add(skill);
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

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        // Backdrop
        var bd = new GameObject("Backdrop");
        bd.transform.SetParent(go.transform, false);
        var bdRT = bd.AddComponent<RectTransform>();
        bdRT.anchorMin = Vector2.zero; bdRT.anchorMax = Vector2.one;
        bdRT.offsetMin = bdRT.offsetMax = Vector2.zero;
        bd.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        canvas.gameObject.SetActive(false);
    }

    GameObject CriarPainel(string nome)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.25f, 0.15f);
        rt.anchorMax = new Vector2(0.75f, 0.88f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = corFundo;

        // Bordas douradas (4 strips de 2px)
        foreach (var (anMin, anMax, offMin, offMax) in new[]{
            (new Vector2(0,1), new Vector2(1,1), new Vector2(0,-2), new Vector2(0,0)),
            (new Vector2(0,0), new Vector2(1,0), new Vector2(0,0),  new Vector2(0,2)),
            (new Vector2(0,0), new Vector2(0,1), new Vector2(0,0),  new Vector2(2,0)),
            (new Vector2(1,0), new Vector2(1,1), new Vector2(-2,0), new Vector2(0,0)),
        })
        {
            var b = new GameObject("Borda");
            b.transform.SetParent(go.transform, false);
            var bRT = b.AddComponent<RectTransform>();
            bRT.anchorMin = anMin; bRT.anchorMax = anMax;
            bRT.offsetMin = offMin; bRT.offsetMax = offMax;
            b.AddComponent<Image>().color = corBorda;
        }

        return go;
    }

    GameObject CriarScrollArea(GameObject pai, string nome, Vector2 anchorMin, Vector2 anchorMax)
    {
        // Viewport
        var vpGO = new GameObject(nome + "_Viewport");
        vpGO.transform.SetParent(pai.transform, false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = anchorMin; vpRT.anchorMax = anchorMax;
        vpRT.offsetMin = new Vector2(4f, 4f); vpRT.offsetMax = new Vector2(-4f, -4f);
        vpGO.AddComponent<Image>().color = new Color(0,0,0,0);
        vpGO.AddComponent<RectMask2D>();

        // Content
        var ctGO = new GameObject(nome + "_Content");
        ctGO.transform.SetParent(vpGO.transform, false);
        var ctRT = ctGO.AddComponent<RectTransform>();
        ctRT.anchorMin = new Vector2(0,1); ctRT.anchorMax = new Vector2(1,1);
        ctRT.pivot = new Vector2(0.5f, 1f);
        ctRT.anchoredPosition = Vector2.zero;
        ctRT.sizeDelta = new Vector2(0, 0);

        var vlg = ctGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = ctGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect
        var sr = vpGO.AddComponent<ScrollRect>();
        sr.content = ctRT;
        sr.viewport = vpRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 20f;

        return ctGO;
    }

    static GameObject CriarTexto(GameObject pai, string nome, string texto, Color cor, float tamanho,
        FontStyles estilo = FontStyles.Normal)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai.transform, false);
        go.AddComponent<RectTransform>();
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = texto;
        txt.fontSize = tamanho;
        txt.fontStyle = estilo;
        txt.color = cor;
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;
        return go;
    }

    static GameObject CriarBotao(GameObject pai, string nome, string label, Color cor, UnityEngine.Events.UnityAction callback)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = cor;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(callback);

        var txtGO = CriarTexto(go, "Label", label, new Color(0.95f, 0.85f, 0.65f), 13f, FontStyles.Bold);
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;

        return go;
    }
}
```

- [ ] **Step 2: Verificar compilação**

Usar `read_console`. Expected: sem erros.

---

### Task 5: SkillElementEffect — aplicação dos efeitos

**Files:**
- Create: `horda/Assets/scripts/Elements/SkillElementEffect.cs`

- [ ] **Step 1: Criar a classe estática de efeitos**

```csharp
// horda/Assets/scripts/Elements/SkillElementEffect.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class SkillElementEffect
{
    // Ponto de entrada: chame após causar dano com uma skill
    public static void Aplicar(SkillData skill, GameObject alvo, float danoBase, MonoBehaviour caller)
    {
        if (skill == null || alvo == null || caller == null) return;
        if (skill.appliedElement == ElementType.None || skill.appliedCharacteristicIndex < 0) return;

        var def = ElementRegistry.Instance?.Get(skill.appliedElement);
        if (def == null || def.caracteristicas == null) return;
        if (skill.appliedCharacteristicIndex >= def.caracteristicas.Length) return;

        var car = def.caracteristicas[skill.appliedCharacteristicIndex];
        if (car == null) return;

        AplicarCaracteristica(car, alvo, danoBase, caller);
    }

    static void AplicarCaracteristica(ElementCharacteristic car, GameObject alvo, float danoBase, MonoBehaviour caller)
    {
        var ic = alvo.GetComponent<InimigoController>();

        switch (car.tipo)
        {
            // ── Fogo ─────────────────────────────────────────────────────────
            case CharacteristicType.Queimadura:
                // valor1 = dano por tick, valor2 = número de ticks (default: danoBase*0.2 por tick, 3 ticks)
                float danoTick = car.valor1 > 0 ? car.valor1 : danoBase * 0.2f;
                float numTicks = car.valor2 > 0 ? car.valor2 : 3f;
                caller.StartCoroutine(AplicarDoT(ic, danoTick, numTicks, 1f, new Color(1f, 0.4f, 0.1f)));
                break;

            case CharacteristicType.Explosao:
                // valor1 = raio (default 2.5f), valor2 = % do dano (default 0.6f)
                float raio = car.valor1 > 0 ? car.valor1 : 2.5f;
                float pctDano = car.valor2 > 0 ? car.valor2 : 0.6f;
                AplicarAoE(alvo.transform.position, raio, danoBase * pctDano, alvo);
                break;

            // ── Ar ───────────────────────────────────────────────────────────
            case CharacteristicType.Recuo:
                // valor1 = força do knockback (default 12f)
                AplicarKnockback(alvo, car.valor1 > 0 ? car.valor1 : 12f);
                break;

            case CharacteristicType.Rajada:
                // Aplicado no momento da ativação da skill — redução de cooldown
                // Gerenciado em SkillData.GetCooldownEfetivo() (ver nota abaixo)
                break;

            // ── Terra ────────────────────────────────────────────────────────
            case CharacteristicType.Atordoamento:
                // valor1 = duração (default 1.5f), valor2 = chance (default 0.4f)
                float chanceStun = car.valor2 > 0 ? car.valor2 : 0.4f;
                if (Random.value <= chanceStun)
                    caller.StartCoroutine(AplicarCC(ic, car.valor1 > 0 ? car.valor1 : 1.5f, "stun"));
                break;

            case CharacteristicType.EscudoPedra:
                // Handled at skill activation, not on hit
                break;

            // ── Agua ─────────────────────────────────────────────────────────
            case CharacteristicType.Lentidao:
                // valor1 = duração (default 3f), valor2 = fator (default 0.5f)
                caller.StartCoroutine(AplicarSlow(ic, car.valor1 > 0 ? car.valor1 : 3f,
                    car.valor2 > 0 ? car.valor2 : 0.5f));
                break;

            case CharacteristicType.Cura:
                // valor1 = % do dano que vira cura (default 0.15f)
                float pctCura = car.valor1 > 0 ? car.valor1 : 0.15f;
                AplicarCura(danoBase * pctCura);
                break;

            // ── Raio ─────────────────────────────────────────────────────────
            case CharacteristicType.Cadeia:
                // valor1 = raio de cadeia (default 4f), valor2 = % dano (default 0.6f)
                AplicarCadeia(alvo, car.valor1 > 0 ? car.valor1 : 4f,
                    danoBase * (car.valor2 > 0 ? car.valor2 : 0.6f), 2);
                break;

            case CharacteristicType.Paralisia:
                // valor1 = duração (default 1f), valor2 = chance (default 0.35f)
                float chanceParalisia = car.valor2 > 0 ? car.valor2 : 0.35f;
                if (Random.value <= chanceParalisia)
                    caller.StartCoroutine(AplicarCC(ic, car.valor1 > 0 ? car.valor1 : 1f, "paralisia"));
                break;

            // ── Gelo ─────────────────────────────────────────────────────────
            case CharacteristicType.Congelamento:
                // valor1 = duração (default 2f)
                caller.StartCoroutine(AplicarCC(ic, car.valor1 > 0 ? car.valor1 : 2f, "gelo"));
                break;

            case CharacteristicType.Fragilidade:
                // valor1 = duração (default 3f), valor2 = % dano extra (default 0.25f)
                caller.StartCoroutine(AplicarFragilidade(ic, car.valor1 > 0 ? car.valor1 : 3f,
                    car.valor2 > 0 ? car.valor2 : 0.25f));
                break;

            // ── Planta ───────────────────────────────────────────────────────
            case CharacteristicType.Veneno:
                float danoVeneno = car.valor1 > 0 ? car.valor1 : danoBase * 0.12f;
                float ticksVeneno = car.valor2 > 0 ? car.valor2 : 5f;
                caller.StartCoroutine(AplicarDoT(ic, danoVeneno, ticksVeneno, 1f, new Color(0.3f, 0.8f, 0.2f)));
                break;

            case CharacteristicType.Enraizamento:
                caller.StartCoroutine(AplicarCC(ic, car.valor1 > 0 ? car.valor1 : 2.5f, "raiz"));
                break;

            // ── Trevas ───────────────────────────────────────────────────────
            case CharacteristicType.Maldicao:
                caller.StartCoroutine(AplicarMaldicao(ic, car.valor1 > 0 ? car.valor1 : 4f,
                    car.valor2 > 0 ? car.valor2 : 0.3f));
                break;

            case CharacteristicType.RouboVida:
                float pctRoubo = car.valor1 > 0 ? car.valor1 : 0.2f;
                AplicarCura(danoBase * pctRoubo);
                break;

            // ── Luz ──────────────────────────────────────────────────────────
            case CharacteristicType.Sagrado:
                // Dano bônus tratado antes de chamar Aplicar (multiplicador externo)
                // valor1 = multiplicador vs elite/boss (default 1.5f) — usado pelo caller
                break;

            case CharacteristicType.Cegamento:
                caller.StartCoroutine(AplicarCegamento(ic, car.valor1 > 0 ? car.valor1 : 3f,
                    car.valor2 > 0 ? car.valor2 : 0.4f));
                break;

            // ── Corrompido ───────────────────────────────────────────────────
            case CharacteristicType.Caos:
                // Já aplicado antes de chamar Aplicar — dano aleatório 50-250%
                // Caller deve usar GetMultiplicadorCaos() antes de causar dano
                break;

            case CharacteristicType.Infeccao:
                float raioInf = car.valor1 > 0 ? car.valor1 : 3.5f;
                float pctInf  = car.valor2 > 0 ? car.valor2 : 0.6f;
                AplicarCadeia(alvo, raioInf, danoBase * pctInf, 3);
                break;
        }
    }

    // ── Helpers de efeito ────────────────────────────────────────────────────

    static IEnumerator AplicarDoT(InimigoController ic, float danoTick, float numTicks, float intervalo, Color corParticula)
    {
        if (ic == null) yield break;
        for (int i = 0; i < (int)numTicks; i++)
        {
            yield return new WaitForSeconds(intervalo);
            if (ic == null || !ic.gameObject.activeInHierarchy) yield break;
            ic.ReceberDano(danoTick, false);
        }
    }

    static IEnumerator AplicarCC(InimigoController ic, float duracao, string tipo)
    {
        if (ic == null) yield break;
        var movi = ic.GetComponent<UnityEngine.AI.NavMeshAgent>() 
            ?? (UnityEngine.Component)ic.GetComponent<Rigidbody2D>();
        
        // Tenta parar movimento via Rigidbody2D
        var rb = ic.GetComponent<Rigidbody2D>();
        Vector2 velOriginal = Vector2.zero;
        if (rb != null) { velOriginal = rb.linearVelocity; rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }

        // Tint visual
        var sr = ic.GetComponent<SpriteRenderer>();
        Color corOriginal = Color.white;
        if (sr != null) { corOriginal = sr.color; sr.color = tipo == "gelo" ? new Color(0.6f, 0.85f, 1f) : new Color(0.8f, 0.8f, 0.8f); }

        yield return new WaitForSeconds(duracao);

        if (rb != null) { rb.bodyType = RigidbodyType2D.Dynamic; rb.linearVelocity = velOriginal; }
        if (sr != null && sr != null) sr.color = corOriginal;
    }

    static IEnumerator AplicarSlow(InimigoController ic, float duracao, float fator)
    {
        if (ic == null) yield break;
        var rb = ic.GetComponent<Rigidbody2D>();
        // Tenta acessar velocidade base via componentes de movimento
        var movi = ic.GetComponent<movi_inimigo_manter_distancia>() 
            ?? (MonoBehaviour)ic.GetComponent<movi_inimigo_perseguir_player>();
        
        if (movi is movi_inimigo_manter_distancia md)
        {
            float velOrig = md.velocidade;
            md.velocidade *= fator;
            yield return new WaitForSeconds(duracao);
            if (md != null) md.velocidade = velOrig;
        }
        else if (movi is movi_inimigo_perseguir_player mp)
        {
            float velOrig = mp.velocidade;
            mp.velocidade *= fator;
            yield return new WaitForSeconds(duracao);
            if (mp != null) mp.velocidade = velOrig;
        }
        else
        {
            yield return new WaitForSeconds(duracao);
        }
    }

    static IEnumerator AplicarFragilidade(InimigoController ic, float duracao, float bonusDano)
    {
        if (ic == null) yield break;
        // Marca o inimigo com um multiplicador de dano recebido
        // InimigoController não tem esse campo — usamos um componente temporário
        var marker = ic.gameObject.AddComponent<FragilidadeMarker>();
        marker.multiplicador = 1f + bonusDano;
        yield return new WaitForSeconds(duracao);
        if (marker != null) UnityEngine.Object.Destroy(marker);
    }

    static IEnumerator AplicarMaldicao(InimigoController ic, float duracao, float reducaoDefesa)
    {
        if (ic == null) yield break;
        // Reduz a defesa do inimigo — InimigoController usa "defesa" para reduzir dano
        // Campo pode não existir; adicionamos via marker
        var marker = ic.gameObject.AddComponent<MaldicaoMarker>();
        marker.reducaoDefesa = reducaoDefesa;
        yield return new WaitForSeconds(duracao);
        if (marker != null) UnityEngine.Object.Destroy(marker);
    }

    static IEnumerator AplicarCegamento(InimigoController ic, float duracao, float reducao)
    {
        if (ic == null) yield break;
        var marker = ic.gameObject.AddComponent<CegamentoMarker>();
        marker.reducaoDano = reducao;
        yield return new WaitForSeconds(duracao);
        if (marker != null) UnityEngine.Object.Destroy(marker);
    }

    static void AplicarKnockback(GameObject alvo, float forca)
    {
        var rb = alvo.GetComponent<Rigidbody2D>();
        if (rb == null) return;
        var player = FindPlayer();
        if (player == null) return;
        var dir = (alvo.transform.position - player.position).normalized;
        rb.AddForce(dir * forca, ForceMode2D.Impulse);
    }

    static void AplicarAoE(Vector3 centro, float raio, float dano, GameObject alvoOriginal)
    {
        var hits = Physics2D.OverlapCircleAll(centro, raio);
        foreach (var hit in hits)
        {
            if (hit.gameObject == alvoOriginal) continue;
            var ic = hit.GetComponent<InimigoController>();
            if (ic != null) ic.ReceberDano(dano, false);
        }
    }

    static void AplicarCadeia(GameObject alvoOriginal, float raio, float dano, int maxAlvos)
    {
        var hits = Physics2D.OverlapCircleAll(alvoOriginal.transform.position, raio);
        int count = 0;
        foreach (var hit in hits)
        {
            if (count >= maxAlvos) break;
            if (hit.gameObject == alvoOriginal) continue;
            var ic = hit.GetComponent<InimigoController>();
            if (ic != null) { ic.ReceberDano(dano, false); count++; }
        }
    }

    static void AplicarCura(float quantidade)
    {
        var player = FindPlayer();
        if (player == null) return;
        var stats = player.GetComponent<player_stats>();
        if (stats == null) return;
        stats.health = Mathf.Min(stats.health + quantidade, stats.maxHealth);
    }

    static Transform _playerCache;
    static Transform FindPlayer()
    {
        if (_playerCache != null) return _playerCache;
        var go = GameObject.FindWithTag("Player");
        if (go != null) _playerCache = go.transform;
        return _playerCache;
    }

    // Obtém multiplicador de dano para Caos (50-250%)
    public static float GetMultiplicadorCaos()
    {
        return Random.Range(0.5f, 2.5f);
    }

    // Obtém multiplicador de dano para Sagrado (+50% vs elite/boss)
    public static float GetMultiplicadorSagrado(GameObject alvo)
    {
        if (alvo == null) return 1f;
        return (alvo.GetComponent<BossController>() != null) ? 1.5f : 1f;
    }
}

// Componentes temporários para markers de status
public class FragilidadeMarker : MonoBehaviour { public float multiplicador = 1.25f; }
public class MaldicaoMarker   : MonoBehaviour { public float reducaoDefesa = 0.3f;  }
public class CegamentoMarker  : MonoBehaviour { public float reducaoDano   = 0.4f;  }
```

- [ ] **Step 2: Checar componentes de movimento disponíveis no projeto**

Usar Grep para confirmar `movi_inimigo_manter_distancia` e `movi_inimigo_perseguir_player` existem:
```
Grep: "public float velocidade" em horda/Assets/scripts/
```
Se os nomes não baterem, ajustar o nome da classe no `AplicarSlow`.

- [ ] **Step 3: Verificar compilação**

Usar `read_console`. Expected: sem erros. Se houver erro em `ReceberDano`, verificar o nome correto do método em InimigoController (pode ser `TakeDamage` ou `ReceberDano`).

---

### Task 6: Modificar SkillIconsHUD — badge de elemento

**Files:**
- Modify: `horda/Assets/scripts/UI/SkillIconsHUD.cs`

- [ ] **Step 1: Adicionar propriedade Instance e dicionário de slots**

Adicionar logo após `List<GameObject> slots = new List<GameObject>();` (linha ~28):

```csharp
public static SkillIconsHUD Instance { get; private set; }
Dictionary<string, GameObject> slotPorNome = new Dictionary<string, GameObject>();
```

Em `Start()` adicionar `Instance = this;` como primeira linha.
Em `OnDestroy()` adicionar `if (Instance == this) Instance = null;` como primeira linha.

- [ ] **Step 2: Modificar CriarSlot para registrar slot e criar badge oculto**

No final de `CriarSlot`, antes de `return slotGO;`, adicionar:

```csharp
// Registrar slot
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

// Se a skill já tem elemento (ex: carregou uma cena), mostrar imediatamente
if (skill.appliedElement != ElementType.None)
    AtualizarBadgeGO(badgeGO, skill);
```

- [ ] **Step 3: Adicionar método público AtualizarBadgeElemento**

Adicionar após `CriarSlot`:

```csharp
public void AtualizarBadgeElemento(SkillData skill)
{
    if (skill == null || !slotPorNome.TryGetValue(skill.skillName, out var slotGO)) return;
    var badge = slotGO.transform.Find("BadgeElemento")?.gameObject;
    if (badge != null) AtualizarBadgeGO(badge, skill);
}

static void AtualizarBadgeGO(GameObject badge, SkillData skill)
{
    if (skill.appliedElement == ElementType.None) { badge.SetActive(false); return; }
    var cor = ElementRegistry.Instance?.GetCor(skill.appliedElement) ?? Color.white;
    badge.GetComponent<Image>().color = cor;
    badge.SetActive(true);
}
```

- [ ] **Step 4: Verificar compilação**

Usar `read_console`. Expected: sem erros.

---

### Task 7: Criar ElementRegistry asset e configurar os 10 elementos

**Files:**
- Create via Unity MCP: `Assets/Resources/Elements/ElementRegistry.asset`

- [ ] **Step 1: Criar pasta Resources/Elements e o ScriptableObject**

Executar via `execute_code` no Unity:

```csharp
// Criar pasta
if (!AssetDatabase.IsValidFolder("Assets/Resources"))
    AssetDatabase.CreateFolder("Assets", "Resources");
if (!AssetDatabase.IsValidFolder("Assets/Resources/Elements"))
    AssetDatabase.CreateFolder("Assets/Resources", "Elements");

// Criar asset
var reg = ScriptableObject.CreateInstance<ElementRegistry>();
AssetDatabase.CreateAsset(reg, "Assets/Resources/Elements/ElementRegistry.asset");
AssetDatabase.SaveAssets();
return "ElementRegistry criado em Assets/Resources/Elements/ElementRegistry.asset";
```

- [ ] **Step 2: Popular os 10 elementos via execute_code**

```csharp
var reg = AssetDatabase.LoadAssetAtPath<ElementRegistry>("Assets/Resources/Elements/ElementRegistry.asset");
reg.elementos = new ElementDefinition[10];

// Helper local
ElementCharacteristic MakeCar(string nome, string desc, CharacteristicType tipo, float v1, float v2)
    => new ElementCharacteristic { nome = nome, descricao = desc, tipo = tipo, valor1 = v1, valor2 = v2 };

Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

reg.elementos[0] = new ElementDefinition { tipo = ElementType.Fogo,      nomeDisplay = "Fogo",      cor = Hex("#FF4400"), emoji = "🔥",
    caracteristicas = new[]{ MakeCar("Queimadura","3 ticks de dano de fogo em 3s",        CharacteristicType.Queimadura,  0f,   3f),
                              MakeCar("Explosao",  "Dano em area ao atingir (raio 2.5m)",  CharacteristicType.Explosao,    2.5f, 0.6f) }};

reg.elementos[1] = new ElementDefinition { tipo = ElementType.Ar,        nomeDisplay = "Ar",        cor = Hex("#44CCFF"), emoji = "💨",
    caracteristicas = new[]{ MakeCar("Recuo",    "Knockback forte nos inimigos",           CharacteristicType.Recuo,   12f,  0f),
                              MakeCar("Rajada",   "Reduz cooldown desta skill em 30%",      CharacteristicType.Rajada,  0.7f, 0f) }};

reg.elementos[2] = new ElementDefinition { tipo = ElementType.Terra,     nomeDisplay = "Terra",     cor = Hex("#AA8844"), emoji = "🪨",
    caracteristicas = new[]{ MakeCar("Atordoamento","Stun 1.5s (chance 40%)",              CharacteristicType.Atordoamento, 1.5f, 0.4f),
                              MakeCar("Escudo Pedra","Escudo temporario ao ativar skill",   CharacteristicType.EscudoPedra,  0f,   0f) }};

reg.elementos[3] = new ElementDefinition { tipo = ElementType.Agua,      nomeDisplay = "Agua",      cor = Hex("#4488FF"), emoji = "💧",
    caracteristicas = new[]{ MakeCar("Lentidao","Inimigos -50% velocidade por 3s",          CharacteristicType.Lentidao, 3f, 0.5f),
                              MakeCar("Cura",    "Regenera 15% do dano causado",             CharacteristicType.Cura,     0.15f, 0f) }};

reg.elementos[4] = new ElementDefinition { tipo = ElementType.Raio,      nomeDisplay = "Raio",      cor = Hex("#FFDD00"), emoji = "⚡",
    caracteristicas = new[]{ MakeCar("Cadeia",   "Salta para 2 inimigos proximos (60%)",    CharacteristicType.Cadeia,   4f,  0.6f),
                              MakeCar("Paralisia","35% chance imobilizar por 1s",             CharacteristicType.Paralisia,1f,  0.35f) }};

reg.elementos[5] = new ElementDefinition { tipo = ElementType.Gelo,      nomeDisplay = "Gelo",      cor = Hex("#AADDFF"), emoji = "❄️",
    caracteristicas = new[]{ MakeCar("Congelamento","Congela por 2s (imovel+vulneravel)",    CharacteristicType.Congelamento, 2f,   0f),
                              MakeCar("Fragilidade", "+25% dano de todas as fontes por 3s",  CharacteristicType.Fragilidade,  3f,   0.25f) }};

reg.elementos[6] = new ElementDefinition { tipo = ElementType.Planta,    nomeDisplay = "Planta",    cor = Hex("#44AA44"), emoji = "🌿",
    caracteristicas = new[]{ MakeCar("Veneno",      "5 ticks de veneno em 5s",              CharacteristicType.Veneno,      0f,   5f),
                              MakeCar("Enraizamento","Imovel por 2.5s",                      CharacteristicType.Enraizamento,2.5f, 0f) }};

reg.elementos[7] = new ElementDefinition { tipo = ElementType.Trevas,    nomeDisplay = "Trevas",    cor = Hex("#6622AA"), emoji = "🌑",
    caracteristicas = new[]{ MakeCar("Maldicao",   "-30% defesa do inimigo por 4s",         CharacteristicType.Maldicao,  4f,  0.3f),
                              MakeCar("Roubo Vida", "20% do dano vira cura",                 CharacteristicType.RouboVida, 0.2f,0f) }};

reg.elementos[8] = new ElementDefinition { tipo = ElementType.Luz,       nomeDisplay = "Luz",       cor = Hex("#FFEE88"), emoji = "☀️",
    caracteristicas = new[]{ MakeCar("Sagrado",   "+50% dano vs Elite e Boss",              CharacteristicType.Sagrado,   1.5f, 0f),
                              MakeCar("Cegamento","-40% dano e vel. ataque por 3s",          CharacteristicType.Cegamento, 3f,  0.4f) }};

reg.elementos[9] = new ElementDefinition { tipo = ElementType.Corrompido, nomeDisplay = "Corrompido", cor = Hex("#AA2288"), emoji = "💜",
    caracteristicas = new[]{ MakeCar("Caos",    "Dano aleatorio 50-250% do normal",         CharacteristicType.Caos,     0f,  0f),
                              MakeCar("Infeccao","Propaga 60% dano a 3 inimigos proximos",   CharacteristicType.Infeccao, 3.5f,0.6f) }};

EditorUtility.SetDirty(reg);
AssetDatabase.SaveAssets();
return $"Registry populado com {reg.elementos.Length} elementos";
```

- [ ] **Step 3: Verificar no console que o asset foi criado**

Usar `read_console`. Expected: "Registry populado com 10 elementos".

---

### Task 8: Adicionar ElementApplicationUI à cena e criar token de teste

**Files:**
- Scene: cena principal do jogo

- [ ] **Step 1: Adicionar ElementApplicationUI à cena via execute_code**

```csharp
// Verificar se já existe
var existente = FindFirstObjectByType<ElementApplicationUI>();
if (existente != null) return "ElementApplicationUI já existe na cena";

// Adicionar ao gerenciadorUI ou criar novo GO
var gerUI = GameObject.Find("gerenciadoUI") ?? GameObject.Find("UIManager") ?? new GameObject("gerenciadoUI");
gerUI.AddComponent<ElementApplicationUI>();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return "ElementApplicationUI adicionado à cena";
```

- [ ] **Step 2: Criar prefab do token de elemento Fogo para teste**

```csharp
// Criar GameObject base
var tokenGO = new GameObject("ElementToken_Fogo");
var sr = tokenGO.AddComponent<SpriteRenderer>();

// Sprite simples colorido (circulo laranja 16x16)
var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
float cx = 8f;
for (int y = 0; y < 16; y++)
for (int x = 0; x < 16; x++)
{
    float d = Vector2.Distance(new Vector2(x+.5f,y+.5f), new Vector2(cx,cx));
    tex.SetPixel(x, y, d < 7.5f ? new Color(1f,0.27f,0.05f,1f) : Color.clear);
}
tex.Apply();
sr.sprite = Sprite.Create(tex, new Rect(0,0,16,16), new Vector2(.5f,.5f), 16f);

var col = tokenGO.AddComponent<CircleCollider2D>();
col.isTrigger = true;
col.radius = 0.5f;

var token = tokenGO.AddComponent<ElementDropToken>();
token.elementType = ElementType.Fogo;

// Salvar como prefab
if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
    AssetDatabase.CreateFolder("Assets", "Prefabs");
if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Elements"))
    AssetDatabase.CreateFolder("Assets/Prefabs", "Elements");

var prefabPath = "Assets/Prefabs/Elements/ElementToken_Fogo.prefab";
PrefabUtility.SaveAsPrefabAsset(tokenGO, prefabPath);
DestroyImmediate(tokenGO);
AssetDatabase.Refresh();

return "Prefab criado em " + prefabPath;
```

- [ ] **Step 3: Teste manual**

1. No Editor: colocar o prefab `ElementToken_Fogo` na cena
2. Entrar em Play Mode
3. Mover o player até o token
4. Verificar que o painel de elementos abre (jogo pausa, Etapa 1 lista skills ativas)
5. Selecionar uma skill → Etapa 2 mostra Queimadura e Explosão
6. Confirmar → HUD mostra badge laranja abaixo da skill
7. Usar `read_console` para checar se há erros

- [ ] **Step 4: Adicionar o token ao drops de um boss para teste real**

No Inspector do BossController (ou InimigoController do boss):
- Expandir `Drops`
- Adicionar nova entrada: Prefab = `ElementToken_Fogo`, Chance = 1.0
- Testar matando o boss → token aparece → UI abre

---

## Notas de Integração

**SkillElementEffect com projéteis:**  
Para que projéteis ativem o efeito, localize onde o projétil causa dano (`TakeDamage` / `ReceberDano` no script do projétil) e adicione:
```csharp
// Após causar dano:
SkillElementEffect.Aplicar(skillData, alvo.gameObject, dano, this);
```

**Rajada (redução de cooldown):**  
No script que gerencia cooldown da skill ativa, multiplicar o cooldown por `0.7f` se `skill.appliedElement == ElementType.Ar && skill.appliedCharacteristicIndex == 1`.

**Caos (dano aleatório):**  
Antes de calcular dano da skill, se `appliedElement == Corrompido && index == 0`, multiplicar o dano por `SkillElementEffect.GetMultiplicadorCaos()`.

**EscudoPedra:**  
No script de ativação da skill (quando player usa a skill), se `appliedElement == Terra && index == 1`, chamar o sistema de escudo do player.

**Sagrado:**  
Antes de calcular dano, se o alvo tem `BossController`, multiplicar por `SkillElementEffect.GetMultiplicadorSagrado(alvo)`.
