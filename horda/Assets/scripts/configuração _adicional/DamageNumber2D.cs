using UnityEngine;
using TMPro;

public class DamageNumber2D : MonoBehaviour
{
    private TMP_Text textMeshPro;
    private float floatSpeed = 2f;
    private float duration = 1f;
    private Vector3 startPosition;
    private float startTime;
    private Color originalColor;

    public void Initialize(float damage, Color color, float duration = 1f, float floatSpeed = 2f)
    {
        textMeshPro = GetComponent<TMP_Text>();
        if (textMeshPro == null)
        {
            Debug.LogError("❌ DamageNumber2D precisa de um componente TMP_Text!");
            return;
        }

        textMeshPro.text = Mathf.RoundToInt(damage).ToString();
        originalColor = color;
        textMeshPro.color = color;
        textMeshPro.alignment = TextAlignmentOptions.Center;

        this.duration = duration;
        this.floatSpeed = floatSpeed;
        this.startPosition = transform.position;
        this.startTime = Time.time;

        // Destrói automaticamente
        Destroy(gameObject, duration + 0.1f);
    }

    void Update()
    {
        if (textMeshPro == null) return;

        float t = (Time.time - startTime) / duration;

        // Movimento para cima
        transform.position = startPosition + Vector3.up * floatSpeed * t;

        // Fade out
        Color color = originalColor;
        color.a = 1f - t;
        textMeshPro.color = color;

        // Efeito de escala
        float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
        transform.localScale = Vector3.one * scale;

        // Rotação leve para efeito dinâmico
        float rotation = Mathf.Sin(t * Mathf.PI * 2) * 5f;
        transform.rotation = Quaternion.Euler(0, 0, rotation);
    }
}