using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MisseisTeleguiadosSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float baseDano      = 12f;
    float multiplicador = 0.5f;
    float intervalo     = 6f;
    int   qtdMisseis    = 3;
    float velocidade    = 10f;
    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervalo;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(1f, 0.5f, 0.1f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano   = data.attackBonus > 0f        ? data.attackBonus        : 12f;
        intervalo  = data.activationInterval > 0f ? data.activationInterval : 6f;
        qtdMisseis = data.projectileCount > 0     ? data.projectileCount    : 3;
        velocidade = data.projectileSpeed > 0f    ? data.projectileSpeed    : 10f;
        timer      = intervalo;
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
        int qtdReal = SkillEvolutionManager.Tem(SkillEvolutionType.SalvaMisseis) ? qtdMisseis + 2 : qtdMisseis;
        var alvos  = EncontrarAlvos(qtdReal);
        Vector2 origem = playerStats.transform.position;

        // Efeito de carregamento antes de disparar
        StartCoroutine(EfeitoCarga(origem, qtdReal));
        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < qtdReal; i++)
        {
            var alvo = i < alvos.Count ? alvos[i] : null;
            var go = new GameObject("Missil");
            // Lança em espiral ao redor do player
            float angSpiral = (360f / qtdReal * i) * Mathf.Deg2Rad;
            go.transform.position = origem + new Vector2(Mathf.Cos(angSpiral), Mathf.Sin(angSpiral)) * 0.5f;
            var mp = go.AddComponent<MissilProjetil>();
            mp.skillDataRef = skillData;
            mp.Iniciar(alvo, velocidade, DanoAtual);
            yield return new WaitForSeconds(0.12f);
        }
    }

    IEnumerator EfeitoCarga(Vector2 pos, int qtd)
    {
        // Partículas convergindo para o player antes do disparo
        for (int i = 0; i < qtd * 3; i++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(1f, 2.5f);
            Vector2 origem2 = pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
            var p = new GameObject("PCarga");
            p.transform.position = origem2;
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(6); psr.color = new Color(1f, 0.5f, 0.1f, 0.8f);
            psr.sortingOrder = 12;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar((pos - origem2).normalized * Random.Range(3f, 6f), 0.2f);
            Destroy(p, 0.4f);
            yield return new WaitForSeconds(0.03f);
        }
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
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
    public SkillData skillDataRef;
    InimigoController alvo;
    Vector2 dir;
    float   vel, dano;
    bool    atingiu;
    float   elapsed;
    SpriteRenderer sr;

    public void Iniciar(InimigoController a, float v, float d)
    {
        alvo = a; vel = v; dano = d;
        dir  = alvo != null
            ? ((Vector2)alvo.transform.position - (Vector2)transform.position).normalized
            : Random.insideUnitCircle.normalized;

        // Sprite do míssil — formato de foguete
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GerarMissil(); sr.color = new Color(1f, 0.5f, 0.1f); sr.sortingOrder = 14;
        transform.localScale = new Vector3(0.35f, 0.55f, 1f);

        // Brilho interno
        var glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(transform, false);
        var glowSR = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite = GerarDisco(8); glowSR.color = new Color(1f, 0.9f, 0.5f, 0.5f); glowSR.sortingOrder = 15;
        glowGO.transform.localScale = new Vector3(1.2f, 0.6f, 1f);

        var col = gameObject.AddComponent<CapsuleCollider2D>();
        col.isTrigger = true; col.size = new Vector2(0.3f, 0.5f);
        Destroy(gameObject, 5f);
        StartCoroutine(Trail());
        StartCoroutine(PulsoCor());
    }

    void Update()
    {
        if (atingiu) return;
        elapsed += Time.deltaTime;

        // Homing mais agressivo conforme se aproxima
        if (alvo != null && !alvo.estaMorrendo)
        {
            float dist = Vector2.Distance(transform.position, alvo.transform.position);
            float fatorHoming = Mathf.Lerp(3f, 12f, 1f - Mathf.Clamp01(dist / 8f));
            dir = Vector2.Lerp(dir, ((Vector2)alvo.transform.position - (Vector2)transform.position).normalized, Time.deltaTime * fatorHoming).normalized;
        }

        transform.position += (Vector3)(dir * vel * Time.deltaTime);
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (atingiu) return;
        var ic = other.GetComponent<InimigoController>() ?? other.GetComponentInParent<InimigoController>();
        if (ic == null) return;
        atingiu = true;
        ic.ReceberDano(dano, false);
        SkillElementEffect.Aplicar(skillDataRef, ic.gameObject, dano, this);
        if (SkillEvolutionManager.Tem(SkillEvolutionType.MisseisExplosivos))
            EvolutionFX.SpawnExplosao(transform.position, 2f, dano * 0.6f, new Color(1f, 0.5f, 0.1f), this);
        StartCoroutine(EfeitoImpacto());
    }

    IEnumerator PulsoCor()
    {
        while (!atingiu && gameObject != null)
        {
            // elapsed já é acumulado em Update(); não incrementar aqui para evitar acúmulo duplo
            float p = Mathf.Sin(elapsed * 12f) * 0.5f + 0.5f;
            if (sr != null) sr.color = new Color(1f, Mathf.Lerp(0.3f, 0.7f, p), 0.05f);
            yield return null;
        }
    }

    IEnumerator EfeitoImpacto()
    {
        // Anel de impacto laranja
        const int S = 32;
        var go = new GameObject("AnelMissil");
        go.transform.position = transform.position;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
        Destroy(go, 0.5f);
        Vector2 pos = transform.position;

        // Partículas de impacto
        for (int i = 0; i < 8; i++)
        {
            var p = new GameObject("PImpacto"); p.transform.position = pos;
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(6);
            psr.color = i < 4 ? new Color(1f, 0.5f, 0.1f) : new Color(1f, 0.9f, 0.3f);
            psr.sortingOrder = 15;
            p.transform.localScale = Vector3.one * Random.Range(0.12f, 0.28f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(Random.insideUnitCircle.normalized * Random.Range(2f, 5f), 0.35f);
            Destroy(p, 0.6f);
        }

        float dur = 0.3f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float prog = t / dur; float r = Mathf.Lerp(0.1f, 1.5f, prog);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.2f, 0.02f, prog);
            lr.startColor = lr.endColor = new Color(1f, 0.6f, 0.1f, Mathf.Lerp(1f, 0f, prog));
            for (int i = 0; i < S; i++) { float a = 360f / S * i * Mathf.Deg2Rad; lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            yield return null;
        }
        if (go != null) Destroy(go);
        Destroy(gameObject);
    }

    IEnumerator Trail()
    {
        while (!atingiu && gameObject != null)
        {
            // Trail principal laranja
            var t = new GameObject("T");
            t.transform.position = transform.position;
            t.transform.rotation = transform.rotation;
            t.transform.localScale = transform.localScale;
            var tsr = t.AddComponent<SpriteRenderer>();
            tsr.sprite = GerarMissil(); tsr.color = new Color(1f, 0.4f, 0.05f, 0.45f); tsr.sortingOrder = 13;
            t.AddComponent<AutoDestroyFade>().Iniciar(0.08f);

            // Chama de fogo menor
            var t2 = new GameObject("Chama");
            t2.transform.position = (Vector2)transform.position - dir * 0.18f;
            var t2sr = t2.AddComponent<SpriteRenderer>();
            t2sr.sprite = GerarDisco(6);
            t2sr.color = new Color(1f, 0.8f, 0.2f, 0.6f); t2sr.sortingOrder = 12;
            t2.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            t2.AddComponent<AutoDestroyFadeMove>().Iniciar(-dir * Random.Range(0.5f, 1.5f) + (Vector2)Random.insideUnitCircle * 0.5f, 0.12f);
            Destroy(t2, 0.25f);

            yield return new WaitForSeconds(0.04f);
        }
    }

    static Sprite GerarMissil()
    {
        int w = 8, h = 20;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear;
        float cx = w * 0.5f;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx; float ny = y / (float)(h - 1);
            // Corpo: oval
            float corpo = ny > 0.2f && ny < 0.85f ? (1f - nx) : 0f;
            // Ponta: triangulo no topo
            float ponta = ny >= 0.85f ? (1f - nx) * (1f - ny) * 7f : 0f;
            // Base: arredondada
            float base_ = ny <= 0.2f ? (1f - nx) * (ny / 0.2f) : 0f;
            float a = Mathf.Clamp01(corpo + ponta + base_ + Mathf.Max(0f, 0.6f - nx * 5f));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.1f), w);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }
}
