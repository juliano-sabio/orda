using System.Collections;
using UnityEngine;

public class CampoEspinhosSkillBehavior : SkillBehavior
{
    float baseDano        = 5f;
    float multiplicador   = 0.5f; // 50% do ataque do player + baseDano
    float raio            = 3f;
    float intervalo       = 3f;
    float timer;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    const int QTD_ESPINHOS = 8;

    GameObject    auraGO;
    LineRenderer[] espinhos;
    LineRenderer   lrAnel;
    SpriteRenderer srFill;
    float          angRot;
    Color          corAura = new Color(0.2f, 1f, 0.3f);

    // ── Inicialização ─────────────────────────────────────────────────────────

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        baseDano      = data.attackBonus > 0f ? data.attackBonus : 10f;
        raio      = data.specialValue > 0f   ? data.specialValue       : 3f;
        intervalo = data.activationInterval > 0f ? data.activationInterval : 1.5f;
        corAura   = data.elementColor != Color.white && data.elementColor != Color.clear
                    ? data.elementColor
                    : new Color(0.2f, 1f, 0.3f);

        timer = intervalo;
        CriarVisual();
    }

    void OnDestroy()
    {
        if (auraGO != null) Destroy(auraGO);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (playerStats == null) return;

        // Segue o player
        Vector2 pos = playerStats.transform.position;
        if (auraGO != null) auraGO.transform.position = pos;

        // Gira espinhos
        angRot += Time.deltaTime * 80f;
        AtualizarEspinhos(pos);

        // Pulso do anel
        if (lrAnel != null)
        {
            float pulso = Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f;
            lrAnel.startColor = lrAnel.endColor =
                new Color(corAura.r, corAura.g, corAura.b, 0.4f + pulso * 0.35f);
            lrAnel.startWidth = lrAnel.endWidth = 0.06f + pulso * 0.04f;
        }

        if (srFill != null)
        {
            float pulso = Mathf.Sin(Time.time * 3f) * 0.5f + 0.5f;
            srFill.color = new Color(corAura.r, corAura.g, corAura.b, 0.04f + pulso * 0.03f);
        }

        // Tick de dano
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = intervalo;
            DanificarInimigos();
            StartCoroutine(FlashEspinhos());
        }
    }

    public override void ApplyEffect() => DanificarInimigos();

    // ── Dano ──────────────────────────────────────────────────────────────────

    void DanificarInimigos()
    {
        if (playerStats == null) return;
        var cols = Physics2D.OverlapCircleAll(playerStats.transform.position, raio);
        foreach (var col in cols)
        {
            var ic = col.GetComponent<InimigoController>()
                  ?? col.GetComponentInParent<InimigoController>();
            if (ic != null) ic.ReceberDano(DanoAtual, false);
        }
    }

    // ── Visual ────────────────────────────────────────────────────────────────

    void CriarVisual()
    {
        if (playerStats == null) return;

        auraGO = new GameObject("CampoEspinhosAura");
        auraGO.transform.position = playerStats.transform.position;

        // Anel externo
        var anelGO = new GameObject("Anel");
        anelGO.transform.SetParent(auraGO.transform, false);
        lrAnel = anelGO.AddComponent<LineRenderer>();
        CriarAnel(lrAnel, raio, corAura, 0.06f, 5);

        // Fill semitransparente
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(auraGO.transform, false);
        srFill               = fillGO.AddComponent<SpriteRenderer>();
        srFill.sprite        = GerarDisco(64);
        srFill.color         = new Color(corAura.r, corAura.g, corAura.b, 0.05f);
        srFill.sortingOrder  = 3;
        fillGO.transform.localScale = Vector3.one * (raio * 2f);

        // Espinhos
        espinhos = new LineRenderer[QTD_ESPINHOS];
        for (int i = 0; i < QTD_ESPINHOS; i++)
        {
            var sgo = new GameObject($"Espinho{i}");
            sgo.transform.SetParent(auraGO.transform, false);
            var lr = sgo.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder  = 6;
            lr.startWidth    = 0.1f;
            lr.endWidth      = 0.01f;
            lr.startColor    = lr.endColor = corAura;
            espinhos[i] = lr;
        }
    }

    void CriarAnel(LineRenderer lr, float r, Color cor, float larg, int order)
    {
        const int SEGS = 48;
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = order;
        lr.startWidth    = lr.endWidth = larg;
        lr.startColor    = lr.endColor = cor;
        // posições serão definidas em AtualizarEspinhos
    }

    void AtualizarEspinhos(Vector2 centro)
    {
        // Anel
        if (lrAnel != null)
        {
            const int SEGS = 48;
            lrAnel.positionCount = SEGS;
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lrAnel.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
            }
        }

        // Espinhos girando
        if (espinhos == null) return;
        for (int i = 0; i < QTD_ESPINHOS; i++)
        {
            if (espinhos[i] == null) continue;
            float a   = (angRot + 360f / QTD_ESPINHOS * i) * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            espinhos[i].SetPosition(0, centro + d * (raio * 0.55f));
            espinhos[i].SetPosition(1, centro + d * raio);
        }
    }

    IEnumerator FlashEspinhos()
    {
        if (espinhos == null) yield break;
        Color brilho = Color.white;
        foreach (var lr in espinhos)
            if (lr != null) lr.startColor = lr.endColor = brilho;

        yield return new WaitForSeconds(0.08f);

        foreach (var lr in espinhos)
            if (lr != null) lr.startColor = lr.endColor = corAura;
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
