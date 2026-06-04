using System.Collections;
using UnityEngine;

public class TeiaProtecaoSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float recarga   = 180f;
    float duracao   = 4f;
    float raio      = 3.5f;
    float forcaEmpurrao = 8f;

    float timerRecarga = 0f;
    bool  ativa        = false;

    public bool  EmRecarga    => timerRecarga > 0f;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => recarga;

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    public void ConfigurarDeSkillData(SkillData data)
    {
        if (data.cooldown > 0f)           recarga   = data.cooldown;
        if (data.specialValue > 0f)       raio      = data.specialValue;
        if (data.activationInterval > 0f) duracao   = data.activationInterval;
    }

    public override void ApplyEffect() => Ativar();

    void OnEnable()  => PlayerStats.OnDanoRecebido += OnDano;
    void OnDisable() => PlayerStats.OnDanoRecebido -= OnDano;

    void OnDano()
    {
        if (timerRecarga <= 0f && !ativa) Ativar();
    }

    void Update()
    {
        if (timerRecarga > 0f) timerRecarga -= Time.deltaTime;
        if (ativa && playerStats != null) EmpurrarInimigos();
    }

    void Ativar()
    {
        if (ativa) return;
        timerRecarga = recarga;
        StartCoroutine(CorotinaAtiva());
    }

    void EmpurrarInimigos()
    {
        var inimigos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        Vector2 centro = playerStats.transform.position;
        float raioReal = raio * (SkillEvolutionManager.Tem(SkillEvolutionType.TeiaPermanente) ? 1.3f : 1f);
        foreach (var ic in inimigos)
        {
            if (ic == null || ic.estaMorrendo) continue;
            float dist = Vector2.Distance(ic.transform.position, centro);
            if (dist > raioReal) continue;
            var rb = ic.GetComponent<Rigidbody2D>();
            if (rb == null) continue;
            Vector2 dir = ((Vector2)ic.transform.position - centro).normalized;
            rb.AddForce(dir * forcaEmpurrao * (1f - dist / raioReal), ForceMode2D.Impulse);

            // TeiaVenenosa — aplica veneno ao empurrar
            if (SkillEvolutionManager.Tem(SkillEvolutionType.TeiaVenenosa))
                EvolutionFX.AplicarVeneno(ic, 2f, 4f);
        }
    }

    IEnumerator CorotinaAtiva()
    {
        ativa = true;
        float duracaoReal = duracao * (SkillEvolutionManager.Tem(SkillEvolutionType.TeiaPermanente) ? 2f : 1f);
        var visual = CriarVisual();
        StartCoroutine(AnimarVisual(visual, duracaoReal));
        yield return new WaitForSeconds(duracaoReal);
        ativa = false;
        if (visual != null) StartCoroutine(FadeDestruir(visual, 0.3f));
    }

    GameObject CriarVisual()
    {
        var root = new GameObject("TeiaVisual");

        // Anéis da teia
        for (int r = 0; r < 3; r++)
        {
            float raioAnel = raio * (0.4f + r * 0.3f);
            var go = new GameObject($"Anel{r}");
            go.transform.SetParent(root.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.loop = true; lr.positionCount = 32;
            lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 8;
            lr.startWidth = lr.endWidth = 0.05f;
            lr.startColor = lr.endColor = new Color(0.3f, 1f, 0.5f, 0.7f);
        }

        // Linhas radiais
        for (int i = 0; i < 8; i++)
        {
            float ang = i / 8f * Mathf.PI * 2f;
            var go = new GameObject($"Radial{i}");
            go.transform.SetParent(root.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.positionCount = 2;
            lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 7;
            lr.startWidth = 0.04f; lr.endWidth = 0.01f;
            lr.startColor = lr.endColor = new Color(0.3f, 1f, 0.5f, 0.5f);
        }

        return root;
    }

    IEnumerator AnimarVisual(GameObject root, float dur)
    {
        float ang = 0f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (root == null || playerStats == null) yield break;
            ang += Time.deltaTime * 30f;
            root.transform.position = playerStats.transform.position;
            root.transform.rotation = Quaternion.Euler(0f, 0f, ang);

            float prog  = t / dur;
            float pulso = Mathf.Sin(t * 6f) * 0.5f + 0.5f;
            Color cor   = new Color(0.3f, 1f, 0.5f, (0.5f + pulso * 0.3f) * (1f - prog * 0.5f));

            // Atualiza anéis
            var lrs = root.GetComponentsInChildren<LineRenderer>();
            int anel = 0;
            foreach (var lr in lrs)
            {
                if (lr.positionCount == 32) // é um anel
                {
                    float r = raio * (0.4f + anel * 0.3f);
                    for (int i = 0; i < 32; i++)
                    {
                        float a = 360f / 32 * i * Mathf.Deg2Rad;
                        lr.SetPosition(i, (Vector2)playerStats.transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                    }
                    lr.startColor = lr.endColor = cor;
                    anel++;
                }
                else if (lr.positionCount == 2) // é radial
                {
                    // Calcula o ângulo do radial pela ordem entre os LRs radiais encontrados
                    int radialIdx = 0;
                    foreach (var lr2 in lrs)
                        if (lr2.positionCount == 2 && lr2 == lr) break;
                        else if (lr2.positionCount == 2) radialIdx++;
                    float radAng = radialIdx / 8f * Mathf.PI * 2f;
                    Vector2 centro2 = playerStats.transform.position;
                    lr.SetPosition(0, centro2);
                    lr.SetPosition(1, centro2 + new Vector2(Mathf.Cos(radAng + ang * Mathf.Deg2Rad), Mathf.Sin(radAng + ang * Mathf.Deg2Rad)) * raio);
                    lr.startColor = new Color(cor.r, cor.g, cor.b, cor.a * 0.6f);
                    lr.endColor   = new Color(cor.r, cor.g, cor.b, 0f);
                }
            }
            yield return null;
        }
    }

    IEnumerator FadeDestruir(GameObject go, float dur)
    {
        var lrs = go.GetComponentsInChildren<LineRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float a = 1f - t / dur;
            foreach (var lr in lrs) { Color c = lr.startColor; c.a *= a; lr.startColor = lr.endColor = c; }
            yield return null;
        }
        Destroy(go);
    }
}
