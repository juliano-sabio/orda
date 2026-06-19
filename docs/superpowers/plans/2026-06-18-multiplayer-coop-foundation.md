# Multiplayer Co-op — Fundação de Rede (Sub-projeto 0) — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans para implementar este plano tarefa-a-tarefa. Os passos usam checkbox (`- [ ]`) para tracking.

**Goal:** Provar a stack de rede do co-op online — dois processos do jogo conectam por join code (Unity Relay) e cada um vê o avatar do outro se movendo em tempo real, em uma cena sandbox isolada.

**Architecture:** Listen-server com Netcode for GameObjects (NGO). Cena `mp_sandbox` isolada com um player de teste mínimo (`NetworkObject` + `NetworkTransform` owner-authoritative). UGS (Authentication anônima + Relay) faz o NAT-traversal e gera o join code. Câmera por cliente. Nada de gameplay real é tocado.

**Tech Stack:** Unity 6 (6000.3.9f1), NGO 2.x, Unity Transport, Unity Services (Core, Authentication, Relay), Multiplayer Play Mode.

**Spec:** `docs/superpowers/specs/2026-06-18-multiplayer-coop-foundation-design.md`

---

## Nota sobre verificação (não-TDD)

Por spec §8, este é um **spike de integração de rede** — não há testes unitários. A verificação de cada tarefa de código é:

1. `refresh_unity` (compile=request, mode=force, scope=all, wait_for_ready=true) — `scope=all` porque há arquivos `.cs` novos a importar.
2. `read_console` (types=["error"], count="20") — esperar **0 erros**.

O aceite final (Task 8) é o **marco observável** no Multiplayer Play Mode.

Todos os scripts novos ficam em `Assets/scripts/net/` e o prefab em `Assets/prefebs/net/`. O projeto usa `Assembly-CSharp` (sem asmdef), e os tipos do NGO ficam acessíveis a partir dele sem configuração extra.

---

## File Structure

| Arquivo | Responsabilidade |
|---|---|
| `Assets/scripts/net/NetBootstrap.cs` | Inicializa UnityServices + login anônimo (idempotente) |
| `Assets/scripts/net/RelayConnector.cs` | `HostAsync(maxPlayers)`→join code; `JoinAsync(code)`; configura UnityTransport com dados do Relay |
| `Assets/scripts/net/SandboxConnectUI.cs` | UI tosca (IMGUI): botões Host/Join, campo de código, status |
| `Assets/scripts/net/SandboxPlayer.cs` | Movimento gated por `IsOwner` + câmera local seguindo o dono |
| `Assets/prefebs/net/SandboxPlayer.prefab` | Player de teste: `NetworkObject` + `NetworkTransform` (Owner) + sprite + `SandboxPlayer` |
| `Assets/Scenes/mp_sandbox.unity` | NetworkManager + UnityTransport + câmera + chão + UI |

---

## Task 1: Branch + pacotes de rede

**Files:**
- Modify: `Packages/manifest.json` (via Package Manager — não editar à mão)

- [ ] **Step 1: Criar branch de trabalho**

```bash
cd "j:/unity/projetos/horda/orda"
git checkout -b feat/mp-foundation
```

- [ ] **Step 2: Adicionar os pacotes via Package Manager**

No Unity: **Window > Package Manager > + > Add package by name…** e adicionar, um a um (deixar o Package Manager resolver a versão compatível com Unity 6):

```
com.unity.netcode.gameobjects
com.unity.services.core
com.unity.services.authentication
com.unity.services.relay
com.unity.multiplayer.playmode
```

> `com.unity.transport` entra automaticamente como dependência do NGO. NÃO adicionar `com.unity.services.lobby` (Lobby é Sub-projeto 5).

- [ ] **Step 3: Verificar compilação limpa**

`refresh_unity` (compile=request, mode=force, scope=all) → `read_console` (types=["error"]).
Esperado: **0 erros**. (NGO e os SDKs de serviços compilam sem uso ainda.)

- [ ] **Step 4: Commit**

```bash
git add Packages/manifest.json Packages/packages-lock.json
git commit -m "chore(mp): adiciona NGO + Unity Services (Relay/Auth) + Multiplayer Play Mode

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2: Vincular o projeto aos Unity Gaming Services

**Files:**
- Modify: `ProjectSettings/` (gerado pelo link de serviços)

> Passo de nuvem, **manual** — não dá pra automatizar via código. Relay e Authentication têm tier gratuito suficiente pro spike.

- [ ] **Step 1: Linkar o projeto a uma organização/projeto UGS**

No Unity: **Edit > Project Settings > Services** → logar com a conta Unity → criar/selecionar uma organização e um projeto UGS (vincula um `ProjectId`).

- [ ] **Step 2: Habilitar Relay e Authentication**

No Unity Dashboard (ou no painel Services): garantir que **Relay** e **Authentication** estão ativos para o projeto. Authentication anônima não exige configuração extra.

- [ ] **Step 3: Verificar o link**

Em **Project Settings > Services**, confirmar que aparece o ProjectId vinculado e que não há aviso de "projeto não vinculado".

- [ ] **Step 4: Commit**

```bash
git add ProjectSettings/
git commit -m "chore(mp): vincula projeto aos Unity Gaming Services (Relay/Auth)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3: NetBootstrap — init de serviços + login anônimo

**Files:**
- Create: `Assets/scripts/net/NetBootstrap.cs`

- [ ] **Step 1: Criar o arquivo**

```csharp
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;

// Inicializa os Unity Gaming Services e faz login anônimo, uma única vez.
// Idempotente: chamar várias vezes é seguro.
public static class NetBootstrap
{
    public static bool Ready { get; private set; }

    public static async Task InitAsync()
    {
        if (Ready) return;

        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Ready = true;
    }
}
```

- [ ] **Step 2: Verificar compilação limpa**

`refresh_unity` (compile=request, mode=force, scope=all) → `read_console` (types=["error"]).
Esperado: **0 erros**.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/net/NetBootstrap.cs Assets/scripts/net/NetBootstrap.cs.meta
git commit -m "feat(mp): NetBootstrap (init UGS + login anônimo)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4: RelayConnector — host/join via Relay

**Files:**
- Create: `Assets/scripts/net/RelayConnector.cs`

- [ ] **Step 1: Criar o arquivo**

```csharp
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

// Cria/entra em uma sessão via Unity Relay e configura o UnityTransport.
// HostAsync devolve o join code REAL — é o que, no Sub-projeto 5, substitui
// o GerarCodigo() falso de LobbyUI.cs.
public static class RelayConnector
{
    static UnityTransport Transport =>
        NetworkManager.Singleton.GetComponent<UnityTransport>();

    public static async Task<string> HostAsync(int maxPlayers)
    {
        Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
        Transport.SetRelayServerData(new RelayServerData(alloc, "dtls"));
        NetworkManager.Singleton.StartHost();
        return joinCode;
    }

    public static async Task JoinAsync(string joinCode)
    {
        JoinAllocation alloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
        Transport.SetRelayServerData(new RelayServerData(alloc, "dtls"));
        NetworkManager.Singleton.StartClient();
    }
}
```

> Nota de API: `new RelayServerData(allocation, "dtls")` é a forma estável que acompanha o Unity Transport. Se a versão de SDK instalada expuser `AllocationUtils.ToRelayServerData(allocation, "dtls")`, qualquer uma serve — ambas produzem o `RelayServerData` que o `SetRelayServerData` espera.

- [ ] **Step 2: Verificar compilação limpa**

`refresh_unity` (compile=request, mode=force, scope=all) → `read_console` (types=["error"]).
Esperado: **0 erros**. Se houver erro de `RelayServerData` não encontrado, conferir o `using Unity.Networking.Transport.Relay;`.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/net/RelayConnector.cs Assets/scripts/net/RelayConnector.cs.meta
git commit -m "feat(mp): RelayConnector (host/join via Relay + join code)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5: SandboxConnectUI — UI tosca de conexão

**Files:**
- Create: `Assets/scripts/net/SandboxConnectUI.cs`

- [ ] **Step 1: Criar o arquivo**

```csharp
using Unity.Netcode;
using UnityEngine;

// UI de teste (IMGUI) só para o sandbox: Host, campo de código, Join, status.
// Feia de propósito — não é UI de produção.
public class SandboxConnectUI : MonoBehaviour
{
    string joinCode = "";
    string status = "Pronto.";
    [SerializeField] int maxPlayers = 4;

    async void StartHost()
    {
        status = "Iniciando host...";
        try
        {
            await NetBootstrap.InitAsync();
            joinCode = await RelayConnector.HostAsync(maxPlayers);
            status = "Host no ar. Código: " + joinCode;
        }
        catch (System.Exception e) { status = "Falha no host: " + e.Message; }
    }

    async void StartJoin()
    {
        status = "Entrando...";
        try
        {
            await NetBootstrap.InitAsync();
            await RelayConnector.JoinAsync(joinCode.Trim());
            status = "Conectado.";
        }
        catch (System.Exception e) { status = "Falha ao entrar: " + e.Message; }
    }

    void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsListening || nm.IsConnectedClient))
        {
            GUI.Label(new Rect(10, 10, 700, 30), status);
            if (GUI.Button(new Rect(10, 45, 140, 36), "Desconectar")) nm.Shutdown();
            return;
        }

        if (GUI.Button(new Rect(10, 10, 140, 40), "Host")) StartHost();
        GUI.Label(new Rect(10, 60, 80, 30), "Código:");
        joinCode = GUI.TextField(new Rect(90, 60, 180, 30), joinCode);
        if (GUI.Button(new Rect(280, 56, 140, 40), "Join")) StartJoin();
        GUI.Label(new Rect(10, 110, 700, 60), status);
    }
}
```

- [ ] **Step 2: Verificar compilação limpa**

`refresh_unity` (compile=request, mode=force, scope=all) → `read_console` (types=["error"]).
Esperado: **0 erros**.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/net/SandboxConnectUI.cs Assets/scripts/net/SandboxConnectUI.cs.meta
git commit -m "feat(mp): SandboxConnectUI (UI tosca host/join/código)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6: SandboxPlayer — movimento por owner + câmera local + prefab

**Files:**
- Create: `Assets/scripts/net/SandboxPlayer.cs`
- Create: `Assets/prefebs/net/SandboxPlayer.prefab`

- [ ] **Step 1: Criar o script**

```csharp
using Unity.Netcode;
using UnityEngine;

// Player de teste do sandbox. Só o dono lê input e move; o NetworkTransform
// (configurado como Owner authority no prefab) replica a posição pros demais.
// A câmera de cada cliente segue o player de que ele é dono.
[RequireComponent(typeof(NetworkObject))]
public class SandboxPlayer : NetworkBehaviour
{
    [SerializeField] float speed = 6f;
    Camera cam;

    public override void OnNetworkSpawn()
    {
        if (IsOwner) cam = Camera.main;
    }

    void Update()
    {
        if (!IsOwner) return;
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(x, y, 0f);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        transform.position += dir * (speed * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (!IsOwner || cam == null) return;
        var p = transform.position;
        cam.transform.position = new Vector3(p.x, p.y, cam.transform.position.z);
    }
}
```

- [ ] **Step 2: Verificar compilação limpa**

`refresh_unity` (compile=request, mode=force, scope=all) → `read_console` (types=["error"]).
Esperado: **0 erros**.

- [ ] **Step 3: Montar o prefab `SandboxPlayer`**

Criar `Assets/prefebs/net/SandboxPlayer.prefab` com, na raiz:
- `SpriteRenderer` com um sprite simples (ex.: o sprite de quadrado/círculo built-in, ou qualquer sprite do projeto) — só pra ser visível.
- `NetworkObject` (NGO).
- `NetworkTransform` (NGO) com **Authority Mode = Owner** (e, opcional, marcar só X/Y já que é 2D).
- `SandboxPlayer` (o script acima).

Pode ser via Unity MCP (`manage_gameobject`/`execute_code`) ou manualmente no editor. Salvar como prefab e remover a instância da cena (o NGO instancia via PlayerPrefab).

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/net Assets/prefebs/net
git commit -m "feat(mp): SandboxPlayer (movimento owner + câmera local) + prefab

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 7: Cena `mp_sandbox`

**Files:**
- Create: `Assets/Scenes/mp_sandbox.unity`

- [ ] **Step 1: Criar a cena e os objetos base**

Nova cena `Assets/Scenes/mp_sandbox.unity` contendo:
- **Main Camera** (tag `MainCamera`), ortográfica, posição z negativa (ex.: z = -10).
- Um **chão**/grid visual (um `SpriteRenderer` grande ou alguns sprites) só pra dar referência de movimento.
- GameObject **`NetManager`** com:
  - `NetworkManager` (NGO).
  - `UnityTransport` (no mesmo GameObject).
  - No `NetworkManager`: **Player Prefab = SandboxPlayer.prefab**; adicionar o `SandboxPlayer.prefab` à lista de **Network Prefabs** (Default Network Prefabs List ou Network Prefabs no NetworkManager).
- GameObject **`ConnectUI`** com o componente `SandboxConnectUI`.

Pode ser via Unity MCP (`manage_scene` + `manage_gameobject`/`manage_components` + `execute_code`) ou manualmente.

- [ ] **Step 2: Registrar a cena no Build Profiles**

Adicionar `mp_sandbox` em **File > Build Profiles > Scene List** (necessário pro NGO scene management e pro Play Mode).

- [ ] **Step 3: Verificar 0 erros e cena válida**

`refresh_unity` (scope=all) → `read_console` (types=["error"]). Esperado: **0 erros**.
Abrir a cena no editor e confirmar que `NetManager` tem `NetworkManager` + `UnityTransport` e que o Player Prefab está atribuído.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scenes/mp_sandbox.unity Assets/Scenes/mp_sandbox.unity.meta ProjectSettings/EditorBuildSettings.asset
git commit -m "feat(mp): cena mp_sandbox (NetworkManager + UnityTransport + UI)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 8: Validar o marco (Multiplayer Play Mode)

**Files:** nenhum (validação + commit final do branch).

- [ ] **Step 1: Ativar um player virtual**

No Unity: **Window > Multiplayer > Multiplayer Play Mode** → ativar **1 virtual player** (Player 2). Isso roda uma segunda instância da mesma cena no editor, sem build.

- [ ] **Step 2: Entrar em Play e conectar**

1. Abrir `mp_sandbox` na instância principal e entrar em Play.
2. Na instância **principal**: clicar **Host** → anotar o código exibido no status.
3. Na instância **virtual** (Player 2): digitar o código no campo e clicar **Join**.

- [ ] **Step 3: Verificar o critério de aceite (spec §2)**

Confirmar **todos**:
- [ ] As duas instâncias mostram **dois avatares**.
- [ ] Mover com WASD/setas em uma instância move o **próprio** avatar, e o movimento **aparece na outra** instância em tempo real.
- [ ] A câmera de cada instância segue o **próprio** player (não o do outro).
- [ ] Clicar **Desconectar** no cliente **não derruba** o host (host segue em Play sem erro).
- [ ] `read_console` (types=["error"]) durante a sessão: **0 erros** de rede.

Se algum item falhar → parar e diagnosticar (não seguir pro merge). Fallback de teste: 1 build standalone + editor, caso o Multiplayer Play Mode não funcione no setup.

- [ ] **Step 4: Finalizar o branch**

Após o marco validado, usar a skill `superpowers:finishing-a-development-branch` para decidir merge/PR. Sugerido: merge de `feat/mp-foundation` em `main` (no-ff) e push.

---

## Self-Review (preenchido pelo autor do plano)

- **Cobertura da spec:** §3 pacotes → Task 1; §3 UGS link → Task 2; §6 NetBootstrap/RelayConnector/SandboxConnectUI/SandboxPlayer → Tasks 3-6; §5 sync/câmera → Task 6; cena/NetworkManager → Task 7; §2 marco + §8 teste → Task 8; §10 lobby = explicitamente fora (Sub-projeto 5). Sem lacunas.
- **Placeholders:** nenhum — todo código está completo.
- **Consistência de tipos:** `NetBootstrap.InitAsync()`, `RelayConnector.HostAsync(int)→string`, `RelayConnector.JoinAsync(string)`, `SandboxPlayer` (NetworkBehaviour) usados de forma consistente entre Tasks 3-8.
