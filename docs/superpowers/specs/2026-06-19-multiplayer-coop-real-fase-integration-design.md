# Multiplayer Co-op — Integração numa Fase Real (`primeira_fase_mp`) (Design)

**Data:** 2026-06-19
**Status:** aprovado (design); pendente plano de implementação
**Depende de:** SP0, SP1, SP2a, SP2b, SP2c — concluídos

---

## 1. Objetivo

Levar o loop de combate co-op (já pronto no SP2) para uma **fase real**: uma cópia `primeira_fase_mp` jogável em co-op, com **terreno, luz, HUD e horda reais**. Dois players entram por join code, lutam a horda configurada, tomam dano, caem e revivem — num ambiente de verdade, não na sandbox. A `primeira_fase` single-player fica **intocada**.

## 2. Contexto

- Sistemas já network-aware (dual-mode): player (`NetworkPlayer`/`PlayerNet`), inimigos (`EnemyNet`/`NetSpawn`), dano (host-routing), downed/revive (SP2c), spawn/eventos host-only.
- `primeira_fase` tem **player fixo na cena**, `EnemySpawnerCompleto` (waves), `UIManege` (HUD), `SkillManager`, `StatusCardSystem`, `GerenciadorEventos`, `TimerManager`, terreno e Light2D.
- `UIManege` acha o player via `FindAnyObjectByType<PlayerStats>()` (linhas ~144/179) — pega um player arbitrário em co-op.
- O usuário escolheu **cópia** (não converter in-place) pra isolar risco do single-player.

## 3. Escopo

### Inclui
- Criar `primeira_fase_mp` (cópia de `primeira_fase`).
- Remover o **player fixo** da cópia.
- Adicionar **NetworkManager + UnityTransport** (PlayerPrefab = `NetworkPlayer`; inimigos registrados via **Default Network Prefabs List**), **ConnectUI** (reusa `SandboxConnectUI`), e **PlayerCameraFollow** na câmera da fase.
- Migrar `UIManege` (HUD) pra `PlayerStats.Local`.
- Manter terreno, luz, spawner, eventos.

### NÃO inclui (deferido)
- **Progressão**: XP/drops, level-up, escolha de skill por player, StatusCard sincronizado → **SP3**.
- **Timer/boss-cycle sincronizado** (`TimerManager`/escolha pós-vitória) → **SP4**. (Na cópia o `TimerManager` roda dessincronizado e cosmético; o boss já é host-only via `NetSpawn`.)
- **Lobby/fluxo de menu** (entrar pela LobbyUI real) → **SP5**. O teste entra direto pela `ConnectUI`.
- **Unificar SP+co-op numa cena só** (dual-mode in-place) → futuro.

## 4. Marco de "pronto"

Via MPPM (2 players) em `primeira_fase_mp`:
- [ ] Os 2 players entram (Host + Join) e aparecem no **terreno real**, iluminados, cada um com sua **câmera** e **HUD** (vida).
- [ ] A **horda real** (waves configuradas) spawna no host e persegue os players nas duas telas.
- [ ] Os inimigos **causam dano** (HUD do dono cai); o `DebugAtaque`/ataques matam inimigos.
- [ ] A 0 HP o player **cai (downed)**; o companheiro **revive** pela barra; **todos caídos → game over de grupo**.
- [ ] `read_console` (errors) = 0 durante a sessão.
- [ ] **Regressão:** `primeira_fase` (single-player) joga **igual a antes** — player fixo, horda, dano, morte normal.

## 5. Arquitetura

### 5.1 Cópia da cena
`primeira_fase_mp` = duplicata de `primeira_fase`. Nela:
- **Remover** a instância do `player.prefab` (player fixo).
- **NetManager**: GameObject com `NetworkManager` + `UnityTransport`; `NetworkConfig.PlayerPrefab = NetworkPlayer.prefab`; usar a **Default Network Prefabs List** (já existe `DefaultNetworkPrefabs.asset`) pra registrar todos os inimigos/boss de uma vez.
- **ConnectUI**: GameObject com `SandboxConnectUI`.
- **Câmera**: adicionar `PlayerCameraFollow` na Main Camera da fase (segue `PlayerStats.Local`).
- Registrar `primeira_fase_mp` no Build Settings.

### 5.2 HUD local (`UIManege`)
Trocar `FindAnyObjectByType<PlayerStats>()` por `PlayerStats.Local` (com guarda de nulo, pois o player spawna depois da conexão). O `UIManege` já tolera player nulo no início e re-busca (linha ~179) — manter esse retry usando `Local`.

### 5.3 Managers
- `SkillManager`: já usa `PlayerStats.Local` (SP1).
- `GerenciadorEventos`: host-only (SP2a) — eventos rodam no host, inimigos replicam.
- `TimerManager`: roda em cada cliente (cosmético/dessincronizado); boss host-only via `NetSpawn`. Sincronização é SP4.
- `StatusCardSystem`: inalterado (progressão → SP3).
- `EnemySpawnerCompleto`: já host-only + spawn ao redor de N players (SP2a).

### 5.4 Câmera por cliente
`PlayerCameraFollow` (do SP1) na Main Camera da `primeira_fase_mp`, seguindo `PlayerStats.Local`. Em co-op, cada cliente renderiza a própria câmera.

## 6. Riscos

| Risco | Mitigação |
|---|---|
| HUD pegar player errado | Migração `UIManege` → `PlayerStats.Local` |
| Default Prefabs List incompleta | Conferir que todos os prefabs de inimigo do spawner estão na lista (já têm NetworkObject do SP2a) |
| `TimerManager` causar comportamento estranho | Cosmético no teste; boss host-only; sincronização real é SP4 |
| Player fixo remanescente na cópia | Garantir remoção da instância na `primeira_fase_mp` |
| Quebrar single-player | A cópia é separada; `primeira_fase` não é tocada; teste de regressão obrigatório |

## 7. Testes
- **Co-op:** MPPM (2 players) em `primeira_fase_mp` — validar §4.
- **Regressão SP:** `primeira_fase` — single-player intacto.
- Sem testes unitários; aceite = comportamento observável.

## 8. Próximo passo
Plano (writing-plans): migrar `UIManege` → `Local` → criar `primeira_fase_mp` (cópia, remover player, NetworkManager+prefabs, ConnectUI, câmera) → registrar no build → validação co-op + regressão SP.
