using UnityEngine;

[CreateAssetMenu(fileName = "New Inimigo", menuName = "Survivor/Inimigo Data")]
public class InimigoData : ScriptableObject
{
    [Header("Identifica��o")]
    public string nomeInimigo;
    public GameObject prefab;
    public Sprite icon;

    [Header("Status Base")]
    public float vidaBase = 50f;
    public float danoBase = 10f;
    public float velocidadeBase = 3f;
    public float tamanho = 1f;

    [Header("Configura��es de Spawn")]
    public float intervaloAtaque = 1f;
    public int xpDrop = 10;
    public bool isBoss = false;
    public float chanceSpawn = 1f;

    [Header("Efeitos")]
    public GameObject efeitoMorte;
    public AudioClip somMorte;
    public Color corDestaque = Color.white;

    [Header("Comportamento")]
    public TipoComportamento comportamento = TipoComportamento.Melee;
    public float distanciaAtaque = 2f;
    public float distanciaPerseguicao = 8f;
}

public enum TipoComportamento
{
    Melee,
    Ranged,
    Tank,
    Fast,
    Boss
}