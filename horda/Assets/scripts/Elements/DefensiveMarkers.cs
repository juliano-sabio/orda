using UnityEngine;

// Markers de buff defensivo aplicados no player por características de infusão.
// Auto-expiram; re-aplicar renova a duração (não empilha duplicado).

public class PeleDePedraMarker : MonoBehaviour
{
    public float reducao = 0.30f;   // fração de dano reduzida (0..1)
    float restante;
    public void Renovar(float dur, float red) { restante = dur; reducao = red; }
    void Update() { restante -= Time.deltaTime; if (restante <= 0f) Destroy(this); }
}

public class FundacaoFirmeMarker : MonoBehaviour
{
    float restante;
    public void Renovar(float dur) { restante = dur; }
    void Update() { restante -= Time.deltaTime; if (restante <= 0f) Destroy(this); }
}

public class EsquivaMarker : MonoBehaviour
{
    public float chance = 0.25f;    // chance de evadir totalmente (0..1)
    float restante;
    public void Renovar(float dur, float ch) { restante = dur; chance = ch; }
    void Update() { restante -= Time.deltaTime; if (restante <= 0f) Destroy(this); }
}
