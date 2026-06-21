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

### A. Singletons compartilhados que deveriam ser por-player — **CRÍTICO** (vaza entre players)

- **`SkillEvolutionManager`** — `DontDestroyOnLoad` singleton; `evolucoesAtivas` é **compartilhada** →
  evolução do player 2 afeta as skills do player 1 (`SkillEvolutionManager.Tem(...)` checado nos dois).
  **Padrão P2.** Fix: um `SkillEvolutionManager` POR player (componente no player), e `Tem(tipo)` /
  `AplicarEvolucao` operam no manager do `PlayerStats.Local`. As skills já rodam no dono → leem o manager do dono.
  ⚠️ É o bug "se o player 2 evolui o efeito aparece no player 1".
- Verificar: `StatusCardSystem` (cartas) — já lê `PlayerStats.Local`, mas confirmar se o estado é por-player.

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

## Plano em lotes (ordem sugerida)

1. **Lote A — Singletons por-player (CRÍTICO):** `SkillEvolutionManager` → por-player. Fecha o vazamento de evolução. (+ auditar outros singletons de estado.)
2. **Lote B — Efeitos por-player roteados:** queima, vinhetas/flashes de dano/morte → rotear pro dono (P1). Varrer categorias C/D pra usar Local/MaisProximo certo.
3. **Lote C — Boss completo:** replicar `IBossHud` nos 3 bosses restantes + fase + banner + partículas de morte (broadcast).
4. **Lote D — Eventos:** UI + mecânicas no cliente (o maior; um evento por vez).
5. **Lote E — Timer/run-cycle + missões** sincronizados.

## Mecanismo central reutilizável

`PlayerNet` já é o ponto de roteamento por-player (RPCs SendTo.Owner). `CoopPauseManager` já serve
de broadcast global (ex.: `TremerClientRpc`). `IBossHud`/`BossHudNet` é o padrão de "UI host-only no
cliente". Esses três cobrem os 4 padrões de conserto — a auditoria é aplicá-los nos pontos acima.
