# Auditoria de paridade co-op — acoplamento ao host/player 1

> Objetivo: o player 2 (cliente) ter a MESMA experiência do host — visuais, efeitos,
> vida, UI — adaptada. Hoje muita coisa está acoplada ao host/player 1.

## Causa raiz

O jogo foi feito **single-player primeiro**. No co-op, o **host processa tudo** e os efeitos
são aplicados via:
- `PlayerStats.Local` / `FindGameObjectWithTag("Player")` em **contexto host** → resolvem pro **player 1**.
- **Singletons compartilhados** (`DontDestroyOnLoad`) que deveriam ser **por-player**.
- **UI/lógica host-only** (o `EnemyNet` desliga scripts no cliente; eventos/timer rodam só no host).

## Padrões de conserto (4)

| Padrão | Quando | Como |
|---|---|---|
| **P1 — Rotear efeito por-player** | Efeito que atinge UM player (tela/buff/dano-visual) disparado no host | `PlayerNet …OwnerRpc` (SendTo.Owner) → aplica na tela do dono. Efeito de tela usa `PlayerStats.Local` **no cliente que renderiza**. (Ex.: escuridão do boss — FEITO) |
| **P2 — Singleton → por-player** | Estado que deveria ser individual mora num singleton global | Mover o estado pra um componente NO player (ou indexar por `OwnerClientId`). |
| **P3 — Alvo certo no host** | Código host mira "o player" via FindTag/FindFirst | `PlayerStats.MaisProximo(pos)` (host) ou rotear pro dono. |
| **P4 — UI/visual host-only no cliente** | UI/feedback criado só no host | NetworkBehaviour preservado pelo EnemyNet (ex.: `BossHudNet`/`IBossHud`) lendo estado sincronizado, OU broadcast (ex.: `CameraShaker` via `CoopPauseManager`). |

## Inventário (por categoria + prioridade)

### A. **[CORREÇÃO da 1ª versão]** Singletons NÃO são o problema

Reverificado: no **MPPM cada virtual player é um PROCESSO separado** → singletons (`SkillEvolutionManager`,
`SkillManager`, `StatusCardSystem`, `UIManager`…) são **por-processo**, NÃO compartilhados entre players.
E a evolução **já é roteada certo**: `OfertarEvolucaoOwnerRpc` é `SendTo.Owner`, o host oferta pra cada
player no processo dele, `AplicarEvolucao` roda no dono. **Não há vazamento via singleton.** A "categoria A"
da 1ª versão estava errada.

> O que o usuário viu como "evolução do player 2 no player 1" é, na verdade, a **categoria A-bis abaixo**
> (efeito de status aplicado no host indo pro player 1) ou a oferta de evolução compartilhada pós-evento
> (os dois players evoluem após um evento do mundo — comportamento correto).

### A-bis. **CRÍTICO — Efeitos no player aplicados host-side NÃO roteiam pro dono**

`PlayerStats.TakeDamage` roteia pro dono (`if (!IsOwner) TomarDanoOwnerRpc; return`), mas os DEMAIS métodos
de efeito **não**:

| Método | Roteia? | Quem aplica (host-side) |
|---|---|---|
| `TakeDamage` | ✅ sim | inimigos/bosses/projéteis |
| `AplicarSlow` | ❌ não | FantasmaGelo, ZonaGelo, projetil_inimigo |
| `AplicarVenenoPlayer` | ❌ não | FantasmaVeneno(+Atirador), SlimeVenenosa, BossCaveira |
| `AplicarQueimaduraPlayer` | ❌ não | (queima de boss/skill) |
| `AplicarParalisiaPlayer` | ❌ não | (paralisia) |
| `Heal` | ❌ não (mas pickups vão via `CurarOwnerRpc`) | zonas de cura host-side |
| `AplicarVisaoReduzida` (escuridão) | ✅ **FEITO** | ProjetilEspecialBoss |

**Consequência:** quando o host aplica esses efeitos no FANTOCHE do player 2, eles afetam só o fantoche no
host (sem efeito real no player 2 → **player 2 fica imune a slow/veneno/queimadura/paralisia de inimigo**) e
os visuais aparecem no player 1. **Esse é o verdadeiro acoplamento sistêmico**, não os singletons.

**Fix (P1, centralizado):** dar a cada método de efeito o mesmo guarda do `TakeDamage` — `if (!IsOwner) {
…OwnerRpc(args); return; }` — com os RPCs correspondentes no `PlayerNet` (SendTo.Owner). Centraliza o
roteamento num lugar e cobre TODOS os callers (inimigos/bosses/eventos) de uma vez.

### B. Efeitos por-player aplicados no host (rotear pro dono) — **P1**

- `ProjetilEspecialBoss` escuridão (visão reduzida) — **FEITO** (demonstração).
- `ProjetilEspecialPrincesa.EfeitoQueima` — efeito de queima na tela do player atingido (host-only hoje).
- Vinhetas/flashes de dano e de morte do player (verificar em `player_stats`/`UIManege`).
- Efeitos de evento que afetam UM player (ver categoria F).

### C. `FindGameObjectWithTag("Player")` (10 vivos; 3 mortos)

Vivos: `SwordSpinSkillBehavior`, `DashPickup`, `SkillElementEffect`, `EspiritoDeLuz`, `EspiritoEvento`,
`PocaoCura`, `RadiusBoostPickup`, `spawn_inimigo`, `UI/CartaSelecaoEfeito`, `XPOrb`.
Mortos (0 prefabs, ignorar): `HealthPickup`, `ImaPickup`, `LightPickup`.
Para cada: **host-side mirando "o player"** → `PlayerStats.MaisProximo` (P3); **pessoal do cliente** → `PlayerStats.Local`.

### D. `FindFirst/AnyObjectByType<PlayerStats>` (8 arquivos)

`bases_skills/ProjectileController`, `CeifadorEvento`, `configuração _adicional/GameSceneManager`,
`danoinimigo`, `Fase2LuzManager`, `GerenciadorEventos`, `UI/CartaSelecaoEfeito`, `UI/SkillCooldownDisplay`.
Mesmo tratamento da categoria C (P3 ou Local conforme o contexto).

### E. UI/lógica host-only (cliente vê parcial/nada) — **P4**

- **Bosses**: barra de vida — FEITO via `IBossHud` (Caveira = template; replicar nos outros 3).
  Pendente: fase/cor de fase + banner "apareceu" + partículas de morte.
- **Eventos** (`GerenciadorEventos`): contador/avisos/zonas + mecânicas próprias (cristal, núcleo,
  ceifador, portal, colapso, eclipse, tempestade) — Update gateado host-only.
- **Timer/run-cycle** (`TimerManager`): roda local em cada cliente (pode divergir) + fim da run host-only.
- **Missões** (`MissaoEspiritoManager`): lógica/UI host-only.

### F. Efeitos de tela de evento (host-side, pessoais) — **P1/P4**

`BordaSangueEvento`, `EventoColapso`, `EventoEclipseTorre`, `TempestadeEletricaEvento`, `Fase2LuzManager`.

## Plano em lotes (ordem sugerida) — CORRIGIDO

1. **Lote A — Roteamento de efeitos no player (CRÍTICO):** dar a `AplicarSlow`/`AplicarVenenoPlayer`/
   `AplicarQueimaduraPlayer`/`AplicarParalisiaPlayer`/`Heal` (host-side) o mesmo guarda de roteamento do
   `TakeDamage` (+ RPCs no `PlayerNet`). Fecha "player 2 imune a status + visual no player 1". (~5 métodos + RPCs.)
2. **Lote B — Find/Local em contexto host:** varrer categorias C/D (FindTag / FindFirst<PlayerStats>) pra usar
   `MaisProximo` (mira host) ou `Local` (tela do cliente). Inclui a queima visual (`EfeitoQueima`).
3. **Lote C — Boss completo:** replicar `IBossHud` nos 3 bosses restantes + fase + banner + partículas de morte.
4. **Lote D — Eventos:** UI + mecânicas no cliente (o maior; um evento por vez).
5. **Lote E — Timer/run-cycle + missões** sincronizados.

> Nota: singletons NÃO entram (são por-processo no MPPM). Evolução já está correta.

## Mecanismo central reutilizável

`PlayerNet` já é o ponto de roteamento por-player (RPCs SendTo.Owner). `CoopPauseManager` já serve
de broadcast global (ex.: `TremerClientRpc`). `IBossHud`/`BossHudNet` é o padrão de "UI host-only no
cliente". Esses três cobrem os 4 padrões de conserto — a auditoria é aplicá-los nos pontos acima.
