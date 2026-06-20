# Multiplayer Co-op — Integração numa Fase Real (`primeira_fase_mp`) — Plano

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development ou superpowers:executing-plans. Passos usam checkbox (`- [ ]`).

**Goal:** Uma cópia `primeira_fase_mp` jogável em co-op (2 players, terreno/luz/HUD/horda reais) com todo o combat loop do SP2, sem tocar na `primeira_fase` single-player.

**Architecture:** Cópia da cena sem player fixo; NetworkManager spawna `NetworkPlayer` por conexão; HUD (`UIManege`) segue `PlayerStats.Local`; câmera por cliente via `PlayerCameraFollow`. Spawner/eventos já host-only (SP2a).

**Tech Stack:** Unity 6, NGO 2.12.

**Spec:** `docs/superpowers/specs/2026-06-19-multiplayer-coop-real-fase-integration-design.md`

---

## Verificação (não-TDD)

Cada tarefa: `refresh_unity` (scope=all) → `read_console` (types=["error"]) = **0 erros**. Aceite final (Task 3): marco co-op no MPPM + regressão SP na `primeira_fase`.

---

## File Structure

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/scripts/UI/UIManege.cs` | Modificar | HUD segue `PlayerStats.Local` |
| `Assets/Scenes/primeira_fase_mp.unity` | Criar | Cópia co-op: sem player fixo, NetworkManager+ConnectUI+câmera |

---

## Task 1: HUD segue o player local

**Files:** Modify `Assets/scripts/UI/UIManege.cs`

- [ ] **Step 1: Migrar as duas buscas**

Em `Assets/scripts/UI/UIManege.cs`, trocar as duas ocorrências de `FindAnyObjectByType<PlayerStats>()` (linhas ~144 e ~179) por `PlayerStats.Local`:

- Linha ~144: `playerStats = FindAnyObjectByType<PlayerStats>();` → `playerStats = PlayerStats.Local;`
- Linha ~179: `if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();` → `if (playerStats == null) playerStats = PlayerStats.Local;`

> Em SP, `PlayerStats.Local` é setado no `Awake` do player (SP1), então o HUD acha o player normalmente. Em co-op, segue o player local de cada cliente. O retry (linha ~179) cobre o caso do player spawnar depois da conexão.

- [ ] **Step 2: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 3: Commit**

```bash
cd "j:/unity/projetos/horda/orda"
git checkout -b feat/mp-coop-real-fase
git add horda/Assets/scripts/UI/UIManege.cs
git commit -m "feat(mp): UIManege (HUD) segue PlayerStats.Local

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2: Criar `primeira_fase_mp`

**Files:** Create `Assets/Scenes/primeira_fase_mp.unity`

> Trabalho de editor — fazer via Unity MCP (`execute_code`).

- [ ] **Step 1: Duplicar a cena e abrir**

```csharp
UnityEditor.AssetDatabase.CopyAsset(
    "Assets/Scenes/primeira_fase.unity",
    "Assets/Scenes/primeira_fase_mp.unity");
UnityEditor.AssetDatabase.Refresh();
var sc = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
    "Assets/Scenes/primeira_fase_mp.unity",
    UnityEditor.SceneManagement.OpenSceneMode.Single);
```

- [ ] **Step 2: Remover o player fixo**

```csharp
var pj = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
if (pj != null) UnityEngine.Object.DestroyImmediate(pj.gameObject);
```

- [ ] **Step 3: NetworkManager + UnityTransport + PlayerPrefab + prefabs de inimigo**

```csharp
var netGO = new UnityEngine.GameObject("NetManager");
var nm = netGO.AddComponent<Unity.Netcode.NetworkManager>();
var utp = netGO.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
if (nm.NetworkConfig == null) nm.NetworkConfig = new Unity.Netcode.NetworkConfig();
nm.NetworkConfig.NetworkTransport = utp;
nm.NetworkConfig.TickRate = 60;
var player = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/prefebs/net/NetworkPlayer.prefab");
nm.NetworkConfig.PlayerPrefab = player;

// registra os prefabs de inimigo usados pelo spawner (todos têm NetworkObject - SP2a)
var sp = UnityEngine.Object.FindFirstObjectByType<EnemySpawnerCompleto>();
System.Action<UnityEngine.GameObject> reg = (pf) => {
    if (pf == null || nm.NetworkConfig.Prefabs == null) return;
    bool tem = false;
    foreach (var np in nm.NetworkConfig.Prefabs.Prefabs) if (np != null && np.Prefab == pf) { tem = true; break; }
    if (!tem && pf.GetComponent<Unity.Netcode.NetworkObject>() != null)
        nm.NetworkConfig.Prefabs.Add(new Unity.Netcode.NetworkPrefab { Prefab = pf });
};
if (sp != null && sp.tiposInimigos != null)
    foreach (var t in sp.tiposInimigos) reg(t.prefab);
```

- [ ] **Step 4: ConnectUI + PlayerCameraFollow**

```csharp
var uiGO = new UnityEngine.GameObject("ConnectUI");
uiGO.AddComponent<SandboxConnectUI>();

var cam = UnityEngine.Camera.main;
if (cam != null && cam.GetComponent<PlayerCameraFollow>() == null)
    cam.gameObject.AddComponent<PlayerCameraFollow>();
```

- [ ] **Step 5: Salvar a cena + registrar no Build Settings**

```csharp
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sc);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);

string path = "Assets/Scenes/primeira_fase_mp.unity";
var lst = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
bool achou = false; foreach (var s in lst) if (s.path == path) { achou = true; break; }
if (!achou) { lst.Add(new UnityEditor.EditorBuildSettingsScene(path, true)); UnityEditor.EditorBuildSettings.scenes = lst.ToArray(); }
UnityEditor.AssetDatabase.SaveAssets();
```

- [ ] **Step 6: Verificar 0 erros + estado da cena**

`refresh_unity` (scope=all) → `read_console` (0 erros). Conferir (via `execute_code`): `NetManager` com `NetworkManager`+`UnityTransport`, PlayerPrefab=NetworkPlayer, Prefabs de inimigo registrados (>0), `ConnectUI` presente, Main Camera com `PlayerCameraFollow`, **sem** PlayerStats fixo na cena.

- [ ] **Step 7: Commit**

```bash
git add horda/Assets/Scenes/primeira_fase_mp.unity horda/Assets/Scenes/primeira_fase_mp.unity.meta horda/ProjectSettings/EditorBuildSettings.asset
git commit -m "feat(mp): primeira_fase_mp (cópia co-op: sem player fixo, NetworkManager+ConnectUI+câmera)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3: Validar marco + regressão SP

**Files:** nenhum.

- [ ] **Step 1: Co-op (MPPM)**

2 players, `primeira_fase_mp`, Host + Join. Confirmar (spec §4):
- [ ] Os 2 players aparecem no **terreno real**, iluminados, com **câmera** e **HUD** próprios
- [ ] A **horda real** spawna no host e persegue nas duas telas
- [ ] Inimigos **causam dano** (HUD cai); ataques matam inimigos
- [ ] A 0 HP → **downed** → **revive** pela barra → **todos caídos = game over**
- [ ] `read_console` (errors) = 0

- [ ] **Step 2: Regressão single-player (OBRIGATÓRIO)**

`primeira_fase` em Play: joga igual a antes (player fixo, horda, dano, morte). 0 erros novos.

- [ ] **Step 3: Finalizar branch**

Checar FF (`git fetch` + `git merge-base --is-ancestor origin/main main`). Se FF: merge + push. Se divergiu: rebase sobre origin/main (descartar ruído NGO/TMP). NUNCA force-push. `superpowers:finishing-a-development-branch`.

---

## Self-Review (autor do plano)

- **Cobertura da spec:** §5.1 cópia/NetworkManager/ConnectUI/câmera → Task 2; §5.2 HUD → Task 1; §5.3 managers (já prontos do SP1/2a) — sem trabalho novo; §5.4 câmera → Task 2; marco+regressão → Task 3. Progressão/timer/lobby: fora (SP3/4/5).
- **Placeholders:** nenhum; os passos MCP têm o código exato.
- **Consistência de tipos:** `PlayerStats.Local`, `NetworkPlayer.prefab`, `SandboxConnectUI`, `PlayerCameraFollow`, `EnemySpawnerCompleto.tiposInimigos` usados de forma consistente (todos já existem dos SPs anteriores).
