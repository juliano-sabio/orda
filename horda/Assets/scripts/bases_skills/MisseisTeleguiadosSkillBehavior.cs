using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MisseisTeleguiadosSkillBehavior : SkillBehavior
{
    float baseDano      = 12f;
    float multiplicador = 0.5f;
    float intervalo     = 6f;
    int   qtdMisseis    = 3;
    float velocidade    = 10f;
    float timer;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano    = data.attackBonus > 0f          ? data.attackBonus        : 25f;
        intervalo   = data.activationInterval > 0f   ? data.activationInterval : 3f;
        qtdMisseis  = data.projectileCount > 0       ? data.projectileCount    : 3;
        velocidade  = data.projectileSpeed > 0f      ? data.projectileSpeed    : 10f;
        timer       = intervalo;
    }

    static readonly Color COR_ORIG = new Color(1f, 0.5f, 0.1f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; StartCoroutine(LancarMisseis()); }
    }

    public override void ApplyEffect() => StartCoroutine(LancarMisseis());

    IEnumerator LancarMisseis()
    {
        var alvos = EncontrarAlvos(qtdMisseis);
        Vector2 origem = playerStats.transform.position;
        Color corMissil = CorElemento();

        for (int i = 0; i < qtdMisseis; i++)
        {
            var alvo = i < alvos.Count ? alvos[i] : null;
            var go = new GameObject("Missil");
            go.transform.position = origem + Random.insideUnitCircle * 0.3f;
            go.AddComponent<MissilProjetil>().Iniciar(alvo, velocidade, DanoAtual, corMissil);
            yield return new WaitForSeconds(0.18f);
        }
    }

    List<InimigoController> EncontrarAlvos(int qtd)
    {
        var todos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        var lista = new List<InimigoController>(todos);
        Vector2 orig = playerStats.transform.position;
        lista.RemoveAll(ic => ic.estaMorrendo);
        lista.Sort((a, b) => Vector2.Distance(a.transform.position, orig).CompareTo(Vector2.Distance(b.transform.position, orig)));
        if (lista.Count > qtd) lista.RemoveRange(qtd, lista.Count - qtd);
        return lista;
    }
}

public class MissilProjetil : MonoBehaviour
{
    InimigoController alvo;
    Vector2 dir;
    float   vel, dano;
    bool    atingiu;
    SpriteRenderer sr;
    Color corBase = new Color(1f, 0.5f, 0.1f);

    public void Iniciar(InimigoController a, float v, float d, Color cor = default)
    {
        alvo = a; vel = v; dano = d;
        if (cor != default && cor != Color.white) corBase = cor;
        dir  = alvo != null ? ((Vector2)alvo.transform.position - (Vector2)transform.position).normalized : Random.insideUnitCircle.normalized;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(8); sr.color = corBase; sr.sortingOrder = 14;
        transform.localScale = Vector3.one * 0.3f;

        var col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true; col.radius = 0.2f;
        Destroy(gameObject, 5f);
        StartCoroutine(Trail());
    }

    void Update()
    {
        if (atingiu) return;
        if (alvo != null && !alvo.estaMorrendo)
            dir = Vector2.Lerp(dir, ((Vector2)alvo.transform.position - (Vector2)transform.position).normalized, Time.deltaTime * 5f).normalized;
        transform.position += (Vector3)(dir * vel * Time.deltaTime);
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (atingiu) return;
        var ic = other.GetComponent<InimigoController>() ?? other.GetComponentInParent<InimigoController>();
        if (ic == null) return;
        atingiu = true;
        ic.ReceberDano(dano, false);
        SkillElementEffect.Aplicar(null, ic.gameObject, dano, this);
        SpawnImpacto();
        Destroy(gameObject);
    }

    IEnumerator Trail()
    {
        while (!atingiu && gameObject != null)
        {
            var t = new GameObject("T"); t.transform.position = transform.position;
            var tsr = t.AddComponent<SpriteRenderer>(); tsr.sprite = GerarDisco(6); tsr.color = new Color(corBase.r, corBase.g * 0.85f, corBase.b, 0.5f); tsr.sortingOrder = 13;
            t.transform.localScale = Vector3.one * 0.2f;
            t.AddComponent<AutoDestroyFade>().Iniciar(0.1f);
            yield return new WaitForSeconds(0.04f);
        }
    }

    void SpawnImpacto()
    {
        for (int i = 0; i < 5; i++)
        {
            var p = new GameObject("P"); p.transform.position = transform.position;
            var psr = p.AddComponent<SpriteRenderer>(); psr.sprite = GerarDisco(6); psr.color = corBase; psr.sortingOrder = 15;
            p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(Random.insideUnitCircle.normalized * Random.Range(1.5f, 4f), 0.25f);
        }
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx)); tex.SetPixel(x, y, new Color(1f, 1f, 1f, d < cx ? 1f : 0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
