using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NecropoleUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float raio             = 12f;
    public float duracao          = 10f;
    public float cooldown         = 30f;
    public float duracaoFantasma  = 6f;
    public float danoFantasma     = 20f;
    public float velocidadeFantasma = 3.5f;
    public float intervaloAtaque  = 1.1f;

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

        InimigoController.OnPreMorte += OnInimigoDentroZona;

        var zonaGO = CriarZona();
        yield return StartCoroutine(AnimarEntrada(zonaGO));

        float elapsed = 0f;
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;
            AnimarZona(zonaGO, elapsed);
            zonaGO.transform.position = transform.position;
            yield return null;
        }

        InimigoController.OnPreMorte -= OnInimigoDentroZona;
        ativo = false;
        StartCoroutine(FadeOutDestruir(zonaGO, 0.4f));
    }

    void OnInimigoDentroZona(InimigoController ic)
    {
        if (ic == null || ic.gameObject == null) return;
        float dist = Vector2.Distance(transform.position, ic.transform.position);
        if (dist > raio) return;

        Vector2 pos = ic.transform.position;
        StartCoroutine(SpawnarFantasma(pos));
    }

    // ─── FANTASMA ALIADO ────────────────────────────────────────────────────

    IEnumerator SpawnarFantasma(Vector2 pos)
    {
        var go = new GameObject("FantasmaAliado");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarSprite(18, new Color(0.5f, 0f, 0.8f));
        sr.color        = new Color(0.6f, 0.2f, 1f, 0.75f);
        sr.sortingOrder = 12;
        go.transform.localScale = Vector3.one * 0.55f;

        // Aura do fantasma
        var auraGO = CriarAuraFantasma(go);

        // Entrada: sobe do chão
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / 0.4f;
            go.transform.position = pos + Vector2.up * Mathf.Lerp(-0.5f, 0f, p);
            Color c = sr.color; c.a = Mathf.Lerp(0f, 0.75f, p); sr.color = c;
            yield return null;
        }

        float vida         = duracaoFantasma;
        float timerAtaque  = 0f;
        float tempoBob     = 0f;

        while (vida > 0f && go != null)
        {
            vida       -= Time.deltaTime;
            timerAtaque -= Time.deltaTime;
            tempoBob    += Time.deltaTime;

            // Flutuação suave
            go.transform.position = (Vector2)go.transform.position
                + Vector2.up * Mathf.Sin(tempoBob * 3f) * 0.003f;

            // Move em direção ao inimigo mais próximo
            var alvo = EncontrarInimigoMaisProximo(go.transform.position);
            if (alvo != null)
            {
                Vector2 dir = ((Vector2)alvo.transform.position - (Vector2)go.transform.position).normalized;
                go.transform.position = (Vector2)go.transform.position + dir * velocidadeFantasma * Time.deltaTime;

                // Ataque ao chegar perto
                if (timerAtaque <= 0f && Vector2.Distance(go.transform.position, alvo.transform.position) < 1.2f)
                {
                    timerAtaque = intervaloAtaque;
                    alvo.ReceberDano(danoFantasma, Random.value < 0.15f);
                    StartCoroutine(FlashAtaque(sr));
                }
            }

            // Fade out no fim
            if (vida < 1f)
            {
                Color c = sr.color; c.a = Mathf.Lerp(0f, 0.75f, vida); sr.color = c;
            }

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    IEnumerator FlashAtaque(SpriteRenderer sr)
    {
        if (sr == null) yield break;
        Color orig = sr.color;
        sr.color = new Color(1f, 0.5f, 1f, 1f);
        yield return new WaitForSeconds(0.08f);
        if (sr != null) sr.color = orig;
    }

    InimigoController EncontrarInimigoMaisProximo(Vector3 pos)
    {
        InimigoController melhor = null;
        float distMin = float.MaxValue;
        foreach (var ic in FindObjectsByType<InimigoController>(FindObjectsSortMode.None))
        {
            if (ic == null || ic.gameObject == null) continue;
            if (!ic.enabled) continue; // ignora projéteis em órbita da Princesa
            if (ic.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) continue;
            if (ic.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) continue;
            float d = Vector3.Distance(pos, ic.transform.position);
            if (d < distMin) { distMin = d; melhor = ic; }
        }
        return melhor;
    }

    // ─── VISUAIS DA ZONA ────────────────────────────────────────────────────

    GameObject CriarZona()
    {
        var root = new GameObject("ZonaNecropole");
        root.transform.position = transform.position;

        // Anel externo
        CriarAnel(root, raio,       new Color(0.3f, 0f, 0.5f, 0.8f), 0.1f,  "AnelExterno", 48);
        CriarAnel(root, raio * 0.6f, new Color(0.4f, 0f, 0.6f, 0.5f), 0.07f, "AnelMedio",   32);

        // Partículas flutuantes (caveiras/orbes)
        for (int i = 0; i < 6; i++)
        {
            var p  = new GameObject($"Orbe{i}");
            p.transform.SetParent(root.transform, false);
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite       = GerarSprite(8, new Color(0.5f, 0f, 0.8f));
            sr.color        = new Color(0.6f, 0.1f, 1f, 0.8f);
            sr.sortingOrder = 13;
            p.transform.localScale = Vector3.one * 0.14f;
            float ang = (360f / 6f) * i * Mathf.Deg2Rad;
            p.transform.localPosition = new Vector3(Mathf.Cos(ang) * raio * 0.8f,
                                                    Mathf.Sin(ang) * raio * 0.8f);
        }

        return root;
    }

    void CriarAnel(GameObject parent, float r, Color cor, float largura, string nome, int segs)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = segs;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 11;
        lr.startWidth    = lr.endWidth = largura;
        lr.startColor    = lr.endColor = cor;
        for (int i = 0; i < segs; i++)
        {
            float a = (360f / segs) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
    }

    GameObject CriarAuraFantasma(GameObject parent)
    {
        const int SEGS = 16;
        var go = new GameObject("AuraFantasma");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 11;
        lr.startWidth    = lr.endWidth = 0.05f;
        lr.startColor    = lr.endColor = new Color(0.6f, 0.1f, 1f, 0.5f);
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(a) * 0.35f));
        }
        return go;
    }

    IEnumerator AnimarEntrada(GameObject root)
    {
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        var srs = root.GetComponentsInChildren<SpriteRenderer>();
        float dur = 0.3f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var lr in lrs) { Color c = lr.startColor; c.a = p * 0.8f; lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color; c.a = p * 0.8f; sr.color = c; }
            yield return null;
        }
    }

    void AnimarZona(GameObject root, float elapsed)
    {
        float pulso = Mathf.Sin(elapsed * 3f) * 0.5f + 0.5f;
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        foreach (var lr in lrs)
        {
            Color c = lr.startColor;
            c.a = 0.4f + pulso * 0.4f;
            lr.startColor = lr.endColor = c;
        }

        // Orbes giram
        var srs = root.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < srs.Length; i++)
        {
            float baseAng = (360f / srs.Length) * i;
            float ang     = (baseAng + elapsed * 40f) * Mathf.Deg2Rad;
            float r       = raio * 0.8f + Mathf.Sin(elapsed * 2f + i) * raio * 0.05f;
            srs[i].transform.localPosition = new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
            Color c = srs[i].color; c.a = 0.5f + pulso * 0.3f; srs[i].color = c;
        }
    }

    IEnumerator FadeOutDestruir(GameObject go, float dur)
    {
        var lrs = go.GetComponentsInChildren<LineRenderer>();
        var srs = go.GetComponentsInChildren<SpriteRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var lr in lrs) { Color c = lr.startColor; c.a = Mathf.Lerp(c.a, 0f, p); lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color; c.a = Mathf.Lerp(c.a, 0f, p); sr.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
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
