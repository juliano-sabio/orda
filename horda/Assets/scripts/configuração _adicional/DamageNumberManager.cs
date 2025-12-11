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
    public Color critColor = Color.yellow;
    public Color fatalColor = Color.red;
    public Color healColor = Color.green; // NOVO: Cor para cura

    [Header("⏱️ TEMPO")]
    public float duration = 1f;
    public float fatalDuration = 1.5f;
    public float healDuration = 1f; // NOVO: Duração para cura
    public float floatSpeed = 2f;
    public float floatHeight = 1.5f;

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

    // ✅ CRIAR POPUP (ATUALIZADO)
    private void CreatePopup(Vector3 position, float value, bool isCrit, bool isFatal, bool isHeal)
    {
        GameObject prefab = isHeal ? (healPrefab != null ? healPrefab : damagePrefab) : damagePrefab;
        if (prefab == null) return;

        GameObject popup = Instantiate(prefab, worldCanvas.transform);

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
        TextMeshProUGUI textUI = popup.GetComponentInChildren<TextMeshProUGUI>();

        if (textUI != null)
        {
            // Formatar texto baseado no tipo
            if (isHeal)
            {
                textUI.text = "+" + Mathf.RoundToInt(value).ToString();
                textUI.color = healColor;
                textUI.fontSize = healFontSize;
            }
            else if (isFatal)
            {
                textUI.text = "💀 " + Mathf.RoundToInt(value).ToString();
                textUI.color = fatalColor;
                textUI.fontSize = fatalFontSize;
                textUI.fontStyle = FontStyles.Bold;
            }
            else if (isCrit)
            {
                textUI.text = Mathf.RoundToInt(value).ToString();
                textUI.color = critColor;
                textUI.fontSize = critFontSize;
                textUI.fontStyle = FontStyles.Bold;
            }
            else
            {
                textUI.text = Mathf.RoundToInt(value).ToString();
                textUI.color = normalColor;
                textUI.fontSize = normalFontSize;
            }

            textUI.alignment = TextAlignmentOptions.Center;
            textUI.enabled = true;
        }
    }
}

// ✅ ANIMAÇÃO DO POPUP (ATUALIZADA)
public class DamagePopupAnimator : MonoBehaviour
{
    private Vector3 startPosition;
    private Color originalColor;
    private float duration;
    private float floatHeight;
    private float floatSpeed;
    private bool isFatal;
    private bool isHeal;
    private float timer = 0f;
    private TextMeshProUGUI textMesh;

    public void Initialize(Vector3 startPos, Color color, float duration,
                          float floatHeight, float floatSpeed,
                          bool isFatal = false, bool isHeal = false)
    {
        this.startPosition = startPos;
        this.originalColor = color;
        this.duration = duration;
        this.floatHeight = floatHeight;
        this.floatSpeed = floatSpeed;
        this.isFatal = isFatal;
        this.isHeal = isHeal;

        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        UpdatePosition();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        if (t >= 1f) return;

        UpdatePosition();
        UpdateAnimation(t);
    }

    void UpdatePosition()
    {
        float currentHeight = floatHeight + (floatSpeed * timer);
        transform.position = new Vector3(
            startPosition.x,
            startPosition.y + currentHeight,
            startPosition.z
        );
    }

    void UpdateAnimation(float t)
    {
        // Fade out
        if (textMesh != null)
        {
            Color color = originalColor;
            color.a = 1f - t;
            textMesh.color = color;
        }

        // Efeitos especiais
        float scale = 1f;

        if (isHeal)
        {
            // Pulso suave para cura
            scale = 1f + Mathf.Sin(t * Mathf.PI * 2) * 0.1f;
        }
        else if (isFatal)
        {
            // Pulso mais forte para fatal
            scale = 1f + Mathf.Sin(t * Mathf.PI * 3) * 0.2f;
        }
        else
        {
            // Leve pulso para dano normal
            scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.05f;
        }

        transform.localScale = Vector3.one * scale;
    }
}