using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarrasAbismoSkillBehavior : SkillBehavior, ISkillComRecarga, IEvoluivel
{
    float baseDano      = 12f;
    float multiplicador = 0.7f;
    float intervalo     = 8f;
    int   qtdAlvos      = 2;
    float duracaoPresa  = 1.2f;
    float raioDeteccao  = 10f;

    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervalo;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    static readonly Color COR_GARRAS = new Color(0.45f, 0.1f, 0.7f, 0.95f);
    static readonly Color COR_AVISO  = new Color(0.3f, 0f, 0.5f, 0.6f);

    
    public void OnEvolucaoAplicada(SkillEvolutionType tipo)
    {
        if (tipo == SkillEvolutionType.GarrasAlcance) raioDeteccao *= 1.6f;
    }
public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(0.45f, 0.1f, 0.7f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano      = data.attackBonus > 0f          ? data.attackBonus        : 25f;
        intervalo     = data.activationInterval > 0f   ? data.activationInterval : 4f;
        qtdAlvos      = data.projectileCount > 0 ? data.projectileCount : 2;
        duracaoPresa  = data.duration > 0f             ? data.duration           : 1.2f;
        timer         = intervalo;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; StartCoroutine(AtivarGarras()); }
    }

    public override void ApplyEffect() => StartCoroutine(AtivarGarras());

    // ── Lógica ────────────────────────────────────────────────────────────────

    IEnumerator AtivarGarras()
    {
        var alvos = EncontrarAlvos(qtdAlvos);
        foreach (var ic in alvos)
        {
            if (ic == null) continue;
            StartCoroutine(GarraNaVitima(ic));
            yield return new WaitForSeconds(0.12f);
        }
    }

    IEnumerator GarraNaVitima(InimigoController ic)
    {
        Vector2 pos = ic.transform.position;

        // Aviso no chão
        float tempoAviso = 0.55f;
        var avisoGO = CriarAvisoChao(pos);
        for (float t = 0f; t < tempoAviso; t += Time.deltaTime)
        {
            if (avisoGO == null) yield break;
            float prog  = t / tempoAviso;
            float pulso = Mathf.Sin(t * 16f) * 0.5f + 0.5f;
            var lr = avisoGO.GetComponent<LineRenderer>();
            if (lr != null)
                lr.startColor = lr.endColor =
                    new Color(COR_AVISO.r, COR_AVISO.g, COR_AVISO.b, 0.3f + pulso * 0.4f);
            yield return null;
        }
        if (avisoGO != null) Destroy(avisoGO);

        if (ic == null || ic.estaMorrendo) yield break;

        // Atualiza posição (inimigo pode ter se movido)
        pos = ic.transform.position;

        // Dano e prende
        bool execucao = SkillEvolutionManager.Tem(SkillEvolutionType.Execucao) &&
                        ic.vidaAtual / Mathf.Max(1f, ic.vidaMaxima) < 0.25f;
        if (cosmetico) { } // co-op: cópia cosmética não aplica dano (só o visual da garra)
        else if (execucao) { ic.ReceberDano(ic.vidaAtual + 1f, false); SkillElementEffect.Aplicar(skillData, ic.gameObject, DanoAtual, this); }
        else
        {
            ic.ReceberDano(DanoAtual, false);
            SkillElementEffect.Aplicar(skillData, ic.gameObject, DanoAtual, this);
            if (SkillEvolutionManager.Tem(SkillEvolutionType.GarrasVenenosas))
                EvolutionFX.AplicarVeneno(ic, DanoAtual * 0.4f, 2.5f);
        }
        StartCoroutine(PrederInimigo(ic, duracaoPresa));
        StartCoroutine(AnimarGarras(pos));
    }

    IEnumerator PrederInimigo(InimigoController ic, float duracao)
    {
        if (ic == null) yield break;

        var rb = ic.GetComponent<Rigidbody2D>();
        var movi = ic.GetComponent<movi_inimigo>();

        // Para o inimigo
        float velOriginal = 0f;
        if (movi != null) { velOriginal = movi.velocidade; movi.velocidade = 0f; }
        if (rb   != null) rb.linearVelocity = Vector2.zero;

        // Flash roxo
        var sr = ic.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.6f, 0.2f, 0.9f);

        yield return new WaitForSeconds(duracao);

        // Restaura
        if (ic != null)
        {
            if (movi != null) movi.velocidade = velOriginal;
            if (sr   != null) sr.color = Color.white;
        }
    }

    // ── Visual das garras ─────────────────────────────────────────────────────

    IEnumerator AnimarGarras(Vector2 centro)
    {
        const int QTD = 4;
        var garrasGO = new GameObject("GarrasAbismo");
        garrasGO.transform.position = centro;

        SomSkill.Tocar(SomSkill.Tipo.GarraErupcaoDark, centro, 0.55f); // co-op: toca tb na cópia cosmética (outro player ouve)

        Color cel = CorElemento();
        Color corGarras = new Color(cel.r, cel.g, cel.b, 0.95f);

        var lrs = new LineRenderer[QTD];
        for (int i = 0; i < QTD; i++)
        {
            var go = new GameObject($"Garra{i}");
            go.transform.SetParent(garrasGO.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 3;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder  = 12;
            lr.startWidth    = 0.18f;
            lr.endWidth      = 0.04f;
            lr.startColor    = lr.endColor = corGarras;
            lr.numCapVertices = 3;
            lrs[i] = lr;
        }

        // Anel de impacto no chão
        var anelGO = new GameObject("AnelGarras");
        anelGO.transform.SetParent(garrasGO.transform, false);
        var lrAnel = anelGO.AddComponent<LineRenderer>();
        lrAnel.useWorldSpace = true; lrAnel.loop = true; lrAnel.positionCount = 32;
        lrAnel.material = new Material(Shader.Find("Sprites/Default")); lrAnel.sortingOrder = 11;
        lrAnel.startWidth = lrAnel.endWidth = 0.12f;
        lrAnel.startColor = lrAnel.endColor = corGarras;
        for (int i = 0; i < 32; i++)
        {
            float a = 360f / 32 * i * Mathf.Deg2Rad;
            lrAnel.SetPosition(i, centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.8f);
        }

        // Fase 1: emergir (0 → altura máxima)
        float alturMax = 1.4f;
        float durEmerg = 0.2f;
        for (float t = 0f; t < durEmerg; t += Time.deltaTime)
        {
            if (garrasGO == null) yield break;
            float p = Mathf.SmoothStep(0f, 1f, t / durEmerg);
            AtualizarPosGarras(lrs, centro, alturMax * p, QTD);
            yield return null;
        }

        // Fase 2: manter presas
        float durPresa = duracaoPresa;
        for (float t = 0f; t < durPresa; t += Time.deltaTime)
        {
            if (garrasGO == null) yield break;
            float pulso = Mathf.Sin(t * 8f) * 0.08f;
            AtualizarPosGarras(lrs, centro, alturMax + pulso, QTD);
            // Pulsa cor
            float p2 = Mathf.Sin(t * 6f) * 0.5f + 0.5f;
            Color cor = Color.Lerp(corGarras, Color.white, p2 * 0.3f);
            foreach (var lr in lrs) if (lr != null) lr.startColor = lr.endColor = cor;
            lrAnel.startColor = lrAnel.endColor = cor;
            yield return null;
        }

        // Fase 3: retrair + fade
        SomSkill.Tocar(SomSkill.Tipo.GarraDissiparDark, centro, 0.3f); // co-op: toca tb na cópia cosmética (outro player ouve)
        float durRetir = 0.25f;
        for (float t = 0f; t < durRetir; t += Time.deltaTime)
        {
            if (garrasGO == null) yield break;
            float p = t / durRetir;
            AtualizarPosGarras(lrs, centro, alturMax * (1f - p), QTD);
            Color cor = new Color(corGarras.r, corGarras.g, corGarras.b, Mathf.Lerp(1f, 0f, p));
            foreach (var lr in lrs) if (lr != null) lr.startColor = lr.endColor = cor;
            lrAnel.startColor = lrAnel.endColor = cor;
            yield return null;
        }

        // Partículas de dispersão
        SpawnParticulas(centro);

        if (garrasGO != null) Destroy(garrasGO);
    }

    void AtualizarPosGarras(LineRenderer[] lrs, Vector2 centro, float altura, int qtd)
    {
        for (int i = 0; i < qtd; i++)
        {
            if (lrs[i] == null) continue;
            float ang  = (360f / qtd * i) * Mathf.Deg2Rad;
            Vector2 base_ = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.55f;
            Vector2 ponta = base_ + Vector2.up * altura;
            // Curva intermediária
            Vector2 meio  = Vector2.Lerp(base_, ponta, 0.5f)
                          + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.2f;
            lrs[i].SetPosition(0, base_);
            lrs[i].SetPosition(1, meio);
            lrs[i].SetPosition(2, ponta);
        }
    }

    GameObject CriarAvisoChao(Vector2 pos)
    {
        const int SEGS = 32;
        var go = new GameObject("AvisoGarras");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 8;
        lr.startWidth = lr.endWidth = 0.07f;
        lr.startColor = lr.endColor = COR_AVISO;
        for (int i = 0; i < SEGS; i++)
        {
            float ang = 360f / SEGS * i * Mathf.Deg2Rad;
            lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.8f);
        }
        return go;
    }

    void SpawnParticulas(Vector2 pos)
    {
        for (int i = 0; i < 8; i++)
        {
            var go = new GameObject("PartGarras");
            go.transform.position = pos + Random.insideUnitCircle * 0.5f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GerarDisco(6);
            sr.color  = new Color(0.6f, 0.2f, 0.9f);
            sr.sortingOrder = 13;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            StartCoroutine(FadeParticula(sr, Random.insideUnitCircle * Random.Range(1.5f, 4f)));
        }
    }

    IEnumerator FadeParticula(SpriteRenderer sr, Vector2 vel)
    {
        Color cor = sr.color;
        float vida = Random.Range(0.3f, 0.6f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel *= Mathf.Pow(0.85f, Time.deltaTime * 60f);
            if (sr != null)
            {
                sr.transform.position += (Vector3)(vel * Time.deltaTime);
                sr.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, t / vida));
            }
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }

    // ── Utilitários ───────────────────────────────────────────────────────────

    List<InimigoController> EncontrarAlvos(int qtd)
    {
        var todos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        var lista = new List<InimigoController>(todos);
        Vector2 orig = playerStats != null ? (Vector2)playerStats.transform.position : Vector2.zero;
        lista.RemoveAll(ic => ic.estaMorrendo ||
            Vector2.Distance(ic.transform.position, orig) > raioDeteccao);
        lista.Sort((a, b) =>
            Vector2.Distance(a.transform.position, orig)
            .CompareTo(Vector2.Distance(b.transform.position, orig)));
        if (lista.Count > qtd) lista.RemoveRange(qtd, lista.Count - qtd);
        return lista;
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, d < cx ? 1f : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}



