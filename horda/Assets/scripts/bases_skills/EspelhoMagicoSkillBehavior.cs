using System.Collections;
using UnityEngine;

public class EspelhoMagicoSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float recarga  = 180f;
    float duracao  = 4f;

    float timerRecarga = 0f;
    public bool  Ativo         => ativoAgora;
    bool         ativoAgora    = false;

    public bool  EmRecarga    => timerRecarga > 0f;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => recarga;

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(0.6f, 0.9f, 1f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        if (data.cooldown > 0f)           recarga = data.cooldown;
        if (data.activationInterval > 0f) duracao = data.activationInterval;
    }

    public override void ApplyEffect() => Ativar();

    void OnEnable()  => PlayerStats.OnDanoRecebido += OnDano;
    void OnDisable() => PlayerStats.OnDanoRecebido -= OnDano;

    void OnDano()
    {
        if (timerRecarga <= 0f && !ativoAgora) Ativar();
    }

    void Update()
    {
        if (timerRecarga > 0f) timerRecarga -= Time.deltaTime;
    }

    void Ativar()
    {
        timerRecarga = recarga;
        StartCoroutine(CorotinaEspelho());
    }

    // Chamado por PlayerStats.TakeDamage para refletir dano
    public bool TentarRefletir(float dano)
    {
        if (!ativoAgora) return false;

        // Causa o dano no inimigo mais próximo
        var inimigos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        InimigoController alvo = null;
        float menorDist = float.MaxValue;
        Vector2 posPlayer = playerStats != null ? (Vector2)playerStats.transform.position : Vector2.zero;
        foreach (var ic in inimigos)
        {
            if (ic == null || ic.estaMorrendo) continue;
            float d = Vector2.Distance(ic.transform.position, posPlayer);
            if (d < menorDist) { menorDist = d; alvo = ic; }
        }
        // EspelhoAmplificado — 150%, padrão — 100%
        float mult = SkillEvolutionManager.Tem(SkillEvolutionType.EspelhoAmplificado) ? 1.5f : 1.0f;
        if (alvo != null) { alvo.ReceberDano(dano * mult, false); SkillElementEffect.Aplicar(skillData, alvo.gameObject, dano * mult, this); }

        // EspelhoExplosivo — explosão em área no alvo
        if (SkillEvolutionManager.Tem(SkillEvolutionType.EspelhoExplosivo) && alvo != null)
            EvolutionFX.SpawnExplosao(alvo.transform.position, 2.5f, dano * 0.8f,
                new Color(0.6f, 0.9f, 1f), this);

        StartCoroutine(EfeitoReflexao(posPlayer));
        return true;
    }

    IEnumerator CorotinaEspelho()
    {
        ativoAgora = true;
        SkillElementEffect.AplicarDefensivo(skillData, playerStats, DefensiveTrigger.OnAtivar, null, this);
        var visual = CriarVisual();
        StartCoroutine(MostrarTexto("ESPELHO MÁGICO!", new Color(0.6f, 0.9f, 1f)));
        yield return new WaitForSeconds(duracao);
        ativoAgora = false;
        if (visual != null) StartCoroutine(FadeDestruir(visual, 0.3f));
    }

    GameObject CriarVisual()
    {
        var root = new GameObject("EspelhoVisual");

        // Anel hexagonal rotacionando
        for (int r = 0; r < 2; r++)
        {
            var go = new GameObject($"Hex{r}");
            go.transform.SetParent(root.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.loop = true; lr.positionCount = 6;
            lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 9;
            lr.startWidth = lr.endWidth = r == 0 ? 0.1f : 0.05f;
            lr.startColor = lr.endColor = new Color(0.6f, 0.9f, 1f, r == 0 ? 0.85f : 0.5f);
        }

        StartCoroutine(AnimarEspelho(root));
        return root;
    }

    IEnumerator AnimarEspelho(GameObject root)
    {
        float ang = 0f;
        while (root != null && ativoAgora && playerStats != null)
        {
            ang += Time.deltaTime * 60f;
            root.transform.position = playerStats.transform.position;

            float pulso = Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f;
            var lrs = root.GetComponentsInChildren<LineRenderer>();

            float[] raios = { 1.4f, 1.0f };
            Color ce = CorElemento();
            for (int r = 0; r < lrs.Length && r < 2; r++)
            {
                float offAng = r == 0 ? ang : -ang * 1.3f;
                for (int i = 0; i < 6; i++)
                {
                    float a = (offAng + 60f * i) * Mathf.Deg2Rad;
                    lrs[r].SetPosition(i, (Vector2)playerStats.transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * raios[r]);
                }
                Color cor = new Color(ce.r, ce.g, ce.b, (0.5f + pulso * 0.4f) * (r == 0 ? 1f : 0.6f));
                lrs[r].startColor = lrs[r].endColor = cor;
                lrs[r].startWidth = lrs[r].endWidth = (r == 0 ? 0.1f : 0.05f) + pulso * 0.03f;
            }

            // Partículas de brilho
            if (Time.frameCount % 8 == 0)
            {
                float pa = Random.Range(0f, Mathf.PI * 2f);
                var p = new GameObject("PEspelho");
                p.transform.position = (Vector2)playerStats.transform.position + new Vector2(Mathf.Cos(pa), Mathf.Sin(pa)) * 1.2f;
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = GerarDisco(6); sr.color = new Color(0.6f, 0.9f, 1f, 0.8f); sr.sortingOrder = 10;
                p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);
                p.AddComponent<AutoDestroyFadeMove>().Iniciar(Random.insideUnitCircle * 0.5f, 0.4f);
                Destroy(p, 0.6f);
            }
            yield return null;
        }
    }

    IEnumerator EfeitoReflexao(Vector2 pos)
    {
        // Anel de reflexão
        const int S = 32;
        var go = new GameObject("Reflexao");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / 0.3f; float r = Mathf.Lerp(0.3f, 2f, p);
            Color ce = CorElemento();
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.2f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(ce.r, ce.g, ce.b, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < S; i++) { float a = 360f/S*i*Mathf.Deg2Rad; lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator MostrarTexto(string msg, Color cor)
    {
        var go = new GameObject("TextoEspelho"); DontDestroyOnLoad(go);
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
        var lrs = go.GetComponentsInChildren<LineRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime) { foreach (var lr in lrs) { Color c = lr.startColor; c.a = Mathf.Lerp(1f,0f,t/dur); lr.startColor = lr.endColor = c; } yield return null; }
        Destroy(go);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz*0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }
}
