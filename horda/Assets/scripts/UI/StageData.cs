using UnityEngine;

[CreateAssetMenu(fileName = "New Stage", menuName = "Stage System/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Informações Básicas")]
    public string stageName;
    [TextArea(2, 3)]
    public string description;
    public Sprite stageImage;
    public string sceneName;

    [Header("Requisitos")]
    public bool unlocked = true;
    public int requiredLevel = 1;

    [Header("Dificuldade e Recompensas")]
    [Range(1, 5)]
    public int difficulty = 1;

    [Header("Multiplicadores")]
    public float xpMultiplier = 1.0f;
    public float coinMultiplier = 1.0f;

    [Header("Recompensas Base")]
    public int baseCoinReward = 100;
    public int baseXPReward = 50;

    [Header("Configurações de Inimigos")]
    public int minEnemies = 5;
    public int maxEnemies = 15;
    public float enemySpawnRate = 1.0f;

    [Header("Ambiente")]
    public Color ambientLight = Color.white;
    public AudioClip backgroundMusic;
}