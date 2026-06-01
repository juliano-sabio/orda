using UnityEngine;

[CreateAssetMenu(fileName = "ElementRegistry", menuName = "Horda/ElementRegistry")]
public class ElementRegistry : ScriptableObject
{
    public ElementDefinition[] elementos = new ElementDefinition[10];

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
