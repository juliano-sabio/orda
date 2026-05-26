# Design: UI Dark Fantasy — CharacterSelection

**Data:** 2026-05-25  
**Cena:** `Assets/Scenes/CharacterSelection.unity`  
**Script principal:** `Assets/scripts/UI/CharacterSelectionUI.cs`

---

## Objetivo

Substituir a UI de seleção de personagem atual (retângulos coloridos em código) por uma interface com pixel art no estilo dark fantasy gótico — pedra esculpida, iluminação avermelhada de tocha, bordas douradas, teias de aranha — inspirada na referência fornecida pelo usuário.

**Fora de escopo:** ícones de skills e passivas (reservado para sprint posterior).

---

## Assets a Criar (Aseprite)

| Asset | Arquivo | Dimensões | Tipo Unity | 9-slice borders |
|---|---|---|---|---|
| Fundo de cena | `bg_charselection.ase` | 384×216px | Simple | — |
| Painel lateral | `panel_stone.ase` | 160×160px | Sliced | 14px todos os lados |
| Moldura do preview | `frame_preview.ase` | 128×192px | Sliced | 16px todos os lados |
| Faixa seleção topo | `bar_charselect.ase` | 256×64px | Sliced | 12px todos os lados |
| Botão | `btn_stone.ase` | 128×32px | Sliced | 10px todos os lados |

**Destino dos PNGs exportados:** `Assets/assets/UI/charselection/`

---

## Paleta

| Uso | Hex |
|---|---|
| Pedra funda (fundo) | `#0D0505`, `#1A0505` |
| Pedra escura (painéis) | `#1A0808`, `#2D1515`, `#3A2020` |
| Pedra média (detalhes) | `#5A3030`, `#7A4040` |
| Pedra clara (highlights) | `#9A6060`, `#BF8080` |
| Dourado (bordas/ornamentos) | `#9A7E30`, `#C8A840`, `#E8C050` |
| Vermelho atmosférico (fundo) | `#3D1010`, `#6B1A1A`, `#8B2A2A` |

---

## Visual Detalhado de Cada Asset

### `bg_charselection.ase` — Fundo de Cena
- Cena de dungeon em perspectiva frontal
- Piso de pedra irregular no rodapé
- Paredes laterais com blocos de pedra texturizados
- Arcos góticos ao fundo com luz vermelha central (tocha/portal)
- Vignette natural: bordas escuras, centro avermelhado
- Teias de aranha nos dois cantos superiores
- Não precisa de muitos detalhes centrais — UI cobre a maior parte

### `panel_stone.ase` — Painel Lateral (STATUS / SKILLS)
- Borda de pedra esculpida com ~14px de espessura
- Exterior: `#1A0808` (pedra escura)
- Interior da borda: entalhe em dois tons — claro (`#9A6060`) no topo/esquerda, escuro (`#3A2020`) no fundo/direita (relevo 3D)
- Centro: `#2D1515` (pedra lisa escalável)
- Cantos: pequeno ornamento em cruz dourada `#C8A840`
- 9-slice: 14px em todos os lados

### `frame_preview.ase` — Moldura do Preview
- Moldura mais ornamentada, borda ~16px
- Topo e base: entalhe decorativo em arco
- Dourado mais presente nos detalhes da borda
- Centro: **alpha 0** (transparente) — deixa o personagem visível
- 9-slice: 16px em todos os lados

### `bar_charselect.ase` — Faixa de Seleção (topo)
- Barra horizontal de pedra
- Bordas superior e inferior com entalhe simples
- Terminais laterais quadrados
- Interior: `#1A0808`
- 9-slice: 12px em todos os lados

### `btn_stone.ase` — Botão
- Retângulo de pedra com bisel
- Topo/esquerda: `#9A6060` (claro — luz)
- Base/direita: `#3A2020` (escuro — sombra)
- Interior: `#2D1515`
- Sensação de botão 3D pronto para ser pressionado
- 9-slice: 10px em todos os lados

---

## Integração no CharacterSelectionUI.cs

### Novos campos no script
```csharp
[Header("Sprites Dark Fantasy")]
public Sprite spriteFundo;
public Sprite spritePainel;
public Sprite spriteMolduraPreview;
public Sprite spriteBarraTopo;
public Sprite spriteBotao;
```

### Funções modificadas

| Função | Mudança |
|---|---|
| `CriarFundo()` | `Image` do fundo recebe `spriteFundo`, `Type = Simple` |
| `CriarPainelIcones()` | `Image` da faixa recebe `spriteBarraTopo`, `Type = Sliced` |
| `CriarAreaCentral()` — painéis | Painéis INFO e STATUS recebem `spritePainel`, `Type = Sliced` |
| `CriarAreaCentral()` — preview | Moldura ao redor do RenderTexture recebe `spriteMolduraPreview`, `Type = Sliced`, `fillCenter = false` |
| `CriarRodape()` — botões | Cada botão recebe `spriteBotao`, `Type = Sliced`; cor = `Color.white` |

### Configuração no Unity Editor
- Importar PNGs em `Assets/assets/UI/charselection/`
- Para cada PNG com 9-slice: abrir Sprite Editor → definir borders
- Arrastar sprites para os campos no Inspector do `gerenciadoUI` GameObject

### Compatibilidade / Fallback
Se qualquer campo de sprite estiver `null` no Inspector, o código mantém o comportamento atual (cor sólida). Transição segura — não quebra nada.

---

## Ordem de Implementação

1. Criar pasta `Assets/assets/UI/charselection/`
2. Criar os 5 assets no Aseprite (bg → panel → frame → bar → btn)
3. Exportar para PNG
4. Configurar borders no Unity Sprite Editor
5. Adicionar campos de sprite ao `CharacterSelectionUI.cs`
6. Modificar funções de criação de UI com fallback
7. Atribuir sprites no Inspector
8. Testar em Play mode e capturar screenshot
