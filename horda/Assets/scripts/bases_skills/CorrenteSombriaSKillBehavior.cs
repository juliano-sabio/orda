using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorrenteSombriaSkillBehavior : SkillBehavior
{
    float baseDano      = 6f;
    float multiplicador = 0.25f;
    float intervalo     = 10f;
    float duracaoAtiva  = 3f;
    int   qtdAlvos      = 3;
    float raioDeteccao  = 12f;
    float timer;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano    = data.attackBonus > 0f          ? data.attackBonus        : 12f;
        intervalo   = data.activationInterval > 0f   ? data.activationInterval : 5f;
        duracaoAtiva = data.duration > 0f            ? data.duration           : 3f;
        qtdAlvos    = data.projectileCount > 0       ? data.projectileCount    : 3;
        timer       = intervalo;
    }

    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? Color.white;
        return Color.white;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; StartCoroutine(AtivarCorrente()); }
    }

    public override void ApplyEffect() => StartCoroutine(AtivarCorrente());

    IEnumerator AtivarCorrente()
    {
        var alvos = EncontrarAlvos(qtdAlvos);
        if (alvos.Count == 0) yield break;

        // Linhas de corrente entre alvos e do player ao primeiro
        var linhas = new List<LineRenderer>();
        var pontos = new List<Transform>();

        pontos.Add(playerStats.transform);
        foreach (var ic in alvos) pontos.Add(ic.transform);

        for (int i = 0; i < pontos.Count - 1; i++)
        {
            var lgo = new GameObject($"Corrente{i}");
            var lr  = lgo.AddComponent<LineRenderer>();
            lr.useWorldSpace  = true; lr.positionCount = 2;
            lr.material       = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 12;
            lr.startWidth     = 0.08f; lr.endWidth = 0.08f;
            lr.numCapVertices = 4;
            linhas.Add(lr);
        }

        float proxDano = 0f;
        float ang = 0f;

        for (float t = 0f; t < duracaoAtiva; t += Time.deltaTime)
        {
            ang += Time.deltaTime * 200f;

            // Atualiza posições e ondulação
            for (int i = 0; i < linhas.Count; i++)
            {
                if (linhas[i] == null || pontos[i] == null || pontos[i + 1] == null) continue;

                Vector2 de  = pontos[i].position;
                Vector2 ate = pontos[i + 1].position;
                // Ondulação lateral
                Vector2 meio = Vector2.Lerp(de, ate, 0.5f);
                Vector2 perp = new Vector2(-(ate - de).y, (ate - de).x).normalized;
                meio += perp * Mathf.Sin(ang * 0.05f + i) * 0.3f;

                linhas[i].positionCount = 3;
                linhas[i].SetPosition(0, de);
                linhas[i].SetPosition(1, meio);
                linhas[i].SetPosition(2, ate);

                float pulso = Mathf.Sin(t * 8f + i) * 0.5f + 0.5f;
                Color baseC = CorElemento(); Color cor = new Color(baseC.r, baseC.g, baseC.b, 0.6f + pulso * 0.4f);
                linhas[i].startColor = linhas[i].endColor = cor;
                linhas[i].startWidth = linhas[i].endWidth = 0.05f + pulso * 0.08f;
            }

            // Dano a cada 0.5s
            proxDano -= Time.deltaTime;
            if (proxDano <= 0f)
            {
                proxDano = 0.5f;
                foreach (var ic in alvos)
                    if (ic != null && !ic.estaMorrendo)
                    {
                        ic.ReceberDano(DanoAtual, false);
                        SkillElementEffect.Aplicar(skillData, ic.gameObject, DanoAtual, this);
                    }
            }

            yield return null;
        }

        // Limpa
        foreach (var lr in linhas)
            if (lr != null) Destroy(lr.gameObject);
    }

    List<InimigoController> EncontrarAlvos(int qtd)
    {
        var todos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        var lista = new List<InimigoController>(todos);
        Vector2 orig = playerStats.transform.position;
        lista.RemoveAll(ic => ic.estaMorrendo || Vector2.Distance(ic.transform.position, orig) > raioDeteccao);
        lista.Sort((a, b) => Vector2.Distance(a.transform.position, orig).CompareTo(Vector2.Distance(b.transform.position, orig)));
        if (lista.Count > qtd) lista.RemoveRange(qtd, lista.Count - qtd);
        return lista;
    }
}
