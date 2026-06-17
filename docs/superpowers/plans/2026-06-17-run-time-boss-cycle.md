# Tempo de Run, Boss Final e Ciclo Infinito — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development ou superpowers:executing-plans. Steps usam checkbox (`- [ ]`).

**Goal:** Run de ~30 min que pausa o countdown durante bosses, culmina no boss final (vitória → escolha Terminar/Infinito; morte → game over), com inimigos E bosses escalando por tempo e a dificuldade definindo a intensidade.

**Architecture:** Estende `EnemyScaling` (intensidade por dificuldade + multiplicadores de boss). Reformula `TimerManager` (30 min, pausa-em-boss, boss final, reinício de ciclo). Adiciona UI de escolha pós-vitória e tela de vitória. Sobrevivência começa já no infinito. Eventos já são random (`GerenciadorEventos`) — sem trabalho.

**Tech:** Unity C#. **Verificação:** este projeto não tem harness de testes — verificar por **compilar via `mcp__unity-mcp__refresh_unity` (compile=request, scope=scripts) + `read_console` (types=["error"], esperado 0)**; lógica pura via `mcp__unity-mcp__execute_code` (corpo de método C# 6, sem `using`/funções locais, nomes qualificados); fluxo via Play mode manual. **Requer Unity MCP conectado.**

**Spec:** `docs/superpowers/specs/2026-06-17-run-time-boss-cycle-design.md`

> Não usar `--no-verify`. Commitar a cada task. Na `main`: criar branch `feat/run-cycle` antes da Task 1.

---

## Mapa de arquivos

| Arquivo | Responsabilidade | Ação |
|---|---|---|
| `scripts/EnemyScaling.cs` | multiplicadores de escala | Modificar (intensidade + boss) |
| `scripts/UI/EscolherTerrenoUI.cs` | seleção de fase | Modificar (grava `Dificuldade`) |
| `scripts/controlei_inimigo.cs` | InimigoController | Modificar (escala boss no spawn; flag boss) |
| `scripts/BossController.cs` | dano de boss | Modificar (escala dano no Start) |
| `scripts/TimerManager.cs` | timer da run | Modificar (30min, pausa-boss, boss final, ciclo) |
| `scripts/UI/EscolhaPosVitoriaUI.cs` | escolha pós-vitória | Criar |
| `scripts/UI/VitoriaUI.cs` | tela de vitória | Criar |
| `Resources/Localization/GameStrings.asset` | i18n | Modificar (chaves novas) |

---

## Task 0: Branch
- [ ] `cd "j:/unity/projetos/horda/orda" && git switch -c feat/run-cycle`

---

## Task 1: EnemyScaling — intensidade por dificuldade + multiplicadores de boss

**Files:** Modify `horda/Assets/scripts/EnemyScaling.cs`

- [ ] **Step 1:** Adicionar campos de boss e a intensidade por dificuldade. Substituir o corpo da classe por:

```csharp
    [Header("Escala por tempo (linear, sem teto)")]
    [Tooltip("Liga/desliga a escala de vida/dano dos inimigos por tempo.")]
    public bool ativo = true;
    [Tooltip("Aumento de VIDA do inimigo comum por minuto. 0.12 = +12%/min.")]
    public float vidaPorMinuto = 0.12f;
    [Tooltip("Aumento de DANO do inimigo comum por minuto. 0.06 = +6%/min.")]
    public float danoPorMinuto = 0.06f;
    [Header("Escala dos BOSSES (mais suave)")]
    [Tooltip("Aumento de VIDA do boss por minuto. 0.06 = +6%/min (~×2.8 aos 30min).")]
    public float bossVidaPorMinuto = 0.06f;
    [Tooltip("Aumento de DANO do boss por minuto. 0.03 = +3%/min (~×1.9 aos 30min).")]
    public float bossDanoPorMinuto = 0.03f;
```

E o cálculo passa a aplicar a **intensidade da dificuldade** (lida do PlayerPrefs gravado pela seleção de fase):

```csharp
    static float Minutos() => Time.timeSinceLevelLoad / 60f;

    // intensidade: dif 1 => x1.0 ... dif 5 => x1.6
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
```

(Manter `Awake` e `Get()` como estão.)

- [ ] **Step 2:** Compilar (`refresh_unity` scope=scripts → `read_console` types=["error"], esperado 0).
- [ ] **Step 3:** Verificar em edit-mode:

```csharp
PlayerPrefs.SetInt("Dificuldade", 5);
float warm = EnemyScaling.VidaMult();
EnemyScaling.Instance.vidaPorMinuto = 0.12f; EnemyScaling.Instance.bossVidaPorMinuto = 0.06f;
float t = Time.timeSinceLevelLoad/60f; float inten = 1f + 0.15f*4f;
PlayerPrefs.SetInt("Dificuldade", 1);
return "dif5 intensidade=" + inten + " | VidaMult(dif1 t≈0)=" + EnemyScaling.VidaMult() + " | BossVidaMult=" + EnemyScaling.BossVidaMult();
```
Esperado: intensidade 1.6; mults ~1.0 em t≈0.

- [ ] **Step 4:** Commit `feat(run-cycle): EnemyScaling com intensidade por dificuldade + mults de boss`.

---

## Task 2: Propagar a dificuldade na seleção de fase

**Files:** Modify `horda/Assets/scripts/UI/EscolherTerrenoUI.cs`

- [ ] **Step 1:** No `btn.onClick` que grava `ProximaCena` (dentro de `if (fase.desbloqueada)`), gravar também a dificuldade. Localizar:

```csharp
                Time.timeScale = 1f;
                PlayerPrefs.SetString("ProximaCena", cena);
                SceneManager.LoadScene("loading_screen");
```
e inserir antes do `SceneManager.LoadScene`:
```csharp
                PlayerPrefs.SetInt("Dificuldade", fase.dificuldade);
```
(Capturar `fase.dificuldade` numa local, como `cena`, para o closure: adicionar `int dif = fase.dificuldade;` ao lado de `string cena = fase.nomeCena;` e usar `dif`.)

- [ ] **Step 2:** Compilar. 0 erros.
- [ ] **Step 3:** Commit `feat(run-cycle): EscolherTerreno grava Dificuldade no PlayerPrefs`.

---

## Task 3: Boss escala vida (no spawn) + flag de boss

**Files:** Modify `horda/Assets/scripts/controlei_inimigo.cs`

- [ ] **Step 1:** No `InicializarComData`, no branch de boss (`dadosInimigo == null`), aplicar a escala de vida do boss. Substituir:

```csharp
        if (dadosInimigo == null)
        {
            // Boss customizado: vida já definida externamente via Awake
            if (vidaMaxima <= 0f) vidaMaxima = 100f;
            if (vidaAtual  <= 0f) vidaAtual  = vidaMaxima;
            return;
        }
```
por:
```csharp
        if (dadosInimigo == null)
        {
            // Boss customizado: vida já definida externamente via Awake
            if (vidaMaxima <= 0f) vidaMaxima = 100f;
            // Escala de boss por tempo (aplicada no spawn, sobre a vida-base já setada)
            vidaMaxima *= EnemyScaling.BossVidaMult();
            vidaAtual  = vidaMaxima;
            return;
        }
```

- [ ] **Step 2:** Adicionar um helper público pra saber se este controller é um boss (usado pelo TimerManager na Task 5). Adicionar à classe:
```csharp
    public bool EhBoss() => GetComponent<BossController>() != null
                         || GetComponentInParent<BossController>() != null;
```

- [ ] **Step 3:** Compilar. 0 erros.

> ⚠️ Detalhe de timing a confirmar na execução: garantir que o boss seta `vidaMaxima` ANTES de `InicializarComData` rodar. Se algum boss setar a vida no próprio `Start` (ordem incerta vs `InimigoController.Start`), mover a multiplicação pra onde o boss seta a vida, OU usar `Awake` no boss. Validar com 1 boss em Play mode (vida final ≈ vidaBase × BossVidaMult).

- [ ] **Step 4:** Commit `feat(run-cycle): boss escala vida no spawn + helper EhBoss`.

---

## Task 4: Boss escala dano

**Files:** Modify `horda/Assets/scripts/BossController.cs` (e checar bosses específicos)

- [ ] **Step 1:** No `BossController`, multiplicar os campos de dano por `EnemyScaling.BossDanoMult()` uma vez no início (Start/Awake, após os campos serializados). Adicionar no começo do método de inicialização do BossController (localizar onde ele referencia `controller = GetComponent<InimigoController>()` ou no `Start`/`Awake`), inserir:

```csharp
        float md = EnemyScaling.BossDanoMult();
        danoProjetil        *= md;
        danoProjetilEspecial*= md;
        danoRaio            *= md;
```
(Confirmar os nomes exatos dos campos de dano lendo o arquivo; aplicar a todos os campos de dano existentes do BossController.)

- [ ] **Step 2:** Checar bosses específicos com dano próprio (`BossCaveira`, `BossPrincesa`, `BossGuarda`, `BossSlimeGuardaElite`): se tiverem campos de dano que NÃO passam pelo `BossController`, multiplicar igual por `EnemyScaling.BossDanoMult()` no setup deles. (Grep por `dano` em cada; aplicar onde houver dano de ataque do boss.)

- [ ] **Step 3:** Compilar. 0 erros.
- [ ] **Step 4:** Commit `feat(run-cycle): boss escala dano no spawn`.

---

## Task 5: TimerManager — 30min, pausa em boss, boss final, ciclo

**Files:** Modify `horda/Assets/scripts/TimerManager.cs`

- [ ] **Step 1:** `levelDuration` default → `1800f`. E adicionar flag `ehFinal` ao `BossEvent`:
```csharp
[System.Serializable]
public class BossEvent
{
    public string bossName;
    [Range(0f, 1f)] public float triggerTime;
    public GameObject bossPrefab;
    public Vector2 spawnPosition;
    public bool ehFinal = false;   // boss final da fase
}
```

- [ ] **Step 2:** Pausar o countdown enquanto houver boss vivo. Adicionar campo de controle e helper:
```csharp
    GameObject bossFinalInstancia;
    bool finalInvocado = false;

    bool HaBossVivo()
    {
        foreach (var ic in FindObjectsByType<InimigoController>(FindObjectsSortMode.None))
            if (ic != null && !ic.estaMorrendo && ic.EhBoss()) return true;
        return false;
    }
```
No `Update`, **não** decrementar se boss vivo:
```csharp
    void Update()
    {
        if (!isRunning) return;
        if (!HaBossVivo())            // countdown pausa durante boss
        {
            currentTime -= Time.deltaTime;
            UpdateTimeBar();
            CheckEvents();
            CheckBossEvents();
            if (currentTime <= 0 && !finalInvocado) InvocarBossFinal();
        }
        else
        {
            VerificarBossFinalMorto();
        }
    }
```

- [ ] **Step 3:** `TriggerBossEvent` guarda a instância do boss final; `InvocarBossFinal` força o boss marcado `ehFinal`; `VerificarBossFinalMorto` dispara a escolha:
```csharp
    void TriggerBossEvent(BossEvent bossEvent)
    {
        GameObject inst = null;
        if (bossEvent.bossPrefab != null)
            inst = Instantiate(bossEvent.bossPrefab, bossEvent.spawnPosition, Quaternion.identity);
        if (bossEvent.ehFinal) { bossFinalInstancia = inst; finalInvocado = true; }
        OnBossSpawn?.Invoke(bossEvent.bossName);
    }

    void InvocarBossFinal()
    {
        finalInvocado = true;
        // acha o BossEvent marcado ehFinal e invoca (se ainda não saiu na timeline)
        foreach (var be in bossEvents)
            if (be.ehFinal) { TriggerBossEvent(be); return; }
        // sem boss final marcado: encerra como vitória direta
        AbrirEscolhaVitoria();
    }

    void VerificarBossFinalMorto()
    {
        if (bossFinalInstancia == null) return;
        var ic = bossFinalInstancia.GetComponent<InimigoController>();
        if (ic == null || ic.estaMorrendo)
        {
            bossFinalInstancia = null;
            AbrirEscolhaVitoria();
        }
    }

    void AbrirEscolhaVitoria()
    {
        isRunning = false;
        EscolhaPosVitoriaUI.Mostrar(this);  // Task 6
    }
```
Remover/ajustar o antigo `if (currentTime <= 0) TimeUp();` (substituído por `InvocarBossFinal`). Manter `OnTimeUp` opcional (pode disparar junto de InvocarBossFinal pra UI mostrar "boss final").

- [ ] **Step 4:** API de reinício de ciclo (modo infinito) — **não** mexe no player:
```csharp
    public void ReiniciarCiclo()
    {
        currentTime = levelDuration;
        currentEventIndex = 0;
        currentBossIndex = 0;
        finalInvocado = false;
        bossFinalInstancia = null;
        Array.Sort(timedEvents, (a, b) => b.triggerTime.CompareTo(a.triggerTime));
        Array.Sort(bossEvents,  (a, b) => b.triggerTime.CompareTo(a.triggerTime));
        isRunning = true;
    }

    // Sobrevivência começa já no infinito: sem boss final, sem escolha — só roda.
    public void ModoInfinitoDireto() { ReiniciarCiclo(); }
```

- [ ] **Step 5:** Compilar. 0 erros (vai falhar até a Task 6 existir — então: criar a Task 6 antes de compilar, ou stubar `EscolhaPosVitoriaUI.Mostrar`). **Ordem:** fazer Task 6 junto.
- [ ] **Step 6:** Commit `feat(run-cycle): TimerManager 30min + pausa em boss + boss final + ciclo`.

---

## Task 6: UI de escolha pós-vitória

**Files:** Create `horda/Assets/scripts/UI/EscolhaPosVitoriaUI.cs`

- [ ] **Step 1:** Criar a UI (estilo dark-fantasy, padrão de criação por código como `GameOverUI`/`ElementApplicationUI`). Estrutura mínima:
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EscolhaPosVitoriaUI : MonoBehaviour
{
    TimerManager timer;

    public static void Mostrar(TimerManager tm)
    {
        var go = new GameObject("EscolhaPosVitoriaUI");
        var ui = go.AddComponent<EscolhaPosVitoriaUI>();
        ui.timer = tm;
        ui.Construir();
        Time.timeScale = 0f;
    }

    void Construir()
    {
        // Canvas overlay + painel + título Loc.T("ui.victory_title")
        // Botão A: Loc.T("ui.end_run")   -> Terminar()
        // Botão B: Loc.T("ui.continue_infinite") -> ContinuarInfinito()
        // (usar o mesmo padrão visual dos outros painéis criados por código)
    }

    void Terminar()
    {
        Time.timeScale = 1f;
        Destroy(gameObject);
        VitoriaUI.Mostrar();   // Task 7
    }

    void ContinuarInfinito()
    {
        Time.timeScale = 1f;
        Destroy(gameObject);
        if (timer != null) timer.ReiniciarCiclo();
    }
}
```
(Implementar o `Construir()` completo com Canvas/painel/botões seguindo o estilo de `GameOverUI.cs` — ler esse arquivo como referência de criação por código.)

- [ ] **Step 2:** Compilar (junto da Task 5). 0 erros.
- [ ] **Step 3:** Commit `feat(run-cycle): UI de escolha pos-vitoria (Terminar/Infinito)`.

---

## Task 7: Tela de vitória

**Files:** Create `horda/Assets/scripts/UI/VitoriaUI.cs`

- [ ] **Step 1:** Criar tela simples (espelhar `GameOverUI.cs`, trocando textos/cor): título `Loc.T("ui.victory")`, botão "Menu" que volta pro menu (`SceneManager.LoadScene` do menu — usar o mesmo destino que o GameOver usa pra "menu"). Pausa o jogo enquanto aberta.
- [ ] **Step 2:** Compilar. 0 erros.
- [ ] **Step 3:** Commit `feat(run-cycle): tela de vitoria`.

---

## Task 8: Sobrevivência começa no infinito

**Files:** decidir o gancho (componente na cena `Modo_sobrevivencia` ou checagem por nome de cena no TimerManager)

- [ ] **Step 1:** No `TimerManager.Start`/`InitializeTimer`, se a cena for a de sobrevivência (ex.: `SceneManager.GetActiveScene().name == "Modo_sobrevivencia"`), não armar boss final (sem `ehFinal`) — só roda o ciclo continuamente (`ReiniciarCiclo` ao zerar, sem escolha). Implementar um flag `modoSobrevivencia` (auto-detectado por nome de cena ou serializado) que, ao `currentTime <= 0`, chama `ReiniciarCiclo()` em vez de `InvocarBossFinal()`.
- [ ] **Step 2:** Compilar. 0 erros.
- [ ] **Step 3:** Commit `feat(run-cycle): sobrevivencia inicia no modo infinito`.

---

## Task 9: Localização das strings novas

**Files:** Modify `horda/Assets/Resources/Localization/GameStrings.asset`

- [ ] **Step 1:** Adicionar chaves (× 14 idiomas, mesmo padrão dos outros): `ui.victory_title` ("Fase Vencida!"), `ui.end_run` ("Terminar Run"), `ui.continue_infinite` ("Modo Infinito"), `ui.victory` ("Vitória!"). (Inserir antes de `- key: difficulty.easy`, com aspas onde houver acento — o Edit normaliza o escape.)
- [ ] **Step 2:** Verificar via execute_code (`data.Get("ui.end_run", Language.EN)` etc. resolvem).
- [ ] **Step 3:** Commit `feat(run-cycle): localizacao das telas de vitoria/escolha`.

---

## Task 10: Ajuste das cenas + integração final

- [ ] **Step 1:** Em cada fase numerada (primeira/segunda/terceira_fase), marcar no `TimerManager` da cena o `BossEvent` final com `ehFinal = true` e `triggerTime ≈ 0`, e setar `levelDuration = 1800` se a cena sobrescrever. (Edição de cena via Unity/execute_code.)
- [ ] **Step 2:** Smoke test em Play mode: run avança; boss aparece → countdown pausa; ao matar boss → resume; no fim → boss final → matar → escolha; "Infinito" reinicia mantendo build; Sobrevivência roda em loop; dificuldade da fase altera a intensidade.
- [ ] **Step 3:** Compilar limpo + console sem erros. Finalizar branch (merge na `main` com aprovação).

---

## Notas / riscos
- **Timing da escala de boss** (Task 3): confirmar ordem de set de vida. Maior risco.
- **Dano de boss espalhado** (Task 4): pode estar em vários scripts; aplicar em todos.
- **Detecção de boss vivo** (Task 5): `FindObjectsByType` por frame é ok (poucos inimigos com BossController). Se pesar, cachear.
- **`OnTimeUp` legado**: hoje só mostra texto; agora o fim real é o boss final. Manter o texto como aviso "Boss Final!" se quiser.
