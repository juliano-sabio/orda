using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SegundaChanceSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float porcentagemCura = 0.3f;
    float recarga         = 360f;
    float timerRecarga    = 0f;
    bool  emRecarga       = false;

    public bool  EmRecarga    => emRecarga;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => recarga;

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    public void ConfigurarDeSkillData(SkillData data)
    {
        if (data.specialValue > 0f) porcentagemCura = data.specialValue / 100f;
        if (data.cooldown > 0f)     recarga         = data.cooldown;
    }

    public override void ApplyEffect() { }

    void Update()
    {
        if (emRecarga)
        {
            timerRecarga -= Time.deltaTime;
            if (timerRecarga <= 0f) emRecarga = false;
        }
    }

    public bool TentarReviver()
    {
        if (emRecarga || playerStats == null) return false;
        emRecarga    = true;
        timerRecarga = recarga;

        float pct = porcentagemCura;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.SegundaChanceCura))
            pct = 0.60f;

        playerStats.health = playerStats.maxHealth * pct;
        StartCoroutine(EfeitoRessurreicao());
        return true;
    }

    IEnumerator EfeitoRessurreicao()
    {
        playerStats.invulneravel = true;
        StartCoroutine(FlashTela());
        StartCoroutine(MostrarTexto("SEGUNDA CHANCE!", new Color(1f, 0.85f, 0.1f)));

        var sr = playerStats.GetComponent<SpriteRenderer>();
        for (int i = 0; i < 6; i++)
        {
            if (sr != null) sr.color = i % 2 == 0 ? new Color(1f, 0.9f, 0.1f) : Color.white;
            yield return new WaitForSecondsRealtime(0.12f);
        }
        if (sr != null) sr.color = Color.white;

        StartCoroutine(AnelExpansivo());

        // Invencível — sem evo = 1.5s, com evo = 3s
        float tempoInvul = SkillEvolutionManager.Tem(SkillEvolutionType.SegundaChanceInvencivel) ? 3f : 1.5f;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.SegundaChanceInvencivel))
            StartCoroutine(MostrarTexto("FÊNIX INVENCÍVEL!", new Color(1f, 0.5f, 0.1f)));

        yield return new WaitForSecondsRealtime(tempoInvul);
        playerStats.invulneravel = false;
    }

    IEnumerator FlashTela()
    {
        var go = new GameObject("FlashSC"); DontDestroyOnLoad(go);
        var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 200;
        go.AddComponent<CanvasScaler>();
        var imgGO = new GameObject("F"); imgGO.transform.SetParent(go.transform, false);
        var rt = imgGO.AddComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = imgGO.AddComponent<Image>(); img.color = new Color(1f, 0.9f, 0.1f, 0f); img.raycastTarget = false;
        for (float t = 0f; t < 0.15f; t += Time.unscaledDeltaTime) { img.color = new Color(1f, 0.9f, 0.1f, t / 0.15f * 0.7f); yield return null; }
        for (float t = 0f; t < 0.5f;  t += Time.unscaledDeltaTime) { img.color = new Color(1f, 0.9f, 0.1f, Mathf.Lerp(0.7f, 0f, t / 0.5f)); yield return null; }
        Destroy(go);
    }

    IEnumerator AnelExpansivo()
    {
        const int SEGS = 48;
        var go = new GameObject("AnelSC"); go.transform.position = playerStats.transform.position;
        var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 15;
        float dur = 0.6f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur; float r = Mathf.Lerp(0.3f, 5f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.3f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(1f, 0.85f, 0.1f, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < SEGS; i++) { float a = 360f / SEGS * i * Mathf.Deg2Rad; lr.SetPosition(i, (Vector2)playerStats.transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator MostrarTexto(string msg, Color cor)
    {
        var go = new GameObject("TextoSC"); DontDestroyOnLoad(go);
        var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 201; go.AddComponent<CanvasScaler>();
        var tGO = new GameObject("T"); tGO.transform.SetParent(go.transform, false);
        var rt = tGO.AddComponent<RectTransform>(); rt.anchorMin = new Vector2(0.1f, 0.5f); rt.anchorMax = new Vector2(0.9f, 0.7f); rt.offsetMin = rt.offsetMax = Vector2.zero;
        var txt = tGO.AddComponent<TextMeshProUGUI>(); txt.text = msg; txt.fontSize = 52; txt.fontStyle = FontStyles.Bold; txt.alignment = TextAlignmentOptions.Center; txt.color = new Color(cor.r, cor.g, cor.b, 0f);
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime) { txt.color = new Color(cor.r, cor.g, cor.b, t / 0.3f); yield return null; }
        yield return new WaitForSecondsRealtime(1.5f);
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime) { txt.color = new Color(cor.r, cor.g, cor.b, 1f - t / 0.5f); yield return null; }
        Destroy(go);
    }
}
