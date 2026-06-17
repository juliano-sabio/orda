using System.Collections;
using UnityEngine;

public class InstintoSobrevivenciaSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float recarga      = 180f;
    float duracao      = 8f;
    float limiarHP     = 0.30f;
    float bonusDefesa  = 15f;
    float bonusRegen   = 5f;

    float timerRecarga = 0f;
    bool  ativo        = false;
    bool  buffsAtivos  = false;

    public bool  EmRecarga    => timerRecarga > 0f;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => recarga;

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(1f, 0.55f, 0.1f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        if (data.cooldown > 0f)           recarga     = data.cooldown;
        if (data.activationInterval > 0f) duracao     = data.activationInterval;
        if (data.specialValue > 0f)       limiarHP    = data.specialValue / 100f;
        if (data.defenseBonus > 0f)       bonusDefesa = data.defenseBonus;
        if (data.healthRegenBonus > 0f)   bonusRegen  = data.healthRegenBonus;
    }

    public override void ApplyEffect() { }

    void Update()
    {
        if (timerRecarga > 0f) timerRecarga -= Time.deltaTime;
        if (playerStats == null || ativo) return;

        float pct = playerStats.health / Mathf.Max(1f, playerStats.maxHealth);
        if (pct <= limiarHP && timerRecarga <= 0f)
            StartCoroutine(Ativar());
    }

    IEnumerator Ativar()
    {
        ativo        = true;
        timerRecarga = recarga;
        SkillElementEffect.AplicarDefensivo(skillData, playerStats, DefensiveTrigger.OnAtivar, null, this);

        // Aplica buffs
        playerStats.defense         += bonusDefesa;
        playerStats.healthRegenRate += bonusRegen;
        buffsAtivos = true;

        // InstintoFurioso — bônus de dano temporário
        float bonusDano = 0f;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.InstintoFurioso))
        {
            bonusDano = playerStats.attack * 0.25f;
            playerStats.attack += bonusDano;
        }

        // InstintoEspirito — cura imediata ao ativar
        if (SkillEvolutionManager.Tem(SkillEvolutionType.InstintoEspirito))
            playerStats.Heal(playerStats.maxHealth * 0.20f);

        // Visual: aura laranja pulsante
        var aura = CriarAura();
        string msg = SkillEvolutionManager.Tem(SkillEvolutionType.InstintoFurioso)
            ? "INSTINTO FURIOSO!" : "INSTINTO ATIVO!";
        StartCoroutine(MostrarTexto(msg, new Color(1f, 0.55f, 0.1f)));

        yield return new WaitForSeconds(duracao);

        // Remove bônus de dano
        if (bonusDano > 0f && playerStats != null)
            playerStats.attack -= bonusDano;

        // Remove buffs
        if (playerStats != null && buffsAtivos)
        {
            playerStats.defense         -= bonusDefesa;
            playerStats.healthRegenRate -= bonusRegen;
            buffsAtivos = false;
        }

        ativo = false;
        if (aura != null) StartCoroutine(FadeDestruir(aura, 0.4f));
    }

    void OnDestroy()
    {
        if (playerStats != null && buffsAtivos)
        {
            playerStats.defense         -= bonusDefesa;
            playerStats.healthRegenRate -= bonusRegen;
        }
    }

    GameObject CriarAura()
    {
        var go = new GameObject("InstintoAura");
        go.transform.SetParent(playerStats.transform, false);
        StartCoroutine(AnimarAura(go));
        return go;
    }

    IEnumerator AnimarAura(GameObject go)
    {
        const int S = 40;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 11;
        for (int i = 0; i < S; i++) { float a = 360f / S * i * Mathf.Deg2Rad; lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 1.0f, Mathf.Sin(a) * 1.0f, 0f)); }

        // Partículas subindo
        StartCoroutine(ParticulasSubindo());

        float t = 0f;
        while (go != null && ativo)
        {
            t += Time.deltaTime;
            float pulso = Mathf.Sin(t * 5f) * 0.5f + 0.5f;
            Color ce = CorElemento();
            lr.startColor = lr.endColor = new Color(ce.r, ce.g, ce.b, 0.55f + pulso * 0.4f);
            lr.startWidth = lr.endWidth = 0.08f + pulso * 0.07f;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, t * 40f);
            yield return null;
        }
    }

    IEnumerator ParticulasSubindo()
    {
        while (ativo && playerStats != null)
        {
            yield return new WaitForSeconds(0.15f);
            var p = new GameObject("PInstinto");
            p.transform.position = (Vector2)playerStats.transform.position + Random.insideUnitCircle * 0.8f;
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = GerarDisco(6); sr.color = new Color(1f, 0.6f, 0.1f, 0.8f); sr.sortingOrder = 12;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.18f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(Vector2.up * Random.Range(1f, 2.5f), 0.6f);
            Destroy(p, 0.8f);
        }
    }

    IEnumerator MostrarTexto(string msg, Color cor)
    {
        var go = new GameObject("TextoInstinto"); DontDestroyOnLoad(go);
        var cv = go.AddComponent<UnityEngine.Canvas>(); cv.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 150; go.AddComponent<UnityEngine.UI.CanvasScaler>();
        var tGO = new GameObject("T"); tGO.transform.SetParent(go.transform, false);
        var rt = tGO.AddComponent<UnityEngine.RectTransform>(); rt.anchorMin = new Vector2(0.1f,0.55f); rt.anchorMax = new Vector2(0.9f,0.75f); rt.offsetMin = rt.offsetMax = Vector2.zero;
        var txt = tGO.AddComponent<TMPro.TextMeshProUGUI>(); txt.text = msg; txt.fontSize = 38; txt.fontStyle = TMPro.FontStyles.Bold; txt.alignment = TMPro.TextAlignmentOptions.Center; txt.color = new Color(cor.r,cor.g,cor.b,0f);
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime) { txt.color = new Color(cor.r,cor.g,cor.b,t/0.3f); yield return null; }
        yield return new WaitForSecondsRealtime(1.5f);
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { txt.color = new Color(cor.r,cor.g,cor.b,1f-t/0.4f); yield return null; }
        Destroy(go);
    }

    IEnumerator FadeDestruir(GameObject go, float dur)
    {
        var lr = go.GetComponent<LineRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime) { if (lr != null) { Color c = lr.startColor; c.a = Mathf.Lerp(1f, 0f, t/dur); lr.startColor = lr.endColor = c; } yield return null; }
        Destroy(go);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz*0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }
}
