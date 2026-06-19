# Multiplayer Co-op — SP2a: Inimigos Host-Autoritativos — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development ou superpowers:executing-plans. Passos usam checkbox (`- [ ]`).

**Goal:** Todos os corpos de inimigo (horda comum + evento + boss) spawnam no host, perseguem o player mais próximo entre N e sincronizam posição pros clientes — sem dano — sem quebrar o single-player.

**Architecture:** Server-authoritative. Helper `NetSpawn` (dual-mode: SP = Instantiate; host = Instantiate+Spawn; cliente = nada). `EnemyNet` faz o inimigo virar fantoche Kinematic no cliente (IA desligada). Aggro N players via `PlayerStats.All`/`MaisProximoTransform`. Dano gateado **num ponto só** (`PlayerStats.TakeDamage`) pela flag `NetCombat`.

**Tech Stack:** Unity 6, NGO 2.12.

**Spec:** `docs/superpowers/specs/2026-06-19-multiplayer-sp2a-enemies-network-design.md`

---

## Verificação (não-TDD)

Spike de rede + gameplay. Cada tarefa de código: `refresh_unity` (scope=all) → `read_console` (types=["error"]) = **0 erros**. Aceite final (Task 9): marco co-op no MPPM **+ regressão single-player** na `primeira_fase`.

---

## File Structure

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/scripts/net/NetSpawn.cs` | Criar | `PodeSpawnar`, `EmRede`, `Spawnar(prefab,pos)` dual-mode |
| `Assets/scripts/net/NetCombat.cs` | Criar | Flag `DanoHabilitado` (false em rede no 2a) |
| `Assets/scripts/net/EnemyNet.cs` | Criar | Fantoche no cliente (Kinematic + desliga gameplay) |
| `Assets/scripts/player_stats.cs` | Modificar | `static All` + `MaisProximoTransform`; gate de `TakeDamage` |
| `Assets/scripts/Editor/AddNetToEnemies.cs` | Criar | Menu de editor: adiciona componentes de rede aos prefabs em lote |
| `Assets/scripts/spawn_inimigo.cs` | Modificar | Spawn via `NetSpawn`, host-only, ao redor de N players |
| `Assets/scripts/movi_inimigo.cs` + IA dos mobs | Modificar | Aggro via `PlayerStats.MaisProximoTransform` |
| `Assets/scripts/GerenciadorEventos.cs`, `TimerManager.cs` | Modificar | Spawns de evento/boss via `NetSpawn` host-only |
| `Assets/Scenes/mp_fase_teste.unity` | Modificar | + spawner + registrar Network Prefabs de inimigo |

---

## Task 1: Branch + NetSpawn + NetCombat

**Files:**
- Create: `Assets/scripts/net/NetSpawn.cs`, `Assets/scripts/net/NetCombat.cs`

- [ ] **Step 1: Branch**

```bash
cd "j:/unity/projetos/horda/orda"
git checkout -b feat/mp-sp2a-enemies
```

- [ ] **Step 2: NetSpawn.cs**

```csharp
using Unity.Netcode;
using UnityEngine;

// Spawn dual-mode: single-player -> Instantiate; host em rede -> Instantiate+Spawn;
// cliente em rede -> nada (clientes não spawnam inimigos).
public static class NetSpawn
{
    public static bool EmRede
    {
        get { var nm = NetworkManager.Singleton; return nm != null && nm.IsListening; }
    }

    // true em single-player OU quando sou o host/server.
    public static bool PodeSpawnar
    {
        get { var nm = NetworkManager.Singleton; return nm == null || !nm.IsListening || nm.IsServer; }
    }

    public static GameObject Spawnar(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return null;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
            return Object.Instantiate(prefab, pos, Quaternion.identity); // single-player
        if (!nm.IsServer) return null;                                   // cliente não spawna
        var go = Object.Instantiate(prefab, pos, Quaternion.identity);
        var no = go.GetComponent<NetworkObject>();
        if (no != null) no.Spawn();
        return go;
    }
}
```

- [ ] **Step 3: NetCombat.cs**

```csharp
// Liga/desliga dano no contexto de rede. No SP2a fica desligado em rede (sem dano);
// o SP2b liga (RedeComDano = true). Em single-player o dano é sempre habilitado.
public static class NetCombat
{
    public static bool RedeComDano = false;

    public static bool DanoHabilitado => !NetSpawn.EmRede || RedeComDano;
}
```

- [ ] **Step 4: Compilar** — `refresh_unity` (scope=all) → `read_console`. Esperado: 0 erros.

- [ ] **Step 5: Commit**

```bash
git add Assets/scripts/net/NetSpawn.cs Assets/scripts/net/NetSpawn.cs.meta \
        Assets/scripts/net/NetCombat.cs Assets/scripts/net/NetCombat.cs.meta
git commit -m "feat(mp-sp2a): helpers NetSpawn (spawn dual-mode) e NetCombat (flag de dano)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2: PlayerStats.All + aggro + gate de dano

**Files:**
- Modify: `Assets/scripts/player_stats.cs`

- [ ] **Step 1: Registro de N players**

No `player_stats.cs`, junto do bloco de rede já existente (perto de `Local`), adicionar:

```csharp
    public static readonly System.Collections.Generic.List<PlayerStats> All =
        new System.Collections.Generic.List<PlayerStats>();

    // Transform do player vivo mais próximo de uma posição (ou null se não há nenhum).
    public static Transform MaisProximoTransform(Vector2 pos)
    {
        Transform melhor = null;
        float menor = float.MaxValue;
        for (int i = 0; i < All.Count; i++)
        {
            var p = All[i];
            if (p == null) continue;
            float d = ((Vector2)p.transform.position - pos).sqrMagnitude;
            if (d < menor) { menor = d; melhor = p.transform; }
        }
        return melhor;
    }
```

- [ ] **Step 2: Inscrição no ciclo de vida**

Adicionar em `player_stats.cs`:

```csharp
    void OnEnable()  { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }
```

> Se a classe já tiver `OnEnable`/`OnDisable`, inserir as linhas dentro deles em vez de criar novos.

- [ ] **Step 3: Gate central de dano**

No início de `public void TakeDamage(float damage)` (linha ~1043), antes de qualquer lógica:

```csharp
        if (!NetCombat.DanoHabilitado) return;
```

Isso desliga **todo** dano ao player no contexto de rede do 2a (em SP, `DanoHabilitado` é true → inalterado).

- [ ] **Step 4: Compilar** — `refresh_unity` (scope=all) → `read_console`. Esperado: 0 erros.

- [ ] **Step 5: Commit**

```bash
git add Assets/scripts/player_stats.cs
git commit -m "feat(mp-sp2a): PlayerStats.All + MaisProximoTransform + gate de dano (NetCombat)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3: EnemyNet — fantoche no cliente

**Files:**
- Create: `Assets/scripts/net/EnemyNet.cs`

- [ ] **Step 1: Criar o componente**

```csharp
using Unity.Netcode;
using UnityEngine;

// Em inimigos/bosses. No CLIENTE, o inimigo é um fantoche movido pelo
// NetworkTransform (server authority): Rigidbody2D Kinematic e scripts de
// gameplay desligados (a IA roda só no host). No HOST, não mexe em nada.
public class EnemyNet : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsServer) return; // host roda a lógica normalmente

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

        // Desliga MonoBehaviours de gameplay no cliente. Visual (SpriteRenderer,
        // Animator) e rede (NetworkBehaviours) NÃO são MonoBehaviour comuns ou
        // são preservados, então continuam funcionando.
        foreach (var c in GetComponents<MonoBehaviour>())
        {
            if (c == this) continue;
            if (c is NetworkBehaviour) continue;
            c.enabled = false;
        }
    }
}
```

- [ ] **Step 2: Compilar** — `refresh_unity` (scope=all) → `read_console`. Esperado: 0 erros.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/net/EnemyNet.cs Assets/scripts/net/EnemyNet.cs.meta
git commit -m "feat(mp-sp2a): EnemyNet (fantoche Kinematic + gameplay off no cliente)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4: Componentes de rede nos prefabs (em lote)

**Files:**
- Create: `Assets/scripts/Editor/AddNetToEnemies.cs`

- [ ] **Step 1: Editor utility em lote**

`Assets/scripts/Editor/AddNetToEnemies.cs`:

```csharp
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

// Menu: Tools/MP/Add Net Components To Enemies
// Adiciona NetworkObject + NetworkTransform(Server) + EnemyNet aos prefabs de
// inimigo/boss nas pastas alvo, idempotente.
public static class AddNetToEnemies
{
    static readonly string[] Pastas =
    {
        "Assets/prefebs/inimigos",
        "Assets/prefebs/boss",
        "Assets/prefebs/skill_mob",
    };

    [MenuItem("Tools/MP/Add Net Components To Enemies")]
    public static void Run()
    {
        int alterados = 0;
        var guids = AssetDatabase.FindAssets("t:Prefab", Pastas);
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var go = PrefabUtility.LoadPrefabContents(path);
            bool mudou = false;

            if (go.GetComponent<NetworkObject>() == null) { go.AddComponent<NetworkObject>(); mudou = true; }

            var nt = go.GetComponent<NetworkTransform>();
            if (nt == null) { nt = go.AddComponent<NetworkTransform>(); mudou = true; }
            nt.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            nt.SyncPositionZ = false; nt.SyncRotAngleX = false; nt.SyncRotAngleY = false;
            nt.SyncScaleX = false; nt.SyncScaleY = false; nt.SyncScaleZ = false;

            if (go.GetComponent<EnemyNet>() == null) { go.AddComponent<EnemyNet>(); mudou = true; }

            if (mudou) { PrefabUtility.SaveAsPrefabAsset(go, path); alterados++; }
            PrefabUtility.UnloadPrefabContents(go);
        }
        Debug.Log($"[MP] Net components adicionados em {alterados} prefabs de inimigo.");
    }
}
```

- [ ] **Step 2: Compilar e rodar o menu**

`refresh_unity` (scope=all) → `read_console` (0 erros). Depois rodar **Tools > MP > Add Net Components To Enemies** (via menu ou `execute_menu_item`). Conferir no log quantos prefabs foram alterados (esperado: dezenas).

- [ ] **Step 3: Verificar 1 prefab por tipo**

Abrir 1 prefab de inimigo comum e 1 de boss; confirmar `NetworkObject` + `NetworkTransform` (Server, sem scale) + `EnemyNet`.

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/Editor/AddNetToEnemies.cs Assets/scripts/Editor/AddNetToEnemies.cs.meta Assets/prefebs
git commit -m "feat(mp-sp2a): NetworkObject+NetworkTransform(Server)+EnemyNet nos prefabs de inimigo (em lote)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5: spawn_inimigo dual-mode + N players

**Files:**
- Modify: `Assets/scripts/spawn_inimigo.cs`

- [ ] **Step 1: Loop de spawn só na autoridade**

No `Update()` do `spawn_inimigo` (onde chama `TentarSpawnar`/`GerenciarSistemaWaves`), envolver a lógica de spawn com:

```csharp
        if (!NetSpawn.PodeSpawnar) return; // clientes não spawnam horda
```

(colocar logo no início do `Update`, após guards existentes).

- [ ] **Step 2: Spawn via NetSpawn**

Em `TentarSpawnar()` (linha ~157), trocar:

```csharp
            GameObject novoInimigo = Instantiate(tipo.prefab, posicaoValida.Value, Quaternion.identity);
            inimigosAtivos.Add(novoInimigo);
```

por:

```csharp
            GameObject novoInimigo = NetSpawn.Spawnar(tipo.prefab, posicaoValida.Value);
            if (novoInimigo != null) inimigosAtivos.Add(novoInimigo);
```

- [ ] **Step 3: Posição ao redor de N players**

Em `ObterPosicaoLivre()` (linha ~170), o código usa `player.position`. Trocar a referência fixa por um player escolhido entre todos: no início do método,

```csharp
        Transform refPlayer = PlayerStats.All.Count > 0
            ? PlayerStats.All[Random.Range(0, PlayerStats.All.Count)].transform
            : player; // fallback single-player
        if (refPlayer == null) return null;
```

e usar `refPlayer.position` no lugar de `player.position` dentro do método. O `ForaDaCamera` (usa `Camera.main`) fica como está — no host a câmera local serve de referência (aceitável no 2a).

- [ ] **Step 4: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 5: Commit**

```bash
git add Assets/scripts/spawn_inimigo.cs
git commit -m "feat(mp-sp2a): spawn_inimigo host-only via NetSpawn + spawn ao redor de N players

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6: Aggro N players na IA dos inimigos

**Files (cada um tem busca de player singular a trocar por `MaisProximoTransform`):**
- `Assets/scripts/movi_inimigo.cs`
- `Assets/scripts/mecanicas_mobs/movi_inimigo_manter_distancia.cs`
- `Assets/scripts/mecanicas_mobs/FantasmaEletrico.cs`, `FantasmaFogo.cs`, `FantasmaGelo.cs`, `FantasmaVeneno.cs`, `FantasmaVenenoAtirador.cs`
- `Assets/scripts/mecanicas_mobs/SlimeElemental.cs`, `SlimeGuarda.cs`, `SlimeMagaFireAttack.cs`, `SlimeProtetoraInimiga.cs`
- `Assets/scripts/SlimePercursoEvento.cs`
- `Assets/scripts/FlowField.cs`

- [ ] **Step 1: Transformação uniforme**

Em cada arquivo, onde houver `GameObject.FindGameObjectWithTag("Player")` (e o `.transform`/`?.transform` correspondente) ou `FindFirstObjectByType<PlayerStats>()`, substituir pela busca do player mais próximo desta posição:

- Quando o resultado é um `Transform` (campo `player`/`alvo`): usar
  ```csharp
  PlayerStats.MaisProximoTransform(transform.position)
  ```
- Quando o resultado é um `PlayerStats`: usar
  ```csharp
  var _t = PlayerStats.MaisProximoTransform(transform.position);
  var ps = _t != null ? _t.GetComponent<PlayerStats>() : null;
  ```

Exemplo concreto em `movi_inimigo.cs` `EncontrarPlayer()` (linha ~122):

```csharp
    void EncontrarPlayer()
    {
        procurandoPlayer = true;
        player = PlayerStats.MaisProximoTransform(transform.position);
        procurandoPlayer = false;
    }
```

O `ProcurarPlayerPeriodicamente()` já re-chama `EncontrarPlayer()` periodicamente → o alvo nearest é reavaliado dinamicamente.

Em `FlowField.cs`, onde define o alvo do flow field a partir do player, usar `PlayerStats.MaisProximoTransform` da posição de referência do field (ou do primeiro player se o field for global). No 2a (campo aberto, sem FlowField na cena de teste) este caminho não é exercido, mas a migração mantém consistência.

- [ ] **Step 2: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 3: Commit**

```bash
git add Assets/scripts/movi_inimigo.cs Assets/scripts/FlowField.cs Assets/scripts/SlimePercursoEvento.cs Assets/scripts/mecanicas_mobs
git commit -m "feat(mp-sp2a): IA de inimigo mira o player mais próximo (PlayerStats.MaisProximoTransform)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 7: Spawns de evento e boss host-only

**Files:**
- Modify: `Assets/scripts/GerenciadorEventos.cs`, `Assets/scripts/TimerManager.cs`

- [ ] **Step 1: GerenciadorEventos — spawns de inimigo via NetSpawn**

Nos pontos que instanciam inimigos/hazards de evento (ceifador ~874, slime colorida ~816, slime percurso ~984, núcleo corrompido, etc.), trocar `Instantiate(prefab, pos, Quaternion.identity)` por `NetSpawn.Spawnar(prefab, pos)` e guardar o retorno (checando null). Garantir que o loop/timer de eventos só dispare spawn quando `NetSpawn.PodeSpawnar` (no `Update` do gerenciador, `if (!NetSpawn.PodeSpawnar) return;` antes de agendar/spawnar eventos que criam inimigos).

> Não trocar `Instantiate` de objetos puramente visuais/UI (borda de sangue, partículas) — só os que criam inimigos/hazards com `NetworkObject`.

- [ ] **Step 2: TimerManager — boss via NetSpawn**

Em `TriggerBossEvent` (onde faz `Instantiate(bossEvent.bossPrefab, bossEvent.spawnPosition, Quaternion.identity)`), trocar por:

```csharp
        if (bossEvent.bossPrefab != null)
            inst = NetSpawn.Spawnar(bossEvent.bossPrefab, bossEvent.spawnPosition);
```

(o `TimerManager` já roda a lógica de boss; em rede o `NetSpawn` garante host-only + Spawn).

- [ ] **Step 3: Compilar** — `refresh_unity` (scope=all) → `read_console`. 0 erros.

- [ ] **Step 4: Commit**

```bash
git add Assets/scripts/GerenciadorEventos.cs Assets/scripts/TimerManager.cs
git commit -m "feat(mp-sp2a): spawns de evento e boss via NetSpawn (host-only)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 8: Cena de teste com spawner

**Files:**
- Modify: `Assets/Scenes/mp_fase_teste.unity`

- [ ] **Step 1: Adicionar spawner + registrar prefabs**

Na `mp_fase_teste`:
- Adicionar um GameObject `SpawnInimigos` com `spawn_inimigo` configurado com 1-2 tipos de inimigo comum (prefab + peso) e limites baixos (ex.: máx 15) pra teste.
- Registrar os prefabs de inimigo usados na lista de **Network Prefabs** do `NetworkManager` (ou usar uma Default Network Prefabs List que inclua a pasta).
- (Opcional) deixar referências pra disparar manualmente 1 evento e 1 boss no teste.

Pode ser via Unity MCP (`manage_gameobject`/`execute_code`).

- [ ] **Step 2: Verificar 0 erros** — `refresh_unity` (scope=all) → `read_console`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scenes/mp_fase_teste.unity Assets/Scenes/mp_fase_teste.unity.meta ProjectSettings
git commit -m "feat(mp-sp2a): mp_fase_teste com spawner + network prefabs de inimigo

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 9: Validar marco + regressão SP

**Files:** nenhum.

- [ ] **Step 1: Co-op (MPPM)**

2 players, `mp_fase_teste`, Host + Join. Confirmar (spec §5):
- [ ] Horda spawna no host e aparece nas duas telas
- [ ] Inimigos perseguem o player **mais próximo** (separar os 2 players e ver alvos diferentes)
- [ ] Movimento suave/sincronizado nos clientes (fantoche, sem jitter)
- [ ] (Se disparar) evento e boss spawnam host-autoritativos nos dois
- [ ] **Nenhum dano** (player não toma, inimigos não morrem)
- [ ] `read_console` (errors) = 0

- [ ] **Step 2: Regressão single-player (OBRIGATÓRIO)**

`primeira_fase` em Play (sem MPPM): horda, eventos e boss final spawnam e perseguem **igual a antes**, **com dano normal** (flag `DanoHabilitado` é true em SP). 0 erros novos.

- [ ] **Step 3: Finalizar branch**

`superpowers:finishing-a-development-branch` → merge `feat/mp-sp2a-enemies` → `main` (FF se possível) + push.

---

## Self-Review (autor do plano)

- **Cobertura da spec:** §6.1 server-auth + EnemyNet → Tasks 3/4; §6.2 NetSpawn dual-mode → Tasks 1/5/7; §6.3 PlayerStats.All/aggro → Tasks 2/6; §6.4 prefabs → Task 4; §6.5 flag de dano → Tasks 1/2 (gate central no TakeDamage); cena → Task 8; marco+regressão → Task 9. FlowField em rede e dano (2b) explicitamente fora.
- **Placeholders:** a migração de aggro (Task 6) e os spawns de evento (Task 7) descrevem a transformação mecânica exata sobre arquivos/métodos nomeados; o gate de dano é centralizado (1 ponto) em vez de 21 arquivos.
- **Consistência de tipos:** `NetSpawn.{EmRede,PodeSpawnar,Spawnar}`, `NetCombat.DanoHabilitado`, `PlayerStats.{All,MaisProximoTransform}`, `EnemyNet` usados de forma consistente entre as tarefas.
