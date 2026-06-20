# Pausa Coordenada Co-op Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tornar a pausa de gameplay (escolhas de skill/carta/evolução/elemento + menu de pausa) host-autoritativa em co-op, congelando horda+timer para todos até todos terminarem, e fazer o game over do grupo voltar ao lobby.

**Architecture:** Um `CoopPauseManager : NetworkBehaviour` (host) mantém quem está segurando a pausa e sincroniza um `NetworkVariable<bool> pausado` que dirige `Time.timeScale` em todos os clientes (o NGO não é gated por timeScale, então RPCs/NetworkVariables seguem fluindo). Uma fachada estática `CoopPause` dual-mode: em rede roteia pro host, em single-player faz `Time.timeScale` direto (comportamento atual intocado). As 5 UIs trocam `Time.timeScale=0/1` por chamadas à fachada.

**Tech Stack:** Unity 6 (6000.3.9f1), Netcode for GameObjects 2.12, C#. Verificação via MCP For Unity (`refresh_unity`, `read_console`, `execute_code`); não há framework de testes unitários em uso — a verificação é compilação 0-erros + checagem de comportamento single-player via `execute_code`.

**Spec:** `docs/superpowers/specs/2026-06-19-coop-pausa-coordenada-design.md`

**Branch:** continua em `feat/mp-lobby`.

**Convenção de verificação Unity (usada em todo o plano):**
- "Compilar e checar 0 erros" = `mcp__unity-mcp__refresh_unity` com `scope:"all"`, depois `mcp__unity-mcp__read_console` com `types:["error"]` → esperar lista vazia. Se houver erro de compilação, corrigir antes de seguir.
- Caminhos relativos ao `Assets/` quando usados em ferramentas Unity; caminhos de disco quando usados em `git`.

---

### Task 1: Fachada `CoopPause` + `CoopPauseManager`

Cria as duas peças centrais. O `CoopPauseManager` ainda não é spawnado (Task 2) nem usado pelas UIs (Task 3+), mas já compila e a fachada já funciona em single-player.

**Files:**
- Create: `horda/Assets/scripts/net/CoopPause.cs`
- Create: `horda/Assets/scripts/net/CoopPauseManager.cs`

- [ ] **Step 1: Criar `CoopPause.cs`**

Conteúdo exato do arquivo `horda/Assets/scripts/net/CoopPause.cs`:

```csharp
using Unity.Netcode;
using UnityEngine;

// Fachada de pausa dual-mode. Em single-player (sem rede) faz Time.timeScale
// direto (comportamento de hoje). Em co-op roteia pro host via CoopPauseManager,
// que sincroniza a pausa pra todos via NetworkVariable.
public static class CoopPause
{
    static bool EmRede
    {
        get
        {
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsListening && CoopPauseManager.Instance != null;
        }
    }

    // Escolha de gameplay (skill/carta/evolução/elemento): segura a pausa enquanto
    // este player estiver escolhendo; libera quando fecha. Pausa fica ativa enquanto
    // QUALQUER player estiver segurando — ou seja, só roda quando todos terminaram.
    public static void ReterEscolha()
    {
        if (EmRede) CoopPauseManager.Instance.ReterEscolhaServerRpc();
        else Time.timeScale = 0f;
    }

    public static void LiberarEscolha()
    {
        if (EmRede) CoopPauseManager.Instance.LiberarEscolhaServerRpc();
        else Time.timeScale = 1f;
    }

    // Menu de pausa do grupo: qualquer um abre (congela todos), qualquer um fecha.
    public static void AbrirMenu()
    {
        if (EmRede) CoopPauseManager.Instance.AbrirMenuServerRpc();
        else Time.timeScale = 0f;
    }

    public static void FecharMenu()
    {
        if (EmRede) CoopPauseManager.Instance.FecharMenuServerRpc();
        else Time.timeScale = 1f;
    }
}
```

- [ ] **Step 2: Criar `CoopPauseManager.cs`**

Conteúdo exato do arquivo `horda/Assets/scripts/net/CoopPauseManager.cs`:

```csharp
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Vive na fase co-op (spawnado pelo host). Centraliza a pausa do grupo: qualquer
// escolha (skill/carta/evolução/elemento) ou o menu de pausa congela a horda + timer
// pra TODOS via Time.timeScale, dirigido por NetworkVariable host-autoritativo.
public class CoopPauseManager : NetworkBehaviour
{
    public static CoopPauseManager Instance { get; private set; }

    const ulong SemDono = ulong.MaxValue;

    // Estado só-no-host.
    readonly HashSet<ulong> retentoresEscolha = new HashSet<ulong>();
    bool menuAberto;

    // Sincronizado pra todos.
    public readonly NetworkVariable<bool> pausado = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // clientId de quem abriu o menu de pausa (SemDono = nenhum menu aberto).
    public readonly NetworkVariable<ulong> donoMenu = new NetworkVariable<ulong>(
        SemDono, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        Instance = this;
        pausado.OnValueChanged += AoMudarPausa;
        AplicarTimeScale(pausado.Value); // cliente que entra no meio respeita a pausa vigente
        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback += AoDesconectar;
    }

    public override void OnNetworkDespawn()
    {
        pausado.OnValueChanged -= AoMudarPausa;
        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback -= AoDesconectar;
        if (Instance == this) Instance = null;
        Time.timeScale = 1f; // não deixar a próxima cena congelada
    }

    void AoMudarPausa(bool _, bool novo) => AplicarTimeScale(novo);
    void AplicarTimeScale(bool p) => Time.timeScale = p ? 0f : 1f;

    [Rpc(SendTo.Server)]
    public void ReterEscolhaServerRpc(RpcParams rpc = default)
    {
        retentoresEscolha.Add(rpc.Receive.SenderClientId);
        Recomputar();
    }

    [Rpc(SendTo.Server)]
    public void LiberarEscolhaServerRpc(RpcParams rpc = default)
    {
        retentoresEscolha.Remove(rpc.Receive.SenderClientId);
        Recomputar();
    }

    [Rpc(SendTo.Server)]
    public void AbrirMenuServerRpc(RpcParams rpc = default)
    {
        menuAberto = true;
        donoMenu.Value = rpc.Receive.SenderClientId;
        Recomputar();
    }

    [Rpc(SendTo.Server)]
    public void FecharMenuServerRpc()
    {
        menuAberto = false;
        donoMenu.Value = SemDono;
        Recomputar();
    }

    void AoDesconectar(ulong id)
    {
        retentoresEscolha.Remove(id);
        if (menuAberto && donoMenu.Value == id) { menuAberto = false; donoMenu.Value = SemDono; }
        Recomputar();
    }

    void Recomputar()
    {
        pausado.Value = retentoresEscolha.Count > 0 || menuAberto;
    }

    // Overlay simples (IMGUI, como o lobby) pro player que NÃO abriu o menu.
    void OnGUI()
    {
        if (!pausado.Value) return;
        ulong dono = donoMenu.Value;
        if (dono == SemDono) return;                                   // pausa por escolha: sem overlay
        if (NetworkManager != null && dono == NetworkManager.LocalClientId) return; // eu mesmo abri
        var style = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        GUI.Box(new Rect(Screen.width / 2f - 200f, 40f, 400f, 50f), NomeDe(dono) + " pausou o jogo", style);
    }

    static string NomeDe(ulong clientId)
    {
        for (int i = 0; i < PlayerStats.All.Count; i++)
        {
            var pn = PlayerStats.All[i] != null ? PlayerStats.All[i].GetComponent<PlayerNet>() : null;
            if (pn != null && pn.OwnerClientId == clientId) return pn.Nome;
        }
        return "Jogador " + (clientId + 1);
    }
}
```

- [ ] **Step 3: Compilar e checar 0 erros**

`refresh_unity scope:"all"` → `read_console types:["error"]`. Esperado: nenhum erro. (Em caso de erro de API NGO em `RpcParams`/`SenderClientId`, confirmar `using Unity.Netcode;` e a assinatura `RpcParams rpc = default`.)

- [ ] **Step 4: Verificar fachada single-player via `execute_code`**

Com nenhuma sessão de rede ativa (Editor parado), rodar `mcp__unity-mcp__execute_code` com o corpo:

```csharp
UnityEngine.Time.timeScale = 1f;
CoopPause.ReterEscolha();
float aposReter = UnityEngine.Time.timeScale;
CoopPause.LiberarEscolha();
float aposLiberar = UnityEngine.Time.timeScale;
CoopPause.AbrirMenu();
float aposAbrir = UnityEngine.Time.timeScale;
CoopPause.FecharMenu();
float aposFechar = UnityEngine.Time.timeScale;
return $"reter={aposReter} liberar={aposLiberar} abrir={aposAbrir} fechar={aposFechar}";
```

Esperado: `reter=0 liberar=1 abrir=0 fechar=1` (sem rede, `EmRede` é false e a fachada faz `Time.timeScale` direto).

- [ ] **Step 5: Commit**

```bash
git add "horda/Assets/scripts/net/CoopPause.cs" "horda/Assets/scripts/net/CoopPauseManager.cs"
git commit -m "feat(mp): CoopPause (fachada dual-mode) + CoopPauseManager (host-autoritativo)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

(Os arquivos `.meta` gerados pelo Unity podem ser incluídos no `git add` se aparecerem como untracked: `git add horda/Assets/scripts/net/CoopPause.cs.meta horda/Assets/scripts/net/CoopPauseManager.cs.meta`.)

---

### Task 2: Prefab do manager + registro + spawn pelo host

Cria o prefab `CoopPauseManager`, registra na lista de network prefabs, e faz o host spawná-lo ao carregar a fase (via `FaseCoopBootstrap`, espelhando `LobbyBootstrap`/`LobbyManager`).

**Files:**
- Create: `horda/Assets/prefebs/net/CoopPauseManager.prefab` (via Unity)
- Modify: `horda/Assets/DefaultNetworkPrefabs.asset` (registro)
- Modify: `horda/Assets/scripts/net/FaseCoopBootstrap.cs`
- Modify: `horda/Assets/Scenes/primeira_fase_mp.unity` (atribuir o prefab no campo do FaseCoopBootstrap)

- [ ] **Step 1: Criar o prefab via `execute_code`**

Rodar `mcp__unity-mcp__execute_code` (cria um GameObject com `NetworkObject` + `CoopPauseManager`, salva como prefab, destrói o temporário):

```csharp
var go = new UnityEngine.GameObject("CoopPauseManager");
go.AddComponent<Unity.Netcode.NetworkObject>();
go.AddComponent<CoopPauseManager>();
string dir = "Assets/prefebs/net";
if (!UnityEditor.AssetDatabase.IsValidFolder(dir))
    UnityEditor.AssetDatabase.CreateFolder("Assets/prefebs", "net");
var prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(go, dir + "/CoopPauseManager.prefab");
UnityEngine.Object.DestroyImmediate(go);
UnityEditor.AssetDatabase.SaveAssets();
return prefab != null ? "prefab criado: " + UnityEditor.AssetDatabase.GetAssetPath(prefab) : "FALHOU";
```

Esperado: `prefab criado: Assets/prefebs/net/CoopPauseManager.prefab`.

- [ ] **Step 2: Registrar o prefab em `DefaultNetworkPrefabs.asset` via `execute_code`**

```csharp
var lista = UnityEditor.AssetDatabase.LoadAssetAtPath<Unity.Netcode.NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/prefebs/net/CoopPauseManager.prefab");
if (lista == null || prefab == null) return "FALHOU: lista ou prefab nulo";
bool jaTem = false;
foreach (var p in lista.PrefabList) { if (p != null && p.Prefab == prefab) { jaTem = true; break; } }
if (!jaTem) lista.Add(new Unity.Netcode.NetworkPrefab { Prefab = prefab });
UnityEditor.EditorUtility.SetDirty(lista);
UnityEditor.AssetDatabase.SaveAssets();
return "registrado=" + (!jaTem) + " total=" + lista.PrefabList.Count;
```

Esperado: `registrado=True total=35` (era 34 após o LobbyManager). Se rodar duas vezes, `registrado=False` e total estável — idempotente.

- [ ] **Step 3: Modificar `FaseCoopBootstrap.cs` — campo do prefab + spawn no host**

Substituir o conteúdo inteiro de `horda/Assets/scripts/net/FaseCoopBootstrap.cs` por:

```csharp
using Unity.Netcode;
using UnityEngine;

// Na fase co-op (carregada pelo lobby): desliga o estado de lobby, spawna o
// CoopPauseManager (host) e reposiciona os players spawnados em pontos separados.
public class FaseCoopBootstrap : MonoBehaviour
{
    public GameObject coopPauseManagerPrefab;

    void Start()
    {
        LobbyState.EmLobby = false;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return; // só o host

        // Spawna o coordenador de pausa do grupo (uma vez).
        if (coopPauseManagerPrefab != null && CoopPauseManager.Instance == null)
        {
            var go = Instantiate(coopPauseManagerPrefab);
            var no = go.GetComponent<NetworkObject>();
            if (no != null) no.Spawn();
        }

        // Reposiciona os players (NetworkTransform replica).
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

- [ ] **Step 4: Compilar e checar 0 erros**

`refresh_unity scope:"all"` → `read_console types:["error"]`. Esperado: nenhum erro.

- [ ] **Step 5: Atribuir o prefab no `FaseCoopBootstrap` da cena `primeira_fase_mp`**

Abrir/garantir a cena carregada e atribuir o campo via `execute_code`:

```csharp
var cena = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/primeira_fase_mp.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var boot = UnityEngine.Object.FindFirstObjectByType<FaseCoopBootstrap>();
if (boot == null) return "FALHOU: FaseCoopBootstrap não encontrado na cena";
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/prefebs/net/CoopPauseManager.prefab");
boot.coopPauseManagerPrefab = prefab;
UnityEditor.EditorUtility.SetDirty(boot);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(cena);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(cena);
return "atribuído=" + (boot.coopPauseManagerPrefab != null);
```

Esperado: `atribuído=True`.

- [ ] **Step 6: Commit**

```bash
git add "horda/Assets/prefebs/net/CoopPauseManager.prefab" "horda/Assets/prefebs/net/CoopPauseManager.prefab.meta" "horda/Assets/DefaultNetworkPrefabs.asset" "horda/Assets/scripts/net/FaseCoopBootstrap.cs" "horda/Assets/Scenes/primeira_fase_mp.unity"
git commit -m "feat(mp): spawn do CoopPauseManager pelo host na fase co-op

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Migrar as 4 UIs de escolha para `CoopPause`

Troca mecânica: onde a UI zera `Time.timeScale` ao abrir, chama `CoopPause.ReterEscolha()`; onde restaura, chama `CoopPause.LiberarEscolha()`. Em single-player a fachada faz exatamente o `timeScale` de antes (`0`/`1`). As UIs mantêm seu `previousTimeScale`/`AudioListener.pause` locais.

**Files:**
- Modify: `horda/Assets/scripts/UI/Skill_choice_UI.cs:781-782, 791`
- Modify: `horda/Assets/scripts/UI/StatusCardChoiceUI.cs:552-553, 560`
- Modify: `horda/Assets/scripts/SkillEvolution/SkillEvolutionUI.cs:44, 152`
- Modify: `horda/Assets/scripts/UI/ElementApplicationUI.cs:69, 401`

- [ ] **Step 1: `Skill_choice_UI.cs` — PauseGame/ResumeGame**

Em `PauseGame()` (linhas 780-783), trocar:

```csharp
            // Guarda 1 se o jogo já estava pausado por outra UI, para não travar ao fechar
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            AudioListener.pause = true;
```

por:

```csharp
            // Guarda 1 se o jogo já estava pausado por outra UI, para não travar ao fechar
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            CoopPause.ReterEscolha(); // SP: timeScale=0; co-op: pausa o grupo via host
            AudioListener.pause = true;
```

Em `ResumeGame()` (linha 791), trocar `Time.timeScale = previousTimeScale;` por:

```csharp
            CoopPause.LiberarEscolha(); // SP: timeScale=1; co-op: libera a pausa do grupo
```

- [ ] **Step 2: `StatusCardChoiceUI.cs` — PauseGame/ResumeGame**

Em `PauseGame()` (linhas 552-553), trocar:

```csharp
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
```

por:

```csharp
        previousTimeScale = Time.timeScale;
        CoopPause.ReterEscolha();
        AudioListener.pause = true;
```

Em `ResumeGame()` (linha 560), trocar `Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;` por:

```csharp
        CoopPause.LiberarEscolha();
```

- [ ] **Step 3: `SkillEvolutionUI.cs` — abrir/fechar**

Na corrotina `Mostrar` (linha 44), trocar `Time.timeScale = 0f;` por:

```csharp
        CoopPause.ReterEscolha();
```

No fechamento (linha 152, dentro do bloco que tem `WaitForSecondsRealtime(0.35f)`), trocar `Time.timeScale      = 1f;` por:

```csharp
        CoopPause.LiberarEscolha();
```

- [ ] **Step 4: `ElementApplicationUI.cs` — Abrir/Fechar**

Em `Abrir(...)` (linha 69), trocar `Time.timeScale = 0f;` por:

```csharp
        CoopPause.ReterEscolha();
```

Em `Fechar()` (linha 401), trocar `Time.timeScale = 1f;` por:

```csharp
        CoopPause.LiberarEscolha();
```

- [ ] **Step 5: Compilar e checar 0 erros**

`refresh_unity scope:"all"` → `read_console types:["error"]`. Esperado: nenhum erro.

- [ ] **Step 6: Verificar single-player intocado via `execute_code`**

Com Editor parado (sem rede), confirmar que as UIs ainda zeram/restauram o timeScale. Como as UIs dependem de cena, validamos o invariante diretamente pela fachada (mesmo caminho que as UIs agora usam):

```csharp
UnityEngine.Time.timeScale = 1f;
CoopPause.ReterEscolha();   float a = UnityEngine.Time.timeScale; // esperado 0
CoopPause.LiberarEscolha(); float b = UnityEngine.Time.timeScale; // esperado 1
return $"escolha_pausa={a} escolha_libera={b}";
```

Esperado: `escolha_pausa=0 escolha_libera=1`.

- [ ] **Step 7: Commit**

```bash
git add "horda/Assets/scripts/UI/Skill_choice_UI.cs" "horda/Assets/scripts/UI/StatusCardChoiceUI.cs" "horda/Assets/scripts/SkillEvolution/SkillEvolutionUI.cs" "horda/Assets/scripts/UI/ElementApplicationUI.cs"
git commit -m "feat(mp): escolhas (skill/carta/evolucao/elemento) usam CoopPause (pausa coordenada)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Migrar o menu de pausa (`PauseManager`)

O `PauseManager` passa a usar `CoopPause.AbrirMenu()`/`FecharMenu()`. Em co-op, abrir o menu congela todos (com overlay "Fulano pausou" pros outros) e qualquer um retoma; em SP é o `timeScale` de hoje.

**Files:**
- Modify: `horda/Assets/scripts/configuração _adicional/PauseManager.cs:435-436, 452`

- [ ] **Step 1: `PauseGame` (linhas 434-436)**

Trocar:

```csharp
        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
```

por:

```csharp
        isPaused = true;
        previousTimeScale = Time.timeScale;
        CoopPause.AbrirMenu(); // SP: timeScale=0; co-op: pausa o grupo (qualquer um retoma)
```

- [ ] **Step 2: `ResumeGame` (linha 452)**

Trocar `Time.timeScale = previousTimeScale;` por:

```csharp
        CoopPause.FecharMenu();
```

> Nota: as linhas 185, 695, 824 (`Time.timeScale = 1f;` em fluxos de sair/menu/cena) NÃO mudam — são transições de cena onde o estado de rede já não vale; mantê-las como estão evita regressão no fluxo de "sair pro menu".

- [ ] **Step 3: Compilar e checar 0 erros**

`refresh_unity scope:"all"` → `read_console types:["error"]`. Esperado: nenhum erro.

- [ ] **Step 4: Verificar single-player via `execute_code`**

```csharp
UnityEngine.Time.timeScale = 1f;
CoopPause.AbrirMenu();  float a = UnityEngine.Time.timeScale; // esperado 0
CoopPause.FecharMenu(); float b = UnityEngine.Time.timeScale; // esperado 1
return $"menu_pausa={a} menu_libera={b}";
```

Esperado: `menu_pausa=0 menu_libera=1`.

- [ ] **Step 5: Commit**

```bash
git add "horda/Assets/scripts/configuração _adicional/PauseManager.cs"
git commit -m "feat(mp): menu de pausa usa CoopPause (pausa coordenada do grupo)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Game over do grupo → volta ao lobby

Quando todos caem (host detecta em `MonitorarHost`), além de mostrar a tela de game over, o host espera ~4s (tempo real, pois `timeScale=0`) e devolve todos ao `lobby_mp`. Guard estático evita múltiplos `LoadScene` (várias instâncias `PlayerNet` rodam `MonitorarHost` no host).

**Files:**
- Modify: `horda/Assets/scripts/net/PlayerNet.cs:29` (campo guard), `:107` (rearme no lobby), `:147` (hook), + nova corrotina

- [ ] **Step 1: Adicionar o guard estático**

Em `PlayerNet.cs`, logo após a linha 29 (`bool gameOverDisparado;`), adicionar:

```csharp
    static bool voltandoAoLobby; // evita múltiplos LoadScene ao game over do grupo
```

- [ ] **Step 2: Rearmar o guard enquanto está no lobby**

Os players são persistentes (DontDestroyOnLoad), então `OnNetworkSpawn` dispara só uma vez — não serve pra rearmar entre runs. Em vez disso, rearma no `MonitorarHost()` (host-only, chamado de `Update`) enquanto o estado de lobby está ativo. No `MonitorarHost()` (linha 107), logo após a `{` de abertura (antes do `// Revive:`), adicionar:

```csharp
        // Enquanto no lobby: sem monitoramento de fase e rearma a volta ao lobby pro próximo run.
        if (LobbyState.EmLobby) { voltandoAoLobby = false; return; }
```

Resultado (contexto):

```csharp
    void MonitorarHost()
    {
        // Enquanto no lobby: sem monitoramento de fase e rearma a volta ao lobby pro próximo run.
        if (LobbyState.EmLobby) { voltandoAoLobby = false; return; }

        // Revive: se EU estou caído e há companheiro vivo no raio, enche a barra.
        if (downed.Value)
```

- [ ] **Step 3: Disparar a corrotina no game over do grupo**

Na linha 147, trocar:

```csharp
            if (todosCaidos) { gameOverDisparado = true; GameOverGrupoRpc(); }
```

por:

```csharp
            if (todosCaidos)
            {
                gameOverDisparado = true;
                GameOverGrupoRpc();
                if (!voltandoAoLobby) { voltandoAoLobby = true; StartCoroutine(VoltarAoLobby()); }
            }
```

- [ ] **Step 4: Adicionar a corrotina `VoltarAoLobby`**

Logo após o método `MonitorarHost()` (após a `}` que fecha o método, antes de `void AoMudarDowned`), adicionar:

```csharp
    // Game over do grupo em co-op: mostra a tela ~4s (tempo real, pois timeScale=0)
    // e o host devolve todos ao lobby. Sessão NGO continua viva → o grupo re-escolhe.
    System.Collections.IEnumerator VoltarAoLobby()
    {
        yield return new WaitForSecondsRealtime(4f);
        Time.timeScale = 1f; // não carregar o lobby congelado
        LobbyState.EmLobby = true;
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsServer)
            nm.SceneManager.LoadScene("lobby_mp", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
```

- [ ] **Step 5: Compilar e checar 0 erros**

`refresh_unity scope:"all"` → `read_console types:["error"]`. Esperado: nenhum erro. (Confirmar que `WaitForSecondsRealtime`, `System.Collections.IEnumerator` e `StartCoroutine` resolvem — `PlayerNet` é `NetworkBehaviour`/`MonoBehaviour`, então `StartCoroutine` existe.)

- [ ] **Step 6: Commit**

```bash
git add "horda/Assets/scripts/net/PlayerNet.cs"
git commit -m "feat(mp): game over do grupo devolve todos ao lobby (host, ~4s)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Verificação co-op manual (MPPM) + documentação

Fechamento: validação em 2 instâncias (o usuário roda, pois play-mode sem foco do editor congela via MCP) e documentação no Obsidian.

**Files:**
- (nenhum código novo)

- [ ] **Step 1: Roteiro de teste co-op (usuário roda via Multiplayer Play Mode, 2 instâncias)**

Pedir ao usuário para validar:
1. Host + cliente entram pelo lobby e iniciam `primeira_fase_mp`.
2. Subir um player de nível → a escolha de skill dele congela a horda e o timer **para os dois**; o outro fica parado até o primeiro escolher.
3. Forçar os dois a escolher ao mesmo tempo → só destrava quando **ambos** fecham.
4. Um aperta ESC (menu de pausa) → os dois congelam; o outro vê "Fulano pausou o jogo"; **qualquer um** retoma.
5. Derrubar os dois (game over do grupo) → tela de game over ~4s e **volta ao `lobby_mp`** com os dois ainda conectados.

- [ ] **Step 2: Documentar no Obsidian**

Atualizar a nota de Multiplayer no vault (`OneDrive\Documentos\Obsidian Vault\Projetos\Horda\`) com a seção da pausa coordenada: arquitetura (`CoopPauseManager` + `CoopPause`), regras (escolha = todos terminam; menu = qualquer um retoma), e game over → lobby. Ver `[[reference_obsidian_vault_horda]]`.

- [ ] **Step 3: Atualizar memória do projeto**

Atualizar `project_multiplayer_coop.md` (memória) marcando a pausa coordenada como concluída e registrando os itens da auditoria ainda pendentes (skills/projéteis em rede; efeito de início de fase).

- [ ] **Step 4: Finalizar a branch**

Anunciar e usar a skill `superpowers:finishing-a-development-branch` para verificar e decidir merge/push (checar FF antes de pushar na main — ver `[[feedback_push_direto_main_ff]]`).
