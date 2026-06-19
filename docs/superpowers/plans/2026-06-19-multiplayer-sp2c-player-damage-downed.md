# Multiplayer Co-op — SP2c: Inimigo → Player + Downed/Revive — Plano

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development ou superpowers:executing-plans. Passos usam checkbox (`- [ ]`).

**Goal:** Inimigos causam dano nos players (vida host→dono); a 0 HP o player cai (downed); um companheiro próximo enche a barra de revive e o traz de volta com efeito; todos caídos → game over de grupo. Single-player intacto.

**Architecture:** Vida owner-autoritativa — o dano de inimigo (no host) é roteado pro dono via RPC (SendTo.Owner). Downed é `NetworkVariable<bool>` (owner-write); o host monitora proximidade e enche `reviveProgresso` (`NetworkVariable<float>` server-write). Quando todos caem, o host dispara game over (RPC SendTo.Everyone).

**Tech Stack:** Unity 6, NGO 2.12 (RPCs universais `[Rpc(SendTo.X)]`).

**Spec:** `docs/superpowers/specs/2026-06-19-multiplayer-sp2c-player-damage-downed-design.md`

---

## Verificação (não-TDD)

Cada tarefa de código: `refresh_unity` (scope=all) → `read_console` (types=["error"]) = **0 erros**. Aceite final (Task 7): marco co-op no MPPM + regressão SP na `primeira_fase`.

---

## File Structure

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/scripts/net/NetCombat.cs` | Modificar | Ligar dano inimigo→player em co-op |
| `Assets/scripts/net/PlayerNet.cs` | Modificar | `downed`/`reviveProgresso`, RPCs, monitor de revive + game over (host) |
| `Assets/scripts/player_stats.cs` | Modificar | Roteamento de dano; `Cair()` vs `Die()`; `ReviverCoop`; gate por downed |
| `Assets/scripts/moviment_player2.cs` | Modificar | Gate de movimento por downed |
| `Assets/scripts/net/ReviveBarUI.cs` | Criar | Barra de revive acima do caído |

---

## Task 1: Branch + habilitar dano inimigo→player

**Files:** Modify `Assets/scripts/net/NetCombat.cs`

- [ ] **Step 1: Branch**

```bash
cd "j:/unity/projetos/horda/orda"
git checkout -b feat/mp-sp2c-downed
```

- [ ] **Step 2: Ligar o dano em rede**

Em `Assets/scripts/net/NetCombat.cs`, trocar:

```csharp
    public static bool RedeComDano = false;
```

por:

```csharp
    // SP2c: dano inimigo->player ligado em co-op.
    public static bool RedeComDano = true;
```

- [ ] **Step 3: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/net/NetCombat.cs
git commit -m "feat(mp-sp2c): liga dano inimigo->player em co-op (NetCombat)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2: PlayerStats — roteamento, downed e revive

**Files:** Modify `Assets/scripts/player_stats.cs`

- [ ] **Step 1: Helper de downed + roteamento no início do TakeDamage**

No `player_stats.cs`, adicionar junto do bloco de rede (perto de `IsLocalAuthority`):

```csharp
    // true quando este player está caído (downed) — co-op.
    public bool EstaCaido
    {
        get { var pn = GetComponent<PlayerNet>(); return pn != null && pn.Caido; }
    }
```

No início de `TakeDamage(float damage)`, **logo após** `if (!NetCombat.DanoHabilitado) return;`:

```csharp
        if (EstaCaido) return; // caído não toma mais dano (sem bleed-out)

        // co-op: dano de inimigo acontece no host; se este player não é meu (não sou dono),
        // roteia pro dono aplicar.
        var _pn = GetComponent<PlayerNet>();
        if (_pn != null && _pn.IsSpawned && !_pn.IsOwner)
        {
            _pn.TomarDanoOwnerRpc(damage);
            return;
        }
```

- [ ] **Step 2: Cair em vez de morrer (co-op)**

No `TakeDamage`, o ponto onde chama `Die();` (após as skills de revive), trocar:

```csharp
            Die();
```

por:

```csharp
            if (NetSpawn.EmRede)
            {
                var pn = GetComponent<PlayerNet>();
                if (pn != null) pn.Cair(); // downed; host decide revive / game over de grupo
            }
            else
            {
                Die(); // single-player: morte normal
            }
```

- [ ] **Step 3: Método de revive co-op**

Adicionar em `player_stats.cs` (público, chamado pelo PlayerNet no dono):

```csharp
    // Revive co-op: restaura uma fração da vida e limpa o estado de dano.
    public void ReviverCoop(float fracaoVida)
    {
        health = Mathf.Clamp(maxHealth * fracaoVida, 1f, maxHealth);
        timeSinceLastDamage = 0f;
        isRegenerating = false;
        UpdateUI();
    }
```

- [ ] **Step 4: Gate de input por downed**

No início de `void Update()`, **logo após** `if (!IsLocalAuthority) return;`:

```csharp
        if (EstaCaido) return; // caído não age
```

- [ ] **Step 5: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros (PlayerNet.Caido/Cair/TomarDanoOwnerRpc/IsSpawned/IsOwner vêm na Task 4; se compilar antes da Task 4, fazer as duas juntas e compilar no fim).

> **Ordem:** as Tasks 2, 3 e 4 referenciam membros de `PlayerNet` (Task 4) e vice-versa. Implementar 2+3+4 e **compilar uma vez no fim da Task 4**.

- [ ] **Step 6: Commit** (após Task 4 compilar)

```bash
git add Assets/scripts/player_stats.cs
git commit -m "feat(mp-sp2c): TakeDamage roteia pro dono + Cair/ReviverCoop + gate por downed

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3: moviment_player2 — gate por downed

**Files:** Modify `Assets/scripts/moviment_player2.cs`

- [ ] **Step 1: Gate**

Em `Update()` e `FixedUpdate()`, logo após `if (!playerStats.IsLocalAuthority) return;`, adicionar em ambos:

```csharp
        if (playerStats.EstaCaido) return;
```

- [ ] **Step 2:** Compilar junto da Task 4 (depende de `EstaCaido`, que é da Task 2).

- [ ] **Step 3: Commit** (após compilar)

```bash
git add Assets/scripts/moviment_player2.cs
git commit -m "feat(mp-sp2c): movimento parado quando caído (downed)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4: PlayerNet — downed, revive monitor, game over

**Files:** Modify `Assets/scripts/net/PlayerNet.cs`

- [ ] **Step 1: NetworkVariables + parâmetros + acessores**

No `PlayerNet`, adicionar campos (junto dos NetworkVariables existentes):

```csharp
    readonly NetworkVariable<bool> downed = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<float> reviveProgresso = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] float reviveRaio = 2.5f;
    [SerializeField] float tempoRevive = 4f;
    [SerializeField] float fracaoRevive = 0.5f;

    bool gameOverDisparado;

    public bool Caido => downed.Value;
    public float ReviveProgresso => reviveProgresso.Value;
```

- [ ] **Step 2: RPCs**

```csharp
    // Dano de inimigo (no host) aplicado no dono.
    [Rpc(SendTo.Owner)]
    public void TomarDanoOwnerRpc(float dano) { stats.TakeDamage(dano); }

    // Chamado pelo dono ao cair (health <= 0 em co-op).
    public void Cair() { if (IsOwner) downed.Value = true; }

    // Host manda o dono reviver.
    [Rpc(SendTo.Owner)]
    public void ReviverOwnerRpc(float fracao)
    {
        downed.Value = false;
        stats.ReviverCoop(fracao);
    }

    // VFX de revive em todos.
    [Rpc(SendTo.Everyone)]
    public void ReviveVfxRpc()
    {
        var ef = GetComponent<PlayerSpawnEffect>();
        if (ef != null) ef.SendMessage("Executar", SendMessageOptions.DontRequireReceiver);
    }

    // Game over de grupo em todos.
    [Rpc(SendTo.Everyone)]
    public void GameOverGrupoRpc() { GameOverUI.Mostrar(); }
```

- [ ] **Step 3: Monitor no host (revive + game over)**

No `Update()` existente, ao final, adicionar:

```csharp
        if (IsServer) MonitorarHost();
```

E o método:

```csharp
    void MonitorarHost()
    {
        // Revive: se EU estou caído e há companheiro vivo no raio, enche a barra.
        if (downed.Value)
        {
            bool temReanimador = false;
            for (int i = 0; i < PlayerStats.All.Count; i++)
            {
                var p = PlayerStats.All[i];
                if (p == null || p == stats) continue;
                var pn = p.GetComponent<PlayerNet>();
                if (pn != null && pn.Caido) continue; // companheiro também caído
                float d2 = ((Vector2)p.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d2 <= reviveRaio * reviveRaio) { temReanimador = true; break; }
            }
            if (temReanimador)
            {
                reviveProgresso.Value += Time.deltaTime / tempoRevive;
                if (reviveProgresso.Value >= 1f)
                {
                    reviveProgresso.Value = 0f;
                    ReviverOwnerRpc(fracaoRevive);
                    ReviveVfxRpc();
                }
            }
            else if (reviveProgresso.Value > 0f)
            {
                reviveProgresso.Value = Mathf.Max(0f, reviveProgresso.Value - Time.deltaTime / tempoRevive);
            }
        }

        // Game over de grupo: todos os players caídos (uma vez).
        if (!gameOverDisparado && PlayerStats.All.Count > 0)
        {
            bool todosCaidos = true;
            for (int i = 0; i < PlayerStats.All.Count; i++)
            {
                var pn = PlayerStats.All[i] != null ? PlayerStats.All[i].GetComponent<PlayerNet>() : null;
                if (pn == null || !pn.Caido) { todosCaidos = false; break; }
            }
            if (todosCaidos)
            {
                gameOverDisparado = true;
                GameOverGrupoRpc();
            }
        }
    }
```

- [ ] **Step 4: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros. (Agora Tasks 2/3/4 fecham juntas.)

- [ ] **Step 5: Commit**

```bash
git add Assets/scripts/net/PlayerNet.cs
git commit -m "feat(mp-sp2c): PlayerNet downed/reviveProgresso + RPCs + monitor host (revive + game over)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5: ReviveBarUI — barra acima do caído

**Files:** Create `Assets/scripts/net/ReviveBarUI.cs`; Modify `Assets/prefebs/net/NetworkPlayer.prefab`

- [ ] **Step 1: Criar o componente**

`Assets/scripts/net/ReviveBarUI.cs` — barra world-space simples (2 SpriteRenderers) acima do player, visível só quando caído:

```csharp
using UnityEngine;

// Barra de revive acima do player caído. Lê PlayerNet.Caido/ReviveProgresso.
public class ReviveBarUI : MonoBehaviour
{
    [SerializeField] float altura = 1.2f;
    [SerializeField] float largura = 1.0f;

    PlayerNet net;
    Transform raiz;
    Transform fill;

    void Awake()
    {
        net = GetComponent<PlayerNet>();
        var sprite = MakeSquareSprite();

        raiz = NovoSR("ReviveBar_BG", new Color(0.08f, 0.05f, 0.05f, 0.9f), sprite, 0).transform;
        raiz.SetParent(transform, false);
        raiz.localPosition = new Vector3(0f, altura, 0f);
        raiz.localScale = new Vector3(largura, 0.14f, 1f);

        fill = NovoSR("ReviveBar_Fill", new Color(0.95f, 0.82f, 0.30f, 1f), sprite, 1).transform;
        fill.SetParent(raiz, false);
        fill.localScale = Vector3.one; // largura ajustada por progresso
        raiz.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        bool caido = net != null && net.Caido;
        if (raiz.gameObject.activeSelf != caido) raiz.gameObject.SetActive(caido);
        if (!caido) return;
        float p = Mathf.Clamp01(net.ReviveProgresso);
        fill.localScale = new Vector3(p, 1f, 1f);
        fill.localPosition = new Vector3(-(1f - p) * 0.5f, 0f, 0f); // ancora à esquerda
        raiz.rotation = Quaternion.identity; // não gira com o player
    }

    GameObject NovoSR(string nome, Color cor, Sprite sp, int ordem)
    {
        var go = new GameObject(nome);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp; sr.color = cor; sr.sortingOrder = 50 + ordem;
        return go;
    }

    static Sprite _sq;
    static Sprite MakeSquareSprite()
    {
        if (_sq != null) return _sq;
        var tex = new Texture2D(2, 2);
        var px = new Color[4]; for (int i = 0; i < 4; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        _sq = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        return _sq;
    }
}
```

- [ ] **Step 2: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 3: Adicionar ao NetworkPlayer**

Adicionar `ReviveBarUI` ao prefab `Assets/prefebs/net/NetworkPlayer.prefab` (via MCP `execute_code`: `AddComponent<ReviveBarUI>()` + `PrefabUtility.SavePrefabAsset`).

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/net/ReviveBarUI.cs Assets/scripts/net/ReviveBarUI.cs.meta "Assets/prefebs/net/NetworkPlayer.prefab"
git commit -m "feat(mp-sp2c): ReviveBarUI (barra de revive acima do caído)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6: Visual de caído (tint)

**Files:** Modify `Assets/scripts/net/PlayerNet.cs`

- [ ] **Step 1: Tint no downed**

No `PlayerNet`, reagir à mudança de `downed` aplicando um tint no `SpriteRenderer` do player (cinza/avermelhado quando caído, normal quando volta). Em `OnNetworkSpawn`, inscrever:

```csharp
        downed.OnValueChanged += AoMudarDowned;
        AoMudarDowned(false, downed.Value);
```

E em `OnNetworkDespawn`: `downed.OnValueChanged -= AoMudarDowned;`

Método:

```csharp
    void AoMudarDowned(bool _, bool caido)
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = caido ? new Color(0.5f, 0.25f, 0.25f, 0.9f) : Color.white;
    }
```

- [ ] **Step 2: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/net/PlayerNet.cs
git commit -m "feat(mp-sp2c): tint visual no player caído

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 7: Validar marco + regressão SP

**Files:** nenhum.

- [ ] **Step 1: Co-op (MPPM)**

2 players, `mp_fase_teste`, Host + Join. Confirmar (spec §5):
- [ ] Inimigos causam dano (vida do dono cai no HUD)
- [ ] A 0 HP, com o outro vivo, o player **cai** (downed: parado, tint, barra aparece)
- [ ] O companheiro chega perto → barra enche → **revive** com vida parcial + VFX, nos dois
- [ ] **Os dois caídos** → **game over** nas duas telas
- [ ] `read_console` (errors) = 0

- [ ] **Step 2: Regressão single-player (OBRIGATÓRIO)**

`primeira_fase` em Play: tomar dano, morrer → GameOver normal (sem downed). 0 erros novos.

- [ ] **Step 3: Finalizar branch**

Checar FF (`git fetch` + `git merge-base --is-ancestor origin/main main`). Se FF: merge + push. Se divergiu: rebase sobre origin/main (descartar ruído NGO/TMP), depois push. NUNCA force-push. Usar `superpowers:finishing-a-development-branch`.

---

## Self-Review (autor do plano)

- **Cobertura da spec:** §6.1 habilitar dano → Task 1; §6.2 roteamento → Task 2; §6.3 downed → Tasks 2/4; §6.4 NetworkVariables/gate → Tasks 2/3/4; §6.5 revive monitor+barra+VFX → Tasks 4/5; §6.6 game over grupo → Task 4; §6.7 SP → Task 2 (`NetSpawn.EmRede`). Visual de caído → Task 6. Barras de vida de companheiro/números de dano/XP: fora (deferidos).
- **Placeholders:** código completo; edição de prefab descreve a ação MCP.
- **Consistência de tipos:** `PlayerNet.{Caido,ReviveProgresso,Cair,TomarDanoOwnerRpc,ReviverOwnerRpc}`, `PlayerStats.{EstaCaido,ReviverCoop}`, `NetCombat.RedeComDano`, `NetSpawn.EmRede` usados de forma consistente. Tasks 2/3/4 compilam juntas (interdependência sinalizada).
```
