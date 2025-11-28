using UnityEngine;

[System.Serializable]
public class StageData
{
    [Header("🔹 Identificação do Stage")]
    public string stageName;
    public string sceneName;
    public int stageIndex;

    [Header("🔹 Configurações de Dificuldade")]
    [Range(1, 5)]
    public int difficulty = 1;
    public int recommendedLevel = 1;
    public int coinReward = 100;
    public int expReward = 50;

    [Header("🔹 Status do Stage")]
    public bool unlocked = false;
    public bool completed = false;
    public float bestTime = 0f;
    [Range(0, 3)]
    public int starsEarned = 0;

    [Header("🔹 Configurações do Ambiente")]
    public string environmentType; // "Floresta", "Deserto", "Caverna", etc.
    public string backgroundMusic;
    public Color stageColor = Color.white;

    [Header("🔹 Inimigos e Recompensas")]
    public int enemyCount = 5;
    public int bossCount = 0;
    public string[] specialRewards;

    [Header("🔹 Requisitos")]
    public int requiredLevel = 1;
    public int requiredCharacters = 1;
    public string requiredStageToUnlock; // Nome do stage que precisa ser completado

    [Header("🔹 Visual")]
    public Sprite stagePreview;
    public string stageDescription;

    // Construtor para criar stages facilmente
    public StageData(string name, string scene, int difficulty = 1)
    {
        this.stageName = name;
        this.sceneName = scene;
        this.difficulty = difficulty;
        this.recommendedLevel = difficulty;
        this.coinReward = difficulty * 100;
        this.expReward = difficulty * 50;
        this.enemyCount = 5 + (difficulty - 1) * 3;
        this.bossCount = difficulty >= 2 ? 1 : 0;
    }

    // Método para verificar se o stage pode ser desbloqueado
    public bool CanUnlock(int playerLevel, int unlockedChars)
    {
        return playerLevel >= requiredLevel && unlockedChars >= requiredCharacters;
    }

    // Método para completar o stage
    public void CompleteStage(float time, int stars)
    {
        completed = true;
        starsEarned = Mathf.Max(starsEarned, stars);

        if (bestTime == 0f || time < bestTime)
        {
            bestTime = time;
        }
    }
}