---
title: Infusão de Elemento para Skills Defensivas — Design
date: 2026-06-16
tags: [elementos, infusao, skills-defensivas, design]
---

# Infusão de Elemento para Skills Defensivas — Design

**Goal:** Dar às skills defensivas um conjunto próprio de características de elemento (defensivas, focadas em proteger o player), em vez das características ofensivas atuais que só fazem sentido para skills de ataque.

**Arquitetura:** Espelha o sistema atual (2 características por elemento), mas com um conjunto paralelo de características **defensivas** por elemento, aplicadas via um modelo de **3 gatilhos** (ao ativar / ao ser atingido / aura contínua). A UI de infusão detecta se a skill é defensiva e oferece o par defensivo. A aplicação roda por `SkillElementEffect.AplicarDefensivo(...)`.

**Tech:** Unity C#, sistema de elementos existente (`ElementType`, `ElementDefinition`, `ElementRegistry`, `SkillElementEffect`, `ElementApplicationUI`).

---

## 1. Problema (sistema atual)

A infusão de elemento hoje:
1. `ElementApplicationUI.Abrir(elemento)` → o jogador escolhe uma skill, depois escolhe **1 de 2 características** (`def.caracteristicas[0..1]`).
2. `ConfirmarEscolha(indice)` grava `skill.appliedElement` + `skill.appliedCharacteristicIndex`.
3. No hit, `SkillElementEffect.Aplicar(skill, alvoInimigo, dano, caller)` aplica a característica **no inimigo**.

As **22 características** (`CharacteristicType`) são **todas ofensivas** (Queimadura, Atordoamento, Veneno, Maldição, etc.), aplicadas no inimigo no momento do hit. Isso não serve para defensivas porque:

- **A maioria das defensivas nunca chama `Aplicar`** (não batem em inimigo): Auréola, Teia, Barreira de Energia (escudo), Fuga, Segunda Chance, Instinto. Só as de reflexão chamam: Barreira Reflexiva, Espelho Mágico, Escudo de Karma. (Escudo Espinhoso bate por contato mas **não** chama `Aplicar`.)
- **Mesmo nas de reflexão**, aplicar um debuff ofensivo no atacante é tematicamente torto. Uma "Barreira + Fogo" deveria dar algo **defensivo** ao player.

> Observação reveladora: `CharacteristicType` já tem `EscudoPedra` (Terra) e `Cura` (Água) com implementação vazia (`case ...: break;`) em `SkillElementEffect`. O sistema já antecipou características defensivas, mas nunca foram implementadas.

---

## 2. Visão geral do design

### 2.1 Modelo de 3 gatilhos

Cada característica defensiva declara **um gatilho** que define quando roda:

| Gatilho | Quando dispara | Skills que casam |
|---|---|---|
| `OnAtivar` | quando a defensiva é ativada (entra em efeito) | Auréola, Barreira de Energia, Instinto, Segunda Chance, Teia, Barreira Reflexiva |
| `OnAtingido` | quando o player é atingido / bloqueia / reflete (atacante conhecido) | Barreira Reflexiva, Espelho, Karma, Escudo Espinhoso, Auréola (redução) |
| `AuraContinua` | tick periódico enquanto a defensiva está ativa | Campo de Espinhos, Escudo Espinhoso, Auréola, Barreiras |

Características de **buff persistente** (Pele de Pedra, Fundação Firme) são modeladas como `OnAtivar` que **aplica um marcador** no player (lido por `PlayerStats.TakeDamage`/CC), removido quando a defensiva desativa.

### 2.2 Conjunto de características defensivas (2 por elemento)

> Números são **propostas iniciais** para tunar depois.

| Elemento | # | Nome | Gatilho | Efeito (proposta) |
|---|---|---|---|---|
| 🔥 Fogo | A | **Aura Ígnea** | AuraContinua | Queima inimigos num raio (~2.5) por ~dano_base*0.15/tick |
| 🔥 Fogo | B | **Retaliação em Chamas** | OnAtingido | Quem te atinge pega DoT de fogo (3 ticks) |
| 💨 Ar | A | **Esquiva Ventosa** | OnAtivar | +X% de chance de evadir totalmente por Y s (buff) |
| 💨 Ar | B | **Sopro Repulsor** | OnAtivar | Empurra (knockback) inimigos num raio ao ativar |
| 🪨 Terra | A | **Pele de Pedra** | OnAtivar (buff) | Redução de dano fixa (~30%) enquanto ativa |
| 🪨 Terra | B | **Fundação Firme** | OnAtivar (buff) | Imune a knockback/CC enquanto ativa |
| 💧 Água | A | **Maré Restauradora** | OnAtivar | Cura ~20% do HP máx ao ativar |
| 💧 Água | B | **Fluxo Vital** | AuraContinua | Regen contínua (~X HP/s) enquanto ativa |
| ⚡ Raio | A | **Descarga Reativa** | OnAtingido | Atordoa o atacante (~1s, chance) |
| ⚡ Raio | B | **Corrente Reflexiva** | OnAtingido | Reflete % do dano recebido em cadeia (2-3 alvos) |
| ❄️ Gelo | A | **Armadura Gélida** | OnAtivar | Ganha escudo extra (`shieldPoints`) ao ativar |
| ❄️ Gelo | B | **Toque Congelante** | OnAtingido | Congela/lenta quem te toca/ataca |
| 🌿 Planta | A | **Espinhos** | OnAtingido | Reflete % do dano de contato ao atacante |
| 🌿 Planta | B | **Raízes Protetoras** | OnAtivar | Enraíza (imobiliza) inimigos próximos ao ativar |
| 🌑 Trevas | A | **Drenagem Sombria** | OnAtingido | Cura ao refletir/bloquear (% do dano absorvido) |
| 🌑 Trevas | B | **Manto Amaldiçoado** | AuraContinua | Reduz defesa dos inimigos próximos enquanto ativa |
| ✨ Luz | A | **Bênção Sagrada** | OnAtivar | Regen + pequeno escudo ao ativar |
| ✨ Luz | B | **Luz Ofuscante** | AuraContinua | Cega inimigos próximos (reduz o dano deles) |
| 🦠 Corrompido | A | **Caos Defensivo** | OnAtivar | Efeito defensivo aleatório (escudo/cura/reflexão) |
| 🦠 Corrompido | B | **Praga Reativa** | OnAtingido | Ao ser atingido, espalha infecção (dano) ao redor |

---

## 3. Mudanças de modelo de dados

### 3.1 Novo enum `DefensiveCharacteristicType`

Separado do `CharacteristicType` ofensivo para não misturar:

```csharp
public enum DefensiveCharacteristicType
{
    // Fogo
    AuraIgnea, RetaliacaoChamas,
    // Ar
    EsquivaVentosa, SoproRepulsor,
    // Terra
    PeleDePedra, FundacaoFirme,
    // Agua
    MareRestauradora, FluxoVital,
    // Raio
    DescargaReativa, CorrenteReflexiva,
    // Gelo
    ArmaduraGelida, ToqueCongelante,
    // Planta
    Espinhos, RaizesProtetoras,
    // Trevas
    DrenagemSombria, MantoAmaldicoado,
    // Luz
    BencaoSagrada, LuzOfuscante,
    // Corrompido
    CaosDefensivo, PragaReativa
}

public enum DefensiveTrigger { OnAtivar, OnAtingido, AuraContinua }
```

### 3.2 Nova classe `DefensiveCharacteristic` + campo em `ElementDefinition`

```csharp
[System.Serializable]
public class DefensiveCharacteristic
{
    public string nome;
    [TextArea(2,3)] public string descricao;
    public DefensiveCharacteristicType tipo;
    public DefensiveTrigger gatilho;
    public float valor1;
    public float valor2;
}
```

`ElementDefinition` ganha:
```csharp
public DefensiveCharacteristic[] caracteristicasDefensivas = new DefensiveCharacteristic[2];
```

> O `ElementRegistry.asset` precisa ter as 2 características defensivas preenchidas por elemento (dado de designer). O plano de implementação inclui um editor-tool/escript para popular os defaults da tabela acima.

### 3.3 `SkillData`

Sem campo novo. `appliedCharacteristicIndex` (0/1) passa a indexar o conjunto **defensivo** quando a skill é defensiva. Quem resolve qual conjunto usar é o tipo da skill (ver §4).

---

## 4. Detecção de skill defensiva

Fonte da verdade: `SkillData.EhSkillDeAtaque()` já existe e retorna `false` para todas as defensivas (Shield, IceBarrier, Aureola, BarreiraReflexiva, BarreiraEnergia, TeiaProtecao, InstintoSobrevivencia, EspelhoMagico, EscudoKarma, SegundaChance, FugaSombras, Heal, HealthRegen, EscudoRotativo, EscudoEspinhoso).

```csharp
bool ehDefensiva = !skill.EhSkillDeAtaque();
```

Usado em 2 lugares: a UI de infusão (qual par mostrar) e a aplicação (qual rotina chamar).

---

## 5. Aplicação: `SkillElementEffect` defensivo

Nova API estática paralela à `Aplicar`:

```csharp
// Disparado pela defensiva no gatilho certo. 'atacante' pode ser null (ex.: OnAtivar).
public static void AplicarDefensivo(SkillData skill, PlayerStats player,
    DefensiveTrigger gatilho, GameObject atacante, MonoBehaviour caller)
```

Comportamento:
1. Resolve a `DefensiveCharacteristic` de `skill.appliedElement` + `skill.appliedCharacteristicIndex` em `def.caracteristicasDefensivas`.
2. Se o `gatilho` da característica **não bate** com o gatilho recebido, ignora (assim a defensiva pode chamar todos os gatilhos sem se preocupar com qual característica está ativa).
3. Roda o efeito (switch por `DefensiveCharacteristicType`), reusando helpers existentes onde der (`AplicarSlow`, `AplicarCC`, knockback, cura, DoT no atacante, etc.).

Buffs persistentes (Pele de Pedra, Fundação Firme, Esquiva) adicionam um `MonoBehaviour` marcador no player com duração (ou ligado/desligado pela defensiva), lido por `PlayerStats`:
- `PeleDePedraMarker { float reducao; }` → `PlayerStats.TakeDamage` multiplica o dano por `(1-reducao)`.
- `FundacaoFirmeMarker {}` → CC/knockback checam e ignoram.
- `EsquivaMarker { float chance; }` → `PlayerStats.TakeDamage` rola esquiva (dano 0).

> `PlayerStats.TakeDamage` (player_stats.cs:1010) já é o ponto central de redução por defesa — os markers entram aqui.

---

## 6. Hooks por skill defensiva

Cada defensiva chama `AplicarDefensivo` no(s) gatilho(s) que ela suporta. A característica ativa decide se roda (via §5.2). Pontos de hook:

| Skill | OnAtivar | OnAtingido | AuraContinua |
|---|---|---|---|
| Auréola | em `Ativar()` | em `AplicarReducao(dano)` | tick no `Update` (já tem) |
| Barreira de Energia | em `AtivarEscudo()` | quando escudo absorve | — |
| Barreira Reflexiva | em `Ativar()` | em `AplicarReflexao(dano)` (atacante já achado) | tick `Update` |
| Espelho Mágico | em `CorotinaEspelho` | em `TentarRefletir(dano)` | — |
| Escudo de Karma | em `Ativar()` | em `AbsorverHit(dano)` | — |
| Escudo Espinhoso | — | no contato (`FixedUpdate`) | tick (já roda) |
| Teia de Proteção | em `Ativar()` | — | `EmpurrarInimigos` |
| Campo de Espinhos | — | — | em `DanificarInimigos` |
| Instinto | em `Ativar()` | — | tick enquanto ativo |
| Segunda Chance | em `TentarReviver()` | — | — |
| Fuga das Sombras | em `Teleportar()` | — | — |

`OnAtingido` genérico (sem atacante específico) pode usar o evento existente `PlayerStats.OnDanoRecebido`; os que precisam do atacante usam o atacante já localizado nos métodos de reflexão.

---

## 7. Mudanças na UI (`ElementApplicationUI`)

Em `MostrarEtapa2()`:
- Se `skillSelecionada.EhSkillDeAtaque() == false` → renderiza cards a partir de `def.caracteristicasDefensivas` (em vez de `def.caracteristicas`).
- `CriarCardPoder` passa a aceitar nome/descrição/ícone vindos da característica defensiva (ou via Loc keys `defchar.name.{tipo}` / `defchar.desc.{tipo}`).
- `ConfirmarEscolha(indice)` **não muda** — continua gravando `appliedElement` + `appliedCharacteristicIndex` (0/1).

Ícones: reusar `UI/caracteristicas_icons` se houver sprite com o nome do `DefensiveCharacteristicType`; senão, fallback para letra A/B (já existe).

---

## 8. Localização

Novas chaves no `GameStrings.asset` (14 idiomas), para cada uma das 20 características defensivas:
- `defchar.name.{tipo_lowercase}`
- `defchar.desc.{tipo_lowercase}`

(Mesmo padrão das `characteristic.name.*` / `characteristic.desc.*` atuais.)

---

## 9. Testes

- **Edit-mode**: `AplicarDefensivo` resolve a característica certa por elemento+índice; gatilho incompatível é ignorado.
- **Play-mode (manual)**: infundir cada elemento numa defensiva e verificar (a) a UI mostra o par defensivo, (b) o efeito dispara no gatilho certo, (c) markers de buff aplicam/removem corretamente.
- **Regressão**: skills de ataque continuam usando o conjunto ofensivo (`EhSkillDeAtaque()==true`).

---

## 10. Riscos / questões em aberto

1. **Pele de Pedra + defesa existente**: empilhar redução fixa com `defense*0.5` pode ficar forte — tunar.
2. **AuraContinua de várias defensivas ativas ao mesmo tempo**: definir se stacka ou não.
3. **`OnAtingido` sem atacante**: algumas características (Retaliação, Descarga, Espinhos) precisam do atacante; nas defensivas sem reflexão (que não localizam atacante), essas características podem não ter alvo — decidir fallback (atacante mais próximo) ou restringir o par por skill.
4. **Caos Defensivo**: definir a lista de efeitos aleatórios possíveis.
5. **Escudo Espinhoso**: hoje não chama `Aplicar` nem `AplicarDefensivo` — precisa do hook novo no contato.

---

## 11. Escopo da implementação (resumo)

1. Enums `DefensiveCharacteristicType` + `DefensiveTrigger`; classe `DefensiveCharacteristic`; campo em `ElementDefinition`.
2. Popular `ElementRegistry.asset` com as 20 defensivas (editor-tool).
3. `SkillElementEffect.AplicarDefensivo` + helpers + markers (`PeleDePedra`, `FundacaoFirme`, `Esquiva`).
4. Hooks de `PlayerStats.TakeDamage` para os markers.
5. Hooks nas ~11 behaviors defensivas (chamar `AplicarDefensivo` nos gatilhos).
6. UI: `MostrarEtapa2` mostra par defensivo para skill defensiva.
7. Localização (`defchar.name/desc.*` × 14 idiomas).
