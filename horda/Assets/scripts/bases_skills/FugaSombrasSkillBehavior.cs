using System.Collections;
using UnityEngine;

public class FugaSombrasSkillBehavior : SkillBehavior
{
    float limiarHP   = 0.50f; // ativa quando HP cai abaixo de 50%
    float recarga    = 360f;  // 6 minutos
    float distancia  = 12f;

    float timerRecarga = 0f;

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        if (data.specialValue > 0f)       limiarHP  = data.specialValue / 100f;
        if (data.cooldown > 0f)           recarga   = data.cooldown;
        if (data.activationInterval > 0f) distancia = data.activationInterval;
    }

    public override void ApplyEffect() { }

    void OnEnable()
    {
        PlayerStats.OnDanoRecebido += OnDanoRecebido;
    }

    void OnDisable()
    {
        PlayerStats.OnDanoRecebido -= OnDanoRecebido;
    }

    void Update()
    {
        if (timerRecarga > 0f) timerRecarga -= Time.deltaTime;
    }

    void OnDanoRecebido()
    {
        if (playerStats == null || timerRecarga > 0f) return;

        float pctHP = playerStats.health / Mathf.Max(1f, playerStats.maxHealth);
        if (pctHP > limiarHP) return; // só ativa abaixo de 50% de HP

        timerRecarga = recarga;
        StartCoroutine(Teleportar());
    }

    IEnumerator Teleportar()
    {
        Vector2 posOrigem = playerStats.transform.position;

        // Efeito na posição original
        StartCoroutine(EfeitoSaida(posOrigem));

        // Invulnerabilidade breve
        playerStats.invulneravel = true;

        // Esconde o player
        var sr = playerStats.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        yield return new WaitForSeconds(0.15f);

        // Encontra posição segura
        Vector2 novaPos = EncontrarPosicaoSegura(posOrigem);
        playerStats.transform.position = novaPos;

        yield return null;

        // Reaparece
        if (sr != null) sr.enabled = true;
        StartCoroutine(EfeitoChegada(novaPos));

        yield return new WaitForSeconds(0.4f);
        playerStats.invulneravel = false;
    }

    Vector2 EncontrarPosicaoSegura(Vector2 origem)
    {
        var ge = GerenciadorEventos.Instance;

        for (int t = 0; t < 30; t++)
        {
            float ang  = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(distancia * 0.5f, distancia);
            Vector2 candidato = origem + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

            if (ge != null && !ge.PosicaoValida(candidato)) continue;

            // Verifica se não há obstáculos
            if (Physics2D.OverlapCircle(candidato, 0.5f) != null) continue;

            return candidato;
        }

        // Fallback: posição oposta simples
        return origem + (Vector2)Random.insideUnitCircle.normalized * distancia;
    }

    // ── Visuais ───────────────────────────────────────────────────────────────

    IEnumerator EfeitoSaida(Vector2 pos)
    {
        // Fumaça sombria na posição original
        for (int i = 0; i < 8; i++)
        {
            var go = new GameObject("FumacaSaida");
            go.transform.position = pos + Random.insideUnitCircle * 0.4f;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = GerarDisco(12);
            sr2.color  = new Color(0.15f, 0.05f, 0.3f, 0.8f);
            sr2.sortingOrder = 15;
            go.transform.localScale = Vector3.one * Random.Range(0.2f, 0.45f);
            Vector2 vel = Random.insideUnitCircle * Random.Range(0.5f, 2f) + Vector2.up * 0.5f;
            StartCoroutine(AnimarFumaca(sr2, vel, new Color(0.15f, 0.05f, 0.3f)));
        }

        // Anel sombrio contraindo
        yield return StartCoroutine(AnelContraindo(pos));
    }

    IEnumerator EfeitoChegada(Vector2 pos)
    {
        // Anel expansivo roxo
        yield return StartCoroutine(AnelExpansivo(pos));

        // Partículas aparecendo
        for (int i = 0; i < 8; i++)
        {
            float ang = i / 8f * Mathf.PI * 2f;
            var go = new GameObject("PartChegada");
            go.transform.position = pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.8f;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = GerarDisco(8);
            sr2.color  = new Color(0.5f, 0.1f, 0.9f);
            sr2.sortingOrder = 14;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(1f, 3f);
            StartCoroutine(AnimarFumaca(sr2, vel, new Color(0.5f, 0.1f, 0.9f)));
        }
    }

    IEnumerator AnelContraindo(Vector2 pos)
    {
        const int SEGS = 32;
        var go = new GameObject("AnelContraindo");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;

        float dur = 0.2f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p    = t / dur;
            float raio = Mathf.Lerp(1.2f, 0f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.15f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(0.4f, 0.1f, 0.8f, Mathf.Lerp(0.9f, 0f, p));
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator AnelExpansivo(Vector2 pos)
    {
        const int SEGS = 40;
        var go = new GameObject("AnelChegada");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 12;

        float dur = 0.4f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p    = t / dur;
            float raio = Mathf.Lerp(0.1f, 2.5f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.22f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(0.5f, 0.1f, 0.9f, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator AnimarFumaca(SpriteRenderer sr2, Vector2 vel, Color cor)
    {
        float vida = Random.Range(0.3f, 0.6f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel *= Mathf.Pow(0.88f, Time.deltaTime * 60f);
            if (sr2 != null)
            {
                sr2.transform.position += (Vector3)(vel * Time.deltaTime);
                sr2.transform.localScale *= 1f + Time.deltaTime * 1.2f;
                sr2.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(0.8f, 0f, t / vida));
            }
            yield return null;
        }
        if (sr2 != null) Destroy(sr2.gameObject);
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
