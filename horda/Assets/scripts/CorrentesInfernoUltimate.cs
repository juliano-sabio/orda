using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorrentesInfernoUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float raio           = 10f;
    public float duracao        = 6f;
    public float cooldown       = 24f;
    public float danoPorSegundo = 15f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float       cooldownRestante;
    private bool        ativo;
    private PlayerStats playerStats;

    readonly List<EstadoAcorrentado> acorrentados = new List<EstadoAcorrentado>();

    struct EstadoAcorrentado
    {
        public InimigoController ic;
        public GameObject        go;
        public MonoBehaviour[]   scripts;
        public Rigidbody2D       rb;
        public GameObject        correnteGO;
    }

    static readonly float[] Angulos8 = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

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

    void FixedUpdate()
    {
        if (!ativo || acorrentados.Count == 0) return;
        foreach (var e in acorrentados)
        {
            if (e.rb == null || e.go == null) continue;
            Vector2 dir = (Vector2)transform.position - (Vector2)e.go.transform.position;
            e.rb.linearVelocity = dir.magnitude > 0.3f ? dir.normalized * 1.5f : Vector2.zero;
        }
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

        yield return StartCoroutine(AnimarDisparo());

        AcorrentarInimigos();

        float elapsed = 0f;
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;
            AplicarDano();
            AtualizarVisuais(elapsed);
            yield return null;
        }

        yield return StartCoroutine(LiberarTodos());
        ativo = false;
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    void AcorrentarInimigos()
    {
        var candidatos = Physics2D.OverlapCircleAll(transform.position, raio);

        foreach (float ang in Angulos8)
        {
            Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));

            InimigoController melhor   = null;
            float             menorDist = float.MaxValue;

            foreach (var col in candidatos)
            {
                if (col.gameObject == gameObject) continue;
                var root = ResolverRaiz(col.gameObject);
                if (root == null) continue;
                var ic = root.GetComponent<InimigoController>();
                if (ic == null || JaAcorrentado(ic)) continue;

                Vector2 toEnemy = (Vector2)root.transform.position - (Vector2)transform.position;
                float dist = toEnemy.magnitude;
                float dot  = Vector2.Dot(dir, toEnemy.normalized);

                if (dot > 0.6f && dist < menorDist)
                {
                    menorDist = dist;
                    melhor    = ic;
                }
            }

            if (melhor != null)
                Acorrentar(melhor, ang);
        }
    }

    void Acorrentar(InimigoController ic, float angDir)
    {
        var go      = ic.gameObject;
        var rb      = go.GetComponent<Rigidbody2D>() ?? go.GetComponentInParent<Rigidbody2D>();
        var scripts = new List<MonoBehaviour>();

        var mi = go.GetComponent<movi_inimigo>();
        if (mi != null) { mi.enabled = false; scripts.Add(mi); }

        var corrGO = CriarCorrenteVisual();

        acorrentados.Add(new EstadoAcorrentado
        {
            ic         = ic,
            go         = go,
            scripts    = scripts.ToArray(),
            rb         = rb,
            correnteGO = corrGO
        });
    }

    void AplicarDano()
    {
        var mortos = new List<EstadoAcorrentado>();
        foreach (var e in acorrentados)
        {
            if (e.ic == null || e.go == null) { mortos.Add(e); continue; }
            e.ic.ReceberDano(danoPorSegundo * Time.deltaTime, false, false);
        }
        foreach (var m in mortos)
        {
            if (m.correnteGO != null) Destroy(m.correnteGO);
            acorrentados.Remove(m);
        }
    }

    bool JaAcorrentado(InimigoController ic)
    {
        foreach (var e in acorrentados)
            if (e.ic == ic) return true;
        return false;
    }

    IEnumerator LiberarTodos()
    {
        float dur = 0.4f;
        var lrs = new List<LineRenderer>();
        foreach (var e in acorrentados)
        {
            if (e.correnteGO != null)
            {
                var lr = e.correnteGO.GetComponent<LineRenderer>();
                if (lr != null) lrs.Add(lr);
            }
            if (e.scripts != null)
                foreach (var s in e.scripts) if (s != null) s.enabled = true;
        }

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var lr in lrs) { Color col = lr.startColor; col.a = 1f - p; lr.startColor = lr.endColor = col; }
            yield return null;
        }

        foreach (var e in acorrentados)
            if (e.correnteGO != null) Destroy(e.correnteGO);
        acorrentados.Clear();
    }

    // ─── VISUAIS ────────────────────────────────────────────────────────────

    void AtualizarVisuais(float elapsed)
    {
        float flicker = Mathf.Sin(elapsed * 20f) * 0.2f + 0.8f;
        float t = Mathf.Clamp01(elapsed / duracao);
        Color corBase = Color.Lerp(new Color(1f, 0.5f, 0.05f), new Color(1f, 0.1f, 0f), t);

        foreach (var e in acorrentados)
        {
            if (e.go == null || e.correnteGO == null) continue;
            var lr = e.correnteGO.GetComponent<LineRenderer>();
            if (lr == null) continue;

            Vector2 origem  = transform.position;
            Vector2 destino = e.go.transform.position;
            int segs = lr.positionCount;

            for (int i = 0; i < segs; i++)
            {
                float p   = (float)i / (segs - 1);
                Vector2 pos = Vector2.Lerp(origem, destino, p);
                float sag = Mathf.Sin(p * Mathf.PI) * 0.35f;
                if (i > 0 && i < segs - 1)
                    pos += (Vector2)(UnityEngine.Random.insideUnitCircle * 0.08f);
                pos.y -= sag;
                lr.SetPosition(i, pos);
            }

            Color c = corBase;
            c.a = (0.7f + flicker * 0.3f);
            lr.startColor = lr.endColor = c;
            lr.startWidth = lr.endWidth = 0.10f + flicker * 0.06f;
        }
    }

    GameObject CriarCorrenteVisual()
    {
        var go = new GameObject("CorrenteInferno");
        go.transform.position = transform.position;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 10;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;
        lr.startWidth    = 0.14f;
        lr.endWidth      = 0.07f;
        lr.startColor    = new Color(1f, 0.5f, 0.05f, 1f);
        lr.endColor      = new Color(1f, 0.1f, 0f, 0.8f);
        return go;
    }

    IEnumerator AnimarDisparo()
    {
        // 8 raios de fogo expandindo
        var raios = new GameObject[8];
        for (int i = 0; i < 8; i++)
        {
            float ang = Angulos8[i] * Mathf.Deg2Rad;
            var go = new GameObject($"RaioCorr{i}");
            go.transform.position = transform.position;
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder  = 16;
            lr.startWidth    = 0.22f;
            lr.endWidth      = 0.04f;
            lr.startColor    = new Color(1f, 0.7f, 0.1f, 1f);
            lr.endColor      = new Color(1f, 0.2f, 0f, 0f);
            raios[i]         = go;
        }

        float dur = 0.3f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            float r = Mathf.Lerp(0f, raio, p);
            for (int i = 0; i < 8; i++)
            {
                if (raios[i] == null) continue;
                float ang = Angulos8[i] * Mathf.Deg2Rad;
                var lr = raios[i].GetComponent<LineRenderer>();
                lr.SetPosition(0, transform.position);
                lr.SetPosition(1, (Vector2)transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
                Color c = lr.startColor; c.a = 1f - p * 0.5f; lr.startColor = c;
            }
            yield return null;
        }

        foreach (var r in raios) if (r != null) Destroy(r);
    }

    static GameObject ResolverRaiz(GameObject go)
    {
        if (go.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) return null;
        if (go.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) return null;

        var ic = go.GetComponent<InimigoController>() ?? go.GetComponentInParent<InimigoController>();
        if (ic != null) return ic.gameObject;
        var mi = go.GetComponent<movi_inimigo>() ?? go.GetComponentInParent<movi_inimigo>();
        if (mi != null) return mi.gameObject;
        return null;
    }
}
