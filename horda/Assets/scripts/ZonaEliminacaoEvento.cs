using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZonaEliminacaoEvento : MonoBehaviour
{
    [HideInInspector] public float raio              = 8f;
    [HideInInspector] public int   quantidade        = 10;
    [HideInInspector] public GameObject[] prefabsInimigos;
    [HideInInspector] public float intervaloSpawn    = 3.5f;
    [HideInInspector] public int   maxSimultaneos    = 6;

    // Co-op: no cliente é só visual (sem kill-tracking nem spawn); o host roda o gameplay.
    [HideInInspector] public bool cosmetico;
    int objEvtId;

    public event Action<int, int> OnProgresso;  // (atual, total)
    public event Action           OnConcluido;

    int kills;
    readonly List<GameObject> inimigosAtivos = new List<GameObject>();

    LineRenderer lrExt, lrInt;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        CriarVisual();
        StartCoroutine(AnimarAnel());
        StartCoroutine(ParticulasBorda());

        if (cosmetico)
        {
            IndicadorSlime.Criar(transform, new Color(0.25f, 0.85f, 1f), "Zona!");
            return; // cliente: só visual + indicador
        }

        InimigoController.OnPreMorte += OnPreMorteHandler;
        StartCoroutine(SpawnLoop());

        // co-op: host registra p/ o cliente reconstruir a cópia visual.
        if (NetSpawn.EmRede && NetSpawn.PodeSpawnar && CoopProgressao.Instance != null)
            objEvtId = CoopProgressao.Instance.RegistrarObjEvento(1, transform.position, raio, 0f);
    }

    void OnDestroy()
    {
        InimigoController.OnPreMorte -= OnPreMorteHandler;
        if (objEvtId != 0 && CoopProgressao.Instance != null)
            CoopProgressao.Instance.RemoverObjEvento(objEvtId);
    }

    // ── Kill tracking ────────────────────────────────────────────────────────

    void OnPreMorteHandler(InimigoController ic)
    {
        if (ic == null || this == null) return;
        if (Vector2.Distance(ic.transform.position, transform.position) > raio) return;
        kills++;
        OnProgresso?.Invoke(kills, quantidade);
        if (kills >= quantidade) OnConcluido?.Invoke();
    }

    // ── Visual ───────────────────────────────────────────────────────────────

    void CriarVisual()
    {
        lrExt = CriarAnel(raio,          new Color(0.25f, 0.85f, 1f, 0.95f), 0.18f);
        lrInt = CriarAnel(raio * 0.92f,  new Color(0.15f, 0.6f,  1f, 0.4f),  0.07f);

        // Fill semitransparente
        var fill = new GameObject("Fill");
        fill.transform.SetParent(transform, false);
        var sr = fill.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarDisco(128, new Color(0.1f, 0.5f, 1f, 0.06f));
        sr.sortingOrder = 1;
        float escala    = raio * 2f / (128f / 100f); // PPU=100
        fill.transform.localScale = Vector3.one * escala;
    }

    LineRenderer CriarAnel(float r, Color cor, float largura)
    {
        const int SEGS = 64;
        var go = new GameObject("Anel");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.loop           = true;
        lr.positionCount  = SEGS;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder   = 3;
        lr.startWidth     = lr.endWidth = largura;
        lr.startColor     = lr.endColor = cor;
        for (int i = 0; i < SEGS; i++)
        {
            float a = 360f / SEGS * i * Mathf.Deg2Rad;
            lr.SetPosition(i, transform.position + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
        return lr;
    }

    IEnumerator AnimarAnel()
    {
        float t = 0f;
        while (lrExt != null)
        {
            t += Time.deltaTime;
            float a = 0.7f + Mathf.Sin(t * 2.5f) * 0.25f;
            AlphaLR(lrExt, a);
            AlphaLR(lrInt, a * 0.45f);

            yield return null;
        }
    }

    // Partículas brilhantes na borda
    IEnumerator ParticulasBorda()
    {
        while (this != null)
        {
            yield return new WaitForSeconds(0.22f);
            StartCoroutine(Particula());
        }
    }

    IEnumerator Particula()
    {
        var go  = new GameObject("P");
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarDisco(8, new Color(0.4f, 0.9f, 1f));
        sr2.sortingOrder = 4;
        float sz = UnityEngine.Random.Range(0.12f, 0.28f);
        go.transform.localScale = Vector3.one * sz;

        float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        go.transform.position = transform.position +
            new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio);

        Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) *
                      UnityEngine.Random.Range(0.4f, 1.1f);

        for (float t = 0f; t < 0.9f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            go.transform.position += (Vector3)(vel * Time.deltaTime);
            vel *= Mathf.Pow(0.94f, Time.deltaTime * 60f);
            var c = sr2.color;
            c.a      = Mathf.Lerp(1f, 0f, t / 0.9f);
            sr2.color = c;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ── Spawn ────────────────────────────────────────────────────────────────

    IEnumerator SpawnLoop()
    {
        // Spawn inicial
        SpawnLote(maxSimultaneos);

        while (kills < quantidade)
        {
            yield return new WaitForSeconds(intervaloSpawn);
            inimigosAtivos.RemoveAll(o => o == null);
            int faltam = maxSimultaneos - inimigosAtivos.Count;
            if (faltam > 0) SpawnLote(faltam);
        }
    }

    void SpawnLote(int n)
    {
        for (int i = 0; i < n; i++) SpawnUm();
    }

    void SpawnUm()
    {
        if (prefabsInimigos == null || prefabsInimigos.Length == 0) return;
        var ge = GerenciadorEventos.Instance;

        for (int t = 0; t < 25; t++)
        {
            float ang  = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float dist = UnityEngine.Random.Range(raio * 0.15f, raio * 0.78f);
            Vector2 pos = (Vector2)transform.position +
                          new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

            if (ge != null && !ge.PosicaoValida(pos)) continue;

            var prefab = prefabsInimigos[UnityEngine.Random.Range(0, prefabsInimigos.Length)];
            if (prefab == null) continue;
            var go = NetSpawn.Spawnar(prefab, new Vector3(pos.x, pos.y, 0f)); // host spawna+replica
            if (go != null) inimigosAtivos.Add(go);
            return;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void AlphaLR(LineRenderer lr, float a)
    {
        if (lr == null) return;
        Color c = lr.startColor; c.a = a;
        lr.startColor = lr.endColor = c;
    }

    static Sprite GerarDisco(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float dx = (x - cx) / cx, dy = (y - cx) / cx;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);
            float a  = Mathf.Clamp01(1f - d);
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, cor.a * a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, raio);
    }
}
