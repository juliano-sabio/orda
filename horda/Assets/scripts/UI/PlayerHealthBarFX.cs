using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class PlayerHealthBarFX : MonoBehaviour
{
    Slider slider;
    Image  fillImg;
    Image  damageImg;
    Image  glowImg;

    float prevTarget;
    float damageDisplay;
    float damageTimer;

    const float DAMAGE_DELAY = 0.6f;
    const float DAMAGE_LERP  = 2f;

    static readonly Color VERDE    = new Color(0.18f, 0.92f, 0.38f);
    static readonly Color AMARELO  = new Color(0.97f, 0.82f, 0.12f);
    static readonly Color VERMELHO = new Color(0.96f, 0.20f, 0.10f);
    static readonly Color COR_DANO = new Color(0.92f, 0.12f, 0.05f, 0.55f);

    void Start()
    {
        slider       = GetComponent<Slider>();
        prevTarget   = slider.value;
        damageDisplay = slider.value;

        fillImg = slider.fillRect?.GetComponent<Image>();

        // Fundo mais escuro e elegante
        var bg = transform.Find("Background")?.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.05f, 0.05f, 0.09f, 0.95f);

        CriarGlow();
        CriarBorda();
        CriarDamageBar();
    }

    void CriarGlow()
    {
        var go = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-6f, -6f); rt.offsetMax = new Vector2(6f, 6f);
        glowImg = go.GetComponent<Image>();
        glowImg.color = new Color(VERDE.r, VERDE.g, VERDE.b, 0.18f);
        glowImg.raycastTarget = false;
    }

    void CriarBorda()
    {
        var go = new GameObject("Borda", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-2f, -2f); rt.offsetMax = new Vector2(2f, 2f);
        go.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.06f, 1f);
        go.GetComponent<Image>().raycastTarget = false;
    }

    void CriarDamageBar()
    {
        Transform fillArea = slider.fillRect?.parent;
        if (fillArea == null) return;

        var go = new GameObject("DamageBar", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(fillArea, false);
        go.transform.SetAsFirstSibling();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        damageImg = go.GetComponent<Image>();
        damageImg.color = COR_DANO;
        damageImg.raycastTarget = false;
    }

    void Update()
    {
        if (slider == null || slider.maxValue <= 0f) return;

        float target = slider.value;
        float pct    = Mathf.Clamp01(target / slider.maxValue);

        // Detecta dano recebido
        if (target < prevTarget - 0.5f)
            damageTimer = DAMAGE_DELAY;
        prevTarget = target;

        // Damage lag
        if (damageTimer > 0f) damageTimer -= Time.deltaTime;
        else damageDisplay = Mathf.Lerp(damageDisplay, target, Time.deltaTime * DAMAGE_LERP);

        float dmgPct = Mathf.Clamp01(damageDisplay / slider.maxValue);

        // Damage bar: anchorMax.x = quanto da barra cobre
        if (damageImg != null)
        {
            var rt = damageImg.rectTransform;
            rt.anchorMax = new Vector2(dmgPct, rt.anchorMax.y);
            damageImg.gameObject.SetActive(dmgPct > pct + 0.01f);
        }

        // Gradiente de cor: verde → amarelo → vermelho
        Color corAlvo = pct > 0.5f
            ? Color.Lerp(AMARELO, VERDE, (pct - 0.5f) * 2f)
            : Color.Lerp(VERMELHO, AMARELO, pct * 2f);
        if (fillImg != null)
            fillImg.color = Color.Lerp(fillImg.color, corAlvo, Time.deltaTime * 8f);

        // Glow: pulsa vermelho quando HP < 30%, verde suave acima
        if (glowImg != null)
        {
            if (pct < 0.30f)
            {
                float a = Mathf.PingPong(Time.time * 3.5f, 0.35f) + 0.1f;
                glowImg.color = new Color(VERMELHO.r, VERMELHO.g, VERMELHO.b, a);
            }
            else
            {
                Color alvo = new Color(VERDE.r, VERDE.g, VERDE.b, pct * 0.18f);
                glowImg.color = Color.Lerp(glowImg.color, alvo, Time.deltaTime * 4f);
            }
        }
    }
}
