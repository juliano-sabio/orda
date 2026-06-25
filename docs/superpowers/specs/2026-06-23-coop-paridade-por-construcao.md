# Paridade Co-op por Construção — Arquitetura + Gate (filtro)

**Objetivo:** parar de caçar bug de paridade um a um. Em vez disso, fazer o **certo virar o default** e o **errado não passar num gate automático**. O solo passa a ser o caso degenerado de "MP com 1 jogador".

**Princípio único:**
> Nunca existe "o player" no singular global. Todo efeito visível em gameplay nasce por uma porta que já replica (ou já vincula no player certo). Quem não passa por uma porta é apontado pelo gate.

---

## As 5 portas

Tudo que é visível em gameplay/UI entra por **uma** destas. Não há outro caminho sancionado.

### Gameplay

**P1 — Spawn de objeto** → `NetSpawn`
- Cobre: inimigos, projéteis, pickups, objetos de evento que SE MOVEM.
- Mecanismo: `NetSpawn.Spawnar(prefab,pos)` (host instancia+Spawn; cliente nada; SP=Instantiate). `NetSpawn.Despawnar(go)` remove em todos. Prefab precisa de `NetworkObject` (+ `EnemyNet` se inimigo, + `NetworkTransform`).
- Proibido: `Instantiate(prefab)` de algo que é gameplay/visível por ambos.

**P2 — Efeito num player** → roteia pro DONO (`PlayerNet` OwnerRpc)
- Cobre: dano, status (slow/veneno/queima/paralisia), cura, buffs, luz/glow, partícula no player, defensivas, ultimates.
- Mecanismo: `if (!IsOwner) { pn.XxxOwnerRpc(args); return; }` (igual `TakeDamage`). O RPC `[Rpc(SendTo.Owner)]` aplica+mostra no cliente do dono. Pra os OUTROS verem o visual: broadcast cosmético (`SendTo.NotOwner`) que re-roda o efeito no fantoche sem dano.
- Proibido: aplicar/instanciar efeito direto no `PlayerStats` que veio de finder global (vai pro fantoche errado, no contexto do host = player 1).

**P3 — Objeto/efeito no MUNDO (host-autoritativo)** → host roda + cliente reconstrói cópia cosmética
- Cobre: zonas, eventos procedurais (colapso/portal/vórtice/núcleo/cristal/tempestade), explosões de área, AoE.
- Mecanismo: host roda o gameplay no objeto real (`new GameObject`/componente); registra via `CoopProgressao.RegistrarObjEvento(tipo,pos,p1,p2)` → ClientRpc → cliente cria cópia com `cosmetico=true` (gera visual, sem dano/spawn). Remove via `RemoverObjEvento(id)`.
- Proibido: construir o objeto só no host sem registrar (cliente nunca vê).

### UI (as duas que mais davam bug)

**P4 — UI PESSOAL** → vincula em `PlayerStats.Local` (NÃO replica)
- Cobre: HUD próprio (vida/XP/cooldown), menu, opções, indicadores do próprio player.
- Regra: cada player vê a SUA. Replicar seria bug.
- Mecanismo: ler sempre de `PlayerStats.Local` (nunca finder global). No SP, `Local` é o único → funciona igual.
- Proibido: `FindFirstObjectByType<PlayerStats>` / `FindGameObjectWithTag("Player")` pra alimentar HUD (pega o player 1, não o local).

**P5 — UI COMPARTILHADA** → dirigida por estado sincronizado (cada cliente desenha a própria cópia)
- Cobre: barra/nome/fase de boss, card/banner de evento, números de dano, avisos globais.
- Regra: os dois veem IGUAL.
- Mecanismo: estado em `NetworkVariable`/RPC (host autoritativo); o cliente constrói a MESMA UI a partir do estado. Padrões que já existem: `IBossHud`+`BossHudNet` (barra de boss), `GerenciadorEventos.AplicarEstadoCoop` (card de evento via `CoopProgressao`), `EnemyNet.ReplicarNumeroDano` (números de dano).
- Proibido: construir UI compartilhada num caminho host-only (só aparece no host).

---

## A decisão (qual porta?)

```
É UI?
├─ sim → é pessoal (mostra MEU estado)?      → P4 (Local, não replica)
│         é do mundo/compartilhada?          → P5 (estado sincronizado)
└─ não → spawna um GameObject que se move?   → P1 (NetSpawn)
          é um efeito que acontece NUM player? → P2 (rotear pro dono)
          é um efeito/objeto no MUNDO?         → P3 (host-auth + rebuild cosmético)
```

Se você não consegue encaixar em nenhuma, é sinal de que é **local-only legítimo** (decal puramente cosmético, UI de menu) → marca explicitamente (ver escape hatch).

---

## Escape hatch (declaração de intenção)

UI/efeito que é local DE PROPÓSITO não é falha — mas tem que ser **declarado**, senão o gate reclama:

```csharp
// coop-local-ok: menu de pausa é por-instância, não replica
Instantiate(painelPausa);
```

O marcador `coop-local-ok:` faz o gate ignorar aquela linha. Sem marcador = aparece na lista suja.

---

## O Gate (o filtro)

Roda em `git bash` na raiz do projeto. Cospe a **lista suja**: tudo que viola o princípio e não tem `coop-local-ok`. Script em `tools/coop_gate.sh`.

**Padrões PROIBIDOS** (cheiro "centrado no player 1"):
- `FindGameObjectWithTag("Player")` / `FindGameObjectsWithTag("Player")`
- `FindFirstObjectByType<PlayerStats>` / `FindAnyObjectByType<PlayerStats>` / `FindObjectOfType<PlayerStats>`
- `GameObject.Find(...)` mirando player

**LEGÍTIMO (não é violação):**
- `other.GetComponent<PlayerStats>()` num trigger/colisão (pega QUEM você acertou — correto).
- `PlayerStats.Local`, `PlayerStats.MaisProximo(pos)`, `PlayerStats.MaisProximoTransform(pos)`.
- `Instantiate` marcado com `coop-local-ok`.

### Lista suja ATUAL (medida em 2026-06-23)

Finders de player = **~20 pontos**, bem menor que o medo abstrato:

- `FindGameObjectWithTag("Player")` — **11** arquivos: DashPickup, EspiritoDeLuz, SwordSpinSkillBehavior, ImaPickup, HealthPickup, EspiritoEvento, LightPickup, PocaoCura, RadiusBoostPickup, spawn_inimigo, XPOrb. *(vários já usam `MaisProximoTransform` e só mantêm o tag como fallback — checar caso a caso)*
- `Find*ObjectByType<PlayerStats>` — **9** ocorrências em 7 arquivos: ProjectileController, danoinimigo, GerenciadorEventos, Fase2LuzManager, CeifadorEvento(2), GameSceneManager(2), SkillCooldownDisplay.

> Obs.: o gate de finders NÃO pega "UI compartilhada construída host-only" (P5) nem "Instantiate de visual" (P1) — esses são por revisão guiada pela árvore de decisão, não por grep simples. O grep cobre o cheiro mais comum e mais barato.

---

## Ordem de migração (portas ANTES do filtro)

1. **Portas prontas o suficiente** (status):
   - P1 `NetSpawn` — ✅ existe e usado.
   - P2 `PlayerNet` OwnerRpc — ✅ existe (dano/status/cura/luz/defensivas/ultimates roteados).
   - P3 rebuild cosmético — 🟡 infra pronta (`CoopProgressao.RegistrarObjEvento`+ClientRpc) + Zona como template; faltam 6 procedurais.
   - P4 `PlayerStats.Local` — ✅ mecanismo existe; faltam ~alguns sites.
   - P5 estado sincronizado — 🟡 padrões existem (BossHudNet/AplicarEstadoCoop); falta garantir que TODA UI compartilhada use.
2. **Ligar o gate** (`tools/coop_gate.sh`) → gerar lista suja.
3. **Moer a lista**: cada item → árvore de decisão → porta certa. Marcar `coop-local-ok` o que for local legítimo.
4. **Travar**: gate roda antes de cada commit/build; violação nova bloqueia.

## Limites honestos
- O gate pega o bug **estrutural** (a maioria). Não pega erro **semântico** (rotear pro player errado) nem divergência de **runtime** (timing/interpolação) — esses ainda exigem teste no MPPM (que só o usuário roda).
- As portas precisam cobrir os casos reais; se ficarem "vazadas", todo mundo usa escape hatch e o filtro erode. Desenhar/robustecer as portas é o trabalho crítico, não o gate.
