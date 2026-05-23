using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RitualAnciaoUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float raio          = 8f;
    public float duracao       = 9f;
    public float cooldown      = 26f;
    public float danoInicial   = 5f;
    public float danoFinal     = 50f;
    public float tempoRampa    = 6f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float       cooldownRestante;
    private bool        ativo;
    private Vector2     centroRitual;
    private PlayerStats playerStats;

    // tempo que cada inimigo passou dentro do pentágono
    readonly Dictionary<InimigoController, float> temposDentro = new Dictionary<InimigoController, float>();

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
        ativo        = true;
        cooldownRestante = cooldown;
        centroRitual = transform.position;
        temposDentro.Clear();

        var ritualGO = CriarPentagono(centroRitual);
        yield return StartCoroutine(AnimarInvocacao(ritualGO));

        float elapsed = 0f;
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;
            AtualizarInimigos();
            AnimarRitual(ritualGO, elapsed);
            yield return null;
        }

        // Explosão final: quanto mais inimigos ainda dentro, maior o dano bônus
        Explodir(ritualGO);
        temposDentro.Clear();
        ativo = false;
        StartCoroutine(FadeOutDestruir(ritualGO, 0.5f));
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    void AtualizarInimigos()
    {
        var dentroPentagonoAgora = new HashSet<InimigoController>();

        foreach (var c in Physics2D.OverlapCircleAll(centroRitual, raio))
        {
            if (c.gameObject == gameObject) continue;
            var root = ResolverInimigo(c.gameObject);
            if (root == null) continue;
            var ic = root.GetComponent<InimigoController>();
            if (ic == null) continue;
            if (!DentroPentagono(root.transform.position, centroRitual, raio)) continue;

            dentroPentagonoAgora.Add(ic);

            if (!temposDentro.ContainsKey(ic))
                temposDentro[ic] = 0f;

            temposDentro[ic] += Time.deltaTime;

            float t    = Mathf.Clamp01(temposDentro[ic] / tempoRampa);
            float dano = Mathf.Lerp(danoInicial, danoFinal, t) * Time.deltaTime;
            ic.ReceberDano(dano, false, false);
        }

        // Remove inimigos que saíram
        var saíram = new List<InimigoController>();
        foreach (var kv in temposDentro)
            if (!dentroPentagonoAgora.Contains(kv.Key)) saíram.Add(kv.Key);
        foreach (var ic in saíram) temposDentro.Remove(ic);
    }

    void Explodir(GameObject ritualGO)
    {
        foreach (var kv in temposDentro)
        {
            if (kv.Key == null) continue;
            float t = Mathf.Clamp01(kv.Value / tempoRampa);
            kv.Key.ReceberDano(Mathf.Lerp(danoInicial, danoFinal, t) * 2f, true);
        }
        StartCoroutine(AnimarExplosaoFinal(centroRitual));
    }

    static bool DentroPentagono(Vector2 ponto, Vector2 centro, float r)
    {
        var v = new Vector2[5];
        for (int i = 0; i < 5; i++)
        {
            float ang = (72f * i - 90f) * Mathf.Deg2Rad;
            v[i] = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
        }
        bool dentro = false;
        int j = 4;
        for (int i = 0; i < 5; i++)
        {
            if ((v[i].y > ponto.y) != (v[j].y > ponto.y) &&
                ponto.x < (v[j].x - v[i].x) * (ponto.y - v[i].y) / (v[j].y - v[i].y) + v[i].x)
                dentro = !dentro;
            j = i;
        }
        return dentro;
    }

    static GameObject ResolverInimigo(GameObject go)
    {
        GameObject root = null;
        var ic = go.GetComponent<InimigoController>() ?? go.GetComponentInParent<InimigoController>();
        if (ic != null) root = ic.gameObject;
        if (root == null) { var mi = go.GetComponent<movi_inimigo>() ?? go.GetComponentInParent<movi_inimigo>(); if (mi != null) root = mi.gameObject; }
        if (root == null) { var bc = go.GetComponent<BossController>() ?? go.GetComponentInParent<BossController>(); if (bc != null) root = bc.gameObject; }
        if (root == null) { var bp = go.GetComponent<BossPrincesa>() ?? go.GetComponentInParent<BossPrincesa>(); if (bp != null) root = bp.gameObject; }
        if (root == null) return null;
        if (root.GetComponent<ProjetilHomingPrincesa>()   != null) return null;
        if (root.GetComponent<ProjetilEspecialPrincesa>() != null) return null;
        return root;
    }

    // ─── VISUAIS ────────────────────────────────────────────────────────────

    GameObject CriarPentagono(Vector2 centro)
    {
        var root = new GameObject("RitualAnciao");
        root.transform.position = centro;

        // Pentágono principal
        var penGO = new GameObject("Pentagono");
        penGO.transform.SetParent(root.transform, false);
        var lr = penGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = 5;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 12;
        lr.startWidth    = lr.endWidth = 0.12f;
        lr.startColor    = lr.endColor = new Color(0.6f, 0.3f, 1f, 0.9f);
        for (int i = 0; i < 5; i++)
        {
            float ang = (72f * i - 90f) * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio));
        }

        // Linhas do centro a cada vértice (estrela)
        for (int i = 0; i < 5; i++)
        {
            float ang  = (72f * i - 90f) * Mathf.Deg2Rad;
            var linhGO = new GameObject($"Raio{i}");
            linhGO.transform.SetParent(root.transform, false);
            var lr2 = linhGO.AddComponent<LineRenderer>();
            lr2.useWorldSpace = false;
            lr2.positionCount = 2;
            lr2.material      = new Material(Shader.Find("Sprites/Default"));
            lr2.sortingOrder  = 11;
            lr2.startWidth    = 0.05f;
            lr2.endWidth      = 0.02f;
            lr2.startColor    = new Color(0.8f, 0.5f, 1f, 0.6f);
            lr2.endColor      = new Color(0.5f, 0.2f, 0.9f, 0.2f);
            lr2.SetPosition(0, Vector3.zero);
            lr2.SetPosition(1, new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio));
        }

        // Runas nos vértices
        for (int i = 0; i < 5; i++)
        {
            float ang = (72f * i - 90f) * Mathf.Deg2Rad;
            var runa  = new GameObject($"Runa{i}");
            runa.transform.SetParent(root.transform, false);
            runa.transform.localPosition = new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio);
            var sr = runa.AddComponent<SpriteRenderer>();
            sr.sprite       = GerarSprite(12, new Color(0.8f, 0.4f, 1f));
            sr.color        = new Color(0.9f, 0.5f, 1f, 0.9f);
            sr.sortingOrder = 14;
            runa.transform.localScale = Vector3.one * 0.22f;
        }

        return root;
    }

    IEnumerator AnimarInvocacao(GameObject root)
    {
        // Pentágono "cresce" do centro
        var lr  = root.GetComponentInChildren<LineRenderer>();
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        var srs = root.GetComponentsInChildren<SpriteRenderer>();

        float dur = 0.6f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var l in lrs) { Color c = l.startColor; c.a = p; l.startColor = l.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color; c.a = p; sr.color = c; }
            yield return null;
        }
    }

    void AnimarRitual(GameObject root, float elapsed)
    {
        float t     = Mathf.Clamp01(elapsed / duracao);
        float pulso = Mathf.Sin(elapsed * (4f + t * 6f)) * 0.5f + 0.5f;

        // Cor vai de roxo para vermelho conforme o dano aumenta
        Color corBase = Color.Lerp(new Color(0.6f, 0.3f, 1f), new Color(1f, 0.1f, 0.1f), t);

        var lrs = root.GetComponentsInChildren<LineRenderer>();
        foreach (var lr in lrs)
        {
            Color c = corBase;
            c.a = 0.5f + pulso * 0.5f;
            lr.startColor = lr.endColor = c;
            lr.startWidth = lr.endWidth = 0.07f + pulso * 0.07f + t * 0.06f;
        }

        // Runas giram e pulsam
        var srs = root.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < srs.Length; i++)
        {
            Color c = Color.Lerp(new Color(0.9f, 0.5f, 1f), new Color(1f, 0.3f, 0.1f), t);
            c.a = 0.7f + pulso * 0.3f;
            srs[i].color = c;
            srs[i].transform.Rotate(0f, 0f, Time.deltaTime * (60f + t * 120f));
            float escala = 0.2f + pulso * 0.06f + t * 0.08f;
            srs[i].transform.localScale = Vector3.one * escala;
        }
    }

    IEnumerator AnimarExplosaoFinal(Vector2 centro)
    {
        const int SEGS = 40;
        var go = new GameObject("ExplosaRitual");
        go.transform.position = centro;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 16;

        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            float p = t / 0.6f;
            float r = Mathf.Lerp(0f, raio * 1.5f, 1f - Mathf.Pow(1f - p, 2f));
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.5f, 0.03f, p);
            lr.startColor = lr.endColor = new Color(1f, Mathf.Lerp(0.3f, 0.1f, p), Mathf.Lerp(0.8f, 0f, p), 1f - p);
            for (int i = 0; i < SEGS; i++)
            {
                float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        Destroy(go);
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
