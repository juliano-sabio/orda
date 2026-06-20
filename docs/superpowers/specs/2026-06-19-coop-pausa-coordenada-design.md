# Pausa coordenada co-op (host-autoritativa) — Design

**Data:** 2026-06-19
**Sub-projeto:** Pausa coordenada (parte do multiplayer co-op; ver [[project_multiplayer_coop]])
**Branch alvo:** continua em `feat/mp-lobby` (sub-projeto próprio dentro do esforço de lobby/co-op)

## Problema

Em co-op, `Time.timeScale = 0` é **local**: não coordena entre clientes nem congela a
simulação autoritativa dos inimigos no host de forma sincronizada. Hoje, quando um player
abre uma escolha (skill/carta/evolução/elemento) ou o menu de pausa, só o `timeScale`
**dele** zera — a horda continua andando pros outros e o timer segue correndo. O usuário
quer: *"nas seleções de skills choise etc o tempo só pode rodar quando os dois tiverem
escolhido suas coisas"*.

Além disso, o **game over do grupo** em co-op hoje só mostra a tela (`GameOverUI.Mostrar()`);
deve **devolver todos ao `lobby_mp`** (sessão NGO viva → o grupo re-escolhe e recomeça).

## Escopo

**Dentro:**
1. Escolhas de gameplay (4 UIs): `Skill_choice_UI`, `StatusCardChoiceUI`, `SkillEvolutionUI`,
   `ElementApplicationUI` — congelam horda+timer **para todos** até **todos** terminarem.
2. Menu de pausa (`PauseManager`) — qualquer player abrir congela todos; **qualquer um** pode retomar.
3. Game over do grupo — mostra a tela ~4s (tempo real) e o **host** devolve todos ao `lobby_mp`
   automaticamente (`LobbyState.EmLobby = true`).

**Fora:**
- Vitória / escolha pós-vitória (`VitoriaUI`, `EscolhaPosVitoriaUI`) → SP4 (run-cycle).
- Skills/projéteis em rede (companheiro ver as skills do outro) → item separado da auditoria.
- Efeito de início de fase (`PlayerSpawnEffect`) em co-op → item separado.

**Single-player intocado:** sem `NetworkManager`, todas as 5 UIs e o game over seguem o
comportamento atual (a migração troca só a *chamada*, não a lógica).

## Princípio-chave

O NGO **não** é gated por `Time.timeScale`: RPCs e `NetworkVariable` continuam fluindo com
`timeScale = 0`. Então a *decisão* de pausar vira host-autoritativa (um `NetworkVariable<bool>
pausado`), mas o *mecanismo* continua sendo `Time.timeScale = 0/1` — dirigido pelo valor
sincronizado em **todos** os clientes, inclusive o host (cujo `deltaTime = 0` congela a
IA/movimento dos inimigos e o timer).

## Arquitetura

Duas peças + migração cirúrgica das UIs:

### `CoopPauseManager : NetworkBehaviour` (host-autoritativo, 1 por fase)

Singleton (padrão do `LobbyManager`): `Instance` setado em `OnNetworkSpawn`, limpo em `OnNetworkDespawn`.

Estado só-no-host (não sincronizado diretamente):
- `HashSet<ulong> retentoresEscolha` — clientIds atualmente numa escolha. Um player só tem
  uma escolha aberta por vez (modais sequenciais), então `Add`/`Remove` idempotente.
- `bool menuAberto` + `ulong menuDono` — menu de pausa do grupo e quem abriu.

Sincronizado:
- `NetworkVariable<bool> pausado` (read Everyone / write Server) =
  `retentoresEscolha.Count > 0 || menuAberto`.
- `NetworkVariable<ulong> donoMenu` (read Everyone / write Server) — pro overlay "Fulano pausou"
  (relevante só quando `menuAberto`).

ServerRpcs (usam `RpcParams` pra extrair o `SenderClientId`):
- `ReterEscolhaServerRpc(RpcParams)` → `retentoresEscolha.Add(sender)` → `Recomputar()`.
- `LiberarEscolhaServerRpc(RpcParams)` → `retentoresEscolha.Remove(sender)` → `Recomputar()`.
- `AbrirMenuServerRpc(RpcParams)` → `menuAberto = true; menuDono = sender` → `Recomputar()`.
- `FecharMenuServerRpc()` → `menuAberto = false` → `Recomputar()`. (qualquer um pode chamar)

`Recomputar()` (só no host): `pausado.Value = retentoresEscolha.Count > 0 || menuAberto;`
`donoMenu.Value = menuDono;`

Aplicação do timeScale (todos os clientes):
- `pausado.OnValueChanged += (_, novo) => Time.timeScale = novo ? 0f : 1f;`
- Em `OnNetworkSpawn`, aplica o valor atual (cliente que entra no meio respeita a pausa vigente).

Robustez (host): no `NetworkManager.OnClientDisconnectCallback` →
`retentoresEscolha.Remove(id)`; se `menuDono == id` → `menuAberto = false`; `Recomputar()`.
(Evita pausa travada por desconexão segurando uma escolha/menu.)

### `CoopPause` (fachada estática — API que as UIs chamam)

- `EmRede` → `NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening &&
  CoopPauseManager.Instance != null`.
- `ReterEscolha()` → co-op: `CoopPauseManager.Instance.ReterEscolhaServerRpc()`; SP: `Time.timeScale = 0f`.
- `LiberarEscolha()` → co-op: `LiberarEscolhaServerRpc()`; SP: `Time.timeScale = 1f`.
- `AbrirMenu()` → co-op: `AbrirMenuServerRpc()`; SP: `Time.timeScale = 0f`.
- `FecharMenu()` → co-op: `FecharMenuServerRpc()`; SP: `Time.timeScale = 1f`.

Observação: em SP a fachada apenas reproduz o `Time.timeScale = 0/1` que as UIs já faziam.
As UIs que guardam `previousTimeScale` continuam fazendo isso localmente (a fachada não
substitui o save/restore — só o ponto onde zera/restaura passa por ela quando em rede).

### Spawn do manager

O host instancia+spawna o prefab `CoopPauseManager` em `FaseCoopBootstrap.Start` (reusa o
padrão `LobbyManager`/`LobbyBootstrap`): se `IsServer` e `CoopPauseManager.Instance == null`,
`Instantiate(prefab)` + `NetworkObject.Spawn()`. Prefab registrado no `DefaultNetworkPrefabs.asset`.

### Migração das UIs (troca mecânica)

| Arquivo | Linha(s) atuais | Troca |
|---|---|---|
| `Skill_choice_UI.cs` | 781-782 (zera), 791 (restaura) | `CoopPause.ReterEscolha()` / `CoopPause.LiberarEscolha()` |
| `StatusCardChoiceUI.cs` | 552-553 (zera), 560 (restaura) | idem |
| `SkillEvolutionUI.cs` | 44 (zera), 152 (restaura) | idem |
| `ElementApplicationUI.cs` | 69 (zera), 401 (restaura) | idem |
| `PauseManager.cs` | 435-436 (zera), 452 (restaura) | `CoopPause.AbrirMenu()` / `CoopPause.FecharMenu()` |

Em cada UI: onde hoje há `Time.timeScale = 0f` (abrir), chamar `CoopPause.ReterEscolha()`
(ou `AbrirMenu` no PauseManager); onde há `Time.timeScale = (previousTimeScale|1f)` (fechar),
chamar `CoopPause.LiberarEscolha()` (ou `FecharMenu`). Em SP a fachada faz exatamente o
`timeScale` de antes, então o comportamento single-player é idêntico.

### Game over do grupo → lobby

Hoje (`PlayerNet.cs`):
- `MonitorarHost()` (host) detecta `todosCaidos` → `GameOverGrupoRpc()`.
- `GameOverGrupoRpc` (`SendTo.Everyone`) → `GameOverUI.Mostrar()`.

Mudança: quando `todosCaidos` dispara no host, além do `GameOverGrupoRpc()`, o host inicia
uma corrotina em **tempo real** (`WaitForSecondsRealtime(4f)` — porque `GameOverUI.Mostrar()`
zera o `timeScale` e `WaitForSeconds` não avançaria) e então
`NetworkManager.SceneManager.LoadScene("lobby_mp", LoadSceneMode.Single)`. Ao carregar o lobby,
o fluxo de lobby reativa `LobbyState.EmLobby = true` (já existente em `LobbyCoopUI.Start`).
Em SP, `GameOverGrupoRpc` nem é chamado (é caminho de rede), então o game over single-player
fica intocado.

Detalhe: antes de carregar o lobby, o host deve restaurar `Time.timeScale = 1f` (senão o
lobby carrega congelado).

## Fluxo de dados

**Escolha (Player B sobe de nível):**
1. UI de B abre → `CoopPause.ReterEscolha()` → `ReterEscolhaServerRpc` (sender=B).
2. Host: `retentoresEscolha={B}` → `pausado=true` → propaga → todos setam `timeScale=0`.
   Inimigos do host congelam (`deltaTime=0`), timer para; RPCs/UI seguem.
3. B escolhe → UI fecha → `LiberarEscolha()` → `{}` → `pausado=false` → todos retomam.
4. Se A também subiu junto: `{A,B}` → pausado até **ambos** fecharem.

**Menu de pausa:** A aperta ESC → `AbrirMenu()` → `menuAberto=true, menuDono=A` → todos
congelam (B vê overlay "A pausou"). Qualquer um fecha → `FecharMenu()` → retoma.

**Combinação:** `pausado = retentoresEscolha.Count>0 || menuAberto`. Fechar o menu não
destrava se uma escolha ainda segura.

**Game over:** todos caídos → host: `GameOverGrupoRpc()` (tela em todos) + corrotina realtime
4s → `timeScale=1` → `LoadScene(lobby_mp)`.

## Casos de borda

- **Desconexão segurando pausa:** host remove o id de `retentoresEscolha` / limpa `menuDono` →
  recomputa. Sem pausa travada.
- **Latência:** quem abre a escolha vê `timeScale=0` ~1 RTT depois (host confirma). A UI já
  está na tela e recebe input; atraso imperceptível. 100% host-dirigido pra não desincronizar.
- **Cliente entrando no meio:** `OnNetworkSpawn` aplica o `pausado` atual.
- **Lobby congelado:** host restaura `timeScale=1` antes de `LoadScene(lobby_mp)`.

## Arquivos

**Criar:**
- `horda/Assets/scripts/net/CoopPauseManager.cs`
- `horda/Assets/scripts/net/CoopPause.cs`
- `horda/Assets/prefebs/net/CoopPauseManager.prefab` (+ registro em `DefaultNetworkPrefabs.asset`)

**Modificar:**
- `horda/Assets/scripts/net/FaseCoopBootstrap.cs` (spawn do manager no host)
- `horda/Assets/scripts/net/PlayerNet.cs` (game over do grupo → corrotina realtime → lobby)
- `horda/Assets/scripts/UI/Skill_choice_UI.cs`
- `horda/Assets/scripts/UI/StatusCardChoiceUI.cs`
- `horda/Assets/scripts/SkillEvolution/SkillEvolutionUI.cs`
- `horda/Assets/scripts/UI/ElementApplicationUI.cs`
- `horda/Assets/scripts/configuração _adicional/PauseManager.cs`

## Testes

Dado o freeze de play-mode sem foco do editor (MCP), o teste de Update contínuo é manual (MPPM).

**Verificável por mim (MCP):**
- Compilação 0 erros após cada mudança (`refresh_unity` → `read_console types=["error"]`).
- `CoopPause` em SP (sem rede) seta `Time.timeScale` corretamente (`execute_code` chamando
  `ReterEscolha`/`LiberarEscolha`/`AbrirMenu`/`FecharMenu` e lendo `Time.timeScale`).
- Lógica de `Recomputar()`: dado um conjunto de retentores + estado de menu, `pausado` correto
  (testável no host via `execute_code` sem 2 instâncias).

**Verificável por você (MPPM, 2 instâncias):**
- Escolha de um trava os dois; escolha simultânea destrava só quando ambos fecham.
- Menu de pausa de um congela ambos; qualquer um retoma.
- Game over do grupo: tela ~4s e volta ao `lobby_mp` com os dois conectados.
