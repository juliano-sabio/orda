using System.Collections;
using UnityEngine;

public class LancaLuzSkillBehavior : SkillBehavior
{
    float baseDano      = 30f;
    float multiplicador = 1.2f;
    float intervalo     = 8f;
    float velocidade    = 20f;
    float alcance       = 12f;
    float timer;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    public void ConfigurarDeSkillData(SkillData data)
    {
        baseDano   = data.attackBonus > 0f          ? data.attackBonus        : 60f;
        intervalo  = data.activationInterval > 0f   ? data.activationInterval : 4f;
        velocidade = data.projectileSpeed > 0f      ? data.projectileSpeed    : 20f;
        alcance    = data.specialValue > 0f         ? data.specialValue       : 12f;
        timer      = intervalo;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; Disparar(); }
    }

    public override void ApplyEffect() => Disparar();

    void Disparar()
    {
        var alvo = EncontrarMaisProximo();
        Vector2 origem = playerStats.transform.position;
        Vector2 dir = alvo != null
            ? ((Vector2)alvo.transform.position - origem).normalized
            : Vector2.up;

        var go = new GameObject("LancaLuz");
        go.transform.position = origem;
        go.AddComponent<LancaLuzProjetil>().Iniciar(dir, velocidade, DanoAtual, alcance);
    }

    InimigoController EncontrarMaisProximo()
    {
        var todos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        InimigoController melhor = null;
        float menorDist = float.MaxValue;
        Vector2 orig = playerStats.transform.position;
        foreach (var ic in todos)
        {
            if (ic.estaMorrendo) continue;
            float d = Vector2.Distance(ic.transform.position, orig);
            if (d < menorDist) { menorDist = d; melhor = ic; }
        }
        return melhor;
    }
}

public class LancaLuzProjetil : MonoBehaviour
{
    Vector2 dir, origem;
    float   vel, dano, alcance;
    bool    atingiu;
    SpriteRenderer sr;

    public void Iniciar(Vector2 d, float v, float dmg, float alc)
    {
        dir = d; vel = v; dano = dmg; alcance = alc; origem = transform.position;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GerarLanca(); sr.color = new Color(1f, 0.95f, 0.5f); sr.sortingOrder = 14;
        transform.localScale = new Vector3(0.5f, 1.2f, 1f);
        var col = gameObject.AddComponent<CapsuleCollider2D>();
        col.isTrigger = true; col.size = new Vector2(0.25f, 1.2f);
        Destroy(gameObject, 3f);
        StartCoroutine(Trail());
    }

    void Update()
    {
        if (atingiu) return;
        transform.position += (Vector3)(dir * vel * Time.deltaTime);
        if (Vector2.Distance(transform.position, origem) >= alcance) { SpawnImpacto(); Destroy(gameObject); }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (atingiu) return;
        var ic = other.GetComponent<InimigoController>() ?? other.GetComponentInParent<InimigoController>();
        if (ic == null) return;
        ic.ReceberDano(dano, false);
        atingiu = true;
        SpawnImpacto();
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        for (float t = 0f; t < 0.15f; t += Time.deltaTime)
        {
            if (sr == null) yield break;
            sr.color = new Color(1f, 0.95f, 0.5f, 1f - t / 0.15f);
            yield return null;
        }
        Destroy(gameObject);
    }

    IEnumerator Trail()
    {
        while (!atingiu && gameObject != null)
        {
            var t = new GameObject("T"); t.transform.position = transform.position; t.transform.rotation = transform.rotation; t.transform.localScale = transform.localScale;
            var tsr = t.AddComponent<SpriteRenderer>(); tsr.sprite = GerarLanca(); tsr.color = new Color(1f, 0.9f, 0.3f, 0.35f); tsr.sortingOrder = 13;
            t.AddComponent<AutoDestroyFade>().Iniciar(0.12f);
            yield return new WaitForSeconds(0.05f);
        }
    }

    void SpawnImpacto()
    {
        for (int i = 0; i < 8; i++)
        {
            var p = new GameObject("P"); p.transform.position = transform.position;
            var psr = p.AddComponent<SpriteRenderer>(); psr.sprite = GerarDisco(6); psr.color = new Color(1f, 0.9f, 0.3f); psr.sortingOrder = 15;
            p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(Random.insideUnitCircle.normalized * Random.Range(2f, 6f), 0.35f);
        }
    }

    static Sprite GerarLanca()
    {
        int w = 8, h = 28; var tex = new Texture2D(w, h, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear;
        float cx = w * 0.5f;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx; float ny = y / (float)(h - 1);
            float larg = ny < 0.8f ? (1f - nx) * (1f - ny * 0.3f) : (1f - nx) * (1f - ny) * 5f;
            float a = Mathf.Clamp01(larg + Mathf.Max(0f, 0.7f - nx * 5f));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), w);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx)); tex.SetPixel(x, y, new Color(1f, 1f, 1f, d < cx ? 1f : 0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
