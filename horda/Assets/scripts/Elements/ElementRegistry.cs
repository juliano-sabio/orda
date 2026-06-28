using UnityEngine;

[CreateAssetMenu(fileName = "ElementRegistry", menuName = "Horda/ElementRegistry")]
public class ElementRegistry : ScriptableObject
{
    public ElementDefinition[] elementos = new ElementDefinition[10];

    [Header("Boss drop")]
    public GameObject tokenPrefab; // prefab networkizado do token de elemento (dropado pelo boss)

    // Boss drop: solta `qtd` tokens de elemento ALEATÓRIO ao redor de `centro`. Só o host/SP
    // chama (NetSpawn.Spawnar é host-autoritativo em rede). Em MP o ElementTokenNet sincroniza
    // o elemento (ícone) e a coleta é host-autoritativa (cada token = 1 coleta).
    public void DroparTokenElemento(Vector3 centro, int qtd)
    {
        if (tokenPrefab == null || elementos == null || elementos.Length == 0) return;
        for (int i = 0; i < qtd; i++)
        {
            var def = elementos[Random.Range(0, elementos.Length)];
            if (def == null) continue;
            Vector3 pos = centro + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(0.6f, 1.4f));
            var go = NetSpawn.Spawnar(tokenPrefab, pos);
            if (go == null) continue;
            var net = go.GetComponent<ElementTokenNet>();
            var no  = go.GetComponent<Unity.Netcode.NetworkObject>();
            if (net != null && no != null && no.IsSpawned) net.DefinirElemento((int)def.tipo); // MP: sincroniza
            else go.GetComponent<ElementDropToken>()?.Configurar(def.tipo);                     // SP: direto
        }
    }

    static ElementRegistry _instance;
    public static ElementRegistry Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<ElementRegistry>("Elements/ElementRegistry");
            return _instance;
        }
    }

    public ElementDefinition Get(ElementType tipo)
    {
        if (elementos == null) return null;
        foreach (var el in elementos)
            if (el != null && el.tipo == tipo) return el;
        return null;
    }

    public Color GetCor(ElementType tipo)
    {
        var def = Get(tipo);
        return def != null ? def.cor : Color.white;
    }

    public string GetNome(ElementType tipo)
    {
        var def = Get(tipo);
        return def != null ? def.nomeDisplay : tipo.ToString();
    }
}
