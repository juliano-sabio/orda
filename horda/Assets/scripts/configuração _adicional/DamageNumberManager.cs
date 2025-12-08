using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DamageNumberManager : MonoBehaviour
{
    [Header("🔧 PREFAB")]
    public GameObject damagePrefab;
    public GameObject fatalDamagePrefab; // 🔥 PREFAB ESPECIAL PARA DANO FATAL

    [Header("📏 CONFIGURAÇÃO DE TAMANHO")]
    [Range(0.05f, 1f)]
    public float canvasScale = 0.15f;

    [Range(10, 100)]
    public int normalFontSize = 24;

    [Range(10, 100)]
    public int critFontSize = 32;

    [Range(10, 100)]
    public int fatalFontSize = 48; // 🔥 TAMANHO MAIOR PARA FATAL

    [Header("🎨 CONFIGURAÇÃO DE CORES")]
    public Color normalColor = Color.white;
    public Color critColor = Color.yellow;
    public Color fatalColor = Color.red; // 🔥 COR ESPECIAL PARA FATAL

    [Header("⏱️ CONFIGURAÇÃO DE TEMPO")]
    public float duration = 1f;
    public float fatalDuration = 1.5f; // 🔥 DURAÇÃO MAIOR PARA FATAL
    public float floatSpeed = 2f;
    public float floatHeight = 1.5f;
    public float fatalFloatHeight = 2.5f; // 🔥 FLUTUA MAIS ALTO

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

        if (Camera.main != null)
        {
            worldCanvas.worldCamera = Camera.main;
        }

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
    }

    // ✅ MÉTODO PARA DANO NORMAL
    public void ShowDamage(Transform targetTransform, float damage, bool isCrit = false)
    {
        if (targetTransform == null || worldCanvas == null) return;

        GameObject prefabToUse = damagePrefab;
        if (prefabToUse == null)
        {
            Debug.LogError("❌ Prefab normal não configurado!");
            return;
        }

        CreateDamagePopup(targetTransform, damage, isCrit, false, prefabToUse);
    }

    // ✅ MÉTODO NOVO PARA DANO FATAL
    public void ShowDamageFatal(Transform targetTransform, float damage, bool isCrit = false)
    {
        if (targetTransform == null || worldCanvas == null) return;

        GameObject prefabToUse = fatalDamagePrefab != null ? fatalDamagePrefab : damagePrefab;
        if (prefabToUse == null)
        {
            Debug.LogError("❌ Nenhum prefab configurado!");
            return;
        }

        CreateDamagePopup(targetTransform, damage, isCrit, true, prefabToUse);
    }

    // ✅ MÉTODO ÚNICO PARA CRIAR POPUP
    private void CreateDamagePopup(Transform targetTransform, float damage, bool isCrit,
                                  bool isFatal, GameObject prefab)
    {
        GameObject popup = Instantiate(prefab, worldCanvas.transform);

        Vector3 enemyPos = targetTransform.position;
        float height = isFatal ? fatalFloatHeight : floatHeight;
        Vector3 popupPos = enemyPos + (Vector3.up * height);

        popup.transform.position = popupPos;
        popup.transform.rotation = Quaternion.identity;

        // Configura texto
        SetupText(popup, damage, isCrit, isFatal);

        // Adiciona animação
        float durationToUse = isFatal ? fatalDuration : duration;
        popup.AddComponent<DamagePopup>().Initialize(
            targetTransform,
            isFatal ? fatalColor : (isCrit ? critColor : normalColor),
            durationToUse,
            height,
            floatSpeed,
            isFatal
        );

        Destroy(popup, durationToUse + 0.5f);

        Debug.Log($"✅ {(isFatal ? "DANO FATAL" : "Dano")} mostrado: {damage}");
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
                textUI.text = "💀 " + textUI.text + " 💀";
                textUI.fontStyle = FontStyles.Bold;
                textUI.outlineWidth = 0.4f;
                textUI.outlineColor = Color.black;
            }
            else if (isCrit)
            {
                textUI.color = critColor;
                textUI.fontSize = critFontSize;
                textUI.fontStyle = FontStyles.Bold;
                textUI.outlineWidth = 0.3f;
                textUI.outlineColor = Color.black;
            }
            else
            {
                textUI.color = normalColor;
                textUI.fontSize = normalFontSize;
                textUI.outlineWidth = 0.2f;
                textUI.outlineColor = Color.black;
            }

            textUI.alignment = TextAlignmentOptions.Center;
            textUI.enabled = true;
            popup.transform.SetAsLastSibling();
        }
    }

    [ContextMenu("🎮 Testar Dano Fatal")]
    public void TestFatalDamage()
    {
        if (Application.isPlaying)
        {
            GameObject testTarget = new GameObject("TestFatalTarget");
            testTarget.transform.position = new Vector3(0, 0, 0);

            ShowDamageFatal(testTarget.transform, 9999, true);

            Destroy(testTarget, 3f);
            Debug.Log("✅ Teste de dano fatal executado!");
        }
    }
}

// ✅ ANIMAÇÃO ATUALIZADA
public class DamagePopup : MonoBehaviour
{
    private Transform target;
    private Color originalColor;
    private float duration;
    private float floatHeight;
    private float floatSpeed;
    private bool isFatal;
    private float timer = 0f;
    private TextMeshProUGUI textMesh;
    private Vector3 startPosition;

    public void Initialize(Transform targetTransform, Color color,
                          float duration, float floatHeight, float floatSpeed, bool isFatal = false)
    {
        this.target = targetTransform;
        this.originalColor = color;
        this.duration = duration;
        this.floatHeight = floatHeight;
        this.floatSpeed = floatSpeed;
        this.isFatal = isFatal;
        this.startPosition = targetTransform != null ? targetTransform.position : Vector3.zero;

        textMesh = GetComponentInChildren<TextMeshProUGUI>();

        // Se o target for destruído, usamos a posição inicial
        if (target == null)
        {
            Debug.LogWarning("⚠️ Target é nulo no início da animação!");
        }
    }

    void Start()
    {
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
        Vector3 currentTargetPos;

        if (target != null && target.gameObject != null)
        {
            // Ainda existe, acompanha
            currentTargetPos = target.position;
        }
        else
        {
            // Já foi destruído, usa posição inicial + flutuação
            currentTargetPos = startPosition;
        }

        float currentHeight = floatHeight + (floatSpeed * timer);

        transform.position = new Vector3(
            currentTargetPos.x,
            currentTargetPos.y + currentHeight,
            currentTargetPos.z
        );

        transform.rotation = Quaternion.identity;
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

        // Efeitos especiais para dano fatal
        if (isFatal)
        {
            // Pulsação
            float pulse = 1f + Mathf.Sin(t * 20f) * 0.15f;
            transform.localScale = Vector3.one * pulse;

            // Rotação leve
            float rotation = Mathf.Sin(t * 10f) * 5f;
            transform.rotation = Quaternion.Euler(0, 0, rotation);
        }
        else
        {
            // Escala normal
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.1f;
            transform.localScale = Vector3.one * scale;
        }
    }
}