using UnityEngine;
using TMPro;

public class DamageNumber2D : MonoBehaviour
{
    TMP_Text tmp;
    float duration;
    float elapsed;
    Vector3 startPos;
    Vector3 velocity;
    Color corBase;
    bool isCrit;
    bool isFatal;

    public void Initialize(float damage, Color color, float dur = 1f, float floatSpeed = 2f)
    {
        tmp = GetComponent<TMP_Text>();
        if (tmp == null) { Destroy(gameObject); return; }

        corBase  = color;
        duration = dur;
        elapsed  = 0f;
        startPos = transform.position;
        isCrit   = color == Color.yellow;
        isFatal  = color == Color.red;

        // Velocidade inicial: para cima + deriva horizontal aleatória
        float dx = Random.Range(-0.5f, 0.5f);
        velocity = new Vector3(dx, floatSpeed * 1.2f, 0f);

        // Texto e estilo
        int val = Mathf.RoundToInt(damage);
        if (isFatal)
        {
            tmp.text      = val.ToString();
            tmp.fontSize  = 7;
            tmp.fontStyle = FontStyles.Bold;
        }
        else if (isCrit)
        {
            tmp.text      = val + "!";
            tmp.fontSize  = 5.5f;
            tmp.fontStyle = FontStyles.Bold;
        }
        else
        {
            tmp.text      = val.ToString();
            tmp.fontSize  = 4f;
            tmp.fontStyle = FontStyles.Normal;
        }

        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 200);

        // Escala inicial de "pop"
        transform.localScale = Vector3.one * (isFatal ? 1.6f : isCrit ? 1.3f : 1f);

        Destroy(gameObject, dur + 0.15f);
    }

    void Update()
    {
        if (tmp == null) return;
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // Movimento com gravidade suave
        velocity.y -= Time.deltaTime * 1.5f;
        transform.position += velocity * Time.deltaTime;

        // Escala: pop-in rápido → normaliza → encolhe no final
        float scaleT = Mathf.Clamp01(elapsed / 0.12f);
        float scaleOut = Mathf.Clamp01(1f - (t - 0.7f) / 0.3f);
        float popScale = Mathf.Lerp(0f, 1.1f, scaleT) * scaleOut;
        if (isFatal) popScale *= 1.3f;
        else if (isCrit) popScale *= 1.15f;
        transform.localScale = Vector3.one * popScale;

        // Fade: fica opaco até 60% depois some
        float alpha = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
        Color c = corBase; c.a = alpha;
        tmp.color = c;

        // Rotação leve só no crit
        if (isCrit)
        {
            float rot = Mathf.Sin(elapsed * 18f) * Mathf.Lerp(8f, 0f, t);
            transform.rotation = Quaternion.Euler(0, 0, rot);
        }
    }
}
