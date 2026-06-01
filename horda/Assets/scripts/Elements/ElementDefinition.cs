using UnityEngine;

[System.Serializable]
public class ElementCharacteristic
{
    public string nome;
    [TextArea(2, 3)]
    public string descricao;
    public CharacteristicType tipo;
    public float valor1;
    public float valor2;
}

[System.Serializable]
public class ElementDefinition
{
    public ElementType tipo;
    public string nomeDisplay;
    public Color cor;
    public Sprite icone;
    [HideInInspector] public string emoji;
    public ElementCharacteristic[] caracteristicas = new ElementCharacteristic[2];
}
