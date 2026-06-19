# Multiplayer Co-op — Sub-projeto 1: Jogador em Rede — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development ou superpowers:executing-plans para implementar tarefa-a-tarefa. Passos usam checkbox (`- [ ]`).

**Goal:** Dois jogadores reais spawnam numa fase de teste, cada um controla o próprio personagem (movimento/dash/ataque), com posição e animação sincronizadas e câmera por cliente — sem quebrar o single-player.

**Architecture:** Variante de rede `NetworkPlayer.prefab` (não toca no `player.prefab` base). Toda lógica NGO vive num componente novo `PlayerNet : NetworkBehaviour` presente **só na variante**; o `player_stats` (2087 linhas) continua `MonoBehaviour` e consulta ownership via interface `INetOwnership`. Gating dual-mode: sem rede → comporta como hoje; em rede → só o dono roda input/lógica, cópias remotas são fantoches movidos por `NetworkTransform`/`NetworkAnimator`.

**Tech Stack:** Unity 6, NGO 2.12, Unity Transport 2.6.

**Spec:** `docs/superpowers/specs/2026-06-18-multiplayer-sp1-player-network-design.md`

---

## Nota de verificação (não-TDD)

Spike de integração de rede + gameplay — sem testes unitários. Cada tarefa de código verifica por:
1. `refresh_unity` (compile=request, mode=force, scope=all, wait_for_ready=true).
2. `read_console` (types=["error"], count="20") → **0 erros**.

Aceite final (Task 7): marco co-op no Multiplayer Play Mode **+ regressão single-player** na `primeira_fase`.

> **Refinamento da spec:** a spec menciona `IsLocalAuthority` em `PlayerStats`. Implementação concreta: a propriedade fica em `PlayerStats` mas delega a um componente `PlayerNet` (via interface `INetOwnership`) que só existe na variante de rede. Isso evita tornar `PlayerStats` um `NetworkBehaviour` (que exigiria `NetworkObject` e quebraria o single-player).

---

## File Structure

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/scripts/net/INetOwnership.cs` | Criar | Interface: `bool IsNetworked { get; }`, `bool IsLocalOwner { get; }` |
| `Assets/scripts/net/PlayerNet.cs` | Criar | `NetworkBehaviour` (só na variante): char index sincronizado, ownership, ativa câmera |
| `Assets/scripts/net/PlayerCameraFollow.cs` | Criar | Câmera por cliente seguindo `PlayerStats.Local` |
| `Assets/scripts/player_stats.cs` | Modificar | `static Local`, `IsLocalAuthority`, gate `Update`, overload `ApplyCharacterData(int)` |
| `Assets/scripts/moviment_player2.cs` | Modificar | Gate `Update`/`FixedUpdate` por `IsLocalAuthority` |
| `Assets/scripts/SkillManager.cs` | Modificar | Trocar `FindFirstObjectByType<PlayerStats>()` por `PlayerStats.Local` |
| `Assets/prefebs/net/NetworkPlayer.prefab` | Criar | Variante de player.prefab + NetworkObject/NetworkTransform/NetworkAnimator/PlayerNet |
| `Assets/Scenes/mp_fase_teste.unity` | Criar | NetworkManager+UTP, chão, spawn points, ConnectUI, câmera, SkillManager |

---

## Task 1: Branch + interface de ownership + gating no player

**Files:**
- Create: `Assets/scripts/net/INetOwnership.cs`
- Modify: `Assets/scripts/player_stats.cs`
- Modify: `Assets/scripts/moviment_player2.cs`

- [ ] **Step 1: Branch**

```bash
cd "j:/unity/projetos/horda/orda"
git checkout -b feat/mp-sp1-player
```

- [ ] **Step 2: Criar a interface**

`Assets/scripts/net/INetOwnership.cs`:

```csharp
// Implementada pelo componente de rede do player (PlayerNet), presente só na
// variante NetworkPlayer. Permite o PlayerStats (MonoBehaviour puro) consultar
// ownership sem depender de tipos do NGO.
public interface INetOwnership
{
    bool IsNetworked { get; }   // true quando há NetworkObject spawnado
    bool IsLocalOwner { get; }  // true quando esta instância é do jogador local
}
```

- [ ] **Step 3: `PlayerStats` — registro Local + IsLocalAuthority**

Em `Assets/scripts/player_stats.cs`, adicionar campos/propriedades na classe (perto do topo, junto dos outros campos):

```csharp
    public static PlayerStats Local { get; private set; }

    // true em single-player (sem componente de rede) ou quando sou o dono.
    public bool IsLocalAuthority
    {
        get
        {
            var net = GetComponent<INetOwnership>();
            return net == null || !net.IsNetworked || net.IsLocalOwner;
        }
    }
```

No `Awake()` (ou `Start()`, onde já há inicialização), registrar o Local **apenas no caso single-player** (sem componente de rede); no co-op o `PlayerNet` registra:

```csharp
        if (GetComponent<INetOwnership>() == null)
            Local = this;   // single-player: este é o player local
```

- [ ] **Step 4: `PlayerStats.Update` — gate por autoridade local**

No início de `void Update()` (linha ~646), antes de `HandleMovement()`:

```csharp
        if (!IsLocalAuthority) return;
```

Assim, cópias remotas não rodam input/lógica de dono; em single-player roda tudo como hoje (`IsLocalAuthority` é `true`).

- [ ] **Step 5: `moviment_player2` — gate Update/FixedUpdate**

Em `Assets/scripts/moviment_player2.cs`, no início de `Update()` (após o guard `if (playerStats == null ...)`) e de `FixedUpdate()`:

```csharp
        if (playerStats != null && !playerStats.IsLocalAuthority) return;
```

- [ ] **Step 6: Verificar compilação**

`refresh_unity` (scope=all) → `read_console` (types=["error"]). Esperado: **0 erros**.

- [ ] **Step 7: Commit**

```bash
git add Assets/scripts/net/INetOwnership.cs Assets/scripts/net/INetOwnership.cs.meta \
        Assets/scripts/player_stats.cs Assets/scripts/moviment_player2.cs
git commit -m "feat(mp-sp1): INetOwnership + PlayerStats.Local/IsLocalAuthority + gate de input

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2: `ApplyCharacterData(int)` — aplicar personagem por índice

**Files:**
- Modify: `Assets/scripts/player_stats.cs`

Hoje `ApplyCharacterData()` lê `PlayerPrefs.GetInt("SelectedCharacter", 0)` internamente. Para o co-op, o índice do personagem do dono precisa ser sincronizado e aplicado nas cópias remotas — então é preciso poder aplicar um índice **explícito**.

- [ ] **Step 1: Extrair overload por índice**

Em `Assets/scripts/player_stats.cs`, refatorar `ApplyCharacterData()` (linha ~145): criar `public void ApplyCharacterData(int charIndex)` contendo a lógica atual, mas usando o parâmetro `charIndex` no lugar de **todas** as leituras de `PlayerPrefs.GetInt("SelectedCharacter", 0)` dentro do corpo (linhas ~150 e ~178). Manter o método sem-argumento como wrapper:

```csharp
    public void ApplyCharacterData()
    {
        ApplyCharacterData(PlayerPrefs.GetInt("SelectedCharacter", 0));
    }
```

> Não alterar a lógica interna além de trocar a origem do índice. O comportamento single-player fica idêntico (o wrapper lê o mesmo PlayerPrefs).

- [ ] **Step 2: Verificar compilação**

`refresh_unity` (scope=all) → `read_console` (types=["error"]). Esperado: **0 erros**.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/player_stats.cs
git commit -m "refactor(mp-sp1): ApplyCharacterData(int) por índice explícito (single-player intacto)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3: `PlayerNet` — componente de rede do player

**Files:**
- Create: `Assets/scripts/net/PlayerNet.cs`

- [ ] **Step 1: Criar o componente**

`Assets/scripts/net/PlayerNet.cs`:

```csharp
using Unity.Netcode;
using UnityEngine;

// Vive SÓ na variante NetworkPlayer. Isola o NGO do player_stats.
// - sincroniza o índice de personagem (dono escreve; todos aplicam)
// - registra PlayerStats.Local no dono
// - implementa INetOwnership pro gating dual-mode
[RequireComponent(typeof(PlayerStats))]
public class PlayerNet : NetworkBehaviour, INetOwnership
{
    readonly NetworkVariable<int> charIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    PlayerStats stats;

    public bool IsNetworked => IsSpawned;
    public bool IsLocalOwner => IsOwner;

    void Awake() { stats = GetComponent<PlayerStats>(); }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            PlayerStats.SetLocal(stats);
            charIndex.Value = PlayerPrefs.GetInt("SelectedCharacter", 0);
        }
        // Aplica o personagem correto em TODAS as cópias (dono e remotas).
        stats.ApplyCharacterData(charIndex.Value);

        // Reaplica se o valor chegar depois (ordem de sync).
        charIndex.OnValueChanged += (_, novo) => stats.ApplyCharacterData(novo);
    }

    public override void OnNetworkDespawn()
    {
        if (PlayerStats.Local == stats) PlayerStats.ClearLocal(stats);
    }
}
```

- [ ] **Step 2: `PlayerStats` — métodos SetLocal/ClearLocal**

Para o `PlayerNet` registrar/limpar o Local de forma controlada, adicionar em `player_stats.cs`:

```csharp
    public static void SetLocal(PlayerStats ps) { Local = ps; }
    public static void ClearLocal(PlayerStats ps) { if (Local == ps) Local = null; }
```

(O `Local`/`IsLocalAuthority` já foram adicionados na Task 1.)

- [ ] **Step 3: Verificar compilação**

`refresh_unity` (scope=all) → `read_console` (types=["error"]). Esperado: **0 erros**.

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/net/PlayerNet.cs Assets/scripts/net/PlayerNet.cs.meta Assets/scripts/player_stats.cs
git commit -m "feat(mp-sp1): PlayerNet (ownership + sync de personagem) + SetLocal/ClearLocal

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4: Câmera por cliente

**Files:**
- Create: `Assets/scripts/net/PlayerCameraFollow.cs`

- [ ] **Step 1: Criar o follow**

`Assets/scripts/net/PlayerCameraFollow.cs`:

```csharp
using UnityEngine;

// Na cena de teste de rede: a câmera segue o player LOCAL (PlayerStats.Local).
// Cada cliente tem sua própria câmera seguindo o próprio player.
public class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] float zOffset = -10f;

    void LateUpdate()
    {
        var local = PlayerStats.Local;
        if (local == null) return;
        var p = local.transform.position;
        transform.position = new Vector3(p.x, p.y, zOffset);
    }
}
```

- [ ] **Step 2: Verificar compilação**

`refresh_unity` (scope=all) → `read_console` (types=["error"]). Esperado: **0 erros**.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/net/PlayerCameraFollow.cs Assets/scripts/net/PlayerCameraFollow.cs.meta
git commit -m "feat(mp-sp1): PlayerCameraFollow (câmera por cliente segue PlayerStats.Local)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5: SkillManager usa o player local

**Files:**
- Modify: `Assets/scripts/SkillManager.cs`

Pro ataque básico/skills dispararem na cena de teste, o `SkillManager` precisa operar sobre o player **local**. Hoje ele acha o player via `FindFirstObjectByType<PlayerStats>()`.

- [ ] **Step 1: Trocar a busca pelo Local**

Em `Assets/scripts/SkillManager.cs`, substituir as chamadas `FindFirstObjectByType<PlayerStats>()` por `PlayerStats.Local`, com guarda de nulo (o Local pode ainda não ter spawnado): onde o código hoje faz algo como `var ps = FindFirstObjectByType<PlayerStats>();`, trocar por `var ps = PlayerStats.Local; if (ps == null) return;` no contexto apropriado (cada ocorrência mantém o fluxo existente, só muda a origem da referência).

> Em single-player, `PlayerStats.Local` é setado no `Awake` do player (Task 1), então o `SkillManager` continua achando o player normalmente — sem regressão.

- [ ] **Step 2: Verificar compilação**

`refresh_unity` (scope=all) → `read_console` (types=["error"]). Esperado: **0 erros**.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/SkillManager.cs
git commit -m "refactor(mp-sp1): SkillManager opera sobre PlayerStats.Local

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6: Variante `NetworkPlayer` + cena `mp_fase_teste`

**Files:**
- Create: `Assets/prefebs/net/NetworkPlayer.prefab`
- Create: `Assets/Scenes/mp_fase_teste.unity`

- [ ] **Step 1: Criar a variante NetworkPlayer**

Criar `Assets/prefebs/net/NetworkPlayer.prefab` como **Prefab Variant** de `Assets/prefebs/perssonagens/player.prefab` (botão direito no prefab base > Create > Prefab Variant, ou via `PrefabUtility`). Na variante, adicionar:
- `NetworkObject` (NGO)
- `NetworkTransform` (NGO) com **Authority Mode = Owner**; 2D: `SyncPositionZ=false`, `SyncRotAngleX=false`, `SyncRotAngleY=false`
- `NetworkAnimator` (NGO) com o campo Animator apontando para o `Animator` do player
- `PlayerNet` (script da Task 3)

Não remover/alterar componentes herdados.

- [ ] **Step 2: Criar a cena de teste**

Nova cena `Assets/Scenes/mp_fase_teste.unity` contendo:
- **Main Camera** (tag MainCamera, ortográfica, z=-10) + componente `PlayerCameraFollow`
- **Chão** (SpriteRenderer grande, sortingOrder negativo) como referência de movimento
- 2+ **spawn points** (Empty GameObjects) — opcional; senão o NGO usa posição padrão
- **NetManager** com `NetworkManager` + `UnityTransport`; `NetworkConfig.PlayerPrefab = NetworkPlayer.prefab`; registrar `NetworkPlayer` na lista de Network Prefabs
- **ConnectUI** com `SandboxConnectUI` (reaproveitado do SP0)
- **SkillManager** (instância na cena, pro ataque básico do player local funcionar)

Registrar a cena no **Build Profiles / EditorBuildSettings**.

- [ ] **Step 3: Verificar 0 erros**

`refresh_unity` (scope=all) → `read_console` (types=["error"]). Esperado: **0 erros**.

- [ ] **Step 4: Commit**

```bash
git add "Assets/prefebs/net/NetworkPlayer.prefab" "Assets/prefebs/net/NetworkPlayer.prefab.meta" \
        Assets/Scenes/mp_fase_teste.unity Assets/Scenes/mp_fase_teste.unity.meta \
        ProjectSettings/EditorBuildSettings.asset
git commit -m "feat(mp-sp1): variante NetworkPlayer + cena mp_fase_teste

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 7: Validar marco + regressão single-player

**Files:** nenhum (validação).

- [ ] **Step 1: Teste co-op (Multiplayer Play Mode)**

Ativar 1 player virtual (Player 2). Abrir `mp_fase_teste`, entrar em Play. Host na principal, Join (código) no Player 2.

Confirmar (critério de aceite da spec §5):
- [ ] Dois avatares; cada instância controla só o seu
- [ ] Movimento + animação do dono aparecem na outra instância
- [ ] Câmera de cada cliente segue o próprio player
- [ ] Cada cliente aplica o próprio personagem (testar mudando `SelectedCharacter` numa instância)
- [ ] `read_console` (types=["error"]) durante a sessão: **0 erros**

- [ ] **Step 2: Regressão single-player (OBRIGATÓRIO)**

Abrir `primeira_fase`, entrar em Play (sem Multiplayer Play Mode). Confirmar:
- [ ] Player se move, dá dash, ataca, usa skills/ultimate, troca elemento — **igual a antes**
- [ ] HUD, level-up, painel de status funcionam
- [ ] `read_console`: 0 erros novos

Se qualquer regressão → parar e corrigir antes de finalizar.

- [ ] **Step 3: Finalizar branch**

Usar `superpowers:finishing-a-development-branch`. Sugestão: merge `feat/mp-sp1-player` → `main` (fast-forward se possível) + push, conforme preferência do usuário.

---

## Self-Review (autor do plano)

- **Cobertura da spec:** §6.1 variante → Task 6; §6.2 IsLocalAuthority/gating → Task 1; §6.3 Local + migração local → Tasks 1/5; §6.4 spawn+personagem → Tasks 2/3/6; §6.5 câmera → Task 4; §5 marco + §9 regressão → Task 7. Projéteis em rede e migração de chamadores de inimigo/evento ficam fora (SP2), como na spec §4.
- **Placeholders:** os refactors de métodos existentes (`ApplyCharacterData`, `SkillManager`) descrevem a transformação mecânica exata sobre arquivos/métodos nomeados, sem código vago.
- **Consistência de tipos:** `INetOwnership` (IsNetworked/IsLocalOwner) implementado por `PlayerNet`; `PlayerStats.Local`/`SetLocal`/`ClearLocal`/`IsLocalAuthority`/`ApplyCharacterData(int)` usados de forma consistente entre Tasks 1-6.
