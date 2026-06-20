# Multiplayer Co-op — Lobby Completo — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development ou superpowers:executing-plans. Passos usam checkbox (`- [ ]`).

**Goal:** Jogadores conectam num lobby por join code, escolhem personagem/passiva/ultimate, o host escolhe a fase e inicia → o NGO carrega a fase com todos juntos. Single-player intocado.

**Architecture:** NetworkManager persistente (DontDestroyOnLoad) na cena `lobby_mp`; o `NetworkPlayer` spawna no lobby e serve de roster (`charIndex`/`ready`/`nome`). `LobbyManager` (host) carrega a fase via `NetworkManager.SceneManager.LoadScene`. Gameplay congelado no lobby via `LobbyState.EmLobby`. Fases co-op perdem o NetworkManager próprio.

**Tech Stack:** Unity 6, NGO 2.12, RPCs universais.

**Spec:** `docs/superpowers/specs/2026-06-19-multiplayer-lobby-coop-design.md`

---

## Verificação (não-TDD)

Cada tarefa de código: `refresh_unity` (scope=all) → `read_console` (types=["error"]) = **0 erros**. Aceite final (Task 8): marco co-op no MPPM + regressão SP.

---

## File Structure

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/scripts/net/LobbyState.cs` | Criar | Estado estático `EmLobby` |
| `Assets/scripts/net/LobbyManager.cs` | Criar | `NetworkBehaviour`: faseEscolhida + IniciarServerRpc (scene-load) |
| `Assets/scripts/UI/LobbyCoopUI.cs` | Criar | UI: código, roster, seleção, fase, ready, iniciar (tema UIDark) |
| `Assets/scripts/net/PlayerNet.cs` | Modificar | + `ready`/`playerName` NetworkVariables + setters |
| `Assets/scripts/player_stats.cs`, `moviment_player2.cs` | Modificar | Gate por `LobbyState.EmLobby` |
| `Assets/scripts/UI/MenuInicialUI.cs` | Modificar | "Criar/Entrar sala" → `lobby_mp` |
| `Assets/Scenes/lobby_mp.unity` | Criar | NetworkManager persistente + LobbyManager + LobbyCoopUI |
| `Assets/Scenes/primeira_fase_mp.unity` | Modificar | Remover ConnectUI + NetworkManager próprio |

---

## Task 1: LobbyState + PlayerNet (ready/nome)

**Files:** Create `Assets/scripts/net/LobbyState.cs`; Modify `Assets/scripts/net/PlayerNet.cs`

- [ ] **Step 1: Branch + LobbyState**

```bash
cd "j:/unity/projetos/horda/orda"
git checkout -b feat/mp-lobby
```

`Assets/scripts/net/LobbyState.cs`:

```csharp
// Estado global: true enquanto estamos na cena de lobby (gameplay congelado).
// Setado true pela LobbyCoopUI no lobby; false quando a fase carrega.
public static class LobbyState
{
    public static bool EmLobby = false;
}
```

- [ ] **Step 2: PlayerNet — ready/playerName + setters**

Em `PlayerNet`, adicionar (junto dos NetworkVariables):

```csharp
    readonly NetworkVariable<bool> ready = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<Unity.Collections.FixedString32Bytes> playerName =
        new NetworkVariable<Unity.Collections.FixedString32Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool Pronto => ready.Value;
    public string Nome => playerName.Value.ToString();
    public int CharIndexLobby => charIndex.Value;

    public void SetPronto(bool v) { if (IsOwner) ready.Value = v; }
    public void SetChar(int idx) { if (IsOwner) charIndex.Value = idx; }
```

> `charIndex` já existe (private/readonly). Garantir que está acessível: se for `readonly` campo, `SetChar` escreve `.Value` (ok). No `OnNetworkSpawn`, o dono define `playerName` default:

No bloco `if (IsOwner)` do `OnNetworkSpawn`, adicionar:

```csharp
            playerName.Value = new Unity.Collections.FixedString32Bytes("Jogador " + (OwnerClientId + 1));
```

- [ ] **Step 3: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/net/LobbyState.cs Assets/scripts/net/LobbyState.cs.meta Assets/scripts/net/PlayerNet.cs
git commit -m "feat(mp-lobby): LobbyState + PlayerNet ready/nome/setters

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2: Gate de gameplay no lobby

**Files:** Modify `Assets/scripts/player_stats.cs`, `Assets/scripts/moviment_player2.cs`

- [ ] **Step 1: player_stats.Update**

Logo após `if (EstaCaido) return;` no início de `Update()`:

```csharp
        if (LobbyState.EmLobby) return; // congelado no lobby
```

E no início de `TakeDamage`, após `if (EstaCaido) return;`:

```csharp
        if (LobbyState.EmLobby) return; // sem dano no lobby
```

- [ ] **Step 2: moviment_player2**

Em `Update()` e `FixedUpdate()`, após `if (playerStats.EstaCaido) ...`:

```csharp
        if (LobbyState.EmLobby) return;
```

(no FixedUpdate, zerar velocidade: `if (LobbyState.EmLobby) { rb.linearVelocity = Vector2.zero; return; }`)

- [ ] **Step 3: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/player_stats.cs Assets/scripts/moviment_player2.cs
git commit -m "feat(mp-lobby): gameplay congelado no lobby (LobbyState.EmLobby)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3: LobbyManager (host carrega a fase)

**Files:** Create `Assets/scripts/net/LobbyManager.cs`

- [ ] **Step 1: Criar**

```csharp
using Unity.Netcode;
using UnityEngine;

// Vive no lobby (spawnado pelo host). Sincroniza a fase escolhida e dispara
// o scene-load NGO quando o host inicia.
public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // Nomes das fases co-op disponíveis (ordem = índice).
    public static readonly string[] Fases = { "primeira_fase_mp" };

    public readonly NetworkVariable<int> faseEscolhida = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn() { Instance = this; }
    public override void OnNetworkDespawn() { if (Instance == this) Instance = null; }

    [Rpc(SendTo.Server)]
    public void EscolherFaseServerRpc(int idx)
    {
        if (idx >= 0 && idx < Fases.Length) faseEscolhida.Value = idx;
    }

    [Rpc(SendTo.Server)]
    public void IniciarServerRpc()
    {
        if (!TodosProntos()) return;
        LobbyState.EmLobby = false; // host sai do estado de lobby
        string fase = Fases[Mathf.Clamp(faseEscolhida.Value, 0, Fases.Length - 1)];
        NetworkManager.SceneManager.LoadScene(fase, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public bool TodosProntos()
    {
        if (PlayerStats.All.Count == 0) return false;
        for (int i = 0; i < PlayerStats.All.Count; i++)
        {
            var pn = PlayerStats.All[i] != null ? PlayerStats.All[i].GetComponent<PlayerNet>() : null;
            if (pn == null || !pn.Pronto) return false;
        }
        return true;
    }
}
```

> Os clientes saem do `EmLobby` quando a cena da fase carrega (Task 7, callback de scene-load).

- [ ] **Step 2: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/net/LobbyManager.cs Assets/scripts/net/LobbyManager.cs.meta
git commit -m "feat(mp-lobby): LobbyManager (faseEscolhida + IniciarServerRpc scene-load)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4: LobbyCoopUI

**Files:** Create `Assets/scripts/UI/LobbyCoopUI.cs`

- [ ] **Step 1: Criar a UI**

UI funcional com tema `UIDark`. Campos públicos: `CharacterData[] characters` (atribuído na cena). Conecta (host/cliente) usando `NetBootstrap`/`RelayConnector`; mostra código (host) ou campo de código (cliente); lista o roster (lendo `PlayerStats.All`); permite escolher personagem (grava PlayerPrefs + `PlayerNet.SetChar`); host escolhe fase e inicia; cliente dá ready.

`Assets/scripts/UI/LobbyCoopUI.cs`:

```csharp
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// UI do lobby co-op (IMGUI funcional; tema simples). Conecta via Relay,
// mostra roster dos players spawnados, seleção de personagem, fase e ready.
public class LobbyCoopUI : MonoBehaviour
{
    public CharacterData[] characters;

    bool souHost;
    string joinCode = "";
    string status = "";
    int charSel;

    void Start()
    {
        LobbyState.EmLobby = true;
        souHost = PlayerPrefs.GetInt("LobbyHost", 1) == 1;
        charSel = PlayerPrefs.GetInt("SelectedCharacter", 0);
        Conectar();
    }

    async void Conectar()
    {
        status = "Conectando...";
        try
        {
            await NetBootstrap.InitAsync();
            if (souHost)
            {
                joinCode = await RelayConnector.HostAsync(4);
                status = "Sala criada. Código: " + joinCode;
            }
            else
            {
                joinCode = PlayerPrefs.GetString("LobbyCode", "");
                status = "Digite o código e clique Entrar.";
            }
        }
        catch (System.Exception e) { status = "Falha: " + e.Message; }
    }

    async void Entrar()
    {
        try { await RelayConnector.JoinAsync(joinCode.Trim()); status = "Conectado."; }
        catch (System.Exception e) { status = "Falha ao entrar: " + e.Message; }
    }

    PlayerNet Meu()
    {
        var l = PlayerStats.Local;
        return l != null ? l.GetComponent<PlayerNet>() : null;
    }

    void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        GUI.Box(new Rect(10, 10, 460, 620), "LOBBY CO-OP");
        float y = 40;
        GUI.Label(new Rect(20, y, 440, 24), status); y += 28;

        // cliente: campo de código + entrar (antes de conectar)
        if (!souHost && nm != null && !nm.IsConnectedClient && !nm.IsListening)
        {
            GUI.Label(new Rect(20, y, 70, 24), "Código:");
            joinCode = GUI.TextField(new Rect(90, y, 200, 24), joinCode);
            if (GUI.Button(new Rect(300, y - 2, 120, 28), "Entrar")) Entrar();
            y += 34;
        }
        if (souHost)
        {
            if (GUI.Button(new Rect(20, y, 200, 26), "Copiar código: " + joinCode))
                GUIUtility.systemCopyBuffer = joinCode;
            y += 34;
        }

        // roster
        GUI.Label(new Rect(20, y, 440, 22), "JOGADORES:"); y += 26;
        for (int i = 0; i < PlayerStats.All.Count; i++)
        {
            var pn = PlayerStats.All[i] != null ? PlayerStats.All[i].GetComponent<PlayerNet>() : null;
            if (pn == null) continue;
            string nome = pn.Nome;
            string perso = (characters != null && pn.CharIndexLobby >= 0 && pn.CharIndexLobby < characters.Length && characters[pn.CharIndexLobby] != null)
                ? characters[pn.CharIndexLobby].characterName : ("#" + pn.CharIndexLobby);
            GUI.Label(new Rect(30, y, 440, 22), (pn.Pronto ? "[PRONTO] " : "[ ] ") + nome + " — " + perso);
            y += 24;
        }
        y += 10;

        // seleção de personagem (escreve PlayerPrefs + charIndex)
        GUI.Label(new Rect(20, y, 440, 22), "PERSONAGEM:"); y += 26;
        if (characters != null)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                bool sel = i == charSel;
                if (GUI.Button(new Rect(30 + (i % 4) * 105, y + (i / 4) * 30, 100, 26),
                    (sel ? "> " : "") + (characters[i] != null ? characters[i].characterName : "?")))
                {
                    charSel = i;
                    PlayerPrefs.SetInt("SelectedCharacter", i);
                    Meu()?.SetChar(i);
                }
            }
            y += ((characters.Length + 3) / 4) * 30 + 10;
        }

        // host: fase + iniciar
        if (souHost && LobbyManager.Instance != null)
        {
            GUI.Label(new Rect(20, y, 440, 22), "FASE:"); y += 26;
            for (int i = 0; i < LobbyManager.Fases.Length; i++)
            {
                bool sel = LobbyManager.Instance.faseEscolhida.Value == i;
                if (GUI.Button(new Rect(30 + i * 160, y, 150, 26), (sel ? "> " : "") + LobbyManager.Fases[i]))
                    LobbyManager.Instance.EscolherFaseServerRpc(i);
            }
            y += 36;
            GUI.enabled = LobbyManager.Instance.TodosProntos();
            if (GUI.Button(new Rect(20, y, 200, 32), "INICIAR")) LobbyManager.Instance.IniciarServerRpc();
            GUI.enabled = true;
        }

        // ready (todos)
        var meu = Meu();
        if (meu != null)
        {
            if (GUI.Button(new Rect(240, y, 160, 32), meu.Pronto ? "CANCELAR PRONTO" : "PRONTO"))
                meu.SetPronto(!meu.Pronto);
        }
        y += 40;

        if (GUI.Button(new Rect(20, y, 120, 28), "Sair"))
        {
            if (nm != null) nm.Shutdown();
            LobbyState.EmLobby = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("menu_inicial");
        }
    }
}
```

> IMGUI funcional (rápido e robusto pro plumbing). O repolimento com `UIDark`/uGUI é um passo cosmético posterior.

- [ ] **Step 2: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/UI/LobbyCoopUI.cs Assets/scripts/UI/LobbyCoopUI.cs.meta
git commit -m "feat(mp-lobby): LobbyCoopUI (código/roster/seleção/fase/ready/iniciar)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5: Cena `lobby_mp`

**Files:** Create `Assets/Scenes/lobby_mp.unity`

- [ ] **Step 1: Criar a cena (via MCP `execute_code`)**

```csharp
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
    UnityEditor.SceneManagement.NewSceneMode.Single);

var camGO = new UnityEngine.GameObject("Main Camera");
camGO.tag = "MainCamera";
var cam = camGO.AddComponent<UnityEngine.Camera>();
cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
cam.backgroundColor = new UnityEngine.Color(0.05f, 0.03f, 0.07f);
camGO.transform.position = new UnityEngine.Vector3(0, 0, -10);

var netGO = new UnityEngine.GameObject("NetManager");
var nm = netGO.AddComponent<Unity.Netcode.NetworkManager>();
var utp = netGO.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
if (nm.NetworkConfig == null) nm.NetworkConfig = new Unity.Netcode.NetworkConfig();
nm.NetworkConfig.NetworkTransport = utp;
nm.NetworkConfig.TickRate = 60;
nm.NetworkConfig.PlayerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/prefebs/net/NetworkPlayer.prefab");

// LobbyManager como network prefab spawnado pelo host: para simplificar,
// cria um prefab LobbyManager e registra. (Ver Step 2.)

var uiGO = new UnityEngine.GameObject("LobbyUI");
var ui = uiGO.AddComponent<LobbyCoopUI>();
// atribui todos os CharacterData
var chs = new System.Collections.Generic.List<CharacterData>();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:CharacterData"))
    chs.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)));
ui.characters = chs.ToArray();

UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Scenes/lobby_mp.unity");
return "lobby_mp criada | personagens=" + ui.characters.Length;
```

- [ ] **Step 2: LobbyManager prefab + spawn no host**

Criar um prefab `Assets/prefebs/net/LobbyManager.prefab` com `NetworkObject` + `LobbyManager`, registrá-lo na Default Network Prefabs List, e fazer o host spawná-lo ao iniciar. Forma simples: um pequeno componente `LobbyBootstrap` na cena que, em `NetworkManager.OnServerStarted`, instancia+spawna o LobbyManager. (Implementar `LobbyBootstrap` como parte deste passo: `if (NetworkManager.Singleton.IsServer) { var go = Instantiate(lobbyManagerPrefab); go.GetComponent<NetworkObject>().Spawn(); }` no callback `OnServerStarted`.) Registrar o prefab via o gerador de Default Network Prefabs (Tools do NGO) ou adicioná-lo manualmente.

- [ ] **Step 3: Registrar `lobby_mp` no Build Settings + 0 erros**

`refresh_unity` (scope=all) → `read_console` (0 erros). Adicionar `lobby_mp` ao EditorBuildSettings.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scenes/lobby_mp.unity Assets/Scenes/lobby_mp.unity.meta "Assets/prefebs/net" ProjectSettings/EditorBuildSettings.asset Assets/scripts/net
git commit -m "feat(mp-lobby): cena lobby_mp (NetworkManager persistente + LobbyManager + UI)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6: Menu → lobby_mp

**Files:** Modify `Assets/scripts/UI/MenuInicialUI.cs`

- [ ] **Step 1: Apontar os botões pro lobby_mp**

Trocar nos handlers de "Criar sala"/"Entrar sala" o `SceneManager.LoadScene("lobby")` por `SceneManager.LoadScene("lobby_mp")`, mantendo `PlayerPrefs.SetInt("LobbyHost", 1/0)`. Para "Entrar sala", se houver um campo de código no menu, gravar `PlayerPrefs.SetString("LobbyCode", textoDigitado)` antes de carregar (senão o cliente digita no lobby).

```csharp
// Criar sala:
PlayerPrefs.SetInt("LobbyHost", 1);
SceneManager.LoadScene("lobby_mp");
// Entrar sala:
PlayerPrefs.SetInt("LobbyHost", 0);
SceneManager.LoadScene("lobby_mp");
```

(Remover o `LobbyCode` "SPIRIT-..." local — o código real vem do Relay no lobby.)

- [ ] **Step 2: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/UI/MenuInicialUI.cs
git commit -m "feat(mp-lobby): menu Criar/Entrar sala -> lobby_mp

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 7: Handoff lobby→fase + limpar primeira_fase_mp

**Files:** Create `Assets/scripts/net/FaseCoopBootstrap.cs`; Modify `Assets/Scenes/primeira_fase_mp.unity`

- [ ] **Step 1: Sair do EmLobby + reposicionar na fase**

`Assets/scripts/net/FaseCoopBootstrap.cs` — componente na fase co-op que, no `Start`, marca fim do lobby e reposiciona os players:

```csharp
using Unity.Netcode;
using UnityEngine;

// Na fase co-op (carregada pelo lobby): desliga o estado de lobby e
// reposiciona os players spawnados em spawn points.
public class FaseCoopBootstrap : MonoBehaviour
{
    void Start()
    {
        LobbyState.EmLobby = false;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return; // só o host reposiciona (NetworkTransform replica)
        int i = 0;
        foreach (var ps in PlayerStats.All)
        {
            if (ps == null) continue;
            ps.transform.position = new Vector3((i - 0.5f) * 4f, 0f, ps.transform.position.z);
            i++;
        }
    }
}
```

- [ ] **Step 2: Limpar primeira_fase_mp (via MCP)**

Abrir `primeira_fase_mp`; remover o `NetManager` (NetworkManager) e o `ConnectUI`; adicionar um GameObject com `FaseCoopBootstrap`. Salvar.

```csharp
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/primeira_fase_mp.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var nm = UnityEngine.Object.FindFirstObjectByType<Unity.Netcode.NetworkManager>();
if (nm != null) UnityEngine.Object.DestroyImmediate(nm.gameObject);
var ui = UnityEngine.Object.FindFirstObjectByType<SandboxConnectUI>();
if (ui != null) UnityEngine.Object.DestroyImmediate(ui.gameObject);
if (UnityEngine.Object.FindFirstObjectByType<FaseCoopBootstrap>() == null)
    new UnityEngine.GameObject("FaseCoopBootstrap").AddComponent<FaseCoopBootstrap>();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
return "primeira_fase_mp limpa";
```

- [ ] **Step 3: Compilar + 0 erros**

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/net/FaseCoopBootstrap.cs Assets/scripts/net/FaseCoopBootstrap.cs.meta Assets/Scenes/primeira_fase_mp.unity
git commit -m "feat(mp-lobby): FaseCoopBootstrap (sai do lobby + reposiciona) + limpa primeira_fase_mp

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 8: Validar marco + regressão SP

**Files:** nenhum.

- [ ] **Step 1: Co-op (MPPM)**

2 players. Player 1 (principal): menu → "Criar sala" → entra no `lobby_mp`, copia o código. Player 2 (virtual): menu → "Entrar sala" → cola o código → Entrar. Confirmar (spec §5):
- [ ] Roster mostra os 2 (nome + personagem + ready) ao vivo
- [ ] Cada um escolhe personagem (aparece no roster do outro)
- [ ] Host escolhe fase; ambos dão Pronto; host clica Iniciar
- [ ] Os dois **carregam a fase juntos** e spawnam com seu personagem
- [ ] No lobby ninguém se move; na fase o gameplay liga (combate do SP2 funciona)
- [ ] `read_console` (errors) = 0

- [ ] **Step 2: Regressão single-player (OBRIGATÓRIO)**

Menu → "Start" → `CharacterSelection` → fase: joga normal, sem lobby. 0 erros novos.

- [ ] **Step 3: Finalizar branch**

Checar FF (`git fetch` + `git merge-base --is-ancestor origin/main main`). Se FF: merge + push. Se divergiu: rebase. NUNCA force-push. `superpowers:finishing-a-development-branch`.

---

## Self-Review (autor do plano)

- **Cobertura da spec:** §6.1 NetworkManager persistente/player-roster → Tasks 5/1; §6.2 ready/nome → Task 1; §6.3 LobbyManager → Task 3; §6.4 UI → Task 4; §6.5 gate EmLobby → Tasks 1/2/7; §6.6 menu → Task 6; §6.7 scene-load+handoff → Tasks 3/7; §6.8 limpar fase → Task 7; marco+regressão → Task 8.
- **Placeholders:** código completo nos arquivos focados; passos de cena/prefab descrevem a ação MCP exata. (LobbyManager prefab + spawn no host: Task 5 Step 2 descreve o `LobbyBootstrap`/`OnServerStarted` — implementar junto.)
- **Consistência de tipos:** `LobbyState.EmLobby`, `PlayerNet.{Pronto,Nome,CharIndexLobby,SetPronto,SetChar}`, `LobbyManager.{Fases,faseEscolhida,EscolherFaseServerRpc,IniciarServerRpc,TodosProntos,Instance}`, `LobbyCoopUI.characters`, `FaseCoopBootstrap` usados de forma consistente.
- **Nota:** maior sub-projeto; testar incrementalmente. O ponto mais incerto é o spawn do `LobbyManager` no host + registro do prefab (Task 5 Step 2) e o handoff de cena (Task 7) — validar com atenção no MPPM.
```
