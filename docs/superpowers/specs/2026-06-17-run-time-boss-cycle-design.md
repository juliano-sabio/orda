---
title: Tempo de Run, Boss Final e Ciclo Infinito — Design
date: 2026-06-17
tags: [run, timer, boss, modo-infinito, dificuldade, escala, design]
---

# Tempo de Run, Boss Final e Ciclo Infinito — Design

**Goal:** Transformar a run num arco de ~30 min que **pausa o contador durante bosses**, **culmina no boss final** (vitória/derrota) e, ao vencer, oferece **terminar** ou seguir num **modo infinito** que repete o ciclo mantendo a build. Inimigos **e bosses** escalam por tempo; a dificuldade da fase define a **intensidade** (não a duração).

**Tech:** Unity C#. Mexe em: `TimerManager`, `controlei_inimigo` (InimigoController), `EnemyScaling`, `spawn_inimigo`, bosses (`BossController` + scripts de boss), `EscolherTerrenoUI`, e uma UI nova de escolha pós-vitória.

---

## 1. Estado atual (o que já existe)

| Sistema | Estado |
|---|---|
| `TimerManager` | Conta **regressivo** de `levelDuration` (**hoje 180s**) até 0. `bossEvents`/`timedEvents` em `triggerTime` normalizado (0–1). Ao zerar dispara `OnTimeUp`. |
| `OnTimeUp` (hoje) | **Só mostra texto** ("time_up") em `TimeBarEffects`. Não tem tela de vitória nem encerra a run. |
| Morte do player | `PlayerStats.Die()` → `GameOverUI.Mostrar()`. |
| `GerenciadorEventos` | Singleton; dispara **eventos aleatórios** a cada `intervaloEventos` (~60s) via `Random.Range`. **Já é randômico.** |
| `EnemyScaling` | Escala vida/dano dos inimigos comuns por tempo (`Time.timeSinceLevelLoad`). **Bosses ficam de fora** (path `dadosInimigo == null`). |
| `spawn_inimigo` | Escala frequência/limite de spawn por tempo (linear, sem teto de tempo). |
| Bosses | Têm `InimigoController` (vida em `vidaMaxima`, flag `estaMorrendo`) + um `BossController`/script de boss. A vida é setada externamente (ex.: `BossSlimeGuardaElite`: `controller.vidaMaxima = vidaBoss`). |
| Dificuldade | **Não propagada por número** — `EscolherTerrenoUI` só grava `PlayerPrefs "ProximaCena"`. Cada fase é uma cena com `dificuldade` fixa (Reino Slime=1, Abismo=2, Caverna Aranha=3, Sobrevivência=5). |

---

## 2. Design

### 2.1 Duração e dificuldade
- `levelDuration` → **1800s (30 min)**, igual em todas as fases.
- **Dificuldade = intensidade**: a dificuldade da fase (1–5) deixa a escala mais agressiva (taxa maior / multiplicador inicial maior), **sem mudar a duração**.
- Propagação: `EscolherTerrenoUI` grava também `PlayerPrefs.SetInt("Dificuldade", fase.dificuldade)` ao escolher a fase; `EnemyScaling` lê isso e aplica um fator de intensidade às suas taxas.

### 2.2 Pausa do contador em boss
- Enquanto **qualquer boss estiver vivo**, o `TimerManager` **não decrementa** `currentTime` (countdown pausado). Volta a correr quando não há boss vivo.
- "Boss vivo" = existe um GameObject com `BossController` (ou marcador `IsBoss`) cujo `InimigoController.estaMorrendo == false`.
- Os eventos do `GerenciadorEventos` e o gameplay continuam normais durante o boss (só o **countdown** pausa).

### 2.3 Boss final = fim da run
- A run **culmina no boss final**. Quando o countdown chega a 0 (ou no `triggerTime` do boss final), o **boss final é invocado** e o countdown fica pausado (ele é um boss).
- **Vitória** = boss final morre → abre a **UI de escolha** (§2.4).
- **Derrota** = player morre (a qualquer momento, contra qualquer coisa) → `GameOverUI` (fluxo atual).
- `OnTimeUp` deixa de ser cosmético: passa a **invocar o boss final** (em vez de só mostrar texto).
- Marcação do "boss final": um flag no `BossEvent` (ex.: `bool ehFinal`) — o último da timeline.

### 2.4 Escolha pós-vitória
- Ao derrotar o boss final, surge uma UI (estilo dark-fantasy, como as outras) com 2 opções:
  - **Terminar Run** → tela/registro de vitória e volta ao menu.
  - **Modo Infinito** → entra no loop (§2.5).

### 2.5 Modo Infinito (ciclo)
- Reinicia o **ciclo da run** mantendo **tudo** do player (vida/atributos, skills, evoluções, level, ultimates).
- Reset por ciclo: `currentTime = levelDuration`, `currentBossIndex = 0`/`currentEventIndex = 0` (re-arma a timeline), **sem** resetar o player.
- **Eventos**: já randômicos (`GerenciadorEventos` continua rodando) — nada a fazer.
- **Bosses**: por enquanto **fixos** (os mesmos da fase), mas **escalados** (mais fortes a cada ciclo, via §2.6).
- Cada ciclo termina de novo no **boss final** → mesma **escolha** (Terminar / continuar).
- A **escala é cumulativa** (não reseta entre ciclos), então fica progressivamente impossível — é o "quanto você aguenta".

### 2.6 Escala dos bosses
- Bosses passam a escalar vida e dano por tempo, usando os mesmos multiplicadores temporais (`EnemyScaling.VidaMult()`/`DanoMult()`), aplicados **no spawn do boss**:
  - Vida: `vidaMaxima *= VidaMult()` (no path de boss do `InicializarComData`, após a vida-base já estar setada).
  - Dano: multiplicar os danos do `BossController` (projétil, raio, contato…) por `DanoMult()` no setup do boss.
- **Tunável**: fator próprio pro boss (pode ser mais suave que o do inimigo comum, já que boss já tem muita vida) — decidir na §4.

> A escala usa tempo **cumulativo** (`Time.timeSinceLevelLoad`), que **não** reseta no loop nem para na pausa do countdown. Assim o modo infinito fica mais difícil a cada ciclo. A pausa do §2.2 afeta só o countdown/timeline, não a escala.

---

## 3. Componentes a construir/alterar

| Arquivo | Mudança |
|---|---|
| `TimerManager.cs` | `levelDuration` 30min; pausa countdown se boss vivo; `OnTimeUp` invoca boss final; flag `ehFinal` no `BossEvent`; API de reset de ciclo (`ReiniciarCiclo()`); detectar morte do boss final → abrir escolha. |
| `controlei_inimigo.cs` | No path boss (`dadosInimigo == null`): aplicar `VidaMult()`. Marcar/expor "é boss". |
| Bosses (`BossController` + scripts) | Aplicar `DanoMult()` aos danos no setup; garantir flag de boss e sinal de morte (já tem `estaMorrendo`). |
| `EnemyScaling.cs` | Ler `PlayerPrefs "Dificuldade"` e aplicar fator de intensidade; expor mult separado pro boss (opcional). |
| `EscolherTerrenoUI.cs` | Gravar `PlayerPrefs.SetInt("Dificuldade", fase.dificuldade)` ao escolher a fase. |
| **UI nova** (`EscolhaPosVitoriaUI.cs`) | Painel com "Terminar" / "Modo Infinito"; pausa o jogo enquanto aberto. |
| (talvez) Tela de vitória | "Terminar" precisa de um destino (tela de vitória simples ou volta ao menu com registro). |

---

## 4. Decisões (fechadas)

1. **Escala do boss** — taxa **própria, ~metade** da do inimigo comum: `bossVidaPorMinuto = 0.06`, `bossDanoPorMinuto = 0.03` (boss ≈ ×2.8 vida / ×1.9 dano aos 30 min). Também multiplicada pela intensidade da dificuldade.
2. **Intensidade por dificuldade** — fator linear `intensidade = 1 + 0.15*(dificuldade-1)` (dif 1 = ×1.0 … dif 5 = ×1.6), aplicado às taxas por minuto (inimigo e boss).
3. **"Terminar Run"** — **tela de vitória dedicada simples** (estilo dark-fantasy, como a de Game Over) → botão volta ao menu.
4. **Sobrevivência (cena Modo_sobrevivencia)** — **começa já no Modo Infinito** desde o início (sem boss-final-e-escolha; é o modo "quanto você aguenta"). As fases numeradas têm o arco completo (30 min → boss final → escolha).
5. **Pausa do countdown** — **todos** os bosses pausam o countdown (mini-bosses inclusos). Eventos do `GerenciadorEventos` e gameplay seguem; só o countdown/timeline para.
6. **Boss final por fase** — marcado por `bool ehFinal` no `BossEvent`; boss final com `triggerTime ≈ 0` (fim do countdown). Marcar em cada cena de fase.

---

## 5. Fora de escopo (agora)
- **Eventos randômicos**: já existem (`GerenciadorEventos`).
- **Bosses randômicos**: ficam fixos por fase por enquanto (pedido do usuário).
- Rebalance fino dos números (fica tunável; ajusta-se em teste).

---

## 6. Resumo do fluxo

```
Início (30min countdown) ──> eventos random + inimigos escalando + spawn subindo
   │  (boss aparece → countdown PAUSA até boss morrer; boss escalado)
   ▼
countdown chega a 0 ──> invoca BOSS FINAL (countdown pausado)
   ├─ player morre ─────────────> GAME OVER (fluxo atual)
   └─ boss final morre ─> ESCOLHA: [Terminar Run]  → vitória/menu
                                   [Modo Infinito] → reinicia ciclo
                                                     (mantém build; escala cumulativa)
                                                     → ... → boss final → ESCOLHA → ...
```
