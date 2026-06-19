# Multiplayer Co-op — Sub-projeto 0: Fundação de Rede (Design)

**Data:** 2026-06-18
**Status:** aprovado (design); pendente plano de implementação
**Jogo:** Horda / Orda — horde roguelike, Unity 6 (6000.3.9f1), 2D

---

## 1. Visão geral do multiplayer (decisões travadas)

Estas decisões valem pra TODO o multiplayer e contextualizam a fundação:

| Dimensão | Decisão |
|---|---|
| Tipo | **Co-op online** (jogadores em máquinas diferentes, pela internet) |
| Nº de jogadores | **Até 4**; fundação **agnóstica à quantidade**; validar com **2** primeiro |
| Conexão | **Código de sala / convite** via **Unity Relay** (+ Lobby depois) |
| Topologia | **Listen-server**: o host é jogador e autoridade. Sem servidor dedicado |
| Stack | **Netcode for GameObjects (NGO)** + Unity Transport + Relay + Authentication |
| Progressão | **Nível/XP compartilhado**; **skills/builds individuais** (cada player escolhe a própria skill no level-up) |
| Morte | **Downed + revive** por companheiro; **todos caídos ao mesmo tempo → game over do grupo** |

### Por que decompor

O jogo é fortemente acoplado a "um jogador local": 265 scripts C#, **19 singletons**, **132 scripts referenciam `PlayerStats`**. `player_stats.cs` lê input diretamente no `Update` (`Input.GetAxis`, ultimate, skills). Transformar isso em rede de uma vez é inviável de implementar e revisar. Em vez disso, o multiplayer é quebrado em sub-projetos encadeados, cada um implementável e testável sozinho.

### Roadmap (cada item = spec + plano + implementação próprios)

0. **Fundação de rede** — *este documento*. Stack + conexão + player sincronizado mínimo num sandbox.
1. **Jogador em rede** — trazer o `player.prefab` real pra rede: `PlayerStats` consciente de owner/autoridade, vida/dano, downed/revive, input gated por `IsOwner`, ataque básico replicado.
2. **Horda em rede (host-autoritativa)** — `SpawnInimigo` no host; `InimigoController` como `NetworkObject` (IA, posição, vida, morte); aggro entre N players; `EnemyScaling` consistente.
3. **Progressão co-op** — XP/nível compartilhado host-autoritativo; level-up abre escolha de skill por player; builds/evoluções/cartas de status individuais e replicados; resolver os 19 singletons (per-player vs server).
4. **Run-cycle co-op** — `TimerManager` host-autoritativo; bosses como `NetworkObject` com escala consistente; regra de todos-caídos → game over; `EscolhaPosVitoriaUI`/`VitoriaUI`/modo infinito sincronizados; eventos host-autoritativos.
5. **Lobby/UX & polimento** — **religar a maquete existente** (`LobbyUI.cs`, hoje 100% simulada) aos serviços reais (Unity Lobby + Relay): roster sincronizado, ready-up real, join code real, seleção de personagem por player, config de sala host-autoritativa, transição lobby→jogo via scene load em rede. Resolver as redundâncias de fluxo (ver §10). Reconexão e host-migration **fora do v1**. Detalhes na §10.

---

## 2. Escopo do Sub-projeto 0

### O que É

Uma **prova de conceito isolada** da stack de rede. Cena nova `mp_sandbox` com um **player mínimo de teste** (cápsula: move + sincroniza posição/animação + câmera própria por cliente). UI de teste tosca (campo de join code + botões Host/Join).

### O que NÃO é (fora de escopo, vem nos próximos sub-projetos)

- O `player.prefab` real, `PlayerStats`, skills, ultimate, dash, elementos.
- Horda/inimigos, spawn, run-cycle, XP, level-up.
- Lobby (roster, ready-up), seleção de personagem, nomes, reconexão, host-migration.
- Qualquer alteração em `primeira_fase`/`segunda_fase`/`terceira_fase` ou nos managers existentes.

### Marco de "pronto" (critério de aceite)

Dois processos do jogo (via Multiplayer Play Mode) conectam por join code e **cada um vê o avatar do outro se movendo em tempo real**, com a câmera de cada cliente seguindo o próprio player. Desconectar um cliente não derruba o host.

---

## 3. Stack & pacotes

Modelo **listen-server**: `StartHost()` torna o host autoridade + jogador; clientes usam `StartClient()`.

Pacotes a adicionar ao `Packages/manifest.json`:

| Pacote | Função |
|---|---|
| `com.unity.netcode.gameobjects` | NGO — sincronização de NetworkObjects, RPCs, spawn |
| `com.unity.transport` | Unity Transport (UTP) — camada de transporte (vem como dependência do NGO) |
| `com.unity.services.core` | Base dos Unity Gaming Services |
| `com.unity.services.authentication` | Login anônimo (exigido pelos UGS) |
| `com.unity.services.relay` | Relay — atravessa NAT/firewall, gera/resolve join code |
| `com.unity.multiplayer.playmode` | Multiplayer Play Mode (virtual players) p/ testar 2+ instâncias no editor |

**Lobby (`com.unity.services.lobby`) NÃO entra agora** — Relay + join code já conecta. Lobby é UX do Sub-projeto 5.

> Pré-requisito de conta: o projeto precisa estar vinculado a uma organização Unity Gaming Services (Relay e Authentication são serviços de nuvem com tier gratuito). Faz parte do setup do plano.

---

## 4. Fluxo de conexão

```
HOST                                   CLIENTE
----                                   -------
UnityServices.InitializeAsync()        UnityServices.InitializeAsync()
SignInAnonymouslyAsync()               SignInAnonymouslyAsync()
RelayService.CreateAllocationAsync(maxPlayers)
GetJoinCodeAsync()  ── join code ──▶   (recebe o código por fora: chat/voz)
configura UnityTransport (Relay host)  RelayService.JoinAllocationAsync(code)
NetworkManager.StartHost()             configura UnityTransport (Relay client)
                                       NetworkManager.StartClient()
        ◀───────── conexão NGO via Relay ─────────▶
```

- `maxPlayers` configurável (default 4) — agnóstico à quantidade.
- O join code é uma string curta (ex.: `ABC123`) exibida na UI do host.
- Troca do código entre jogadores é manual no v1 (fora do jogo). Convite/Lobby é Sub-projeto 5.

---

## 5. Sincronização do player & câmera

- **Player de teste**: prefab `SandboxPlayer` com `NetworkObject` + `NetworkTransform` (modo **owner-authoritative / client network transform**: o dono escreve a posição, os demais leem). Sprite/cápsula simples + `Rigidbody2D` opcional só pra mover.
- **Spawn**: NGO instancia automaticamente o Player Prefab por conexão (`NetworkManager.NetworkConfig.PlayerPrefab = SandboxPlayer`). Cada cliente vira dono do seu.
- **Input**: lido **somente** quando `IsOwner == true`. Sem isso, todo cliente moveria todos os avatares.
- **Movimento**: client-authoritative (owner-writes). Justificativa: responsivo, trivial de implementar, e não há ameaça de cheating entre amigos no v1. Server-authoritative com predição/reconciliação é explicitamente adiado (custo alto, ganho nulo agora).
- **Câmera**: cada cliente instancia/ativa a própria câmera seguindo o player de que é dono (em `OnNetworkSpawn`, `if (IsOwner)` liga a câmera local). Estilo Risk of Rain 2: cada máquina renderiza a própria visão — sem split-screen, sem amarra.
- **Animação** (se a cápsula tiver): um `NetworkAnimator` ou uma `NetworkVariable` de estado simples. Mínimo possível.

---

## 6. Estrutura de arquivos

Tudo novo, isolado em `Assets/scripts/net/` e `Assets/Scenes/`:

| Arquivo | Responsabilidade |
|---|---|
| `Assets/Scenes/mp_sandbox.unity` | Cena de teste: NetworkManager, UI tosca, chão, luz/câmera base |
| `Assets/scripts/net/NetBootstrap.cs` | Inicializa UnityServices + login anônimo (uma vez) |
| `Assets/scripts/net/RelayConnector.cs` | `HostAsync(maxPlayers)` → join code; `JoinAsync(code)`; configura UnityTransport. **O join code real produzido aqui é o que, no Sub-projeto 5, substitui o `GerarCodigo()` falso do `LobbyUI.cs`** |
| `Assets/scripts/net/SandboxConnectUI.cs` | UI de teste: campo de código, botões Host/Join, exibe join code/estado |
| `Assets/scripts/net/SandboxPlayer.cs` | Movimento gated por `IsOwner` + ativação da câmera local |
| `Assets/prefebs/net/SandboxPlayer.prefab` | Player de teste (NetworkObject + NetworkTransform) |

Cada unidade tem uma responsabilidade clara e interface enxuta. `RelayConnector` não conhece UI; `SandboxConnectUI` não conhece detalhes do Relay além de chamar `HostAsync`/`JoinAsync`.

---

## 7. Tratamento de erros

- Falha de init/login dos UGS → mensagem na UI tosca, botões desabilitados.
- Join code inválido / allocation expirada → catch da exceção do Relay, mensagem "código inválido"; permanecer na tela.
- Cliente desconecta → host continua (listen-server resiliente a saída de cliente). Host desconecta → sessão acaba (esperado no v1; reconexão é Sub-projeto 5).

---

## 8. Testes

- **Principal:** Multiplayer Play Mode (virtual players) — abre 2 players virtuais no editor; um faz Host, outro Join pelo código; valida visualmente o marco da §2.
- **Fallback:** 1 build standalone + editor, caso MPPM não funcione no setup.
- Não há testes unitários automatizados aqui — é um spike de integração de rede; o aceite é o comportamento observável do marco.

---

## 9. Riscos & mitigações

| Risco | Mitigação |
|---|---|
| Setup de UGS/conta trava o início | Tratar como primeiro passo do plano, com checagem explícita; tier gratuito cobre o spike |
| MPPM instável no setup do dev | Fallback build+editor documentado |
| Acoplamento futuro do player real | Justamente por isso a fundação usa player de teste; trazer o real é o Sub-projeto 1, já isolado |
| Versão de NGO x Unity 6.3 | Usar a versão de NGO compatível com Unity 6 resolvida pelo Package Manager |

---

## 10. Lobby atual & fluxo (achados da checagem)

> Pertence ao **Sub-projeto 5**, mas documentado aqui pra não se perder. NÃO é trabalho da Fundação.

### Estado atual: maquete visual, não funcional

`Assets/scripts/UI/LobbyUI.cs` (~940 linhas) é uma UI dark-fantasy completa e conceitualmente correta (slots de jogador, ready-up, host/iniciar, código + copiar, config de sala, seleção de personagem). Mas o backend é **simulado**:

- `jogadores[]` é um array falso; `SimularEntrada()` finge um player entrando após 3,5s.
- `codigoSala` é gerado **localmente** (`GerarCodigo()` → `"SPIRIT-XXXX"` random) — não é join code de Relay.
- `MenuInicialUI.cs:469-476`: "Criar sala" grava `LobbyHost=1` + código local e carrega `lobby`; **"Entrar sala" não captura código** (só carrega a cena) — não há onde digitar o código do amigo.
- Config de sala (mapa/dificuldade/visibilidade) apenas **recolore botões**; não persiste nem sincroniza.
- `IniciarJogo()` usa `SceneManager.LoadScene` **local** (não em rede).

### Fluxo atual

```
Single-player: menu → Start → CharacterSelection → ... → escolher terreno → loading → fase
Multiplayer:   menu → [Criar/Entrar sala] → lobby (mockup) → escolher terreno → loading → fase
```

### Problemas de fluxo a resolver no Sub-projeto 5

1. **Seleção de mapa duplicada**: o lobby configura MAPA e depois o fluxo passa por `escolher terreno`. No co-op, o host escolhe mapa/dificuldade no lobby e `Iniciar` vai direto pra fase; `escolher terreno` fica só no single-player.
2. **Personagem duplicado**: existe na cena `CharacterSelection` (SP) e na aba PERSONAGEM do lobby. No co-op, cada player escolhe o próprio personagem **no lobby**, sincronizado.
3. **Visibilidade público/privado**: implica lista pública/matchmaking, fora do escopo (decidido: só join code/convite). Simplificar pra privado/código no v1.
4. **Scene load local → em rede**: a transição lobby→jogo precisa virar `NetworkManager.SceneManager.LoadScene` (host carrega, clientes seguem juntos).
5. **Config host-autoritativa**: mapa/dificuldade/máx jogadores viram estado sincronizado; clientes veem read-only.

### Fluxo co-op recomendado (a confirmar no Sub-projeto 5)

```
menu → Multiplayer
   ├─ Criar sala  → aloca Relay → join code REAL → entra no lobby (host)
   └─ Entrar sala → digita código → JoinAllocation → entra no lobby (cliente)
lobby (real): roster sincronizado · cada um escolhe SEU personagem ·
              host define mapa+dificuldade+máx · ready-up · host "Inicia"
   → host: NetworkManager.SceneManager.LoadScene(fase) → todos juntos
```

### Implicação positiva

A UI e os conceitos do `LobbyUI.cs` **são reaproveitáveis**: o Sub-projeto 5 vira "ligar a maquete aos serviços reais + corrigir fluxo", não "desenhar do zero". O join code real da Fundação (§6, `RelayConnector`) é o que substitui o `GerarCodigo()` falso.

---

## 11. Próximo passo

Transformar este design em plano de implementação (skill writing-plans), com tarefas bite-sized: adicionar pacotes → vincular UGS → cena sandbox → bootstrap/login → RelayConnector → UI → SandboxPlayer + câmera → testar o marco.
