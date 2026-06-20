# Multiplayer Co-op — Lobby Completo (Design)

**Data:** 2026-06-19
**Status:** aprovado (design); pendente plano de implementação
**Depende de:** SP0..SP2c + integração de fase (primeira_fase_mp) — concluídos
**Substitui parcialmente:** a integração in-fase (a conexão sai da fase e vai pro lobby)

---

## 1. Objetivo

A conexão e a preparação do co-op acontecem num **lobby**, não dentro da fase: os jogadores conectam por **join code**, escolhem **personagem, passiva e ultimate**, o host escolhe a **fase**, todos dão **ready**, e o host inicia → o NGO **carrega a fase com todos juntos**. Single-player intocado.

## 2. Por que (motivação do usuário)

Conectar já dentro da fase (o `ConnectUI` da integração) é o fluxo errado e tecnicamente frágil (scene-sync de cena complexa). O padrão correto do NGO: conectar numa **cena de lobby simples** → o host faz `NetworkManager.SceneManager.LoadScene(fase)`. O lobby também é onde se escolhe personagem/passiva/ultimate/fase.

## 3. Estado atual relevante

- `RelayConnector`/`NetBootstrap` (SP0): host/join por código. `SandboxConnectUI` é teste.
- `MenuInicialUI`: botões "Criar sala" (`LobbyHost=1`, código local) e "Entrar sala" carregam a cena `lobby`. Tudo simulado.
- `LobbyUI` (940 linhas) é **maquete** (jogadores falsos, `GerarCodigo`, `SimularEntrada`).
- Seleção real em `Character_Selection_Manager` (cena `CharacterSelection`): `SelectedCharacter`, `SelectedPassiva_{char}`, `SelectedUltimate_{char}` em PlayerPrefs.
- `PlayerNet` (SP1+) já sincroniza `charIndex`. `player_stats.ApplyCharacterData(idx)` aplica char + lê passiva/ultimate do PlayerPrefs.
- `NetworkPlayer` é o PlayerPrefab; `UIDark` (helper dark-fantasy) existe.

## 4. Escopo

### Inclui
- Cena de lobby em rede `lobby_mp` com **NetworkManager persistente** (DontDestroyOnLoad).
- Menu "Criar sala"/"Entrar sala" → `lobby_mp` (host/cliente) conectando via `RelayConnector`.
- **Roster sincronizado**: os `NetworkPlayer` spawnados servem de roster (`charIndex` + `ready` + `nome`).
- **UI de lobby nova** (código, tema `UIDark`): código da sala + copiar, lista de jogadores (nome/personagem/ready), seleção de **personagem/passiva/ultimate** por jogador, **seletor de fase** (host), **ready**, **iniciar** (host).
- **Gameplay congelado** no lobby (player não move/ataca/toma dano).
- **Scene-load NGO** → fase escolhida; players persistem; gameplay liga; cada um aplica suas escolhas.
- Limpeza: `primeira_fase_mp` perde `ConnectUI` + `NetworkManager` próprio (recebe os players do lobby).

### NÃO inclui (deferido)
- Reconexão / host-migration.
- Unity **Lobby service** (matchmaking/lista pública) — usamos só Relay + join code (roster via NGO).
- Seleção de dificuldade sincronizada além do básico (mapa/fase): difficulty fica simples por ora.
- Repolimento fino do visual da maquete antiga (o `LobbyUI` antigo pode ser aposentado).

## 5. Marco de "pronto"

Via MPPM (2 players):
- [ ] Menu "Criar sala" → host entra no `lobby_mp`, vê **join code**; "Entrar sala" → cliente digita o código e entra.
- [ ] O **roster** mostra os 2 jogadores (nome + personagem + estado ready), atualizando ao vivo.
- [ ] Cada jogador escolhe **personagem/passiva/ultimate**; o personagem aparece no roster pros dois.
- [ ] O **host escolhe a fase**; ambos veem qual é.
- [ ] Ambos dão **ready**; o host clica **Iniciar** → os dois **carregam a fase juntos** e spawnam com suas escolhas.
- [ ] No lobby ninguém "joga" (sem movimento/dano).
- [ ] **Regressão SP:** menu "Start" (single-player) → `CharacterSelection` → fase, **igual a antes**.

## 6. Arquitetura

### 6.1 NetworkManager persistente + player como roster
O `NetworkManager` (com `UnityTransport`, PlayerPrefab=`NetworkPlayer`) vive em `lobby_mp` e é DontDestroyOnLoad (padrão do NGO ao iniciar). Persiste na fase. **As fases co-op não têm NetworkManager próprio.** Ao conectar, o `NetworkPlayer` spawna no lobby e **persiste** no scene-load pra fase (não re-spawna). O roster lê os players spawnados (`PlayerStats.All`/`PlayerNet`).

### 6.2 Estado de lobby no PlayerNet
Adicionar NetworkVariables (owner-write) ao `PlayerNet`:
- `ready` (bool) — pronto.
- `playerName` (FixedString) — nome (default "Jogador N" por OwnerClientId).
- (`charIndex` já existe — passa a ser editável no lobby.)

### 6.3 LobbyManager (host-autoritativo)
`LobbyManager` (`NetworkBehaviour`, spawnado pelo host no `lobby_mp`):
- `NetworkVariable<int> faseEscolhida` (server-write) — índice/nome da fase escolhida pelo host (pra todos verem).
- `IniciarServerRpc()` (host) — checa todos ready → `NetworkManager.SceneManager.LoadScene(fase, Single)`.

### 6.4 UI de lobby (`LobbyCoopUI`, nova, tema UIDark)
- Painel de sala: código + botão copiar (do `RelayConnector`).
- Lista de jogadores: por player spawnado, mostra nome + personagem (de `charIndex`) + ready.
- Seleção: grid de personagens + passiva + ultimate → grava PlayerPrefs (`SelectedCharacter`, `SelectedPassiva_{c}`, `SelectedUltimate_{c}`) e seta `charIndex` (owner). 
- Host: seletor de fase (`faseEscolhida`) + botão **Iniciar** (habilitado quando todos ready).
- Cliente: botão **Pronto** (toggla `ready`).
- Botão **Sair** → desconecta e volta ao menu.

### 6.5 Gameplay congelado no lobby
Estado `LobbyState.EmLobby` (estático): true no `lobby_mp`, false quando a fase carrega (callback de scene-load do NGO ou no `Start` da fase). `player_stats`/`moviment_player2` gateiam por `!EmLobby` (além de `IsLocalAuthority`/`EstaCaido`). Dano também gateado (`NetCombat`/EmLobby).

### 6.6 Menu → lobby
`MenuInicialUI`:
- "Criar sala": carrega `lobby_mp`; no `Start` do lobby, se host, `NetBootstrap.InitAsync` + `RelayConnector.HostAsync` → exibe o código.
- "Entrar sala": carrega `lobby_mp` em modo cliente com um campo pra digitar o código → `RelayConnector.JoinAsync`.
(Usa um flag tipo `PlayerPrefs["LobbyHost"]` como hoje.)

### 6.7 Scene-load → fase
`LobbyManager.Iniciar` (host) → `NetworkManager.SceneManager.LoadScene(faseEscolhida, Single)`. NGO carrega a fase em todos; o `NetworkPlayer` persiste. No `Start`/callback da fase: `LobbyState.EmLobby=false`; os players são reposicionados (spawn points da fase) e o gameplay liga. Cada player aplica char/passiva/ultimate do próprio PlayerPrefs.

### 6.8 Fases co-op
`primeira_fase_mp`: remover `ConnectUI` e o `NetworkManager` próprio (o persistente do lobby cuida). Manter spawner/terreno/HUD/`PlayerCameraFollow` via câmera-filha do player.

## 7. Estrutura de arquivos

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/Scenes/lobby_mp.unity` | Criar | Cena de lobby: NetworkManager persistente, LobbyManager, LobbyCoopUI |
| `Assets/scripts/net/LobbyManager.cs` | Criar | `NetworkBehaviour`: faseEscolhida, IniciarServerRpc (scene-load) |
| `Assets/scripts/net/LobbyState.cs` | Criar | Estado estático `EmLobby` + toggle no scene-load |
| `Assets/scripts/UI/LobbyCoopUI.cs` | Criar | UI de lobby (código, roster, seleção, fase, ready, iniciar) — tema UIDark |
| `Assets/scripts/net/PlayerNet.cs` | Modificar | + `ready`/`playerName` NetworkVariables; charIndex editável no lobby |
| `Assets/scripts/player_stats.cs`, `moviment_player2.cs` | Modificar | Gate por `LobbyState.EmLobby` |
| `Assets/scripts/UI/MenuInicialUI.cs` | Modificar | "Criar/Entrar sala" → `lobby_mp` + Relay real |
| `Assets/Scenes/primeira_fase_mp.unity` | Modificar | Remover ConnectUI + NetworkManager próprio |

## 8. Riscos

| Risco | Mitigação |
|---|---|
| Handoff lobby→fase (player persiste) | NetworkManager persistente; player é PlayerPrefab spawnado uma vez; reposicionar na fase |
| Scene-sync da fase complexa | Carregar via NGO a partir do lobby (sem inimigos pré-spawnados no load); NetworkManager limpo (auto-default) resolve prefabs como no mp_fase_teste |
| Gameplay rodando no lobby | `LobbyState.EmLobby` gateia movimento/ataque/dano |
| Seleção por player não sincronizar visual | `charIndex` (owner NetVar) já replica o personagem |
| Quebrar single-player | `lobby_mp` é fluxo separado (botão "Criar/Entrar sala"); "Start" SP intocado; teste de regressão |
| Tamanho do sub-projeto | Maior até agora; o plano deve quebrar em tarefas bem pequenas e testar incrementalmente |

## 9. Testes
- **Co-op:** MPPM (2 players) — menu → lobby → seleção → host inicia → fase. Validar §5.
- **Regressão SP:** menu "Start" → CharacterSelection → fase normal.
- Sem testes unitários; aceite = comportamento observável.

## 10. Próximo passo
Plano (writing-plans), em tarefas pequenas: PlayerNet (ready/nome) → LobbyState + gates → LobbyManager → cena lobby_mp (NetworkManager persistente) → LobbyCoopUI (código/roster/seleção/fase/ready/iniciar) → menu → scene-load → limpar primeira_fase_mp → validação.
