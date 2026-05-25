using System.Collections;
using UnityEngine;

public class UltimoFolegoPassiva : MonoBehaviour
{
    public float hpAoSobreviver = 30f;
    public float cooldown       = 45f;
    public float duracaoInvul   = 2f;

    private float proximoUso = 0f;

    public bool TentarSobreviver()
    {
        if (Time.time < proximoUso) return false;

        var stats = GetComponent<PlayerStats>();
        if (stats == null) return false;

        proximoUso         = Time.time + cooldown;
        stats.health       = hpAoSobreviver;
        stats.invulneravel = true;

        StartCoroutine(RemoverInvul(stats));
        StartCoroutine(FlashDourado());

        var go = new GameObject("UltimoFolegoEfeito");
        go.transform.position = transform.position;
        go.AddComponent<UltimoFolegoEfeito>().Iniciar();

        return true;
    }

    IEnumerator RemoverInvul(PlayerStats stats)
    {
        yield return new WaitForSeconds(duracaoInvul);
        if (stats != null) stats.invulneravel = false;
    }

    IEnumerator FlashDourado()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color original = sr.color;
        for (int i = 0; i < 5; i++)
        {
            sr.color = new Color(1f, 0.88f, 0.15f);
            yield return new WaitForSeconds(0.12f);
            sr.color = original;
            yield return new WaitForSeconds(0.12f);
        }
    }
}

public class UltimoFolegoEfeito : MonoBehaviour
{
    public void Iniciar() => StartCoroutine(Animar());

    IEnumerator Animar()
    {
        const int   SEGS      = 48;
        const float DURACAO   = 0.75f;
        const float RAIO_MAX  = 3.2f;
        const int   NUM_RAIOS = 8;

        Vector2 centro = transform.position;
        var mat = new Material(Shader.Find("Sprites/Default"));

        // Anel externo dourado
        var anel1 = CriarLR(mat, SEGS, sortOrder: 14);

        // Anel interno branco (expande mais devagar)
        var anel2 = CriarLR(mat, SEGS, sortOrder: 15);

        // Raios radiais
        var raios = new LineRenderer[NUM_RAIOS];
        for (int r = 0; r < NUM_RAIOS; r++)
        {
            var go = new GameObject("Raio");
            go.transform.SetParent(transform);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace  = true;
            lr.positionCount  = 2;
            lr.material       = mat;
            lr.sortingOrder   = 16;
            raios[r] = lr;
        }

        for (float t = 0f; t < DURACAO; t += Time.deltaTime)
        {
            float p = t / DURACAO;

            // Anel 1: dourado, expande rápido, some no fim
            float r1     = Mathf.Lerp(0f, RAIO_MAX, p);
            float alpha1 = Mathf.Lerp(1f, 0f, p);
            float w1     = Mathf.Lerp(0.35f, 0.04f, p);
            DesenharCirculo(anel1, centro, r1, new Color(1f, 0.85f, 0.1f, alpha1), w1, SEGS);

            // Anel 2: branco, expande mais rápido e some antes
            float r2     = Mathf.Lerp(0f, RAIO_MAX * 0.65f, Mathf.Min(p * 1.6f, 1f));
            float alpha2 = Mathf.Lerp(1f, 0f, Mathf.Min(p * 2f, 1f));
            float w2     = Mathf.Lerp(0.25f, 0.02f, p);
            DesenharCirculo(anel2, centro, r2, new Color(1f, 1f, 0.9f, alpha2), w2, SEGS);

            // Raios: disparam para fora e somem rápido
            float alphaR = Mathf.Lerp(1f, 0f, Mathf.Min(p * 2.5f, 1f));
            float comprR = Mathf.Lerp(0f, 1.5f, Mathf.Min(p * 3f, 1f));
            for (int r = 0; r < NUM_RAIOS; r++)
            {
                float ang = r / (float)NUM_RAIOS * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                raios[r].SetPosition(0, centro);
                raios[r].SetPosition(1, centro + dir * comprR);
                raios[r].startColor = raios[r].endColor = new Color(1f, 0.95f, 0.5f, alphaR);
                raios[r].startWidth = raios[r].endWidth = Mathf.Lerp(0.18f, 0.02f, p);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    static LineRenderer CriarLR(Material mat, int segs, int sortOrder)
    {
        var go = new GameObject("Ring");
        var lr         = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.loop           = true;
        lr.positionCount  = segs;
        lr.material       = mat;
        lr.sortingOrder   = sortOrder;
        return lr;
    }

    static void DesenharCirculo(LineRenderer lr, Vector2 centro, float raio, Color cor, float largura, int segs)
    {
        lr.startColor = lr.endColor = cor;
        lr.startWidth = lr.endWidth = largura;
        for (int i = 0; i < segs; i++)
        {
            float ang = i / (float)segs * Mathf.PI * 2f;
            lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
        }
    }
}
