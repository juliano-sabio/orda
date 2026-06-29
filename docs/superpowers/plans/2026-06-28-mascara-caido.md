# Máscara ao Cair — Plano de Implementação

> **Para workers:** implementar tarefa-a-tarefa. Verificação = compila limpo (refresh_unity +
> read_console) + checagem visual no editor (não há test framework no projeto; é feature
> visual/cosmética). Passos com checkbox `- [ ]`.

**Goal:** quando o player morre, o corpo some e a máscara do servo cai no chão; em co-op a
máscara persiste como marcador de revive, em single-player é só animação de morte.

**Architecture:** componente cosmético `MascaraCaido` (anexado em runtime no player, como o
`MovementDust`) dispara o sumiço do corpo + cria um objeto `MascaraChao` que cai com quique.
Dirigido pelo estado caído já existente: co-op via `PlayerNet.downed` (NetworkVariable
sincronizado → roda em todas as cópias, sem RPC novo), SP via `player_stats.Die()`.

**Tech Stack:** Unity 2D, C#, NGO (NetworkVariable), sprites procedurais/coroutines (padrão do
projeto). Asset da máscara recortado do sprite do servo via Aseprite, em `Resources` (build-safe).

---

### Task 1: Asset — extrair a máscara do servo pro Resources

**Files:**
- Create: `horda/Assets/Resources/ui/mascara_servo.png` (+ .meta gerado)
- Fonte: `horda/Assets/assets/player/servo movimentação direito 1.ase` (63x63, frame 1 = máscara branca/chifres no topo)

- [ ] **Step 1:** Exportar o frame 1 do servo pra um PNG temporário (aseprite MCP `export_sprite`, frame_number=1) e inspecionar (`get_pixels`/visualizar) pra achar o bbox da região clara (máscara + chifres) — topo do sprite.
- [ ] **Step 2:** Recortar o sprite ao bbox da máscara (aseprite `crop_sprite` numa cópia) e exportar como PNG.
- [ ] **Step 3:** Copiar pro projeto via execute_code:
```csharp
var dst = "Assets/Resources/ui/mascara_servo.png";
AssetDatabase.CopyAsset("<png temporário importado>", dst); // ou ImportAsset de um PNG colocado na pasta
AssetDatabase.ImportAsset(dst);
var ti = AssetImporter.GetAtPath(dst) as TextureImporter;
ti.textureType = TextureImporterType.Sprite;
ti.spriteImportMode = SpriteImportMode.Single;
ti.filterMode = FilterMode.Point;          // pixel art crisp
ti.textureCompression = TextureImporterCompression.Uncompressed;
ti.SaveAndReimport();
return "sprite=" + (AssetDatabase.LoadAssetAtPath<Sprite>(dst) != null);
```
- [ ] **Step 4:** Confirmar `Resources.Load<Sprite>("ui/mascara_servo") != null` (execute_code). Resultado esperado: `True`.
- [ ] **Step 5 (iteração visual):** mostrar o recorte ao usuário; ajustar bbox se a máscara vier cortada/com manto. (Sem commit ainda — asset entra no commit do Task final.)

---

### Task 2: `MascaraChao` — a máscara que cai no chão

**Files:**
- Create: `horda/Assets/scripts/MascaraChao.cs`

- [ ] **Step 1:** Criar o arquivo:
```csharp
using System.Collections;
using UnityEngine;

// A máscara no chão: cai com quique + leve rotação até assentar. Co-op: fica como marcador
// (destruída pelo MascaraCaido.Levantar). SP: some sozinha após ~0.8s.
public class MascaraChao : MonoBehaviour
{
    public static GameObject Criar(Vector3 posPlayer, SpriteRenderer refCorpo, bool persistente)
    {
        var sp = Resources.Load<Sprite>("ui/mascara_servo");
        if (sp == null) return null;
        var go = new GameObject("MascaraChao");
        go.transform.position = posPlayer + Vector3.up * 0.5f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        if (refCorpo != null) { sr.sortingLayerID = refCorpo.sortingLayerID; sr.sortingOrder = refCorpo.sortingOrder; }
        go.AddComponent<MascaraChao>().StartCoroutine_Queda(posPlayer, persistente);
        return go;
    }

    void StartCoroutine_Queda(Vector3 posPlayer, bool persistente) => StartCoroutine(Queda(posPlayer, persistente));

    IEnumerator Queda(Vector3 posPlayer, bool persistente)
    {
        Vector3 chao = posPlayer + Vector3.down * 0.2f; // assenta um tico abaixo dos pés
        Vector3 ini  = transform.position;
        float dur = 0.45f;
        float ang = Random.Range(-25f, 25f);
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float p = t / dur;
            float y = Mathf.Lerp(ini.y, chao.y, 1f - Mathf.Pow(1f - p, 2f)); // ease-out (gravidade)
            float quique = Mathf.Sin(p * Mathf.PI) * 0.12f * (1f - p);
            transform.position = new Vector3(Mathf.Lerp(ini.x, chao.x, p), y + quique, ini.z);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(ang, ang * 0.3f, p));
            yield return null;
        }
        transform.position = chao;

        if (!persistente) // SP: some sozinha
        {
            yield return new WaitForSecondsRealtime(0.8f);
            var sr = GetComponent<SpriteRenderer>();
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                if (sr == null) yield break;
                var c = sr.color; c.a = Mathf.Lerp(1f, 0f, t / 0.4f); sr.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
```
- [ ] **Step 2:** Compilar (refresh_unity scope=all pelo arquivo novo) + read_console. Esperado: 0 erros CS.

---

### Task 3: `MascaraCaido` — some o corpo + dispara a máscara

**Files:**
- Create: `horda/Assets/scripts/MascaraCaido.cs`

- [ ] **Step 1:** Criar o arquivo:
```csharp
using System.Collections;
using UnityEngine;

// Animação de "morte/caído": o corpo do personagem some (fade) e a máscara cai no chão.
// Co-op: máscara persiste (marcador de revive). SP: animação de morte (máscara some sozinha).
// Anexado em runtime (moviment_player2.Start) → cobre SP e co-op. Dirigido pelo estado caído,
// que em co-op é sincronizado (PlayerNet.downed) → roda em todas as cópias sem RPC.
public class MascaraCaido : MonoBehaviour
{
    SpriteRenderer corpo;
    GameObject mascaraAtual;
    Coroutine anim;

    void Awake() => corpo = GetComponent<SpriteRenderer>();

    public void Cair(bool persistente)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(RotinaCair(persistente));
    }

    public void Levantar()
    {
        if (anim != null) StopCoroutine(anim);
        if (mascaraAtual != null) { Destroy(mascaraAtual); mascaraAtual = null; }
        anim = StartCoroutine(RotinaLevantar());
    }

    IEnumerator RotinaCair(bool persistente)
    {
        if (corpo != null)
        {
            Color c0 = corpo.color;
            for (float t = 0f; t < 0.16f; t += Time.unscaledDeltaTime)
            {
                corpo.color = new Color(c0.r, c0.g, c0.b, Mathf.Lerp(c0.a, 0f, t / 0.16f));
                yield return null;
            }
            corpo.enabled = false;
            corpo.color = c0; // restaura a cor pra reaparecer no revive
        }
        mascaraAtual = MascaraChao.Criar(transform.position, corpo, persistente);
        anim = null;
    }

    IEnumerator RotinaLevantar()
    {
        if (corpo != null)
        {
            corpo.enabled = true;
            Color c0 = corpo.color;
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                corpo.color = new Color(c0.r, c0.g, c0.b, Mathf.Lerp(0f, 1f, t / 0.2f));
                yield return null;
            }
            corpo.color = c0;
        }
        anim = null;
    }
}
```
- [ ] **Step 2:** Compilar + read_console. Esperado: 0 erros CS.

---

### Task 4: Anexar o componente em runtime (SP + co-op)

**Files:**
- Modify: `horda/Assets/scripts/moviment_player2.cs` (no `Start`, perto do `AddComponent<MovementDust>`)

- [ ] **Step 1:** No fim do `Start()`, depois de garantir o `MovementDust`, adicionar:
```csharp
        // Animação de máscara ao cair/morrer (cosmética; dirigida pelo estado caído).
        if (GetComponent<MascaraCaido>() == null) gameObject.AddComponent<MascaraCaido>();
```
- [ ] **Step 2:** Compilar + read_console. Esperado: 0 erros CS.

---

### Task 5: Hook co-op (PlayerNet.downed → Cair/Levantar)

**Files:**
- Modify: `horda/Assets/scripts/net/PlayerNet.cs` (no `OnNetworkSpawn`/`OnNetworkDespawn` + novo handler)

- [ ] **Step 1:** No `OnNetworkSpawn`, assinar a mudança do `downed`:
```csharp
        downed.OnValueChanged += AoMudarCaido;
        if (downed.Value) AoMudarCaido(false, true); // já caído ao spawnar (raro)
```
- [ ] **Step 2:** No `OnNetworkDespawn`, desassinar:
```csharp
        downed.OnValueChanged -= AoMudarCaido;
```
- [ ] **Step 3:** Adicionar o handler na classe:
```csharp
    void AoMudarCaido(bool _, bool caido)
    {
        var mc = GetComponent<MascaraCaido>();
        if (mc == null) mc = gameObject.AddComponent<MascaraCaido>();
        if (caido) mc.Cair(true);   // co-op: máscara persiste (marcador de revive)
        else        mc.Levantar();
    }
```
- [ ] **Step 4:** Compilar + read_console. Esperado: 0 erros CS. (Se `OnNetworkSpawn`/`OnNetworkDespawn` não existirem como override, criar — mas o PlayerNet já os tem; só inserir as linhas.)

---

### Task 6: Hook single-player (player_stats.Die → Cair(false))

**Files:**
- Modify: `horda/Assets/scripts/player_stats.cs` (início do método `Die()`)

- [ ] **Step 1:** Ler o início de `Die()` (find_in_file/Read) pra confirmar que ele não destrói/recarrega a cena imediatamente (a animação usa unscaled time e dura ~0.6s; se Die troca de cena na hora, a máscara fica muito breve — aceitável, mas registrar).
- [ ] **Step 2:** Na primeira linha de `Die()`, antes de qualquer game-over:
```csharp
        GetComponent<MascaraCaido>()?.Cair(false); // animação de morte (SP); máscara some sozinha
```
- [ ] **Step 3:** Compilar + read_console. Esperado: 0 erros CS.

---

### Task 7: Verificação visual + commit + merge

- [ ] **Step 1:** Pedir ao usuário pra testar:
  - **SP:** morrer → corpo some + máscara cai → game over.
  - **Co-op (MPPM):** P1/P2 cair → corpo some + máscara cai e FICA no chão; aproximar o aliado → barra de revive enche → reviver → máscara some + personagem reaparece. Conferir que a animação aparece nos DOIS clientes (dono + fantoche).
- [ ] **Step 2:** Ajustar (recorte da máscara, altura da queda, durações) conforme feedback.
- [ ] **Step 3:** Commit (asset + 4 scripts) e merge FF na main:
```bash
git add horda/Assets/Resources/ui/mascara_servo.png horda/Assets/Resources/ui/mascara_servo.png.meta \
  horda/Assets/scripts/MascaraChao.cs horda/Assets/scripts/MascaraChao.cs.meta \
  horda/Assets/scripts/MascaraCaido.cs horda/Assets/scripts/MascaraCaido.cs.meta \
  horda/Assets/scripts/moviment_player2.cs horda/Assets/scripts/net/PlayerNet.cs \
  horda/Assets/scripts/player_stats.cs
git commit -m "feat(juice): máscara cai ao morrer; revive na máscara (co-op)"
# FF merge na main (checar merge-base --is-ancestor origin/main feat/mp-lobby)
```

---

## Self-review

- **Cobertura do spec:** asset (Task 1) ✓; MascaraCaido (Task 3) ✓; MascaraChao (Task 2) ✓;
  co-op via downed (Task 5) ✓; SP via Die (Task 6) ✓; anexar runtime (Task 4) ✓; persistência
  co-op vs some-no-SP (`persistente` em Task 2/3/5/6) ✓; revive na máscara = sem mudança de
  lógica ✓.
- **Placeholders:** nenhum — todo código está completo. O único "iterativo" é o bbox do recorte
  (Task 1 step 5), inerente a um asset visual.
- **Consistência de tipos:** `MascaraCaido.Cair(bool)`/`Levantar()`, `MascaraChao.Criar(Vector3,
  SpriteRenderer, bool)` — assinaturas batem entre Task 2/3/5/6. `corpo` = SpriteRenderer do
  player. `persistente`: true=co-op, false=SP — consistente em todas as tasks.
