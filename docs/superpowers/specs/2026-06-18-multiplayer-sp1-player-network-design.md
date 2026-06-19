# Multiplayer Co-op — Sub-projeto 1: Jogador em Rede (Design)

**Data:** 2026-06-18
**Status:** aprovado (design); pendente plano de implementação
**Depende de:** Sub-projeto 0 (Fundação de rede) — concluído
**Spec da fundação:** `docs/superpowers/specs/2026-06-18-multiplayer-coop-foundation-design.md`

---

## 1. Objetivo

Trazer o **player real** (`player.prefab`) para a rede: dois jogadores reais spawnam numa fase de teste, cada um controla o próprio personagem (movimento, dash, ataque, skills disparando), com **posição e animação sincronizadas** para os outros e câmera por cliente — **sem quebrar o single-player**.

Vida, dano, downed/revive e inimigos **não** entram aqui (Sub-projeto 2).

## 2. Decisões travadas (contexto)

- **Single-player sempre jogável** durante toda a transição → caminho de rede em paralelo, sem quebrar o existente.
- Cena de **teste isolada** (sem spawners de inimigos/managers que ainda não são de rede).
- Listen-server, host autoritativo (da Fundação).

## 3. Estado atual relevante (achados)

- `player.prefab` é **colocado na cena** (instância fixa; ~31 refs em `primeira_fase`), não instanciado. Componentes: `Animator`, `Rigidbody2D`, `SpriteRenderer`, `moviment_player2`, `player_stats` (2087 linhas — input, vida, skills, ultimate, elementos, morte), além de efeitos (dash, spawn, morte, etc.).
- Personagem vem de `PlayerPrefs["SelectedCharacter"]` aplicado em `ApplyCharacterData()` no `Awake`.
- **43 arquivos** usam `FindFirstObjectByType<PlayerStats>()` / `FindWithTag("Player")` — a suposição "um player só". A maioria é inimigo/evento (Sub-projeto 2); poucos são locais (câmera/HUD).

## 4. Escopo

### Inclui

- Cena de teste `mp_fase_teste` (chão + spawn points; **sem** spawners de inimigos/managers).
- `NetworkPlayer.prefab` (variante de `player.prefab`) como PlayerPrefab do NGO.
- Gating dual-mode (`IsLocalAuthority`) para input e lógica de dono.
- Registro `PlayerStats.Local` + migração dos chamadores **locais** (câmera/HUD/painel de status).
- Sync de posição (`NetworkTransform` Owner) e animação (`NetworkAnimator`).
- Câmera por cliente seguindo `PlayerStats.Local`.

### NÃO inclui (fora de escopo)

- Vida, dano, downed, revive, game over de grupo → Sub-projeto 2.
- **Projéteis/skills em rede**: no SP1 o colega vê seu **corpo e animação**, mas **não os seus tiros** (cópias remotas não disparam skills; networking de projéteis vem no Sub-projeto 2, onde causam dano a inimigos de rede).
- Inimigos, spawners, eventos.
- Conversão das cenas de fase reais (`primeira_fase`, etc.) → Sub-projeto 2.
- Migração dos chamadores de `FindFirstObjectByType<PlayerStats>` de inimigo/evento → Sub-projeto 2.
- Lobby/seleção de personagem em rede → Sub-projeto 5.

## 5. Marco de "pronto" (critério de aceite)

Via Multiplayer Play Mode (2 players virtuais), em `mp_fase_teste`:

- [ ] Dois avatares de player aparecem; cada instância controla **só o seu**.
- [ ] Movimento e **animação** (andar/atacar) do dono **aparecem na outra instância**.
- [ ] A câmera de cada cliente segue o **próprio** player.
- [ ] Cada cliente aplica o próprio personagem (`SelectedCharacter` local).
- [ ] **Single-player intacto:** abrir `primeira_fase` e jogar normalmente — movimento, dash, skills, ultimate funcionam como antes (0 regressão).

## 6. Arquitetura

### 6.1 Prefab variante (não tocar no original)

`NetworkPlayer.prefab` = **Prefab Variant** de `player.prefab`, adicionando:
- `NetworkObject` (NGO).
- `NetworkTransform` com **Authority Mode = Owner** (2D: sincroniza X/Y, sem Z/rotação).
- `NetworkAnimator` (NGO) referenciando o `Animator` existente.

O `player.prefab` base permanece **inalterado** e continua sendo usado pelas cenas single-player. Como os scripts são compartilhados, o gating dual-mode (§6.2) garante comportamento idêntico no SP.

### 6.2 Gating dual-mode — `IsLocalAuthority`

Helper acessível aos scripts do player (propriedade em `PlayerStats`, e/ou util estático `NetAuthority.IsLocal(GameObject)`):

```
IsLocalAuthority =
    (sem NetworkObject) OU (NetworkObject não spawnado) OU (NetworkObject.IsOwner)
```

- **Single-player** (base prefab, sem `NetworkObject`): sempre `true` → input e lógica rodam como hoje.
- **Co-op, dono**: `true` → lê input, roda skills/ultimate/dash/troca de elemento, ataca.
- **Co-op, cópia remota**: `false` → não lê input, não roda lógica de dono; é fantoche movido por `NetworkTransform`/`NetworkAnimator`.

Pontos a gatear (em `player_stats.cs` e `moviment_player2.cs`): leitura de input de movimento, dash, ataque/skills, ultimate, troca de elemento, e qualquer `Update` que dependa de input ou de ser "o jogador local".

### 6.3 Registro `PlayerStats.Local`

```
public static PlayerStats Local { get; private set; }
```

Setado quando a instância é a autoridade local:
- Single-player: no `Awake`/`Start` (é o único player).
- Co-op: em `OnNetworkSpawn`, se `IsOwner`.

Limpo em `OnDestroy`/`OnNetworkDespawn` se for o Local.

Migrar **somente os chamadores locais** de `FindFirstObjectByType<PlayerStats>()` para `PlayerStats.Local`: câmera de gameplay, HUD/`UIManager`, painel de status (Tab). Os chamadores de inimigo/evento ficam intocados (Sub-projeto 2). O plano listará exatamente quais arquivos são "locais".

### 6.4 Spawn + personagem

- `mp_fase_teste` **não** tem player fixo na cena; `NetworkManager.NetworkConfig.PlayerPrefab = NetworkPlayer.prefab`.
- Ao conectar, o NGO spawna um `NetworkPlayer` por cliente; cada um aplica o próprio `SelectedCharacter` (PlayerPrefs local) via `ApplyCharacterData()`.
- Spawn points: o `NetworkPlayer` é posicionado num spawn point (lista simples na cena ou offset por `OwnerClientId`) para não nascerem sobrepostos.

### 6.5 Câmera por cliente

Câmera de gameplay segue `PlayerStats.Local` (padrão estabelecido no SP0): em `OnNetworkSpawn`/`Start`, se autoridade local, a câmera principal passa a seguir este player (script de follow simples em `LateUpdate`).

## 7. Estrutura de arquivos

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/prefebs/net/NetworkPlayer.prefab` | Criar | Variante de player.prefab + NetworkObject/NetworkTransform/NetworkAnimator |
| `Assets/Scenes/mp_fase_teste.unity` | Criar | Cena de teste: NetworkManager+UTP, chão, spawn points, ConnectUI (reusa `SandboxConnectUI`) |
| `Assets/scripts/net/NetAuthority.cs` | Criar | Helper `IsLocalAuthority`/`IsLocal(GameObject)` |
| `Assets/scripts/net/PlayerCameraFollow.cs` | Criar | Câmera por cliente seguindo `PlayerStats.Local` |
| `Assets/scripts/player_stats.cs` | Modificar | `IsLocalAuthority`, `static Local`, gatear input/lógica de dono |
| `Assets/scripts/moviment_player2.cs` | Modificar | Gatear leitura de input de movimento por `IsLocalAuthority` |
| (chamadores locais de FindFirstObjectByType) | Modificar | Trocar por `PlayerStats.Local` — lista no plano |

## 8. Tratamento de erros / riscos

| Risco | Mitigação |
|---|---|
| Gating quebrar o single-player | `IsLocalAuthority` é `true` por padrão sem rede; teste de regressão da `primeira_fase` é critério de aceite |
| `NetworkAnimator` divergir de Animator com muitos parâmetros | Começar sincronizando o essencial (`Speed`/triggers de ataque); ajustar no teste |
| Variante divergir do base ao editar o original | Variante herda mudanças do base automaticamente; só os componentes de rede são overrides |
| `PlayerStats.Local` nulo em sistemas locais antes do spawn | Sistemas locais checam null e/ou se inscrevem após o spawn |
| Player fixo remanescente em cena de teste causar 2 players locais | `mp_fase_teste` não tem player fixo; só o spawnado |

## 9. Testes

- **Co-op:** Multiplayer Play Mode (2 virtuais) em `mp_fase_teste` — validar §5 (avatares, movimento+animação replicados, câmera por cliente, personagem por cliente).
- **Regressão single-player:** abrir `primeira_fase`, jogar — movimento/dash/skills/ultimate iguais ao de hoje. Esse teste é obrigatório antes de fechar.
- Sem testes unitários automatizados (integração de rede + gameplay); aceite é comportamento observável.

## 10. Próximo passo

Transformar em plano de implementação (writing-plans), com tarefas bite-sized: `NetAuthority` → `PlayerStats.Local` + gating → variante `NetworkPlayer` → câmera por cliente → migrar chamadores locais → cena `mp_fase_teste` → testar marco + regressão SP.
