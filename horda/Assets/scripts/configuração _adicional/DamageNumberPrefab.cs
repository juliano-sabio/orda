using UnityEngine;
using TMPro;

public class DamageNumberPrefab : MonoBehaviour
{
    [Header("Referências")]
    public TextMeshProUGUI damageText;

    [Header("Configurações Locais")]
    [Tooltip("Sobrescreve as configurações do manager")]
    public bool useLocalSettings = false;

    [Tooltip("Tamanho local da fonte")]
    public int localFontSize = 24;

    void Start()
    {
        // Se não tiver referência, tenta encontrar
        if (damageText == null)
        {
            damageText = GetComponent<TextMeshProUGUI>();
        }

        Debug.Log($"✅ Prefab DamageNumber inicializado. Texto: {damageText?.text}");
    }

    public void SetupDamage(float damage, Color color, int fontSize = 24)
    {
        if (damageText == null)
        {
            Debug.LogError("❌ damageText não encontrado no prefab!");
            return;
        }

        damageText.text = Mathf.RoundToInt(damage).ToString();
        damageText.color = color;

        // Usa configuração local se ativada
        if (useLocalSettings)
        {
            damageText.fontSize = localFontSize;
        }
        else
        {
            damageText.fontSize = fontSize;
        }

        Debug.Log($"🎨 Prefab configurado: {damage}");
    }
}