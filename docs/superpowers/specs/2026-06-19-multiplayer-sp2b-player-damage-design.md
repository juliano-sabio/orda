# Multiplayer Co-op — Sub-projeto 2b: Player → Inimigo (Ataques em Rede) (Design)

**Data:** 2026-06-19
**Status:** aprovado (design); pendente plano de implementação
**Depende de:** SP0, SP1, SP2a — concluídos
**Specs anteriores:** `docs/superpowers/specs/2026-06-*-multiplayer-*.md`

---

## 1. Objetivo

Permitir que os ataques de **qualquer player** causem dano nos inimigos host-autoritativos, com **morte/despawn decididos pelo host**, de forma que todos os clientes vejam os inimigos tomando dano e morrendo. Sem quebrar o single-player. O dano **inimigo→player** continua desligado (SP2c).

## 2. Contexto

- Inimigos são `NetworkObject` server-authoritative (SP2a); no cliente são fantoches (`EnemyNet` desliga gameplay + Rigidbody Kinematic).
- Todo dano player→inimigo passa por **`InimigoController.ReceberDano`** (~30+ callers: ataque básico, skills, ultimates).
- `NetSpawn` (dual-mode) e `NetCombat` já existem (SP2a). `NetCombat.DanoHabilitado` gateia o dano **inimigo→player** (`PlayerStats.TakeDamage`) — fica false em rede até o SP2c.

## 3. Escopo

### Inclui

- **Roteamento de dano centralizado** em `InimigoController.ReceberDano`: numa cópia cliente, a requisição vai pro host (ServerRpc); no host/SP, aplica como hoje.
- **ServerRpc** em `EnemyNet` (`RequireOwnership=false`) que aplica `ReceberDano` no host.
- **Morte/despawn host-autoritativo**: `NetSpawn.Despawnar(go)` (dual-mode) no ponto de morte comum do inimigo.
- **Debug attack** no player de teste pra validar o caminho de rede.

### NÃO inclui (deferido)

- **Drops** (orbes de XP, powerups) na morte → host-only por ora; rede de pickups é SP3.
- **Barras de vida nos clientes** depletando → o `InimigoController` está desligado no fantoche; feedback de morte é o despawn. Polimento posterior.
- **Projéteis/ataques visíveis pro colega** → continuam locais (cada um vê os seus).
- **Dano inimigo→player + downed/revive** → SP2c.
- **Networking dos ~30 behaviors de skill / SkillManager no sandbox** → as skills reais usam o mesmo `ReceberDano`, então roteiam automaticamente quando as fases reais entrarem na rede; o teste do 2b usa o debug attack.
- **Morte de boss** (efeitos `IniciarEfeitoMorte`) → o despawn host-autoritativo do boss é tratado quando os bosses forem exercitados em rede; o 2b foca no inimigo comum.

## 4. Marco de "pronto"

Via Multiplayer Play Mode (2 players) em `mp_fase_teste`:

- [ ] Os **dois players** atacam os slimes (debug attack) e causam dano.
- [ ] O dano do **cliente** é aplicado no **host** (autoridade) — o slime morre quando a vida zera no host.
- [ ] O slime morto **some nas duas telas** (despawn host-autoritativo).
- [ ] O host matando um slime também some nas duas telas.
- [ ] **Dano inimigo→player continua 0** (players não tomam dano).
- [ ] `read_console` (errors) = 0.
- [ ] **Regressão single-player:** `primeira_fase` — player mata inimigos normalmente, drops caem, com dano normal nos dois sentidos.

## 5. Arquitetura

### 5.1 Roteamento em `InimigoController.ReceberDano`

No início de `ReceberDano(float dano, bool isCrit, bool mostrarNumero)`:

```
// co-op: numa cópia cliente (fantoche), pede pro host aplicar e sai.
var en = GetComponent<EnemyNet>();
if (en != null && en.IsSpawned && !en.IsServer)
{
    en.ReceberDanoServerRpc(dano, isCrit);
    return;
}
// host/SP: segue a lógica atual (vidaAtual, morte).
```

- SP: sem `EnemyNet` → aplica direto (inalterado).
- Co-op host: `IsServer` true → aplica.
- Co-op cliente: roteia via ServerRpc; o host aplica e o resultado (morte/despawn) replica.

### 5.2 ServerRpc em `EnemyNet`

```csharp
[ServerRpc(RequireOwnership = false)]
public void ReceberDanoServerRpc(float dano, bool isCrit)
{
    var ic = GetComponent<InimigoController>();
    if (ic != null) ic.ReceberDano(dano, isCrit); // roda no host -> IsServer -> aplica
}
```

`RequireOwnership=false` permite qualquer cliente requisitar dano a qualquer inimigo (co-op de amigos, sem anti-cheat).

### 5.3 Morte/despawn host-autoritativo

`NetSpawn.Despawnar(GameObject go)` (novo):

```csharp
public static void Despawnar(GameObject go)
{
    if (go == null) return;
    var no = go.GetComponent<NetworkObject>();
    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
        && no != null && no.IsSpawned)
    {
        if (NetworkManager.Singleton.IsServer) no.Despawn(); // destrói em todos
        return; // cliente não despawna
    }
    Object.Destroy(go); // single-player
}
```

No `InimigoController.Morrer()`, o ramo do inimigo comum (`else Destroy(gameObject);`) vira `else NetSpawn.Despawnar(gameObject);`. Os drops (`DroparOrbesXP`/`DroparPowerup`) rodam no host (Instantiate local) — deferido pra SP3.

### 5.4 Debug attack (validação)

`Assets/scripts/net/DebugAtaque.cs` — componente no player de teste, **só pro dono**: a cada `intervalo` (ex.: 0.4s) acha o inimigo mais próximo num raio e chama `ReceberDano(dano)`. No cliente, isso roteia pro host; no host, aplica.

```
if (!IsLocalAuthority do player) desabilita;
periodicamente: InimigoController maisProx = inimigo no raio; maisProx?.ReceberDano(danoDebug);
```

> É harness de teste — fica só na variante/cena de teste, não em produção.

## 6. Estrutura de arquivos

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/scripts/net/NetSpawn.cs` | Modificar | + `Despawnar(go)` dual-mode |
| `Assets/scripts/net/EnemyNet.cs` | Modificar | + `ReceberDanoServerRpc` |
| `Assets/scripts/controlei_inimigo.cs` | Modificar | Roteamento no `ReceberDano`; `Despawnar` na morte comum |
| `Assets/scripts/net/DebugAtaque.cs` | Criar | Ataque de debug (dono) pra validar |
| `Assets/prefebs/net/NetworkPlayer.prefab` | Modificar | + `DebugAtaque` (só na variante de teste) |

## 7. Riscos

| Risco | Mitigação |
|---|---|
| ServerRpc em objeto não-owned | `RequireOwnership=false` permite qualquer cliente |
| `ReceberDano` recursivo (host) | No host `!IsServer` é false → aplica direto, sem re-rotear |
| Boss usa caminho de morte diferente | 2b foca no inimigo comum (slimes do teste); boss despawn é follow-up |
| `DebugAtaque` vazar pra produção | Só na variante/cena de teste; documentado como harness |
| Drops/efeitos no cliente | Deferidos (SP3); morte visível pelo despawn |

## 8. Testes

- **Co-op:** MPPM (2 players) em `mp_fase_teste` — validar §4 (ambos matam, despawn nos dois, sem dano ao player).
- **Regressão SP:** `primeira_fase` — matar inimigos normal, drops caem, dano nos dois sentidos.
- Sem testes unitários (integração de rede); aceite = comportamento observável.

## 9. Próximo passo

Plano de implementação (writing-plans): `NetSpawn.Despawnar` → `EnemyNet.ReceberDanoServerRpc` → roteamento no `ReceberDano` + despawn na morte → `DebugAtaque` + prefab → validação co-op + regressão SP.
