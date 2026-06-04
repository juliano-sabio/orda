using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DamageNumberManager : MonoBehaviour
{
    [Header("🔧 PREFABS")]
    public GameObject damagePrefab;
    public GameObject healPrefab; // NOVO: Prefab para cura

    [Header("📏 CONFIGURAÇÃO")]
    public float canvasScale = 0.15f;
    public int normalFontSize = 24;
    public int critFontSize = 32;
    public int fatalFontSize = 48;
    public int healFontSize = 28; // NOVO: Tamanho para cura

    [Header("🎨 CORES")]
    public Color normalColor = Color.white;
    public Color critColor   = Color.red;   // crit = vermelho
    public Color fatalColor  = Color.red;
    public Color healColor   = new Color(0.4f, 1f, 0.4f);

    [Header("⏱️ TEMPO")]
    public float duration = 1f;
    public float fatalDuration = 1.5f;
    public float healDuration = 1f; // NOVO: Duração para cura
    public float floatSpeed = 2f;
    public float floatHeight = 1.5f;

    // Rastreia o popup ativo por inimigo para destruir o anterior
    private readonly System.Collections.Generic.Dictionary<int, GameObject> popupAtivo
        = new System.Collections.Generic.Dictionary<int, GameObject>();

    private Canvas worldCanvas;

    private static DamageNumberManager _instance;
    public static DamageNumberManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DamageNumberManager>();
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        CreateCanvas();
    }

    void CreateCanvas()
    {
        Canvas[] oldCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in oldCanvases)
        {
            if (canvas.name.Contains("DamageCanvas"))
            {
                Destroy(canvas.gameObject);
            }
        }

        GameObject canvasObj = new GameObject("DamageCanvas");
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingLayerName = "UI"; // Ou o nome de uma layer que esteja na frente
        worldCanvas.sortingOrder = 999;
        canvasObj.transform.position = Vector3.zero;
        canvasObj.transform.localScale = new Vector3(canvasScale, canvasScale, canvasScale);

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            worldCanvas.worldCamera = mainCamera;
        }

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
    }

    // ✅ DANO NORMAL
    public void ShowDamage(Transform targetTransform, float damage, bool isCrit = false)
    {
        if (targetTransform == null || worldCanvas == null || damagePrefab == null)
            return;

        CreatePopup(targetTransform.position, damage, isCrit, false, false);
    }

    // ✅ DANO FATAL
    public void ShowDamageFatal(Transform targetTransform, float damage, bool isCrit = false)
    {
        if (targetTransform == null || worldCanvas == null || damagePrefab == null)
            return;

        CreatePopup(targetTransform.position, damage, isCrit, true, false);
    }

    // ✅ NOVO: MOSTRAR CURA
    public void ShowHeal(Transform targetTransform, float healAmount)
    {
        if (targetTransform == null || worldCanvas == null)
            return;

        // Usar healPrefab se existir, senão usar damagePrefab
        GameObject prefabToUse = healPrefab != null ? healPrefab : damagePrefab;
        if (prefabToUse == null) return;

        CreatePopup(targetTransform.position, healAmount, false, false, true);
    }

    // ✅ CRIAR POPUP — 1 por inimigo (cancela o anterior)
    private void CreatePopup(Vector3 position, float value, bool isCrit, bool isFatal, bool isHeal)
    {
        GameObject prefab = isHeal ? (healPrefab != null ? healPrefab : damagePrefab) : damagePrefab;
        if (prefab == null) return;

        // Destroy popup anterior na mesma posição (evita empilhamento)
        int key = Mathf.RoundToInt(position.x * 100) ^ Mathf.RoundToInt(position.y * 100);
        if (popupAtivo.TryGetValue(key, out var antigo) && antigo != null)
            Destroy(antigo);

        GameObject popup = Instantiate(prefab, worldCanvas.transform);
        popupAtivo[key] = popup;

        float height = isFatal ? floatHeight * 1.5f : floatHeight;
        if (isHeal) height += 0.3f; // Cura aparece um pouco mais alto

        Vector3 popupPos = position + (Vector3.up * height);
        popup.transform.position = popupPos;
        popup.transform.rotation = Quaternion.identity;

        // Configurar texto
        SetupText(popup, value, isCrit, isFatal, isHeal);

        // Adicionar animação
        DamagePopupAnimator anim = popup.AddComponent<DamagePopupAnimator>();
        anim.Initialize(
            popupPos,
            GetColor(isCrit, isFatal, isHeal),
            GetDuration(isFatal, isHeal),
            height,
            floatSpeed,
            isFatal,
            isHeal
        );

        Destroy(popup, GetDuration(isFatal, isHeal) + 0.5f);
    }

    Color GetColor(bool isCrit, bool isFatal, bool isHeal)
    {
        if (isHeal) return healColor;
        if (isFatal) return fatalColor;
        if (isCrit) return critColor;
        return normalColor;
    }

    float GetDuration(bool isFatal, bool isHeal)
    {
        if (isHeal) return healDuration;
        if (isFatal) return fatalDuration;
        return duration;
    }

    void SetupText(GameObject popup, float value, bool isCrit, bool isFatal, bool isHeal)
    {
        var textUI = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (textUI == null) return;

        int val = Mathf.RoundToInt(value);

        if (isHeal)
        {
            textUI.text      = "+" + val;
            textUI.color     = healColor;
            textUI.fontSize  = healFontSize;
            textUI.fontStyle = FontStyles.Bold | FontStyles.Italic;
        }
        else if (isFatal)
        {
            // Tamanho proporcional ao dano — mais dano = número maior
            float sizeBonus = Mathf.Clamp(val / 100f, 0f, 1f) * 16f;
            textUI.text      = val.ToString();
            textUI.color     = fatalColor;
            textUI.fontSize  = fatalFontSize + sizeBonus;
            textUI.fontStyle = FontStyles.Bold;
        }
        else if (isCrit)
        {
            textUI.text      = val + "!";
            textUI.color     = critColor;
            textUI.fontSize  = critFontSize;
            textUI.fontStyle = FontStyles.Bold;
        }
        else
        {
            textUI.text      = val.ToString();
            textUI.color     = normalColor;
            textUI.fontSize  = normalFontSize;
            textUI.fontStyle = FontStyles.Normal;
        }

        textUI.alignment     = TextAlignmentOptions.Center;
        textUI.outlineWidth  = 0.3f;
        textUI.outlineColor  = new Color32(0, 0, 0, 200);
        textUI.enabled       = true;
    }
}

// Animação melhorada dos popups de dano/cura
public class DamagePopupAnimator : MonoBehaviour
{
    Vector3   startPos;
    Vector3   velocity;
    Color     corBase;
    float     duration;
    float     timer;
    bool      isFatal, isHeal, isCrit;
    TextMeshProUGUI txt;

    public void Initialize(Vector3 pos, Color color, float dur,
                           float floatHeight, float floatSpeed,
                           bool fatal = false, bool heal = false)
    {
        startPos   = pos;
        corBase    = color;
        duration   = dur;
        isFatal    = fatal;
        isHeal     = heal;
        isCrit     = !fatal && !heal && color != Color.white;
        txt        = GetComponentInChildren<TextMeshProUGUI>();

        // Velocidade: para cima + deriva lateral aleatória
        float dx = Random.Range(-0.4f, 0.4f);
        float dy = floatSpeed * (fatal ? 1.6f : heal ? 1.3f : 1.1f);
        velocity = new Vector3(dx, dy, 0f);
        transform.position = pos;

        // Outline para legibilidade
        if (txt != null)
        {
            txt.outlineWidth = 0.3f;
            txt.outlineColor = new Color32(0, 0, 0, 220);

            if (fatal)  { txt.fontStyle = FontStyles.Bold; }
            else if (heal) { txt.fontStyle = FontStyles.Italic; }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        // Física: gravidade leve
        velocity.y -= Time.deltaTime * (isHeal ? 0.5f : 1.8f);
        transform.position += velocity * Time.deltaTime;

        // Escala: pop-in → estabiliza → encolhe
        float scaleIn  = Mathf.Clamp01(timer / 0.1f);
        float scaleOut = t < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.65f) / 0.35f);
        float baseScale = isFatal ? 1.4f : isHeal ? 1.2f : isCrit ? 1.15f : 1f;
        float popBounce = 1f + Mathf.Sin(Mathf.Clamp01(timer / 0.15f) * Mathf.PI) * 0.25f;
        transform.localScale = Vector3.one * Mathf.Lerp(0f, baseScale * popBounce, scaleIn) * scaleOut;

        // Fade: opaco até 60%, depois some
        if (txt != null)
        {
            float alpha = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
            Color c = corBase; c.a = alpha;
            txt.color = c;
        }

        // Rotação oscilatória só no crit
        if (isCrit && !isFatal)
        {
            float rot = Mathf.Sin(timer * 15f) * Mathf.Lerp(10f, 0f, t);
            transform.rotation = Quaternion.Euler(0, 0, rot);
        }
    }
}