# Multiplayer Co-op — Sub-projeto 2c: Inimigo → Player + Downed/Revive (Design)

**Data:** 2026-06-19
**Status:** aprovado (design); pendente plano de implementação
**Depende de:** SP0, SP1, SP2a, SP2b — concluídos

---

## 1. Objetivo

Fechar o loop de combate co-op: inimigos causam dano nos players, a vida é sincronizada, e a 0 HP o player vai pra **downed** (em vez de morrer); um companheiro próximo enche uma **barra de revive** e o traz de volta com efeito visual; **todos caídos ao mesmo tempo → game over do grupo**. Single-player intocado (morte normal).

## 2. Contexto

- Player owner-authoritative pra movimento (SP1); inimigos host-authoritative (SP2a); dano player→inimigo roteado pro host (SP2b).
- `PlayerStats.TakeDamage` é o ponto único de dano ao player (gated por `NetCombat.DanoHabilitado`, hoje **false** em rede).
- `health` é campo local; regen/cura rodam no dono (gated por `IsLocalAuthority`). A 0 HP: skills de revive (Fôlego/Segunda Chance) → senão `Die()` → `GameOverUI`.
- `PlayerStats.All` lista os players; `PlayerNet` carrega a lógica NGO do player.

## 3. Decisões de design (travadas com o usuário)

- **Revive por proximidade com barra:** companheiro vivo chega perto do caído → barra enche ao longo de `tempoRevive` → revive + **efeito visual**. Sem segurar tecla.
- **Sem bleed-out:** o caído fica downed até ser revivido OU todos caírem (→ game over). (Da visão original.)
- **Vida owner-autoritativa:** mantém regen/cura/dano no dono; dano de inimigo (no host) é roteado pro dono.

## 4. Escopo

### Inclui
- Ligar dano **inimigo→player** em co-op (flag `NetCombat`).
- Rotear dano host→dono (`PlayerNet`, RPC SendTo.Owner) — simétrico ao 2b.
- Estado **downed** (NetworkVariable owner-write) substituindo `Die()` em co-op quando há companheiro vivo.
- **Revive** monitorado pelo host: proximidade + barra (NetworkVariable de progresso) + restauração de vida + VFX.
- **Game over de grupo** quando todos caem (ClientRpc → GameOverUI em todos).
- Visual de downed + barra de revive acima do caído.

### NÃO inclui (deferido)
- **Barras de vida de companheiro** no HUD (ver a vida do outro player) → polimento; a vida local do dono já funciona.
- Números de dano sincronizados (SP3, como no 2b).
- XP/drops em rede (SP3).
- Skills de revive existentes (Fôlego/Segunda Chance) interagindo com downed — em co-op, a 0 HP elas ainda tentam atuar primeiro (mantidas); só o `Die()` final vira downed.

## 5. Marco de "pronto"

Via MPPM (2 players) em `mp_fase_teste`:
- [ ] Inimigos **causam dano** nos players; a vida cai (HUD do dono atualiza).
- [ ] A 0 HP, com o companheiro vivo, o player vai pra **downed** (não morre): não move/ataca, visual de caído.
- [ ] O companheiro **chega perto** → barra de revive enche → o caído **volta** (com vida parcial) + **efeito visual**, nos dois.
- [ ] Quando **os dois caem** (sem ninguém pra reviver) → **game over** nas duas telas.
- [ ] **Regressão SP:** `primeira_fase` — tomar dano, morrer e ir pro GameOver normal (sem downed).

## 6. Arquitetura

### 6.1 Habilitar dano inimigo→player
`NetCombat.RedeComDano = true` (ou um flag dedicado `DanoInimigoAoPlayer`) → `NetCombat.DanoHabilitado` passa a ser true em co-op, e `PlayerStats.TakeDamage` aplica. (SP já é true.)

### 6.2 Roteamento de dano host→dono
No `PlayerStats.TakeDamage`, após o gate `DanoHabilitado`: se a instância **não é do dono** (host aplicando dano a um player de cliente), roteia pro dono e sai:

```
if (PlayerNet existe && IsSpawned && !IsOwner)
{
    PlayerNet.TomarDanoOwnerRpc(damage);  // [Rpc(SendTo.Owner)]
    return;
}
// dono / SP: aplica como hoje (shields, reduções, health -= ...).
```

`PlayerNet.TomarDanoOwnerRpc(float)` chama `PlayerStats.TakeDamage(dano)` no dono → aplica.

### 6.3 Downed substitui o Die em co-op
No `TakeDamage`, no ponto `Die();` (após as skills de revive): em vez de morrer direto:

```
if (EhRedeComOutrosVivos())  Cair();   // downed
else                         Die();    // SP, ou sou o último -> game over
```

- `Cair()`: `PlayerNet.downed.Value = true` (owner-write); para movimento/ataque (gate por `downed`); aplica visual de caído.
- `EhRedeComOutrosVivos()`: em rede E existe outro player em `PlayerStats.All` **não-caído**.
- Se sou o último vivo a cair → `Die()` não; em vez disso o host dispara **game over de grupo** (§6.6).

### 6.4 Estado downed (PlayerNet)
- `NetworkVariable<bool> downed` (write: Owner).
- `NetworkVariable<float> reviveProgresso` (write: Server, 0..1) — pra barra.
- Gate de input: `player_stats`/`moviment_player2` também checam `!downed` (além de `IsLocalAuthority`).

### 6.5 Revive (host monitora)
Um monitor no **host** (em `PlayerNet.Update` quando `IsServer`, ou um `ReviveManager`):
- Pra cada player **caído**: se existe companheiro **vivo** (não-caído) dentro de `reviveRaio`, `reviveProgresso += dt/tempoRevive`; senão decai/zera.
- `reviveProgresso >= 1` → **revive**: avisa o dono do caído (`Rpc SendTo.Owner`) → dono faz `downed=false`, `health = maxHealth * fracaoRevive`; `reviveProgresso=0`; dispara **VFX** em todos (ClientRpc) — reusa/clona um efeito (ex.: `PlayerSpawnEffect`).
- Barra de revive: UI acima do caído lendo `reviveProgresso` (criada em código, só visível quando `downed`).

### 6.6 Game over de grupo
O host detecta quando **todos** os players estão caídos (todos `downed`): dispara um `ClientRpc` que chama `GameOverUI.Mostrar()` em todos. (Adicionar overload sem screenshot ou capturar screenshot local por cliente.)

### 6.7 Single-player
Sem rede: `TakeDamage` aplica local, a 0 HP → skills de revive → `Die()` → GameOver, **igual a hoje**. Downed não dispara (sem companheiro). Critério: `EhRedeComOutrosVivos()` é false em SP.

## 7. Estrutura de arquivos

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/scripts/net/NetCombat.cs` | Modificar | Ligar dano inimigo→player em co-op |
| `Assets/scripts/net/PlayerNet.cs` | Modificar | `downed`/`reviveProgresso` NetworkVariables, RPCs (dano→dono, revive→dono, game over), monitor de revive no host |
| `Assets/scripts/player_stats.cs` | Modificar | Roteamento de dano; `Cair()`/downed no lugar do `Die()` em co-op; gate de input por downed |
| `Assets/scripts/moviment_player2.cs` | Modificar | Gate de movimento por downed |
| `Assets/scripts/net/ReviveBarUI.cs` | Criar | Barra de revive acima do caído (lê `reviveProgresso`) |
| `Assets/scripts/UI/GameOverUI.cs` | Modificar | Overload `Mostrar()` co-op (sem screenshot externo) |

## 8. Riscos

| Risco | Mitigação |
|---|---|
| Vida owner-auth vs host enemies | Dano roteado host→dono (RPC SendTo.Owner); dono é a fonte da verdade |
| Downed não gateia tudo | Gate por `downed` em player_stats/moviment + (se preciso) desabilitar ataque |
| Revive monitor custoso | Roda só no host, sobre `PlayerStats.All` (poucos players) |
| Game over disparado cedo (1 caiu) | Só dispara quando **todos** `downed`; ao cair, checar se sobrou alguém vivo |
| Quebrar SP | `EhRedeComOutrosVivos()` false em SP → caminho `Die()` atual intacto; teste de regressão |

## 9. Testes
- **Co-op:** MPPM (2 players) em `mp_fase_teste` — validar §5.
- **Regressão SP:** `primeira_fase` — morte/GameOver normal.
- Sem testes unitários; aceite = comportamento observável.

## 10. Próximo passo
Plano (writing-plans): habilitar dano → roteamento host→dono → downed + gate → revive (monitor + barra + VFX) → game over de grupo → validação co-op + regressão SP.
