using UnityEngine;

public class DamageDebugSystem : MonoBehaviour
{
    public KeyCode debugKey = KeyCode.F5;
    public Transform testTarget;
    public float testDamage = 25f;
    public bool testCrit = false;

    void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            RunCompleteDebug();
        }
    }

    void RunCompleteDebug()
    {
        Debug.Log("🔍 === DAMAGE SYSTEM DEBUG === 🔍");

        // 1. Verifica DamageNumberManager
        DamageNumberManager manager = DamageNumberManager.Instance;
        if (manager == null)
        {
            Debug.LogError("❌ DamageNumberManager.Instance é NULL!");
            return;
        }
        Debug.Log($"✅ DamageNumberManager encontrado: {manager.name}");

        // 2. Verifica prefab
        if (manager.damagePrefab == null)
        {
            Debug.LogError("❌ Prefab não atribuído ao DamageNumberManager!");
            return;
        }
        Debug.Log($"✅ Prefab atribuído: {manager.damagePrefab.name}");

        // 3. Verifica TextMeshPro no prefab
        var textMesh = manager.damagePrefab.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textMesh == null)
        {
            Debug.LogError("❌ TextMeshProUGUI não encontrado no prefab!");
            return;
        }
        Debug.Log($"✅ TextMeshProUGUI encontrado no prefab");

        // 4. Verifica Canvas
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Debug.Log($"✅ Número de Canvases na cena: {allCanvases.Length}");
        foreach (Canvas canvas in allCanvases)
        {
            Debug.Log($"   - {canvas.name} | Mode: {canvas.renderMode} | Scale: {canvas.transform.localScale}");
        }

        // 5. Testa instanciação
        if (testTarget != null)
        {
            Debug.Log("🎮 Executando teste de instanciação...");
            manager.ShowDamage(testTarget, testDamage, testCrit);
        }
        else
        {
            Debug.LogWarning("⚠️ Test target não configurado!");
        }

        Debug.Log("🔍 === DEBUG COMPLETO === 🔍");
    }

    void OnGUI()
    {
        GUI.BeginGroup(new Rect(10, 10, 400, 200));

        GUI.Box(new Rect(0, 0, 400, 200), "Damage System Debug");

        DamageNumberManager manager = DamageNumberManager.Instance;

        GUI.Label(new Rect(10, 30, 380, 30), $"Manager: {(manager != null ? "✅ ENCONTRADO" : "❌ NÃO ENCONTRADO")}");

        if (manager != null)
        {
            GUI.Label(new Rect(10, 60, 380, 30), $"Prefab: {(manager.damagePrefab != null ? "✅ " + manager.damagePrefab.name : "❌ NÃO ATRIBUÍDO")}");

            // Mostra configurações
            GUI.Label(new Rect(10, 90, 380, 30), $"Canvas Scale: {manager.canvasScale}");
            GUI.Label(new Rect(10, 120, 380, 30), $"Font Size: {manager.normalFontSize}/{manager.critFontSize}");

            // Botão de teste
            if (GUI.Button(new Rect(10, 150, 180, 30), "TESTAR DANO (F5)"))
            {
                if (testTarget != null)
                {
                    manager.ShowDamage(testTarget, testDamage, testCrit);
                }
            }

            if (GUI.Button(new Rect(200, 150, 180, 30), "VER CANVAS"))
            {
                HighlightCanvas();
            }
        }

        GUI.EndGroup();
    }

    void HighlightCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                // Destaca o Canvas na cena
                Debug.Log($"🎯 Canvas encontrado: {canvas.name} na posição {canvas.transform.position}");

                // Adiciona um Gizmo temporário
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.transform.position = canvas.transform.position;
                marker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                marker.GetComponent<Renderer>().material.color = Color.red;
                Destroy(marker, 2f);
            }
        }
    }
}