using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BencaoAnciaoUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float cooldown        = 27f;
    public float duracao         = 6f;
    public float curaPorPulso    = 0.08f; // 8% do HP máximo
    public float reducaoCooldown = 0.5f;  // segundos removidos das skills por pulso
    public float intervaloPulso  = 1f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float       cooldownRestante;
    private bool        ativo;
    private PlayerStats playerStats;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            if (playerStats.ultimateSkill != null)
                playerStats.ultimateSkill.isActive = true;
            playerStats.ultimateCooldown   = cooldown;
            playerStats.ultimateChargeTime = 0f;
            playerStats.ultimateReady      = false;
        }
    }

    void Update()
    {
        if (cooldownRestante > 0f) cooldownRestante -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.R) && cooldownRestante <= 0f && !ativo)
            StartCoroutine(CorotinaAtivacao());
        SincronizarUI();
    }

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    // ─── COROUTINE PRINCIPAL ────────────────────────────────────────────────

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        Vector3 posTotem = transform.position + Vector3.up * 0.5f;
        var totem = CriarTotem(posTotem);

        yield return StartCoroutine(AnimarEntrada(totem));

        // BencaoIntensa: cura por pulso para 15% do HP
        float curaPorPulsoEfetiva = curaPorPulso;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.BencaoIntensa))
            curaPorPulsoEfetiva = 0.15f;

        // BencaoRapida: intervalo reduzido para 0.6s
        float intervaloPulsoEfetivo = intervaloPulso;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.BencaoRapida))
            intervaloPulsoEfetivo = 0.6f;

        int pulsos = Mathf.RoundToInt(duracao / intervaloPulsoEfetivo);
        for (int i = 0; i < pulsos; i++)
        {
            yield return new WaitForSeconds(intervaloPulsoEfetivo);
            AplicarPulsoComCura(posTotem, totem, i, curaPorPulsoEfetiva);
        }

        yield return StartCoroutine(AnimarSaida(totem));
        ativo = false;
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    void AplicarPulso(Vector3 pos, GameObject totem, int indice)
    {
        AplicarPulsoComCura(pos, totem, indice, curaPorPulso);
    }

    void AplicarPulsoComCura(Vector3 pos, GameObject totem, int indice, float curaPct)
    {
        // Cura
        if (playerStats != null)
        {
            float cura = playerStats.maxHealth * curaPct;
            playerStats.health = Mathf.Min(playerStats.health + cura, playerStats.maxHealth);
        }

        // Reduz cooldowns de todas as skills no jogador
        foreach (var skill in GetComponents<SkillBehavior>())
            skill.ReducirCooldown(reducaoCooldown);

        // Visual do pulso (anel saindo do totem)
        StartCoroutine(AnimarPulso(pos, totem));

        // Efeito no próprio jogador ao ser curado
        StartCoroutine(AnimarCuraJogador());
    }

    // ─── VISUAIS ────────────────────────────────────────────────────────────

    static readonly Color corOuro     = new Color(1f,   0.82f, 0.1f,  1f);
    static readonly Color corAmbar    = new Color(1f,   0.55f, 0.05f, 1f);
    static readonly Color corCreme    = new Color(1f,   0.95f, 0.7f,  1f);

    GameObject CriarTotem(Vector3 pos)
    {
        var root = new GameObject("TotemBencao");
        root.transform.position = pos;

        // Haste vertical (dois lados)
        CriarHaste(root, new Vector3(-0.05f, 0f), new Vector3(-0.05f, 2.2f), corAmbar, 0.12f);
        CriarHaste(root, new Vector3( 0.05f, 0f), new Vector3( 0.05f, 2.2f), corAmbar, 0.12f);

        // Travessas horizontais (rúnicas)
        CriarHaste(root, new Vector3(-0.4f, 1.5f), new Vector3(0.4f, 1.5f), corOuro, 0.08f);
        CriarHaste(root, new Vector3(-0.25f, 0.9f), new Vector3(0.25f, 0.9f), corOuro, 0.06f);
        CriarHaste(root, new Vector3(-0.15f, 0.3f), new Vector3(0.15f, 0.3f), corOuro, 0.05f);

        // Orbe no topo
        CriarOrbe(root, new Vector3(0f, 2.5f), 0.32f);

        // Base (arco)
        CriarBase(root);

        // Runas flutuantes ao redor
        for (int i = 0; i < 4; i++)
        {
            float ang = 90f * i * Mathf.Deg2Rad;
            CriarRuna(root, new Vector3(Mathf.Cos(ang) * 0.55f, 0.7f + Mathf.Sin(ang) * 0.3f));
        }

        return root;
    }

    void CriarHaste(GameObject parent, Vector3 de, Vector3 ate, Color cor, float largura)
    {
        var go = new GameObject("Haste");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, de);
        lr.SetPosition(1, ate);
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 12;
        lr.startWidth    = lr.endWidth = largura;
        lr.startColor    = lr.endColor = cor;
    }

    void CriarOrbe(GameObject parent, Vector3 localPos, float raio)
    {
        const int SEGS = 24;
        var go = new GameObject("Orbe");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;

        // Anel externo brilhante
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 14;
        lr.startWidth    = lr.endWidth = 0.07f;
        lr.startColor    = lr.endColor = corOuro;
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * raio, Mathf.Sin(a) * raio));
        }

        // Núcleo sólido (sprite)
        var sprite = new GameObject("Nucleo");
        sprite.transform.SetParent(go.transform, false);
        var sr = sprite.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarSprite(16, corCreme);
        sr.color        = corCreme;
        sr.sortingOrder = 15;
        sprite.transform.localScale = Vector3.one * (raio * 2.2f);
    }

    void CriarBase(GameObject parent)
    {
        const int SEGS = 16;
        var go = new GameObject("Base");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 11;
        lr.startWidth    = lr.endWidth = 0.06f;
        lr.startColor    = lr.endColor = new Color(corAmbar.r, corAmbar.g, corAmbar.b, 0.7f);
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.55f, Mathf.Sin(a) * 0.18f));
        }
    }

    void CriarRuna(GameObject parent, Vector3 localPos)
    {
        var go = new GameObject("Runa");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarSprite(8, corOuro);
        sr.color        = new Color(corOuro.r, corOuro.g, corOuro.b, 0.85f);
        sr.sortingOrder = 13;
        go.transform.localScale = Vector3.one * 0.13f;
    }

    IEnumerator AnimarEntrada(GameObject root)
    {
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        var srs = root.GetComponentsInChildren<SpriteRenderer>();

        // Surge do chão para cima
        Vector3 posAlvo   = root.transform.position;
        Vector3 posInicio = posAlvo + Vector3.down * 2f;

        float dur = 0.5f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            root.transform.position = Vector3.Lerp(posInicio, posAlvo, p);
            foreach (var lr in lrs) { Color c = lr.startColor; c.a *= p; lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color;       c.a  = p; sr.color = c; }
            yield return null;
        }
        root.transform.position = posAlvo;

        StartCoroutine(AnimarIdle(root));
    }

    IEnumerator AnimarIdle(GameObject root)
    {
        float t = 0f;
        Vector3 posBase = root.transform.position;
        while (root != null)
        {
            t += Time.deltaTime;

            // Flutuação suave
            root.transform.position = posBase + Vector3.up * Mathf.Sin(t * 1.8f) * 0.08f;

            // Pulso suave do orbe e das runas
            float pulso = Mathf.Sin(t * 3f) * 0.5f + 0.5f;
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>())
            {
                Color c = sr.color;
                c.a = 0.75f + pulso * 0.25f;
                sr.color = c;
                sr.transform.localScale = Vector3.one * (0.12f + pulso * 0.02f);
            }

            yield return null;
        }
    }

    IEnumerator AnimarPulso(Vector3 centro, GameObject totem)
    {
        // Flash no orbe
        if (totem != null)
        {
            foreach (var sr in totem.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.color = Color.white;
                sr.transform.localScale = Vector3.one * 0.22f;
            }
        }

        // Anel de cura expandindo (dourado → verde)
        const int SEGS = 32;
        var anel = new GameObject("AnelCura");
        anel.transform.position = centro;
        var lr = anel.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 16;

        float dur = 0.7f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            float r = Mathf.Lerp(0.1f, 15f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.18f, 0.03f, p);
            Color cor = Color.Lerp(corOuro, new Color(0.3f, 1f, 0.4f, 1f), p);
            cor.a = 1f - p;
            lr.startColor = lr.endColor = cor;
            for (int i = 0; i < SEGS; i++)
            {
                float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, centro + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.4f));
            }
            yield return null;
        }
        Destroy(anel);

        // Partículas de luz subindo
        StartCoroutine(AnimarParticulasCura(centro));
    }

    IEnumerator AnimarParticulasCura(Vector3 centro)
    {
        var particulas = new List<(GameObject go, Vector2 vel)>();
        for (int i = 0; i < 6; i++)
        {
            var p = new GameObject("ParticulaCura");
            p.transform.position = centro + new Vector3(Random.Range(-1f, 1f), Random.Range(-0.3f, 0.3f));
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite       = GerarSprite(6, new Color(0.5f, 1f, 0.5f));
            sr.color        = new Color(0.5f, 1f, 0.5f, 1f);
            sr.sortingOrder = 17;
            p.transform.localScale = Vector3.one * 0.1f;
            Vector2 vel = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(1.5f, 3f));
            particulas.Add((p, vel));
        }

        float dur = 0.8f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var (go, vel) in particulas)
            {
                if (go == null) continue;
                go.transform.position += (Vector3)(vel * Time.deltaTime);
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) { Color c = sr.color; c.a = 1f - p; sr.color = c; }
            }
            yield return null;
        }

        foreach (var (go, _) in particulas)
            if (go != null) Destroy(go);
    }

    IEnumerator AnimarCuraJogador()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color corOriginal = sr.color;

        // Flash verde → dourado → normal em 0.5s
        float dur = 0.5f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            Color flash;
            if (p < 0.3f)
                flash = Color.Lerp(corOriginal, new Color(0.3f, 1f, 0.4f, 1f), p / 0.3f);
            else
                flash = Color.Lerp(new Color(0.3f, 1f, 0.4f, 1f), corOriginal, (p - 0.3f) / 0.7f);
            sr.color = flash;
            yield return null;
        }
        sr.color = corOriginal;

        // Partículas verdes emanando do player
        StartCoroutine(AnimarParticulasCura(transform.position));

        // Anel de cura pequeno centrado no player
        const int SEGS = 24;
        var anel = new GameObject("AnelCuraPlayer");
        anel.transform.position = transform.position;
        var lr = anel.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 18;

        float durAnel = 0.5f;
        for (float t = 0f; t < durAnel; t += Time.deltaTime)
        {
            float p = t / durAnel;
            float r = Mathf.Lerp(0.1f, 1.8f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.2f, 0.02f, p);
            Color cor = new Color(0.3f, 1f, 0.45f, 1f - p);
            lr.startColor = lr.endColor = cor;
            Vector3 pos = transform.position;
            for (int i = 0; i < SEGS; i++)
            {
                float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.5f));
            }
            yield return null;
        }
        Destroy(anel);
    }

    IEnumerator AnimarSaida(GameObject root)
    {
        StopCoroutine(AnimarIdle(root)); // para o idle

        var lrs = root.GetComponentsInChildren<LineRenderer>();
        var srs = root.GetComponentsInChildren<SpriteRenderer>();
        Vector3 posBase = root.transform.position;
        Vector3 posAlvo = posBase + Vector3.down * 2f;

        float dur = 0.45f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            root.transform.position = Vector3.Lerp(posBase, posAlvo, p);
            foreach (var lr in lrs) { Color c = lr.startColor; c.a = Mathf.Lerp(c.a, 0f, p); lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color;       c.a = Mathf.Lerp(c.a, 0f, p); sr.color = c; }
            yield return null;
        }
        Destroy(root);
    }

    static Sprite GerarSprite(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            float a = Mathf.Clamp01(1f - d / cx);
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
