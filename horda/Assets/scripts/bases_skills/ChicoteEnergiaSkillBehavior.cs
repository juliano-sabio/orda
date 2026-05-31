using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChicoteEnergiaSkillBehavior : SkillBehavior
{
    float baseDano      = 15f;
    float multiplicador = 0.5f;
    float intervalo     = 4f;
    float raio          = 4f;
    float timer;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    public void ConfigurarDeSkillData(SkillData data)
    {
        baseDano  = data.attackBonus > 0f        ? data.attackBonus        : 30f;
        intervalo = data.activationInterval > 0f ? data.activationInterval : 2f;
        raio      = data.specialValue > 0f       ? data.specialValue       : 4f;
        timer     = intervalo;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; StartCoroutine(Chicotear()); }
    }

    public override void ApplyEffect() => StartCoroutine(Chicotear());

    IEnumerator Chicotear()
    {
        Vector2 centro = playerStats.transform.position;
        float angInicio = 0f;
        float angFim    = 360f;
        float dur       = 0.4f;
        var atingidos   = new HashSet<int>();

        var go = new GameObject("ChicoteBeam");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.loop           = false;
        lr.positionCount  = 24;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder   = 13;
        lr.numCapVertices = 4;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null || playerStats == null) yield break;

            float prog   = t / dur;
            float angAtu = Mathf.Lerp(angInicio, angFim, prog);

            centro = playerStats.transform.position;

            // Desenha arco do ângulo início até atual
            int segs = 24;
            lr.positionCount = segs;
            for (int i = 0; i < segs; i++)
            {
                float a = Mathf.Lerp(angInicio, angAtu, i / (float)(segs - 1)) * Mathf.Deg2Rad;
                float ondulacao = Mathf.Sin(t * 20f + i * 0.5f) * 0.15f;
                float r = raio + ondulacao;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }

            float largura = Mathf.Lerp(0.25f, 0.06f, prog);
            lr.startWidth = largura; lr.endWidth = largura * 0.3f;
            Color cor = new Color(0.2f, 0.8f, 1f, Mathf.Lerp(1f, 0.3f, prog));
            lr.startColor = cor; lr.endColor = new Color(cor.r, cor.g, cor.b, 0f);

            // Dano em área
            var hits = Physics2D.OverlapCircleAll(centro, raio + 0.3f);
            foreach (var col in hits)
            {
                var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
                if (ic == null || ic.estaMorrendo) continue;
                int id = ic.gameObject.GetInstanceID();
                if (atingidos.Contains(id)) continue;
                atingidos.Add(id);
                ic.ReceberDano(DanoAtual, false);
                StartCoroutine(FlashInimigo(ic));
            }

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    IEnumerator FlashInimigo(InimigoController ic)
    {
        var sr = ic?.GetComponent<SpriteRenderer>(); if (sr == null) yield break;
        Color orig = sr.color; sr.color = new Color(0.3f, 0.9f, 1f);
        yield return new WaitForSeconds(0.08f);
        if (sr != null) sr.color = orig;
    }
}
