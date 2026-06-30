using UnityEngine;

public class InimigoVenenoMorte : MonoBehaviour
{
    [Header("Configurações da Poça")]
    public GameObject prefabPoca;
    public float raioPoca = 2.5f;
    public float duracaoPoca = 5f;
    public float danoPorTick = 5f;
    public float intervaloTick = 1f;
    public LayerMask layerParaAcertar;

    private InimigoController controller;
    private bool jaSpawnou = false;

    void Start()
    {
        controller = GetComponent<InimigoController>();
        InimigoController.OnPreMorte += AoPreMorte;
    }

    // Roda no host/SP DENTRO do Morrer() (antes do despawn) → timing seguro pra spawnar o
    // NetworkObject da poça (replica pro P2). No cliente o Morrer não roda → não duplica.
    void AoPreMorte(InimigoController ic)
    {
        if (ic == controller && !jaSpawnou) SpawnarVeneno();
    }

    // Chamado quando o Inimigo é destruído. Fallback: morreu sem passar pelo Morrer() (raro).
    private void OnDestroy()
    {
        InimigoController.OnPreMorte -= AoPreMorte;
        if (controller != null && controller.vidaAtual <= 0 && !jaSpawnou && gameObject.scene.isLoaded)
        {
            SpawnarVeneno();
        }
    }

    void SpawnarVeneno()
    {
        jaSpawnou = true;

        if (prefabPoca == null)
        {
            Debug.LogError($"❌ Prefab da poça não foi arrastado para o inimigo {name}!");
            return;
        }

        // Co-op: host spawna em rede (a poça é NetworkObject → replica pro P2). SP: Instantiate.
        GameObject novaPoca = NetSpawn.Spawnar(prefabPoca, transform.position);
        if (novaPoca == null) return; // cliente não spawna

        // Configura os dados da poça (só no host/SP; no cliente a cópia em rede já aparece)
        AreaVeneno scriptPoca = novaPoca.GetComponent<AreaVeneno>();
        if (scriptPoca != null)
        {
            scriptPoca.Inicializar(danoPorTick, intervaloTick, duracaoPoca, raioPoca, layerParaAcertar);
        }
    }
}