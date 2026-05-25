using System.Collections;
using UnityEngine;

public class SlimeCurativaOnda : MonoBehaviour
{
    [Header("Onda de Cura ao Morrer")]
    public float     raioOnda    = 6f;
    public float     curaOnda    = 40f;
    public float     duracaoOnda = 0.6f;
    public LayerMask camadaInimigos;

    private InimigoController ic;

    void Start()
    {
        ic = GetComponent<InimigoController>();
        InimigoController.OnPreMorte += OnMorte;
    }

    void OnDestroy()
    {
        InimigoController.OnPreMorte -= OnMorte;
    }

    void OnMorte(InimigoController morto)
    {
        if (morto != ic) return;

        Vector2 pos = transform.position;

        var cols = Physics2D.OverlapCircleAll(pos, raioOnda, camadaInimigos);
        foreach (var c in cols)
        {
            var alvo = c.GetComponent<InimigoController>() ?? c.GetComponentInParent<InimigoController>();
            if (alvo != null && alvo != ic && !alvo.estaMorrendo)
                alvo.ReceberCura(curaOnda);
        }

        var go = new GameObject("OndaCura");
        go.transform.position = pos;
        go.AddComponent<OndaCuraVisual>().Iniciar(raioOnda, duracaoOnda);
    }
}

public class OndaCuraVisual : MonoBehaviour
{
    public void Iniciar(float raioFinal, float duracao)
    {
        StartCoroutine(Animar(raioFinal, duracao));
    }

    IEnumerator Animar(float raioFinal, float duracao)
    {
        const int SEGS = 48;
        var lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 10;

        Vector2 centro = transform.position;

        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float p         = t / duracao;
            float raioAtual = Mathf.Lerp(0f, raioFinal, p);
            float alpha     = Mathf.Lerp(0.9f, 0f, p);
            float largura   = Mathf.Lerp(0.4f, 0.05f, p);
            Color cor       = new Color(1f, 0.9f, 0.1f, alpha);

            lr.startColor = lr.endColor = cor;
            lr.startWidth = lr.endWidth = largura;

            for (int i = 0; i < SEGS; i++)
            {
                float ang = i / (float)SEGS * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioAtual);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
