using UnityEngine;

// Escala vida e dano dos inimigos conforme o tempo de jogo da run.
// Crescimento LINEAR SEM TETO: mult = 1 + taxaPorMinuto * minutos.
// Aplicado no SPAWN de cada inimigo (InimigoController.InicializarComData),
// então inimigos que nascem depois ficam mais fortes; os que já existem não mudam.
// Bosses ficam de fora (definem vida própria, sem InimigoData — não passam pelo scaling).
//
// O tempo usa Time.timeSinceLevelLoad (escalado): congela durante pause/level-up.
// Singleton lazy: funciona sem precisar colocar na cena (usa os defaults). Se você
// adicionar um GameObject com este componente na cena, os valores ficam tunáveis no Inspector.
public class EnemyScaling : MonoBehaviour
{
    public static EnemyScaling Instance { get; private set; }

    [Header("Escala por tempo (linear, sem teto)")]
    [Tooltip("Liga/desliga a escala de vida/dano dos inimigos por tempo.")]
    public bool ativo = true;
    [Tooltip("Aumento de VIDA por minuto. 0.12 = +12%/min (×1.6 aos 5min, ×2.2 aos 10min).")]
    public float vidaPorMinuto = 0.12f;
    [Tooltip("Aumento de DANO por minuto. 0.06 = +6%/min (×1.3 aos 5min, ×1.6 aos 10min).")]
    public float danoPorMinuto = 0.06f;

    [Header("Escala dos BOSSES (mais suave)")]
    [Tooltip("Aumento de VIDA do boss por minuto. 0.06 = +6%/min (~×2.8 aos 30min).")]
    public float bossVidaPorMinuto = 0.06f;
    [Tooltip("Aumento de DANO do boss por minuto. 0.03 = +3%/min (~×1.9 aos 30min).")]
    public float bossDanoPorMinuto = 0.03f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Minutos de jogo da run (tempo escalado — para no pause/level-up).
    static float Minutos() => Time.timeSinceLevelLoad / 60f;

    // Intensidade da dificuldade escolhida na fase: dif 1 => x1.0 ... dif 5 => x1.6.
    static float Intensidade()
    {
        int dif = Mathf.Clamp(PlayerPrefs.GetInt("Dificuldade", 1), 1, 5);
        return 1f + 0.15f * (dif - 1);
    }

    public static float VidaMult()
    {
        var i = Get();
        return (i != null && i.ativo) ? 1f + i.vidaPorMinuto * Intensidade() * Minutos() : 1f;
    }

    public static float DanoMult()
    {
        var i = Get();
        return (i != null && i.ativo) ? 1f + i.danoPorMinuto * Intensidade() * Minutos() : 1f;
    }

    public static float BossVidaMult()
    {
        var i = Get();
        return (i != null && i.ativo) ? 1f + i.bossVidaPorMinuto * Intensidade() * Minutos() : 1f;
    }

    public static float BossDanoMult()
    {
        var i = Get();
        return (i != null && i.ativo) ? 1f + i.bossDanoPorMinuto * Intensidade() * Minutos() : 1f;
    }

    static EnemyScaling Get()
    {
        if (Instance == null)
        {
            var existente = FindFirstObjectByType<EnemyScaling>();
            if (existente != null) Instance = existente;
            else Instance = new GameObject("EnemyScaling").AddComponent<EnemyScaling>();
        }
        return Instance;
    }
}
