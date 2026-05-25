using System.Collections;
using UnityEngine;

public class RessurgenciaPassiva : MonoBehaviour
{
    public float cooldown     = 35f;
    public float cura         = 25f;
    public float bonusVel     = 0.15f;
    public float duracaoBoost = 4f;

    private PlayerStats stats;
    private float proximoUso = 0f;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        InimigoController.OnPreMorte += OnInimigoMorto;
    }

    void OnDestroy()
    {
        InimigoController.OnPreMorte -= OnInimigoMorto;
    }

    void OnInimigoMorto(InimigoController morto)
    {
        if (Time.time < proximoUso) return;
        proximoUso = Time.time + cooldown;
        StartCoroutine(Ativar());
    }

    IEnumerator Ativar()
    {
        stats.Heal(cura);
        float boost = stats.speed * bonusVel;
        stats.speed += boost;

        StartCoroutine(EfeitoVerde());

        yield return new WaitForSeconds(duracaoBoost);
        if (stats != null) stats.speed -= boost;
    }

    IEnumerator EfeitoVerde()
    {
        const int   SEGS    = 40;
        const float DUR     = 0.55f;
        const float RAIO_MX = 2.0f;

        var go           = new GameObject("RessurgenciaEfeito");
        go.transform.position = transform.position;
        var lr           = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 13;

        var go2          = new GameObject("RessurgenciaEfeito2");
        var lr2          = go2.AddComponent<LineRenderer>();
        lr2.useWorldSpace = true;
        lr2.loop          = true;
        lr2.positionCount = SEGS;
        lr2.material      = new Material(Shader.Find("Sprites/Default"));
        lr2.sortingOrder  = 14;

        Vector2 centro = transform.position;

        for (float t = 0f; t < DUR; t += Time.deltaTime)
        {
            float p = t / DUR;

            float r1 = Mathf.Lerp(0f, RAIO_MX, p);
            lr.startColor = lr.endColor = new Color(0.15f, 0.9f, 0.4f, Mathf.Lerp(0.9f, 0f, p));
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.32f, 0.04f, p);
            Circulo(lr, centro, r1, SEGS);

            float r2 = Mathf.Lerp(0f, RAIO_MX * 0.5f, Mathf.Min(p * 2f, 1f));
            lr2.startColor = lr2.endColor = new Color(1f, 0.88f, 0.2f, Mathf.Lerp(1f, 0f, Mathf.Min(p * 2.5f, 1f)));
            lr2.startWidth = lr2.endWidth = Mathf.Lerp(0.22f, 0.02f, p);
            Circulo(lr2, centro, r2, SEGS);

            yield return null;
        }
        Destroy(go2);
        Destroy(go);

        // Flash verde no sprite
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color original = sr.color;
        sr.color = new Color(0.3f, 1f, 0.5f);
        yield return new WaitForSeconds(0.15f);
        sr.color = original;
    }

    static void Circulo(LineRenderer lr, Vector2 c, float r, int segs)
    {
        for (int i = 0; i < segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            lr.SetPosition(i, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
        }
    }
}
