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
    }

    // Este método é chamado automaticamente quando o Inimigo é destruído (pelo Morrer())
    private void OnDestroy()
    {
        // Só spawna se o inimigo realmente morreu (vida zero) 
        // e se não estivermos fechando o jogo (quitting)
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

        // Cria a poça na posição onde o inimigo morreu
        GameObject novaPoca = Instantiate(prefabPoca, transform.position, Quaternion.identity);

        // Configura os dados da poça
        AreaVeneno scriptPoca = novaPoca.GetComponent<AreaVeneno>();
        if (scriptPoca != null)
        {
            scriptPoca.Inicializar(danoPorTick, intervaloTick, duracaoPoca, raioPoca, layerParaAcertar);
        }
    }
}