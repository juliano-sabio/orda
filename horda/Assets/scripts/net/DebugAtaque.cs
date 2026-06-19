using UnityEngine;

// Harness de teste: o player local causa dano periódico no inimigo mais próximo
// num raio. Valida o roteamento de dano em rede do SP2b. Não é ataque de produção.
public class DebugAtaque : MonoBehaviour
{
    [SerializeField] float intervalo = 0.4f;
    [SerializeField] float raio = 6f;
    [SerializeField] float dano = 10f;

    PlayerStats stats;
    float t;

    void Awake() { stats = GetComponent<PlayerStats>(); }

    void Update()
    {
        if (stats == null || !stats.IsLocalAuthority) return;
        t += Time.deltaTime;
        if (t < intervalo) return;
        t = 0f;

        var hits = Physics2D.OverlapCircleAll(transform.position, raio);
        InimigoController alvo = null;
        float menor = float.MaxValue;
        foreach (var h in hits)
        {
            var ic = h.GetComponent<InimigoController>();
            if (ic == null) continue;
            float d = ((Vector2)ic.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < menor) { menor = d; alvo = ic; }
        }
        if (alvo != null) alvo.ReceberDano(dano);
    }
}
