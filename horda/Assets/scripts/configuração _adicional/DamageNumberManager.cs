using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DamageNumberManager : MonoBehaviour
{
    [Header("🔧 PREFAB")]
    public GameObject damagePrefab;

    [Header("📏 CONFIGURAÇÃO")]
    public float canvasScale = 0.15f;
    public int normalFontSize = 24;
    public int critFontSize = 32;
    public int fatalFontSize = 48;

    [Header("🎨 CORES")]
    public Color normalColor = Color.white;
    public Color critColor = Color.yellow;
    public Color fatalColor = Color.red;

    [Header("⏱️ TEMPO")]
    public float duration = 1f;
    public float fatalDuration = 1.5f;
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
                // 🔥 Usando FindFirstObjectByType
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
        // 🔥 Usando FindObjectsByType
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

        CreatePopup(targetTransform.position, damage, isCrit, false);
    }

    // ✅ DANO FATAL
    public void ShowDamageFatal(Transform targetTransform, float damage, bool isCrit = false)
    {
        if (targetTransform == null || worldCanvas == null || damagePrefab == null)
            return;

        CreatePopup(targetTransform.position, damage, isCrit, true);
    }

    // ✅ CRIAR POPUP
    private void CreatePopup(Vector3 position, float damage, bool isCrit, bool isFatal)
    {
        GameObject popup = Instantiate(damagePrefab, worldCanvas.transform);

        float height = isFatal ? floatHeight * 1.5f : floatHeight;
        Vector3 popupPos = position + (Vector3.up * height);

        popup.transform.position = popupPos;
        popup.transform.rotation = Quaternion.identity;

        // Configurar texto
        SetupText(popup, damage, isCrit, isFatal);

        // Adicionar animação
        DamagePopupAnimator anim = popup.AddComponent<DamagePopupAnimator>();
        anim.Initialize(
            popupPos,
            isFatal ? fatalColor : (isCrit ? critColor : normalColor),
            isFatal ? fatalDuration : duration,
            height,
            floatSpeed,
            isFatal
        );

        Destroy(popup, (isFatal ? fatalDuration : duration) + 0.5f);
    }

    void SetupText(GameObject popup, float damage, bool isCrit, bool isFatal)
    {
        TextMeshProUGUI textUI = popup.GetComponentInChildren<TextMeshProUGUI>();

        if (textUI != null)
        {
            textUI.text = Mathf.RoundToInt(damage).ToString();

            if (isFatal)
            {
                textUI.color = fatalColor;
                textUI.fontSize = fatalFontSize;
                textUI.fontStyle = FontStyles.Bold;
            }
            else if (isCrit)
            {
                textUI.color = critColor;
                textUI.fontSize = critFontSize;
                textUI.fontStyle = FontStyles.Bold;
            }
            else
            {
                textUI.color = normalColor;
                textUI.fontSize = normalFontSize;
            }

            textUI.alignment = TextAlignmentOptions.Center;
            textUI.enabled = true;
        }
    }
}

// ✅ ANIMAÇÃO DO POPUP
public class DamagePopupAnimator : MonoBehaviour
{
    private Vector3 startPosition;
    private Color originalColor;
    private float duration;
    private float floatHeight;
    private float floatSpeed;
    private bool isFatal;
    private float timer = 0f;
    private TextMeshProUGUI textMesh;

    public void Initialize(Vector3 startPos, Color color, float duration,
                          float floatHeight, float floatSpeed, bool isFatal = false)
    {
        this.startPosition = startPos;
        this.originalColor = color;
        this.duration = duration;
        this.floatHeight = floatHeight;
        this.floatSpeed = floatSpeed;
        this.isFatal = isFatal;

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

        // Escala
        float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.1f;
        if (isFatal) scale += 0.2f;
        transform.localScale = Vector3.one * scale;
    }
}