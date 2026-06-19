# Multiplayer Co-op — Sub-projeto 2a: Inimigos Host-Autoritativos (Design)

**Data:** 2026-06-19
**Status:** aprovado (design); pendente plano de implementação
**Depende de:** SP0 (Fundação), SP1 (Jogador em rede) — concluídos
**Specs anteriores:** `docs/superpowers/specs/2026-06-18-multiplayer-*.md`

---

## 1. Objetivo

Colocar **todos os corpos de inimigos** (horda comum + inimigos de evento + bosses) na rede, host-autoritativos: spawnam **no host**, perseguem **N players** e têm posição/movimento **sincronizados** para todos os clientes — **sem dano** (contato, projéteis e habilidades de boss ficam desligados; entram no SP2b). Sem quebrar o single-player.

## 2. Contexto (decisões travadas)

- Listen-server, host autoritativo (SP0).
- Single-player sempre jogável → caminho de rede dual-mode (SP1).
- SP2 foi decomposto em **2a** (inimigos sem dano, este doc), **2b** (player→inimigo, ataques em rede) e **2c** (inimigo→player + vida/downed/revive).
- O usuário pediu o slice **completo** de corpos: comuns + evento + boss já no 2a.

## 3. Estado atual relevante

- **33 prefabs** de inimigo/boss.
- **Caminhos de spawn de inimigo** (a tornar host-only + NGO Spawn):
  - `spawn_inimigo.cs:157` — horda comum por waves (`Instantiate(tipo.prefab, ...)`), ao redor de `player.position` singular, usando `Camera.main`.
  - `GerenciadorEventos.cs` — inimigos/hazards de evento: ceifador (874), slime colorida (816), slime percurso (984), núcleo corrompido (`NucleoCorrompidoEvento.cs:124`), etc.
  - `TimerManager.TriggerBossEvent` — spawn do boss (`Instantiate(bossEvent.bossPrefab, ...)`).
- **IA de inimigo:** `movi_inimigo.cs` mira `GameObject.FindGameObjectWithTag("Player")` (um só) e usa `FlowField` p/ pathfinding. **17 scripts** usam `player.position` singular; **12 arquivos** de inimigo/evento usam `FindFirstObjectByType<PlayerStats>`.
- `InimigoController` tem `vidaAtual`/`ReceberDano`/morte e drops (`controlei_inimigo.cs:583+`) — **fora do 2a** (sem dano/morte).

## 4. Escopo

### Inclui

- `NetworkObject` em todos os prefabs de inimigo/boss usados pelos 3 caminhos de spawn.
- `NetworkTransform` (server authority) nos inimigos → posição/movimento sincronizados.
- Dual-mode nos 3 caminhos de spawn: SP = `Instantiate` como hoje; co-op = **só host** (`IsServer`) + `NetworkObject.Spawn()`.
- Spawn ao redor de **N players** (escolher um player alvo entre todos; off-screen relativo a ele).
- Registro `PlayerStats.All` + helper `PlayerStats.MaisProximo(pos)`; IA de inimigo mira o player mais próximo (migrar `movi_inimigo` + os callers de inimigo).
- **Desligar comportamentos de dano** no 2a: contato (`OnTriggerEnter2D`/`OnCollisionEnter2D` que chamam `TakeDamage`), projéteis de inimigo, e habilidades de boss que causam dano/spawnam projéteis. Gateados por uma flag central "dano de rede habilitado" (false no 2a).

### NÃO inclui (fora de escopo)

- **Dano em qualquer sentido** → contato, projéteis de inimigo e habilidades de boss ligam no **SP2b**; vida/dano do player + downed/revive no **SP2c**.
- Sincronização de **vida** dos inimigos (sem dano, vida não muda) → SP2b.
- **Morte de inimigo + drops** (XP/pickups) → SP2b/SP3.
- **Projéteis/ataques do player** → SP2b.
- **FlowField em rede** (fases reais com obstáculos) → no campo aberto da `mp_fase_teste` os inimigos perseguem direto o player mais próximo; FlowField em rede fica como follow-up.
- Conversão das cenas de fase reais → quando o conjunto estiver pronto.

## 5. Marco de "pronto"

Via Multiplayer Play Mode (2 players) em `mp_fase_teste` (com spawner + um gatilho de evento + um gatilho de boss):

- [ ] A horda comum spawna **no host** e aparece nas **duas** telas.
- [ ] Inimigos **perseguem o player mais próximo** (testar com os 2 players separados — inimigos escolhem alvos diferentes conforme proximidade).
- [ ] Movimento dos inimigos é **suave e sincronizado** nos clientes (sem jitter — fantoches via NetworkTransform).
- [ ] Um inimigo de **evento** e um **boss** spawnam host-autoritativos e aparecem nos dois.
- [ ] **Nenhum dano** acontece (nem o player toma, nem os inimigos morrem).
- [ ] **Regressão single-player:** `primeira_fase` com a horda spawnando/perseguindo igual a antes (waves, eventos e boss final funcionam).

## 6. Arquitetura

### 6.1 Autoridade — server-authoritative

Inimigos rodam **só no host**: spawn, IA e movimento. `NetworkTransform` em **Server authority** replica posição/rotação. Clientes recebem fantoches (não rodam IA). É o espelho do player (que é Owner-authority); aqui é Server.

Cópia no cliente = fantoche: o `Rigidbody2D` do inimigo vira **Kinematic** no cliente (mesma lição do SP1 — física não briga com NetworkTransform). A IA (`movi_inimigo`, scripts de boss) **não roda** no cliente (gate por `IsServer`/`!IsSpawned`).

### 6.2 Spawn dual-mode

Helper `NetSpawn.Spawnar(GameObject prefab, Vector3 pos)`:
- Sem rede (NetworkManager ausente/não-listening) → `Instantiate` e devolve o GameObject (comportamento SP atual).
- Em rede e **host** → `Instantiate` + `GetComponent<NetworkObject>().Spawn()`.
- Em rede e **cliente** → não faz nada (clientes não spawnam; retornam null).

Cada caminho (`spawn_inimigo`, `GerenciadorEventos`, `TimerManager`) troca seus `Instantiate` de inimigo por `NetSpawn.Spawnar(...)` e roda o loop de spawn só quando autoridade (`NetSpawn.PodeSpawnar`). Spawn ao redor de um player escolhido entre `PlayerStats.All`.

### 6.3 N players — registro + aggro

```
PlayerStats.All  // lista estática; cada PlayerStats se inscreve no OnEnable, remove no OnDisable
PlayerStats.MaisProximo(Vector2 pos)  // o player vivo mais próximo (ou null)
```

- Em SP, `All` tem 1 player → aggro idêntico ao de hoje.
- `movi_inimigo.EncontrarPlayer` e os callers de inimigo passam a usar `MaisProximo(transform.position)`.
- No host (co-op), `All` contém todos os players (o host enxerga todos os NetworkObjects de player).

### 6.4 Prefabs

Adicionar `NetworkObject` + `NetworkTransform` (Server) aos prefabs de inimigo/boss, **direto no base** (33 variantes seria inviável). Registrar todos na lista de Network Prefabs do NetworkManager. Em SP (sem NetworkManager) o `NetworkObject` fica inerte (verificar ausência de warnings).

### 6.5 Flag central de dano (desligada no 2a)

`NetCombat.DanoHabilitado` (estático, default **false** no 2a; vira true no 2b). Os pontos de dano de inimigo (contato em `danoinimigo`/`controlei_inimigo`, projéteis de inimigo, habilidades de boss) checam essa flag e **não aplicam dano** enquanto false. Em SP, a flag é **true** (single-player tem dano normal) — a flag só desliga dano no contexto de rede 2a. (Critério: `DanoHabilitado = !EmRede || RedeComDanoLigado`.)

## 7. Estrutura de arquivos

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/scripts/net/NetSpawn.cs` | Criar | `PodeSpawnar`, `Spawnar(prefab,pos)` dual-mode |
| `Assets/scripts/net/NetCombat.cs` | Criar | Flag `DanoHabilitado` (false em rede no 2a) |
| `Assets/scripts/net/EnemyNet.cs` | Criar | `NetworkBehaviour` nos inimigos: fantoche Kinematic + gate de IA no cliente |
| `Assets/scripts/player_stats.cs` | Modificar | `static All` + `MaisProximo` (registro N players) |
| `Assets/scripts/spawn_inimigo.cs` | Modificar | Spawn via `NetSpawn`, host-only, ao redor de N players |
| `Assets/scripts/GerenciadorEventos.cs` | Modificar | Spawns de inimigo de evento via `NetSpawn`, host-only |
| `Assets/scripts/TimerManager.cs` | Modificar | Spawn de boss via `NetSpawn`, host-only |
| `Assets/scripts/movi_inimigo.cs` + callers de inimigo | Modificar | Aggro via `PlayerStats.MaisProximo` |
| Prefabs de inimigo/boss (33) | Modificar | + `NetworkObject` + `NetworkTransform` (Server) + `EnemyNet` |
| `Assets/Scenes/mp_fase_teste.unity` | Modificar | + spawner + registrar Network Prefabs de inimigo |

## 8. Riscos

| Risco | Mitigação |
|---|---|
| 33 prefabs é muito | Script de editor pra adicionar os componentes em lote; verificar 1 por tipo |
| `NetworkObject` inerte quebrar SP | Inimigos são instanciados (não scene-placed); testar regressão da primeira_fase |
| IA de boss rodando no cliente | `EnemyNet` gateia IA por `IsServer`; abilities desligadas pela flag de dano |
| Spawn ao redor de N players com câmeras diferentes | Host escolhe um player e usa a câmera/posição dele como referência; aceitável no 2a |
| FlowField single-target | Fora do 2a (campo aberto); follow-up |

## 9. Testes

- **Co-op:** MPPM (2 players) em `mp_fase_teste` — validar §5.
- **Regressão SP:** `primeira_fase` — horda/eventos/boss spawnam e perseguem igual; com dano normal (flag true em SP).
- Sem testes unitários (integração de rede + gameplay); aceite = comportamento observável.

## 10. Próximo passo

Plano de implementação (writing-plans): `NetSpawn`/`NetCombat` → `PlayerStats.All`/aggro → `EnemyNet` + componentes nos prefabs → spawn dual-mode nos 3 caminhos → cena de teste → validação co-op + regressão SP.
