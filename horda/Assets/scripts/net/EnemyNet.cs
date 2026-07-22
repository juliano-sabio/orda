using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Em inimigos/bosses. No CLIENTE, o inimigo é um fantoche movido pelo
// NetworkTransform (server authority): Rigidbody2D Kinematic e scripts de
// gameplay desligados (a IA roda só no host). No HOST, não mexe em nada.
public class EnemyNet : NetworkBehaviour
{
    // Escala do inimigo sincronizada do host. O NetworkTransform dos inimigos não sincroniza
    // scale, e a escala é setada no Awake (controlei_inimigo: dadosInimigo.tamanho) — que no
    // cliente pode divergir (ex.: SlimeColorida tem dadosInimigo==null → fica no default do
    // prefab). Sem isto o fantoche aparecia com "tamanho errado".
    readonly NetworkVariable<float> escalaNet = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // Host: a escala é publicada no Update (não aqui). InimigoController.InicializarComData
        // seta a escala no Start, que roda DEPOIS do OnNetworkSpawn → capturar aqui pegava a
        // escala default do prefab. No Update (Abs) o NGO só sincroniza quando o valor muda.
        if (IsServer) return;

        // cliente: aplica a escala REAL do host (corrige tamanho do fantoche)
        escalaNet.OnValueChanged += AoMudarEscala;
        AplicarEscala(escalaNet.Value);

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

        // Desliga MonoBehaviours de gameplay no cliente. Visual (SpriteRenderer,
        // Animator) não são MonoBehaviour; NetworkBehaviours são preservados.
        foreach (var c in GetComponents<MonoBehaviour>())
        {
            if (c == this) continue;
            if (c is NetworkBehaviour) continue;
            c.enabled = false;
        }

        // Co-op: inimigos que criam efeitos visuais por script (luzes/partículas) ficariam
        // "pelados" no cliente (o script foi desligado acima). Deixa cada um montar só o visual.
        var cosm = GetComponent<IEnemyCosmetic>();
        if (cosm != null) cosm.SetupVisualCosmetico();
    }

    void Update()
    {
        // Host publica a escala REAL (Abs = magnitude; movi_inimigo inverte só o sinal p/ facing).
        // O NGO só envia quando o valor muda → barato; cobre a escala setada no Start do
        // InimigoController (que roda depois do OnNetworkSpawn) e qualquer mudança em runtime.
        if (IsServer) escalaNet.Value = Mathf.Abs(transform.localScale.x);
    }

    void AoMudarEscala(float _, float v) => AplicarEscala(v);
    void AplicarEscala(float s)
    {
        if (s > 0f) transform.localScale = new Vector3(s, s, transform.localScale.z);
    }

    // Qualquer cliente pode requisitar dano a qualquer inimigo (co-op de amigos).
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReceberDanoServerRpc(float dano, bool isCrit, bool mostrarNumero = true)
    {
        var ic = GetComponent<InimigoController>();
        if (ic != null) ic.ReceberDano(dano, isCrit, mostrarNumero); // roda no host -> aplica
    }

    // Co-op: o CLIENTE dono detectou que seu player está encostando neste inimigo (hitbox
    // preciso, do ponto de vista dele) e pede o dano de contato. O host aplica o dano REAL
    // (escalado) do inimigo no player — autoritativo. Resolve o "toma dano sem encostar".
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ContatoDanoServerRpc(ulong playerObjId)
    {
        if (!IsServer) return;
        var dano = GetComponent<DanoInimigo>();
        if (dano == null) return;
        var sm = NetworkManager != null ? NetworkManager.SpawnManager : null;
        if (sm != null && sm.SpawnedObjects.TryGetValue(playerObjId, out var no) && no != null)
        {
            var ps = no.GetComponent<PlayerStats>();
            if (ps != null) ps.TakeDamage(dano.dano);
        }
    }

    // Co-op: o host mostra o número de dano (pós-mitigação) também nos clientes. Sem isto,
    // o cliente que bate via ServerRpc nunca vê o próprio número (o controller está
    // desligado no cliente, e o dano é processado só no host).
    public void ReplicarNumeroDano(float dano, bool isCrit)
    {
        if (IsServer && IsSpawned) MostrarNumeroDanoClientRpc(dano, isCrit);
    }

    [ClientRpc]
    void MostrarNumeroDanoClientRpc(float dano, bool isCrit)
    {
        if (IsServer) return; // o host já mostrou localmente
        if (DamageNumberManager.Instance != null)
            DamageNumberManager.Instance.ShowDamage(transform, dano, isCrit);
        FlashAcertoCliente(); // hit feedback: pisca o fantoche igual ao host (P2 vê o inimigo reagir)
        if (isCrit) Impacto.Critico(); // micro shake do crit também na tela do cliente (hit-stop se auto-desliga em rede)
    }

    // Co-op: o flash de acerto (EfeitoVisualDano = vermelho ~0.1s) roda no host, dentro do
    // ReceberDano → o cliente não via os inimigos piscarem. Aqui o EnemyNet (preservado, ao
    // contrário do InimigoController que é desligado no cliente) pisca o fantoche pelo MESMO
    // RPC do número de dano — sem tráfego novo.
    SpriteRenderer _srHit;
    Color _corBaseHit;
    Coroutine _hitCo;

    void FlashAcertoCliente()
    {
        if (_srHit == null) _srHit = GetComponent<SpriteRenderer>();
        if (_srHit == null) return;
        if (_hitCo == null) _corBaseHit = _srHit.color; // captura a base só quando não está piscando
        else StopCoroutine(_hitCo);
        _hitCo = StartCoroutine(FlashAcertoRotina());
    }

    IEnumerator FlashAcertoRotina()
    {
        float a = _srHit.color.a;
        _srHit.color = new Color(1f, 0f, 0f, a); // vermelho, igual ao host
        yield return new WaitForSeconds(0.1f);
        if (_srHit != null) { Color c = _corBaseHit; c.a = _srHit.color.a; _srHit.color = c; }
        _hitCo = null;
    }

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

    // ── Co-op: VFX cosmético genérico de habilidade de mob ──────────────────────
    // A IA/gameplay das slimes roda só no host; os VFX (procedurais, criados em runtime) não
    // replicavam → o P2 não via nada. O host dispara o efeito REAL localmente E chama isto;
    // os clientes recriam SÓ o visual (sem collider/dano — o gameplay é host-autoritativo).
    public void BroadcastCosmetico(byte tipo, Vector3 pos, float p1 = 0f, float p2 = 0f, float p3 = 0f, float p4 = 0f, float p5 = 0f)
    {
        if (IsServer && IsSpawned) CosmeticoClientRpc(tipo, pos, p1, p2, p3, p4, p5);
    }

    [Rpc(SendTo.NotServer)]
    void CosmeticoClientRpc(byte tipo, Vector3 pos, float p1, float p2, float p3, float p4, float p5)
    {
        MobCosmeticos.Criar(tipo, pos, p1, p2, p3, p4, p5, this);
    }
}

// Inimigos que criam efeitos visuais por script (luzes, partículas, glows) implementam isto.
// No cliente co-op o EnemyNet desliga o gameplay e chama este método pra montar SÓ os visuais.
public interface IEnemyCosmetic { void SetupVisualCosmetico(); }

// Fábrica de VFX cosméticos de habilidades de mob (co-op). Cada 'tipo' recria no CLIENTE o mesmo
// visual procedural que o host cria — sem dano (o gameplay é host-autoritativo). Extensível:
// adicione um const + um case aqui e chame EnemyNet.BroadcastCosmetico(tipo, ...) no host.
public static class MobCosmeticos
{
    public const byte FumacaVeneno = 1;
    public const byte OndaCura     = 2;
    public const byte BolaFogoMaga = 3;
    public const byte SplatGuarda  = 4;
    public const byte OndaCor        = 5;
    public const byte CacosGelo      = 6;
    public const byte ProjetilAntiUlti = 7;
    public const byte ZonaGeloAoe    = 8;
    public const byte VorticeAoe     = 9;
    public const byte MeteoroAoe     = 10;
    // Cargas/windups (o telegraph roda no fantoche, seguindo o NetworkTransform)
    public const byte CargaElemental  = 11;
    public const byte CargaMaga       = 12;
    public const byte CargaGuarda     = 13;
    public const byte CargaProtetora  = 14;
    // Protetora: ondas de cast com som embutido
    public const byte OndaEscudo      = 15;
    public const byte OndaBuff        = 16;
    // Projétil de lentidão (slime_de_projetil): som + vinhas no impacto
    public const byte ImpactoLentidao = 17;
    // Fantasmas elementais
    public const byte FogoCarga         = 18;
    public const byte FogoProjetil      = 19;
    public const byte FogoExplosaoMorte = 20;
    public const byte FogoFlashDash     = 21;
    public const byte EletricoRaio      = 22;
    public const byte EletricoDescarga  = 23;
    public const byte EletricoBurst     = 24;
    public const byte GeloProjetil      = 25;
    public const byte GeloZona          = 26;
    public const byte GeloCristaisMorte = 27;
    public const byte VenenoNuvem       = 28;
    public const byte VenenoProjetil    = 29;

    // 'origem' = EnemyNet do mob que castou (no CLIENTE). Permite reusar as corrotinas visuais
    // do próprio script do mob (desligado, mas o GameObject está ativo → coroutine roda) em vez
    // de duplicar VFX. Efeitos de morte rodam no FxRunner (sobrevivem ao despawn do mob).
    public static void Criar(byte tipo, Vector3 pos, float p1, float p2, float p3, float p4, float p5, EnemyNet origem = null)
    {
        switch (tipo)
        {
            case FumacaVeneno: // p1 = raio, p2 = duração
            {
                var go = new GameObject("FumacaVenenosa(coop)");
                go.transform.position = pos;
                go.AddComponent<FumacaVenenosaCloud>().InicializarCosmetico(p1, p2);
                break;
            }
            case OndaCura: // p1 = raio, p2 = duração
            {
                var go = new GameObject("OndaCura(coop)");
                go.transform.position = pos;
                go.AddComponent<OndaCuraVisual>().Iniciar(p1, p2);
                SomSkill.Tocar(SomSkill.Tipo.SlimeCurativaCura, pos, 0.5f); // som também pro P2
                break;
            }
            case BolaFogoMaga: // p1,p2 = dir(x,y); p3 = vel; p4 = raioExplosão; p5 = duraçãoFogo
            {
                SomSkill.Tocar(SomSkill.Tipo.SlimeMagaDisparo, pos, 0.5f); // som do disparo pro P2
                var go = new GameObject("BolaDeFogoMaga(coop)");
                go.transform.position = pos;
                go.AddComponent<BolaDeFogoInimigo>().InicializarCosmetico(
                    new Vector2(p1, p2), p3, p4, p5);
                break;
            }
            case SplatGuarda: // splat verde de morte
            {
                SlimeGuarda.CriarSplat(pos);
                break;
            }
            case OndaCor: // p1=raio, p2,p3,p4=cor(rgb), p5=duração — onda de cast (protetora)
            {
                var go = new GameObject("OndaCor(coop)");
                go.transform.position = pos;
                go.AddComponent<OndaCorVisual>().Iniciar(p1, new Color(p2, p3, p4), p5);
                break;
            }
            case CacosGelo: // cacos de gelo da morte da elemental
            {
                SlimeElemental.CriarCacosGelo(pos);
                break;
            }
            case ProjetilAntiUlti: // p1,p2 = dir(x,y); p3 = vel — projétil roxo da protetora
            {
                SomSkill.Tocar(SomSkill.Tipo.SlimeProtProjetil, pos, 0.55f);
                // Versão completa (rastro + explosão perto do player local), sem gameplay.
                SlimeProtetoraInimiga.CriarProjetilVisual(pos, new Vector2(p1, p2), p3, cosmetico: true);
                break;
            }
            // Ataques da Elemental — p1 = raio, p2 = duração. Recria o VFX REAL (as corrotinas
            // visuais tocam o próprio som); fallback: anel marcador se o componente sumiu.
            case ZonaGeloAoe:
            {
                var el = origem != null ? origem.GetComponent<SlimeElemental>() : null;
                if (el != null) el.CosmeticoZonaGelo(pos);
                else { NovoMarcador(pos, p1, new Color(0.45f, 0.8f, 1f), p2); SomSkill.Tocar(SomSkill.Tipo.SlimeElemGelo, pos, 0.55f); }
                break;
            }
            case VorticeAoe:
            {
                var el = origem != null ? origem.GetComponent<SlimeElemental>() : null;
                if (el != null) el.CosmeticoVortice(pos);
                else { NovoMarcador(pos, p1, new Color(0.5f, 1f, 0.6f), p2); SomSkill.Tocar(SomSkill.Tipo.SlimeElemVento, pos, 0.55f); }
                break;
            }
            case MeteoroAoe:
            {
                var el = origem != null ? origem.GetComponent<SlimeElemental>() : null;
                if (el != null) el.CosmeticoMeteoro(pos);
                else NovoMarcador(pos, p1, new Color(1f, 0.4f, 0.05f), p2);
                break;
            }

            // ── Cargas/windups ──────────────────────────────────────────────
            case CargaElemental: // p1..p3 = cor, p4 = duração
                origem?.GetComponent<SlimeElemental>()?.CosmeticoCarga(new Color(p1, p2, p3), p4);
                break;
            case CargaMaga: // p1 = duração
                origem?.GetComponent<SlimeMagaFireAttack>()?.CosmeticoCarga(p1);
                break;
            case CargaGuarda: // p1 = duração
                origem?.GetComponent<SlimeGuarda>()?.CosmeticoTelegrafo(p1);
                break;
            case CargaProtetora: // p1..p3 = cor, p4 = duração
                origem?.GetComponent<SlimeProtetoraInimiga>()?.CosmeticoCarga(new Color(p1, p2, p3), p4);
                break;

            // ── Protetora: ondas de cast (visual + som) ─────────────────────
            case OndaEscudo: // p1 = raio, p2 = duração
            {
                SomSkill.Tocar(SomSkill.Tipo.SlimeProtEscudo, pos, 0.55f);
                var go = new GameObject("OndaEscudo(coop)");
                go.transform.position = pos;
                go.AddComponent<OndaCorVisual>().Iniciar(p1, new Color(0.6f, 0.2f, 1f), p2);
                break;
            }
            case OndaBuff: // p1 = raio, p2 = duração
            {
                SomSkill.Tocar(SomSkill.Tipo.SlimeProtBuff, pos, 0.55f);
                var go = new GameObject("OndaBuff(coop)");
                go.transform.position = pos;
                go.AddComponent<OndaCorVisual>().Iniciar(p1, new Color(1f, 0.85f, 0.1f), p2);
                break;
            }

            case ImpactoLentidao: // som de impacto + vinhas no ponto do hit
                origem?.GetComponent<projetil_inimigo>()?.ImpactoCosmetico(pos);
                break;

            // ── Fantasma de Fogo ────────────────────────────────────────────
            case FogoCarga: // p1 = duração
                SomSkill.Tocar(SomSkill.Tipo.FantasmaFogo, pos, 0.5f);
                origem?.GetComponent<FantasmaFogo>()?.CosmeticoCarga(p1);
                break;
            case FogoProjetil: // p1,p2 = dir; p3 = 1 → toca o som da rajada
            {
                if (p3 > 0.5f) SomSkill.Tocar(SomSkill.Tipo.FantasmaFogo, pos, 0.45f);
                var ff = origem != null ? origem.GetComponent<FantasmaFogo>() : null;
                if (ff != null) FxRunner.Instance.StartCoroutine(ff.ProjetilFantasma(pos, new Vector2(p1, p2), comDano: false));
                break;
            }
            case FogoExplosaoMorte: // p1 = raio
            {
                var ff = origem != null ? origem.GetComponent<FantasmaFogo>() : null;
                if (ff != null) FxRunner.Instance.StartCoroutine(ff.ExplosaoMorte(pos, comDano: false));
                break;
            }
            case FogoFlashDash: // p1 = raio do flash
            {
                var ff = origem != null ? origem.GetComponent<FantasmaFogo>() : null;
                if (ff != null) FxRunner.Instance.StartCoroutine(ff.FlashExplosao(pos, p1));
                break;
            }

            // ── Fantasma Elétrico ───────────────────────────────────────────
            case EletricoRaio: // p1,p2 = destino (telegraph + raio, sem dano)
            {
                SomSkill.Tocar(SomSkill.Tipo.FantasmaEletrico, pos, 0.5f);
                var fe = origem != null ? origem.GetComponent<FantasmaEletrico>() : null;
                if (fe != null) fe.StartCoroutine(fe.RaioSequencia(new Vector2(p1, p2), comDano: false));
                break;
            }
            case EletricoDescarga: // p1 = raio
                origem?.GetComponent<FantasmaEletrico>()?.DescargaVisual(pos, p1);
                break;
            case EletricoBurst:
            {
                var fe = origem != null ? origem.GetComponent<FantasmaEletrico>() : null;
                if (fe != null) fe.StartCoroutine(fe.BurstEletrico(pos));
                break;
            }

            // ── Fantasma de Gelo ────────────────────────────────────────────
            case GeloProjetil: // p1,p2 = dir
            {
                SomSkill.Tocar(SomSkill.Tipo.FantasmaGelo, pos, 0.45f);
                var fg = origem != null ? origem.GetComponent<FantasmaGelo>() : null;
                if (fg != null) FxRunner.Instance.StartCoroutine(fg.ProjetilGelo(pos, new Vector2(p1, p2), comDano: false));
                break;
            }
            case GeloZona: // p1 = raio, p2 = duração
            {
                var fg = origem != null ? origem.GetComponent<FantasmaGelo>() : null;
                if (fg != null) FxRunner.Instance.StartCoroutine(fg.ZonaGelo(pos, p1, p2, comDano: false));
                break;
            }
            case GeloCristaisMorte:
            {
                var fg = origem != null ? origem.GetComponent<FantasmaGelo>() : null;
                if (fg != null) FxRunner.Instance.StartCoroutine(fg.ExplodirCristais(pos));
                break;
            }

            // ── Fantasma de Veneno (comum e atirador) ───────────────────────
            case VenenoNuvem: // p1 = raio, p2 = duração
            {
                var fv = origem != null ? origem.GetComponent<FantasmaVeneno>() : null;
                if (fv != null) { FxRunner.Instance.StartCoroutine(fv.NuvemDeVeneno(pos, p1, p2, 0f, comDano: false)); break; }
                var fva = origem != null ? origem.GetComponent<FantasmaVenenoAtirador>() : null;
                if (fva != null) FxRunner.Instance.StartCoroutine(fva.NuvemDeVeneno(pos, p1, p2, 0f, comDano: false));
                break;
            }
            case VenenoProjetil: // p1,p2 = dir; p3 = 1 → toca o som do disparo
            {
                if (p3 > 0.5f) SomSkill.Tocar(SomSkill.Tipo.FantasmaVeneno, pos, 0.45f);
                var fv = origem != null ? origem.GetComponent<FantasmaVeneno>() : null;
                if (fv != null) { FxRunner.Instance.StartCoroutine(fv.ProjetilFantasma(pos, new Vector2(p1, p2), comDano: false)); break; }
                var fva = origem != null ? origem.GetComponent<FantasmaVenenoAtirador>() : null;
                if (fva != null) FxRunner.Instance.StartCoroutine(fva.ProjetilVeneno(pos, new Vector2(p1, p2), comDano: false));
                break;
            }
        }
    }

    static void NovoMarcador(Vector3 pos, float raio, Color cor, float dur)
    {
        var go = new GameObject("ZonaAoe(coop)");
        go.transform.position = pos;
        go.AddComponent<ZonaMarcadorCosmetico>().Iniciar(raio, cor, dur);
    }
}

// Marcador de zona de AoE (anel colorido pulsante) — mostra pro P2 onde o ataque da Elemental
// vai bater/pegar. É simplificado (não recria o VFX inteiro), mas transmite o perigo.
public class ZonaMarcadorCosmetico : MonoBehaviour
{
    public void Iniciar(float raio, Color cor, float duracao)
    {
        Destroy(gameObject, duracao + 0.6f);
        StartCoroutine(Animar(raio, cor, duracao));
    }

    System.Collections.IEnumerator Animar(float raio, Color cor, float duracao)
    {
        const int SEGS = 48;
        var lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 11;
        lr.startWidth = lr.endWidth = 0.12f;
        Vector2 centro = transform.position;
        for (int i = 0; i < SEGS; i++)
        {
            float ang = i / (float)SEGS * Mathf.PI * 2f;
            lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
        }
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float pulso = Mathf.Sin(t * 6f) * 0.25f + 0.6f;
            float fade  = t > duracao - 0.4f ? (duracao - t) / 0.4f : 1f;
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, pulso * Mathf.Clamp01(fade));
            yield return null;
        }
        Destroy(gameObject);
    }
}

// Projétil cosmético genérico (co-op): disco que viaja em linha reta, só visual. Usado pra
// replicar projéteis de mob que no host têm gameplay (o dano/efeito é host-autoritativo).
public class MobProjetilCosmetico : MonoBehaviour
{
    public void Iniciar(Vector2 dir, float vel, float escala, Color cor, float vida)
    {
        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = FogoSprites.Disco; // disco branco gerado — tingido pela cor
        sr.color        = cor;
        sr.sortingOrder = 13;
        transform.localScale = Vector3.one * escala;

        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.gravityScale   = 0f;
        rb.linearVelocity = dir * vel;

        Destroy(gameObject, vida);
    }
}

// Onda circular colorida expansiva (cast de habilidade). Reutilizável pelo cosmético de co-op.
public class OndaCorVisual : MonoBehaviour
{
    public void Iniciar(float raioFinal, Color cor, float duracao)
    {
        Destroy(gameObject, duracao + 0.5f);
        StartCoroutine(Animar(raioFinal, cor, duracao));
    }

    System.Collections.IEnumerator Animar(float raioFinal, Color cor, float duracao)
    {
        const int SEGS = 48;
        var lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 10;
        Vector2 centro = transform.position;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float p    = t / duracao;
            float r    = Mathf.Lerp(0f, raioFinal, p);
            float a    = Mathf.Lerp(0.85f, 0f, p);
            float larg = Mathf.Lerp(0.35f, 0.05f, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, a);
            lr.startWidth = lr.endWidth = larg;
            for (int i = 0; i < SEGS; i++)
            {
                float ang = i / (float)SEGS * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
