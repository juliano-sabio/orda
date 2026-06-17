using System.Collections;
using UnityEngine;

public class FugaSombrasSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float limiarHP   = 0.50f;
    float recarga    = 360f;
    float distancia  = 30f;
    float timerRecarga = 0f;

    public bool  EmRecarga    => timerRecarga > 0f;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => recarga;

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(0.4f, 0.1f, 0.8f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        if (data.specialValue > 0f)       limiarHP  = data.specialValue / 100f;
        if (data.cooldown > 0f)           recarga   = data.cooldown;
        if (data.activationInterval > 0f) distancia = data.activationInterval;
    }

    public override void ApplyEffect() { }

    void OnEnable()  => PlayerStats.OnDanoRecebido += OnDanoRecebido;
    void OnDisable() => PlayerStats.OnDanoRecebido -= OnDanoRecebido;

    void Update() { if (timerRecarga > 0f) timerRecarga -= Time.deltaTime; }

    void OnDanoRecebido()
    {
        if (playerStats == null || timerRecarga > 0f) return;
        float pct = playerStats.health / Mathf.Max(1f, playerStats.maxHealth);
        if (pct > limiarHP) return;
        timerRecarga = recarga;
        StartCoroutine(Teleportar());
    }

    IEnumerator Teleportar()
    {
        SkillElementEffect.AplicarDefensivo(skillData, playerStats, DefensiveTrigger.OnAtivar, null, this);
        Vector2 origem = playerStats.transform.position;
        StartCoroutine(EfeitoSaida(origem));
        playerStats.invulneravel = true;
        var sr = playerStats.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        yield return new WaitForSeconds(0.15f);
        Vector2 nova = EncontrarPosicao(origem);
        playerStats.transform.position = nova;
        yield return null;
        if (sr != null) sr.enabled = true;
        StartCoroutine(EfeitoChegada(nova));

        // FugaCura — cura 20% do HP máximo ao teleportar
        if (SkillEvolutionManager.Tem(SkillEvolutionType.FugaCura) && playerStats != null)
            playerStats.Heal(playerStats.maxHealth * 0.20f);

        // FugaInvulneravel — invulnerável por 2s adicionais após chegada
        float tempoInvul = SkillEvolutionManager.Tem(SkillEvolutionType.FugaInvulneravel) ? 2.0f : 0.4f;
        yield return new WaitForSeconds(tempoInvul);
        playerStats.invulneravel = false;
    }

    Vector2 EncontrarPosicao(Vector2 origem)
    {
        var ge      = GerenciadorEventos.Instance;
        var inimigos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        const float raioSemInimigos = 6f; // distância mínima de qualquer inimigo

        // Tenta encontrar posição sem inimigos próximos
        for (int t = 0; t < 60; t++)
        {
            float ang  = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(distancia * 0.7f, distancia);
            Vector2 c  = origem + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

            if (ge != null && !ge.PosicaoValida(c)) continue;
            if (Physics2D.OverlapCircle(c, 0.5f) != null) continue;

            // Verifica se há inimigos perto
            bool temInimigo = false;
            foreach (var ic in inimigos)
            {
                if (ic == null || ic.estaMorrendo) continue;
                if (Vector2.Distance(c, ic.transform.position) < raioSemInimigos)
                { temInimigo = true; break; }
            }
            if (temInimigo) continue;

            return c;
        }

        // Fallback: aceita qualquer posição válida mesmo com inimigos
        for (int t = 0; t < 20; t++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(distancia * 0.7f, distancia);
            Vector2 c  = origem + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
            if (ge != null && !ge.PosicaoValida(c)) continue;
            return c;
        }

        return origem + (Vector2)Random.insideUnitCircle.normalized * distancia;
    }

    IEnumerator EfeitoSaida(Vector2 pos)
    {
        Color ce = CorElemento();
        for (int i = 0; i < 6; i++)
        {
            var go = new GameObject("F"); go.transform.position = pos + Random.insideUnitCircle * 0.4f;
            var sr2 = go.AddComponent<SpriteRenderer>(); sr2.sprite = GerarDisco(10); sr2.color = new Color(ce.r, ce.g, ce.b, 0.8f); sr2.sortingOrder = 14;
            go.transform.localScale = Vector3.one * Random.Range(0.15f, 0.35f);
            StartCoroutine(FadeMove(sr2, Random.insideUnitCircle * 1.5f + Vector2.up * 0.5f));
        }
        yield return StartCoroutine(AnelFX(pos, true));
    }

    IEnumerator EfeitoChegada(Vector2 pos)
    {
        yield return StartCoroutine(AnelFX(pos, false));
        Color ce = CorElemento();
        for (int i = 0; i < 6; i++)
        {
            float ang = i / 6f * Mathf.PI * 2f;
            var go = new GameObject("F"); go.transform.position = pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.8f;
            var sr2 = go.AddComponent<SpriteRenderer>(); sr2.sprite = GerarDisco(8); sr2.color = ce; sr2.sortingOrder = 14;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
            StartCoroutine(FadeMove(sr2, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(1f, 3f)));
        }
    }

    IEnumerator AnelFX(Vector2 pos, bool contrair)
    {
        const int S = 32; var go = new GameObject("AnelFX"); go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
        float dur = 0.2f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur; float r = contrair ? Mathf.Lerp(1.2f, 0f, p) : Mathf.Lerp(0f, 2.5f, p);
            Color ce = CorElemento();
            lr.startWidth = lr.endWidth = contrair ? Mathf.Lerp(0.15f, 0.02f, p) : Mathf.Lerp(0.22f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(ce.r, ce.g, ce.b, contrair ? Mathf.Lerp(0.9f, 0f, p) : Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < S; i++) { float a = 360f / S * i * Mathf.Deg2Rad; lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator FadeMove(SpriteRenderer sr2, Vector2 vel)
    {
        Color c = sr2.color; float vida = Random.Range(0.3f, 0.6f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel *= Mathf.Pow(0.88f, Time.deltaTime * 60f);
            if (sr2 != null) { sr2.transform.position += (Vector3)(vel * Time.deltaTime); sr2.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, t / vida)); }
            yield return null;
        }
        if (sr2 != null) Destroy(sr2.gameObject);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx)); tex.SetPixel(x, y, new Color(1f, 1f, 1f, d < cx ? 1f : 0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
