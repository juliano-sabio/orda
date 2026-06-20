# Multiplayer Co-op — SP2b: Player → Inimigo (Ataques em Rede) — Plano

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development ou superpowers:executing-plans. Passos usam checkbox (`- [ ]`).

**Goal:** Os ataques de qualquer player causam dano host-autoritativo nos inimigos; a morte/despawn é decidida pelo host e os dois clientes veem o inimigo sumir. Single-player intacto.

**Architecture:** Roteamento de dano centralizado em `InimigoController.ReceberDano` (cliente → ServerRpc no `EnemyNet` → host aplica). Morte comum via `NetSpawn.Despawnar` (host despawna o NetworkObject). Debug attack no player de teste pra validar.

**Tech Stack:** Unity 6, NGO 2.12.

**Spec:** `docs/superpowers/specs/2026-06-19-multiplayer-sp2b-player-damage-design.md`

---

## Verificação (não-TDD)

Cada tarefa de código: `refresh_unity` (scope=all) → `read_console` (types=["error"]) = **0 erros**. Aceite final (Task 5): marco co-op no MPPM + regressão SP na `primeira_fase`.

---

## File Structure

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/scripts/net/NetSpawn.cs` | Modificar | + `Despawnar(go)` dual-mode |
| `Assets/scripts/net/EnemyNet.cs` | Modificar | + `ReceberDanoServerRpc` |
| `Assets/scripts/controlei_inimigo.cs` | Modificar | Roteamento no `ReceberDano`; `Despawnar` na morte comum |
| `Assets/scripts/net/DebugAtaque.cs` | Criar | Ataque de debug (dono) pra validar |
| `Assets/prefebs/net/NetworkPlayer.prefab` | Modificar | + `DebugAtaque` |

---

## Task 1: Branch + NetSpawn.Despawnar

**Files:** Modify `Assets/scripts/net/NetSpawn.cs`

- [ ] **Step 1: Branch**

```bash
cd "j:/unity/projetos/horda/orda"
git checkout -b feat/mp-sp2b-damage
```

- [ ] **Step 2: Adicionar Despawnar**

Em `Assets/scripts/net/NetSpawn.cs`, dentro da classe `NetSpawn`, adicionar:

```csharp
    // Despawn dual-mode: host em rede -> NetworkObject.Despawn (destrói em todos);
    // cliente em rede -> nada; single-player -> Destroy.
    public static void Despawnar(GameObject go)
    {
        if (go == null) return;
        var nm = NetworkManager.Singleton;
        var no = go.GetComponent<NetworkObject>();
        if (nm != null && nm.IsListening && no != null && no.IsSpawned)
        {
            if (nm.IsServer) no.Despawn(); // destrói em todos os clientes
            return;                        // cliente não despawna
        }
        Object.Destroy(go); // single-player
    }
```

- [ ] **Step 3: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/net/NetSpawn.cs
git commit -m "feat(mp-sp2b): NetSpawn.Despawnar (despawn dual-mode)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2: EnemyNet.ReceberDanoServerRpc

**Files:** Modify `Assets/scripts/net/EnemyNet.cs`

- [ ] **Step 1: Adicionar o ServerRpc**

Em `Assets/scripts/net/EnemyNet.cs`, dentro da classe `EnemyNet`, adicionar:

```csharp
    // Qualquer cliente pode requisitar dano a qualquer inimigo (co-op de amigos).
    [Unity.Netcode.ServerRpc(RequireOwnership = false)]
    public void ReceberDanoServerRpc(float dano, bool isCrit)
    {
        var ic = GetComponent<InimigoController>();
        if (ic != null) ic.ReceberDano(dano, isCrit); // roda no host -> aplica
    }
```

- [ ] **Step 2: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

> Nota: o NGO gera o código do RPC em compile-time; após adicionar, conferir que não há erro de "ServerRpc must be on a NetworkBehaviour" (EnemyNet já é NetworkBehaviour).

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/net/EnemyNet.cs
git commit -m "feat(mp-sp2b): EnemyNet.ReceberDanoServerRpc (dano host-autoritativo)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3: Roteamento no ReceberDano + despawn na morte

**Files:** Modify `Assets/scripts/controlei_inimigo.cs`

- [ ] **Step 1: Roteamento no início de ReceberDano**

Em `controlei_inimigo.cs`, no início de `public void ReceberDano(float dano, bool isCrit = false, bool mostrarNumero = true)` (linha ~129), antes de `if (estaMorrendo || imuneAoDano) return;`:

```csharp
        // co-op: numa cópia cliente (fantoche), pede pro host aplicar e sai.
        var _en = GetComponent<EnemyNet>();
        if (_en != null && _en.IsSpawned && !_en.IsServer)
        {
            _en.ReceberDanoServerRpc(dano, isCrit);
            return;
        }
```

- [ ] **Step 2: Despawn host-autoritativo na morte comum**

Em `Morrer()` (linha ~485), o ramo do inimigo comum:

```csharp
            else                      Destroy(gameObject);
```

trocar por:

```csharp
            else                      NetSpawn.Despawnar(gameObject);
```

> Só o ramo comum (último `else`). Os ramos de boss (`IniciarEfeitoMorte`) ficam como estão — boss despawn é follow-up.

- [ ] **Step 3: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/controlei_inimigo.cs
git commit -m "feat(mp-sp2b): ReceberDano roteia pro host + morte comum via NetSpawn.Despawnar

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4: DebugAtaque + prefab de teste

**Files:** Create `Assets/scripts/net/DebugAtaque.cs`; Modify `Assets/prefebs/net/NetworkPlayer.prefab`

- [ ] **Step 1: Criar o componente**

`Assets/scripts/net/DebugAtaque.cs`:

```csharp
using UnityEngine;

// Harness de teste: o player local causa dano periódico no inimigo mais próximo
// num raio. Valida o roteamento de dano em rede do SP2b. Não é ataque de produção.
public class DebugAtaque : MonoBehaviour
{
    [SerializeField] float intervalo = 0.4f;
    [SerializeField] float raio = 6f;
    [SerializeField] float dano = 10f;

    PlayerStats stats;
    float t;

    void Awake() { stats = GetComponent<PlayerStats>(); }

    void Update()
    {
        if (stats == null || !stats.IsLocalAuthority) return;
        t += Time.deltaTime;
        if (t < intervalo) return;
        t = 0f;

        var hits = Physics2D.OverlapCircleAll(transform.position, raio);
        InimigoController alvo = null;
        float menor = float.MaxValue;
        foreach (var h in hits)
        {
            var ic = h.GetComponent<InimigoController>();
            if (ic == null) continue;
            float d = ((Vector2)ic.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < menor) { menor = d; alvo = ic; }
        }
        if (alvo != null) alvo.ReceberDano(dano);
    }
}
```

- [ ] **Step 2: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 3: Adicionar à variante NetworkPlayer**

Adicionar o componente `DebugAtaque` ao prefab `Assets/prefebs/net/NetworkPlayer.prefab` (via MCP `execute_code`: carregar o prefab, `AddComponent<DebugAtaque>()`, `PrefabUtility.SavePrefabAsset`). Não adicionar ao `player.prefab` base (single-player não usa).

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/net/DebugAtaque.cs Assets/scripts/net/DebugAtaque.cs.meta "Assets/prefebs/net/NetworkPlayer.prefab"
git commit -m "feat(mp-sp2b): DebugAtaque (harness) no NetworkPlayer pra validar dano em rede

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5: Validar marco + regressão SP

**Files:** nenhum.

- [ ] **Step 1: Co-op (MPPM)**

2 players, `mp_fase_teste`, Host + Join. Confirmar (spec §4):
- [ ] Os slimes tomam dano do debug attack dos **dois** players
- [ ] O dano do **cliente** mata o slime (aplicado no host) e ele **some nas duas telas**
- [ ] O host matando também some nos dois
- [ ] **Player não toma dano** (inimigo→player ainda off)
- [ ] `read_console` (errors) = 0

- [ ] **Step 2: Regressão single-player (OBRIGATÓRIO)**

`primeira_fase` em Play: matar inimigos normal, **drops caem**, dano nos dois sentidos funciona. 0 erros novos.

- [ ] **Step 3: Finalizar branch**

`superpowers:finishing-a-development-branch` → merge `feat/mp-sp2b-damage` → `main` (FF) + push.

---

## Self-Review (autor do plano)

- **Cobertura da spec:** §5.1 roteamento → Task 3; §5.2 ServerRpc → Task 2; §5.3 despawn → Tasks 1/3; §5.4 debug attack → Task 4; marco+regressão → Task 5. Drops, barras-no-cliente, dano inimigo→player, boss despawn: explicitamente fora.
- **Placeholders:** todo código está completo; a edição do prefab descreve a ação MCP exata.
- **Consistência de tipos:** `NetSpawn.Despawnar(GameObject)`, `EnemyNet.ReceberDanoServerRpc(float,bool)`, `InimigoController.ReceberDano(float,bool,bool)`, `DebugAtaque` usados de forma consistente.
