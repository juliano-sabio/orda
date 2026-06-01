# Sistema de Elementos — Design Spec

## Objetivo

Bosses dropam tokens de elemento. O player pode infundir um elemento em qualquer skill ativa (não ultimates, não passivas). A skill ganha uma característica do elemento escolhida pelo player entre 2 opções, é permanente e visualmente distinta.

## Escopo

**Incluído:**
- 10 elementos: Fogo, Ar, Terra, Água, Raio, Gelo, Planta, Trevas, Luz, Corrompido
- Drop de token de elemento ao matar boss
- UI de 2 etapas para infundir elemento em skill ativa
- 2 características por elemento (player escolhe 1)
- 1 elemento por skill, permanente, não substituível
- Badge de elemento no HUD abaixo do ícone da skill
- Efeito visual na skill com elemento (tint de cor + partículas)

**Excluído:** Ultimates e Passivas não recebem elementos.

---

## Elementos e Características

| Elemento | Cor | Opção A | Opção B |
|---|---|---|---|
| 🔥 Fogo | #FF4400 | **Queimadura** — DoT 3 ticks em 3s | **Explosão** — dano AoE ao atingir |
| 💨 Ar | #44CCFF | **Recuo** — knockback forte | **Rajada** — -30% cooldown desta skill |
| 🪨 Terra | #AA8844 | **Atordoamento** — stun 1.5s (chance 40%) | **Escudo de Pedra** — escudo temporário ao ativar |
| 💧 Água | #4488FF | **Lentidão** — -50% velocidade por 3s | **Cura** — regenera 15% do dano causado |
| ⚡ Raio | #FFDD00 | **Cadeia** — salta para 2 inimigos próximos (60% dano) | **Paralisia** — 35% chance de imobilizar por 1s |
| ❄️ Gelo | #AADDFF | **Congelamento** — congela por 2s (imóvel + vulnerável) | **Fragilidade** — +25% dano de todas as fontes por 3s |
| 🌿 Planta | #44AA44 | **Veneno** — DoT 5 ticks em 5s | **Enraizamento** — imóvel por 2.5s |
| 🌑 Trevas | #6622AA | **Maldição** — -30% defesa por 4s | **Roubo de Vida** — 20% do dano vira cura |
| ☀️ Luz | #FFEE88 | **Sagrado** — +50% dano vs Elite e Boss | **Cegamento** — -40% dano e vel. ataque por 3s |
| 💜 Corrompido | #AA2288 | **Caos** — dano aleatório 50–250% | **Infecção** — propaga 60% do dano a 3 inimigos próximos |

---

## Arquitetura

### Novos arquivos

**`Assets/scripts/Elements/ElementType.cs`**
```csharp
public enum ElementType { None, Fogo, Ar, Terra, Agua, Raio, Gelo, Planta, Trevas, Luz, Corrompido }

public enum CharacteristicType {
    // Fogo
    Queimadura, Explosao,
    // Ar
    Recuo, Rajada,
    // Terra
    Atordoamento, EscudoPedra,
    // Agua
    Lentidao, Cura,
    // Raio
    Cadeia, Paralisia,
    // Gelo
    Congelamento, Fragilidade,
    // Planta
    Veneno, Enraizamento,
    // Trevas
    Maldicao, RouboVida,
    // Luz
    Sagrado, Cegamento,
    // Corrompido
    Caos, Infeccao
}
```

**`Assets/scripts/Elements/ElementDefinition.cs`**
```csharp
[System.Serializable]
public class ElementCharacteristic {
    public string nome;
    public string descricao;
    public CharacteristicType tipo;
    public float valor1;   // ex: duração, multiplicador, chance
    public float valor2;   // ex: dano tick, raio AoE
}

[System.Serializable]
public class ElementDefinition {
    public ElementType tipo;
    public string nomeDisplay;
    public Color cor;
    public Sprite icone;
    public ElementCharacteristic[] caracteristicas; // sempre 2
}
```

**`Assets/scripts/Elements/ElementRegistry.cs`**
```csharp
[CreateAssetMenu(fileName = "ElementRegistry", menuName = "Horda/ElementRegistry")]
public class ElementRegistry : ScriptableObject {
    public ElementDefinition[] elementos; // 10 entradas
    public ElementDefinition Get(ElementType tipo) { ... }
}
```

**`Assets/scripts/Elements/ElementDropToken.cs`**
```csharp
public class ElementDropToken : MonoBehaviour {
    public ElementType elementType;
    void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;
        ElementApplicationUI.Instance.Abrir(elementType);
        Destroy(gameObject);
    }
}
```

**`Assets/scripts/UI/ElementApplicationUI.cs`**
- Singleton, Canvas em ScreenSpaceOverlay acima dos outros (sortingOrder 200)
- `Abrir(ElementType)`: pausa o jogo (`Time.timeScale = 0`), exibe Etapa 1
- **Etapa 1**: lista skills com `SkillType.Active` e `appliedElement == ElementType.None`; skills com elemento já aplicado aparecem desabilitadas
- **Etapa 2**: mostra as 2 características do elemento; player confirma
- Ao confirmar: aplica na SkillData, `Time.timeScale = 1`, fecha painel
- Dark fantasy palette: fundo `#120F1E`, borda dourada `#C8A840`

**`Assets/scripts/Elements/SkillElementEffect.cs`**
- Componente adicionado ao GameObject da skill ao ser ativada
- `ExecutarEfeito(CharacteristicType tipo, float valor1, float valor2, GameObject alvo)`
- Implementa cada `CharacteristicType` via switch
- Para DoT (Queimadura, Veneno): adiciona `StatusDoT` no inimigo
- Para CC (Stun, Slow, Root, Freeze): adiciona `StatusCC` no inimigo
- Para Cura/RouboVida: chama `player.Curar(amount)`
- Para Cadeia/Infecção: busca inimigos próximos via `Physics2D.OverlapCircleAll`

### Arquivos modificados

**`SkillData.cs`** — adicionar campos:
```csharp
public ElementType appliedElement = ElementType.None;
public int appliedCharacteristicIndex = -1; // -1 = sem característica
```

**`SkillIconsHUD.cs`** — após criar o slot da skill:
- Se `skill.appliedElement != None`: cria GameObject filho com `Image` circular (18×18px), cor do elemento, posição `(0, -26)` relativa ao ícone da skill
- Usa `ElementRegistry` para obter cor e ícone do elemento

**`BossController.cs`** — em `OnDeath()`:
```csharp
[Header("Elemento Drop")]
public ElementType elementoDrop = ElementType.None; // configurar no Inspector por boss
public GameObject elementoDropPrefab;
// no OnDeath: Instantiate(elementoDropPrefab, transform.position, Quaternion.identity)
//   + setar ElementDropToken.elementType = elementoDrop
```

### Efeitos visuais da skill com elemento

Quando `appliedElement != None`, a skill recebe visualmente:
- **Borda do ícone no HUD**: cor do elemento (já implementado via border color no SkillIconsHUD)
- **Badge abaixo**: bolinha colorida com ícone do elemento (18×18px)
- **Partículas**: prefab de partículas por elemento instanciado na skill; cada elemento tem `ParticleSystem` com cor correspondente (loops enquanto skill existe)
- **Tint da skill**: `SpriteRenderer.color` tintado com 30% da cor do elemento + 70% branco

---

## Fluxo de Dados

```
Boss morre → Instantiate(elementoDropPrefab) com ElementDropToken.elementType setado
Player colide com token → ElementApplicationUI.Abrir(elementType) + Destroy(token)
UI pausa jogo → mostra skills ativas sem elemento
Player seleciona skill → mostra 2 características
Player confirma → skill.appliedElement = tipo, skill.appliedCharacteristicIndex = index (0 ou 1)
SkillIconsHUD.AtualizarSlot(skill) → exibe badge do elemento
Skill ativada → SkillElementEffect.ExecutarEfeito() no hit/uso
```

---

## Assets necessários

- `Assets/Resources/Elements/ElementRegistry.asset` — SO com os 10 elementos configurados
- `Assets/Prefabs/Elements/ElementToken_[Fogo|Ar|Terra|Agua|Raio|Gelo|Planta|Trevas|Luz|Corrompido].prefab` — 10 prefabs de pickup
- `Assets/Prefabs/Elements/Particles_[elemento].prefab` — 10 prefabs de partículas
- `Assets/assets/UI/elements/icon_[elemento].png` — 10 ícones (32×32 sprites)

---

## Restrições

- Skill só pode ter 1 elemento (verificado antes de mostrar na lista)
- Elemento aplicado é permanente (sem remoção)
- Apenas `SkillType.Active` aparece na lista de infusão
- Bosses têm campo `elementoDrop` configurável no Inspector (pode ser `None` para bosses que não dropam elemento)
- Se player pegar token mas não tiver nenhuma skill ativa sem elemento, exibir mensagem "Nenhuma skill disponível" e o token é descartado (sem guardar no inventário)
- `Time.timeScale = 0` durante a UI de aplicação para pausar o jogo
