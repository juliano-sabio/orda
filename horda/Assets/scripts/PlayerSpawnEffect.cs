using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpawnEffect : MonoBehaviour
{
    [Header("Raio")]
    public float duracaoRaio  = 0.18f;
    public float larguraRaio  = 0.18f;
    public Color corRaio      = new Color(0.6f, 0.9f, 1f, 1f);

    [Header("Impacto")]
    public float duracaoLuz   = 0.35f;
    public float raioLuz      = 1.4f;
    public Color corLuz       = new Color(0.6f, 0.9f, 1f, 0.9f);

    [Header("Aparecer")]
    public float duracaoFade  = 0.12f;

    private SpriteRenderer sr;
    private Rigidbody2D    rb;
    private Vector3        posicaoFinal;

    void Start()
    {
        sr           = GetComponent<SpriteRenderer>();
        rb           = GetComponent<Rigidbody2D>();
        posicaoFinal = transform.position;

        // Co-op: no fantoche remoto NÃO bloqueia movimento nem mexe no Rigidbody — ele fica
        // Kinematic porque o NetworkTransform é quem manda (OnNetworkSpawn já setou isso).
        // Voltar pra Dynamic aqui causava jitter/arrasto. Só toca o VFX (raio+luz) pra paridade.
        var pn = GetComponent<PlayerNet>();
        if (pn != null && pn.IsSpawned && !pn.IsOwner)
        {
            Reproduzir();
            return;
        }

        sr.color = new Color(1, 1, 1, 0);
        BloquearMovimento(true);
        StartCoroutine(Executar());
    }

    // Co-op: revive reusa o VFX (raio + luz) na posição ATUAL, SEM bloquear movimento nem
    // mexer no Rigidbody (isto roda em todas as cópias, inclusive fantoches remotos).
    public void Reproduzir()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        posicaoFinal = transform.position;
        StartCoroutine(VfxSemBloquear());
    }

    IEnumerator VfxSemBloquear()
    {
        Camera cam = Camera.main;
        float topoTela = cam != null
            ? cam.ViewportToWorldPoint(new Vector3(0f, 1f, 0f)).y
            : posicaoFinal.y + 10f;
        yield return StartCoroutine(AnimarRaio(topoTela));
        StartCoroutine(LuzImpacto());
    }

    IEnumerator Executar()
    {
        Camera cam     = Camera.main;
        float topoTela = cam != null
            ? cam.ViewportToWorldPoint(new Vector3(0f, 1f, 0f)).y
            : posicaoFinal.y + 10f;

        // desenha o raio descendo do topo até o player
        yield return StartCoroutine(AnimarRaio(topoTela));

        // player aparece no impacto
        StartCoroutine(FadeInPlayer());
        StartCoroutine(LuzImpacto());

        yield return new WaitForSeconds(duracaoLuz);
        BloquearMovimento(false);
    }

    IEnumerator AnimarRaio(float topoTela)
    {
        GameObject go = new GameObject("Raio");
        LineRenderer lr  = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.startColor    = corRaio;
        lr.endColor      = new Color(corRaio.r, corRaio.g, corRaio.b, 0f);
        lr.startWidth    = larguraRaio;
        lr.endWidth      = larguraRaio * 0.3f;
        lr.sortingOrder  = sr.sortingOrder + 2;

        Vector3 topo   = new Vector3(posicaoFinal.x, topoTela, 0f);
        Vector3 chegou = posicaoFinal;

        float t = 0f;
        while (t < duracaoRaio)
        {
            t += Time.deltaTime;
            float p    = Mathf.Clamp01(t / duracaoRaio);
            // ponta de baixo do raio desce rapidamente
            Vector3 base3 = Vector3.Lerp(topo, chegou, p * p);
            lr.SetPosition(0, topo);
            lr.SetPosition(1, base3);
            yield return null;
        }

        Destroy(go);
    }

    IEnumerator FadeInPlayer()
    {
        float t = 0f;
        while (t < duracaoFade)
        {
            t += Time.deltaTime;
            sr.color = new Color(1, 1, 1, Mathf.Clamp01(t / duracaoFade));
            yield return null;
        }
        sr.color = Color.white;
    }

    IEnumerator LuzImpacto()
    {
        int seg      = 32;
        GameObject go = new GameObject("LuzImpacto");
        go.transform.position = posicaoFinal;

        LineRenderer lr  = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = seg;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = sr.sortingOrder - 1;

        for (int i = 0; i < seg; i++)
        {
            float a = (360f / seg) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
        }

        float t = 0f;
        while (t < duracaoLuz)
        {
            t += Time.deltaTime;
            float p    = Mathf.Clamp01(t / duracaoLuz);
            float raio = Mathf.Lerp(0.05f, raioLuz, Mathf.SmoothStep(0f, 1f, p));
            float alpha = Mathf.Lerp(1f, 0f, p);

            go.transform.localScale = Vector3.one * raio;
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.2f, 0.01f, p);
            lr.startColor = lr.endColor = new Color(corLuz.r, corLuz.g, corLuz.b, alpha);

            yield return null;
        }

        Destroy(go);
    }

    void BloquearMovimento(bool bloquear)
    {
        var mov = GetComponent<moviment_player2>();
        if (mov != null) mov.enabled = !bloquear;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = bloquear ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        }
    }
}
