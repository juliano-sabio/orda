# Kill Juice (efeito de morte de inimigo, co-op) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Toda morte de inimigo NORMAL ganha um "pop" satisfatório (squash + estilhaços + anel + flash), replicado em co-op.

**Architecture:** Um efeito local auto-destrutível (`KillPopVFX`) tocado na posição da morte. `InimigoController.Morrer` (ramo não-boss) toca local + chama `EnemyNet.BroadcastMorteVFX` (RPC leve, padrão do `ReplicarNumeroDano`) pro cliente tocar igual. Bosses intocados.

**Tech Stack:** Unity 6 (6000.3.9f1), C#, Netcode for GameObjects (NGO). Efeito por `SpriteRenderer`/`LineRenderer` + lerp (sem `ParticleSystem`/`Physics2D`).

**Nota de verificação (domínio Unity VFX):** não há framework de unit-test pra efeito visual de runtime no projeto. A "verificação" de cada task é: **compila 0 erros** (`refresh_unity` + `read_console` filtrando `CS`) + **smoke test** via `execute_code` (spawna o efeito, confirma 0 erros de runtime no console). O "ficou bonito/sente bem" é confirmado pelo usuário no teste ao vivo (SP + MPPM). Spec: `docs/superpowers/specs/2026-06-27-kill-juice-coop-design.md`.

---

## File Structure
- **Create** `horda/Assets/scripts/KillPopVFX.cs` — o efeito (4 camadas + factory `Tocar`). Responsabilidade única: montar/animar/auto-destruir o pop.
- **Modify** `horda/Assets/scripts/net/EnemyNet.cs` — `BroadcastMorteVFX` + `MorteVFXClientRpc` (replicação).
- **Modify** `horda/Assets/scripts/controlei_inimigo.cs` — em `Morrer()`, ramo não-boss: tocar + broadcast antes do `NetSpawn.Despawnar`.

---

### Task 1: `KillPopVFX` — o efeito de pop (SP, sem rede)

**Files:**
- Create: `horda/Assets/scripts/KillPopVFX.cs`

- [ ] **Step 1: Criar o arquivo com o efeito completo**

```csharp
using System.Collections;
using UnityEngine;

// Kill juice: pop satisfatório na morte de inimigo normal (squash + estilhaços + anel + flash).
// Local e auto-destrutível; em co-op é tocado nos dois lados via EnemyNet.BroadcastMorteVFX.
public class KillPopVFX : MonoBehaviour
{
    // ── Tuning (dialar "explosivo vs limpo") ──
    const int   NUM_CACOS   = 5;
    const float DUR_TOTAL   = 0.42f;
    const float DUR_SQUASH  = 0.12f;
    const float DUR_ANEL    = 0.25f;
    const float DUR_FLASH   = 0.08f;
    const float RAIO_ANEL   = 0.9f;
    const float MULT_ESCALA = 1.0f;
    const float VEL_CACO    = 3.5f;
    const int   SORT_BASE   = 50;

    public static void Tocar(Vector3 pos, Color cor, float escala)
    {
        var root = new GameObject("KillPop");
        root.transform.position = pos;
        root.AddComponent<KillPopVFX>().Iniciar(cor, Mathf.Max(0.1f, escala) * MULT_ESCALA);
    }

    void Iniciar(Color cor, float escala)
    {
        StartCoroutine(Squash(cor, escala));
        StartCoroutine(Flash(escala));
        StartCoroutine(Anel(cor, escala));
        for (int i = 0; i < NUM_CACOS; i++) StartCoroutine(Caco(cor, escala, i));
        Destroy(gameObject, DUR_TOTAL + 0.1f);
    }

    // 1. Squash-pop: blob na cor, esmaga (largo+baixo) → estoura + fade
    IEnumerator Squash(Color cor, float escala)
    {
        var go = NovoDisco(cor, SORT_BASE + 1);
        float t = 0f;
        while (t < DUR_SQUASH)
        {
            t += Time.deltaTime;
            float p = t / DUR_SQUASH;
            float sx = Mathf.Lerp(1.4f, 1.6f, p) * escala;
            float sy = Mathf.Lerp(0.5f, 1.6f, p) * escala;
            go.transform.localScale = new Vector3(sx, sy, 1f);
            SetAlpha(go, Mathf.Lerp(0.95f, 0f, p));
            yield return null;
        }
        Destroy(go);
    }

    // 4. Flash branco no 1º frame
    IEnumerator Flash(float escala)
    {
        var go = NovoDisco(Color.white, SORT_BASE + 3);
        go.transform.localScale = Vector3.one * escala * 1.1f;
        float t = 0f;
        while (t < DUR_FLASH)
        {
            t += Time.deltaTime;
            SetAlpha(go, Mathf.Lerp(0.9f, 0f, t / DUR_FLASH));
            yield return null;
        }
        Destroy(go);
    }

    // 3. Anel expandindo
    IEnumerator Anel(Color cor, float escala)
    {
        var go = new GameObject("Anel");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        const int SEG = 28;
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = SEG;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = SORT_BASE + 2;
        float t = 0f;
        while (t < DUR_ANEL)
        {
            t += Time.deltaTime;
            float p = t / DUR_ANEL;
            float raio = Mathf.Lerp(0.1f, RAIO_ANEL * escala, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.12f * escala, 0.01f, p);
            Color c = cor; c.a = Mathf.Lerp(0.8f, 0f, p);
            lr.startColor = lr.endColor = c;
            for (int i = 0; i < SEG; i++)
            {
                float a = (360f / SEG) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * raio, Mathf.Sin(a) * raio, 0f));
            }
            yield return null;
        }
        Destroy(go);
    }

    // 2. Estilhaço: caco voando pra fora + caindo + sumindo
    IEnumerator Caco(Color cor, float escala, int idx)
    {
        var go = NovoDisco(cor, SORT_BASE + 2);
        float ang = (360f / NUM_CACOS) * idx + Random.Range(-25f, 25f);
        Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
        Vector2 vel = dir * VEL_CACO * Random.Range(0.7f, 1.3f) * escala;
        float tam = Random.Range(0.1f, 0.18f) * escala;
        go.transform.localScale = Vector3.one * tam;
        float dur = DUR_TOTAL * Random.Range(0.7f, 1f);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            vel.y -= 6f * Time.deltaTime; // gravidade leve
            vel *= 0.92f;                 // arrasto
            go.transform.localPosition += (Vector3)(vel * Time.deltaTime);
            go.transform.localScale = Vector3.one * Mathf.Lerp(tam, tam * 0.3f, p);
            SetAlpha(go, Mathf.Lerp(0.9f, 0f, p * p));
            yield return null;
        }
        Destroy(go);
    }

    // ── helpers ──
    GameObject NovoDisco(Color cor, int sort)
    {
        var go = new GameObject("d");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Disco();
        sr.color = cor;
        sr.sortingOrder = sort;
        return go;
    }
    static void SetAlpha(GameObject go, float a)
    {
        if (go == null) return;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) { Color c = sr.color; c.a = a; sr.color = c; }
    }
    static Sprite s_disco;
    static Sprite Disco()
    {
        if (s_disco != null) return s_disco;
        const int sz = 32;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
            float a = Mathf.Clamp01(1f - d); a *= a;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        s_disco = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        return s_disco;
    }
}
```

- [ ] **Step 2: Compilar e confirmar 0 erros**

`refresh_unity(compile=request, scope=scripts, wait_for_ready=true)` então `read_console(types=["error"], filter_text="CS")`.
Esperado: 0 entradas.

- [ ] **Step 3: Smoke test em play mode (sem rede)**

`execute_code` entrando em Play e spawnando o efeito perto da origem da câmera:
```csharp
if (!UnityEditor.EditorApplication.isPlaying) UnityEditor.EditorApplication.isPlaying = true;
KillPopVFX.Tocar(Vector3.zero, new Color(0.6f, 1f, 0.4f, 1f), 5f);
```
Depois `read_console(types=["error"])`.
Esperado: 0 erros de runtime (NRE etc.). O objeto "KillPop" some sozinho em ~0.5s (sem leak).

- [ ] **Step 4: Commit**

```bash
git add horda/Assets/scripts/KillPopVFX.cs
git commit -m "feat(juice): KillPopVFX — efeito de morte de inimigo (squash/estilhaços/anel/flash)"
```

---

### Task 2: Replicação co-op — `EnemyNet.BroadcastMorteVFX`

**Files:**
- Modify: `horda/Assets/scripts/net/EnemyNet.cs`

- [ ] **Step 1: Adicionar broadcast + RPC**

Logo após o método `MostrarNumeroDanoClientRpc` (e antes do fim da classe `EnemyNet`), inserir:

```csharp
    // Co-op: o host replica o pop de morte (kill juice) nos clientes — o Morrer roda só no host.
    public void BroadcastMorteVFX(Vector2 pos, Color cor, float escala)
    {
        if (IsServer && IsSpawned) MorteVFXClientRpc(pos, cor.r, cor.g, cor.b, escala);
    }

    [Rpc(SendTo.NotServer)]
    void MorteVFXClientRpc(Vector2 pos, float r, float g, float b, float escala)
    {
        KillPopVFX.Tocar(pos, new Color(r, g, b), escala);
    }
```

- [ ] **Step 2: Compilar e confirmar 0 erros**

`refresh_unity(compile=request, scope=scripts, wait_for_ready=true)` então `read_console(types=["error"], filter_text="CS")`.
Esperado: 0 entradas.

- [ ] **Step 3: Commit**

```bash
git add horda/Assets/scripts/net/EnemyNet.cs
git commit -m "feat(juice): EnemyNet.BroadcastMorteVFX replica o pop de morte no cliente"
```

---

### Task 3: Hook no `Morrer()` (só inimigo normal)

**Files:**
- Modify: `horda/Assets/scripts/controlei_inimigo.cs` (no método `Morrer`, ramo final `else`)

- [ ] **Step 1: Trocar o ramo não-boss pra tocar + broadcast + despawn**

Localizar em `Morrer()` a linha:
```csharp
            else                      NetSpawn.Despawnar(gameObject); // host-autoritativo em rede
```
e substituir por:
```csharp
            else
            {
                // Kill juice: pop satisfatório na morte (SP toca local; co-op host toca + replica no P2).
                Color corMorte = spriteRenderer != null ? spriteRenderer.color : Color.white;
                float escMorte = Mathf.Abs(transform.localScale.x);
                KillPopVFX.Tocar(transform.position, corMorte, escMorte);
                GetComponent<EnemyNet>()?.BroadcastMorteVFX(transform.position, corMorte, escMorte);
                NetSpawn.Despawnar(gameObject); // host-autoritativo em rede
            }
```

- [ ] **Step 2: Compilar e confirmar 0 erros**

`refresh_unity(compile=request, scope=scripts, wait_for_ready=true)` então `read_console(types=["error"], filter_text="CS")`.
Esperado: 0 entradas.

- [ ] **Step 3: Smoke test SP (matar um inimigo)**

`execute_code` em Play: pega o primeiro `InimigoController` da cena, zera a vida e chama `ReceberDano` pra forçar a morte; confirma 0 erros:
```csharp
var ic = Object.FindFirstObjectByType<InimigoController>();
if (ic != null) ic.ReceberDano(99999f, false);
```
Depois `read_console(types=["error"])`.
Esperado: 0 erros; o inimigo some e o "KillPop" aparece+some (verificação visual fina = usuário).

- [ ] **Step 4: Commit**

```bash
git add horda/Assets/scripts/controlei_inimigo.cs
git commit -m "feat(juice): inimigo normal toca KillPopVFX na morte (local + broadcast co-op)"
```

---

## Verificação final (após as 3 tasks)
- Compila 0 erros.
- **SP:** matar inimigos → cada um deixa um pop curto; bosses mantêm o efeito de morte próprio; sem leak (objetos "KillPop" somem).
- **Co-op (MPPM, usuário):** matar no host → pop nos DOIS; matar via P2 (dano roteado) → pop nos dois; sem pop em despawn não-morte; horda densa morrendo sem queda perceptível de FPS.
- Tunar `NUM_CACOS`/`RAIO_ANEL`/`MULT_ESCALA`/durações se o usuário quiser "mais explosivo vs mais limpo".

## Self-review (feita)
- **Cobertura do spec:** 4 camadas (Task 1) ✓; replicação co-op (Task 2) ✓; hook não-boss + cor/escala (Task 3) ✓; bosses intocados ✓; perf sem física/particle ✓.
- **Placeholders:** nenhum — código completo em cada step.
- **Consistência de tipos:** `KillPopVFX.Tocar(Vector3, Color, float)` usado igual no smoke test, no `MorteVFXClientRpc` e no `Morrer`. `BroadcastMorteVFX(Vector2, Color, float)` ↔ `MorteVFXClientRpc(Vector2, float,float,float, float)` batem.
