using System.Collections;
using UnityEngine;

public class ProjetilEspecialPrincesa : MonoBehaviour
{
    public enum Tipo { Raiz, Queima, Empurrao }

    public Tipo  tipo;
    public float dano = 20f;

    bool atingiu;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (atingiu || !other.CompareTag("Player")) return;
        atingiu = true;

        var ps  = other.GetComponent<PlayerStats>();
        var rb2 = other.GetComponent<Rigidbody2D>();

        switch (tipo)
        {
            case Tipo.Raiz:
                if (ps != null) ps.TakeDamage(dano);
                EfeitoRunner.Criar().StartCoroutine(Enraizar(other.gameObject, 1f));
                break;

            case Tipo.Queima:
                EfeitoRunner.Criar().StartCoroutine(Queimar(ps, dano * 0.4f, 3f));
                break;

            case Tipo.Empurrao:
                if (ps != null) ps.TakeDamage(dano);
                if (rb2 != null)
                {
                    Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
                    rb2.AddForce(dir * 20f, ForceMode2D.Impulse);
                }
                break;
        }

        Destroy(gameObject);
    }

    static IEnumerator Enraizar(GameObject player, float duracao)
    {
        var rb2 = player != null ? player.GetComponent<Rigidbody2D>() : null;
        if (rb2 == null) yield break;

        var constraintsOrig = rb2.constraints;
        rb2.linearVelocity  = Vector2.zero;
        rb2.constraints     = RigidbodyConstraints2D.FreezeAll;

        // Anel visual amarelo pulsando ao redor do player
        var anel = CriarAnelEnraizamento(player.transform);

        yield return new WaitForSeconds(duracao);

        if (rb2 != null) rb2.constraints = constraintsOrig;
        if (anel  != null) Destroy(anel);
    }

    static IEnumerator Queimar(PlayerStats ps, float danoPorSegundo, float duracao)
    {
        float elapsed = 0f, tick = 0.5f;
        while (elapsed < duracao && ps != null)
        {
            yield return new WaitForSeconds(tick);
            elapsed += tick;
            ps.TakeDamage(danoPorSegundo * tick);
        }
    }

    static GameObject CriarAnelEnraizamento(Transform alvo)
    {
        var go = new GameObject("AnelRaiz");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.loop           = true;
        lr.positionCount  = 24;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth     = lr.endWidth = 0.08f;
        lr.sortingOrder   = 8;

        EfeitoRunner.Criar().StartCoroutine(AnimarAnelRaiz(go, lr, alvo));
        return go;
    }

    static IEnumerator AnimarAnelRaiz(GameObject go, LineRenderer lr, Transform alvo)
    {
        float t = 0f;
        while (go != null && alvo != null)
        {
            t += Time.deltaTime;
            float r = 0.55f + 0.05f * Mathf.Sin(t * 10f);
            float a = 0.6f + 0.4f * Mathf.Sin(t * 8f);
            for (int i = 0; i < 24; i++)
            {
                float ang = (360f / 24) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, alvo.position + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r));
            }
            lr.startColor = lr.endColor = new Color(1f, 0.9f, 0.1f, a);
            yield return null;
        }
    }
}

// Runner leve para executar coroutines independentes do projétil
public class EfeitoRunner : MonoBehaviour
{
    public static EfeitoRunner Criar()
    {
        var go = new GameObject("EfeitoRunner");
        Destroy(go, 10f);
        return go.AddComponent<EfeitoRunner>();
    }
}
