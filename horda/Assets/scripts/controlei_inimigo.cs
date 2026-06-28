using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Se InimigoData estiver em um namespace diferente, adicione:
// using Survivor; // ou o namespace onde InimigoData está

public class InimigoController : MonoBehaviour
{
    public static event System.Action OnInimigoDerrotado;

    [Header("Dados do Inimigo")]
    public InimigoData dadosInimigo;

    [Header("Status Atuais")]
    public float vidaAtual;
    public float danoAtual;
    public float vidaMaxima;

    [Header("Sistema de Drop de XP")]
    public GameObject xpOrbPrefab;
    public int minOrbs = 1;
    public int maxOrbs = 3;
    public float xpPorOrbe = 5f;
    public float forcaDrop = 2f;

    [Header("Drops Adicionais")]
    public List<DropEntry> drops = new List<DropEntry>();

    [Header("Dano Flutuante")]
    public float alturaDanoFlutuante = 2f;
    public bool mostrarDanoAposMorte = true;
    public bool mostrarDanoFlutuante = true;
    public bool imuneAoDano = false;

    [Header("Sistema de Cura")]
    public bool podeReceberCura = true;
    public float multiplicadorCuraRecebida = 1f;
    public bool mostrarCuraFlutuante = true;
    public Color corCura = Color.green;
    private bool temInimigoSuporte = false;
    private InimigoSuporte suporteComponent;

    [Header("Efeitos de Status")]
    public bool temBuffDefesa = false;
    public float bonusDefesa = 0f;
    public float tempoRestanteBuff = 0f;
    private float danoOriginal;

    [Header("Configurações de Movimento")]
    public float velocidadeBase = 3f;
    public float velocidadeAtual;
    public bool estaAtordoado = false;
    public float tempoAtordoado = 0f;

    // ✅ Variável pública para acesso externo
    public bool estaMorrendo = false;

    private SpriteRenderer spriteRenderer;
    private Color corOriginal;
    private Coroutine _flashDanoCoroutine;
    private Rigidbody2D rb;
    private DanoInimigo danoInimigoComponent;
    private float proximoContato = 0f;
    private const float INTERVALO_CONTATO = 1f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        danoInimigoComponent = GetComponent<DanoInimigo>();

        if (spriteRenderer != null)
            corOriginal = spriteRenderer.color;

        InicializarComData();
    }

    public void InicializarComData()
    {
        if (dadosInimigo == null)
        {
            // Boss customizado: vida já definida externamente via Awake
            if (vidaMaxima <= 0f) vidaMaxima = 100f;
            // Escala de boss por tempo (sobre a vida-base já setada no spawn)
            vidaMaxima *= EnemyScaling.BossVidaMult();
            vidaAtual  = vidaMaxima;
            return;
        }

        // Escala de stats por tempo de jogo (linear, sem teto). Aplicada no spawn:
        // inimigos mais novos nascem mais fortes. Bosses (sem dadosInimigo) não passam por aqui.
        float multVida = EnemyScaling.VidaMult();
        float multDano = EnemyScaling.DanoMult();

        vidaMaxima = dadosInimigo.vidaBase * multVida;
        vidaAtual = vidaMaxima;
        danoAtual = dadosInimigo.danoBase * multDano;
        danoOriginal = danoAtual;
        velocidadeBase = dadosInimigo.velocidadeBase;
        velocidadeAtual = velocidadeBase;

        DanoInimigo danoComponent = GetComponent<DanoInimigo>();
        if (danoComponent != null)
        {
            danoComponent.dano = danoAtual;
            danoComponent.intervaloAtaque = dadosInimigo.intervaloAtaque;
        }

        if (spriteRenderer != null && dadosInimigo.icon != null)
        {
            spriteRenderer.sprite = dadosInimigo.icon;
        }

        transform.localScale = Vector3.one * dadosInimigo.tamanho;
        gameObject.name = dadosInimigo.nomeInimigo;

        suporteComponent = GetComponent<InimigoSuporte>();
        temInimigoSuporte = (suporteComponent != null);

    }

    // Este controller pertence a um boss? (usado pelo TimerManager pra pausar o countdown)
    public bool EhBoss() => GetComponent<IBoss>() != null
                         || GetComponentInParent<IBoss>() != null;

    public void ReceberDano(float dano, bool isCrit = false, bool mostrarNumero = true)
    {
        // co-op: numa cópia cliente (fantoche), pede pro host aplicar e sai.
        var _en = GetComponent<EnemyNet>();
        if (_en != null && _en.IsSpawned && !_en.IsServer)
        {
            // mostrarNumero propagado: senão dano "silencioso" (ex.: tick do vampirismo,
            // ~0.3/frame) virava "0" replicado a cada frame em vários inimigos = chuva de 0.
            _en.ReceberDanoServerRpc(dano, isCrit, mostrarNumero);
            return;
        }

        if (estaMorrendo || imuneAoDano) return;

        if (temBuffDefesa && bonusDefesa > 0)
        {
            float reducao = dano * bonusDefesa;
            dano -= reducao;
        }

        Escudo escudo = GetComponent<Escudo>();
        if (escudo != null && escudo.Ativo)
        {
            float danoAoEscudo = Mathf.Min(dano, escudo.vidaAtual);
            dano = escudo.AbsorverDano(dano);
            if (mostrarNumero && mostrarDanoFlutuante) CriarTextoFlutuante(danoAoEscudo, new Color(0.7f, 0.4f, 1f), "", 28);
            if (dano <= 0) return;
        }

        vidaAtual -= dano;

        if (mostrarNumero && mostrarDanoFlutuante)
        {
            MostrarDanoFlutuante(dano, isCrit);
            ReplicarNumeroDanoCoop(dano, isCrit);   // co-op: mostra o número nos clientes também
        }
        if (_flashDanoCoroutine != null) StopCoroutine(_flashDanoCoroutine);
        _flashDanoCoroutine = StartCoroutine(EfeitoVisualDano());

        if (vidaAtual <= 0)
        {
            vidaAtual = 0;
            estaMorrendo = true;

            if (mostrarNumero) { MostrarDanoFatal(dano, isCrit); ReplicarNumeroDanoCoop(dano, isCrit); }
            Morrer();
        }
    }

    // Co-op: replica o número de dano (pós-mitigação) pros clientes via EnemyNet.
    void ReplicarNumeroDanoCoop(float dano, bool isCrit)
    {
        var en = GetComponent<EnemyNet>();
        if (en != null) en.ReplicarNumeroDano(dano, isCrit);
    }

    public void ReceberCura(float quantidadeCura, bool mostrarEfeito = true)
    {
        if (estaMorrendo || !podeReceberCura || vidaAtual >= vidaMaxima) return;

        float curaFinal = quantidadeCura * multiplicadorCuraRecebida;
        float vidaAntes = vidaAtual;

        vidaAtual = Mathf.Min(vidaAtual + curaFinal, vidaMaxima);
        float curaReal = vidaAtual - vidaAntes;


        if (mostrarEfeito && mostrarCuraFlutuante)
        {
            MostrarCuraFlutuante(curaReal);
            StartCoroutine(EfeitoVisualCura());
        }
    }

    public void AplicarBuffDefesa(float bonus, float duracao)
    {
        if (estaMorrendo) return;

        bonusDefesa = bonus;
        tempoRestanteBuff = duracao;

        if (!temBuffDefesa)
        {
            temBuffDefesa = true;
            danoOriginal = danoAtual;
            danoAtual = danoOriginal * (1f - bonusDefesa);

            DanoInimigo danoComponent = GetComponent<DanoInimigo>();
            if (danoComponent != null)
            {
                danoComponent.SetDano(danoAtual);
            }
        }


        StopCoroutine("GerenciarBuffDefesa");
        StartCoroutine(GerenciarBuffDefesa(duracao));

        StartCoroutine(EfeitoVisualBuff());
    }

    // 🆕 NOVO MÉTODO: Aplicar Slow (redução de velocidade)
    public void AplicarSlow(float reducaoVelocidade, float duracao)
    {
        if (estaMorrendo || reducaoVelocidade <= 0) return;


        // Calcula a redução
        float reducaoAplicada = velocidadeAtual * reducaoVelocidade;
        velocidadeAtual -= reducaoAplicada;

        // Garante que não fique negativo
        velocidadeAtual = Mathf.Max(0.5f, velocidadeAtual);

        // Restaura após a duração
        StartCoroutine(RestaurarVelocidade(reducaoAplicada, duracao));

        // Efeito visual
        StartCoroutine(EfeitoVisualSlow());
    }

    private IEnumerator RestaurarVelocidade(float reducao, float duracao)
    {
        yield return new WaitForSeconds(duracao);
        velocidadeAtual += reducao;
    }

    private IEnumerator EfeitoVisualSlow()
    {
        if (spriteRenderer == null) yield break;

        Color corSlow = new Color(0.2f, 0.6f, 1f, 1f); // Azul para slow
        spriteRenderer.color = corSlow;
        yield return new WaitForSeconds(0.2f);

        if (!estaMorrendo)
        {
            spriteRenderer.color = corOriginal;
        }
    }

    // 🆕 NOVO MÉTODO: Aplicar Stun (atordoamento)
    public void AplicarStun(float duracao)
    {
        if (estaMorrendo) return;


        estaAtordoado = true;
        tempoAtordoado = duracao;

        // Pode adicionar lógica para parar movimento/ataques aqui
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        StartCoroutine(RemoverStun(duracao));
        StartCoroutine(EfeitoVisualStun());
    }

    private IEnumerator RemoverStun(float duracao)
    {
        yield return new WaitForSeconds(duracao);
        estaAtordoado = false;
        tempoAtordoado = 0f;
    }

    private IEnumerator EfeitoVisualStun()
    {
        if (spriteRenderer == null) yield break;

        while (estaAtordoado && tempoAtordoado > 0)
        {
            spriteRenderer.color = Color.yellow;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = corOriginal;
            yield return new WaitForSeconds(0.2f);
        }

        if (!estaMorrendo)
        {
            spriteRenderer.color = corOriginal;
        }
    }

    // 🆕 NOVO MÉTODO: Aplicar Veneno (dano contínuo)
    public void AplicarVeneno(float danoPorTick, float intervalo, int quantidadeTicks, Color corVeneno)
    {
        if (estaMorrendo) return;


        StartCoroutine(EfeitoVenenoCoroutine(danoPorTick, intervalo, quantidadeTicks, corVeneno));
    }

    private IEnumerator EfeitoVenenoCoroutine(float danoPorTick, float intervalo, int quantidadeTicks, Color corVeneno)
    {
        for (int i = 0; i < quantidadeTicks && !estaMorrendo; i++)
        {
            // Aplica dano
            ReceberDano(danoPorTick, false);

            // Efeito visual
            if (spriteRenderer != null)
            {
                spriteRenderer.color = corVeneno;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = corOriginal;
            }

            yield return new WaitForSeconds(intervalo);
        }
    }

    private IEnumerator GerenciarBuffDefesa(float duracao)
    {
        tempoRestanteBuff = duracao;

        while (tempoRestanteBuff > 0)
        {
            yield return null;
            tempoRestanteBuff -= Time.deltaTime;

            if (tempoRestanteBuff < 3f && tempoRestanteBuff > 0)
            {
                float pingPong = Mathf.PingPong(Time.time * 10f, 1f);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(corOriginal, new Color(0.5f, 0.5f, 1f, 1f), pingPong);
                }
            }
        }

        RemoverBuffDefesa();
    }

    public void RemoverBuff() => RemoverBuffDefesa();

    private void RemoverBuffDefesa()
    {
        temBuffDefesa = false;
        bonusDefesa = 0f;
        tempoRestanteBuff = 0f;
        danoAtual = danoOriginal;

        DanoInimigo danoComponent = GetComponent<DanoInimigo>();
        if (danoComponent != null)
        {
            danoComponent.SetDano(danoAtual);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = corOriginal;
        }

    }

    private void MostrarCuraFlutuante(float quantidade)
    {
        if (DamageNumberManager.Instance != null && DamageNumberManager.Instance is DamageNumberManager manager)
        {
            var method = manager.GetType().GetMethod("ShowHeal");
            if (method != null)
            {
                method.Invoke(manager, new object[] { this.transform, quantidade });
                return;
            }
        }

        // Fallback
        CriarTextoFlutuante(quantidade, corCura, "+", 24);
    }

    private void MostrarDanoFlutuante(float dano, bool isCrit)
    {
        // Normal = branco | Crit = vermelho
        Color cor = isCrit ? Color.red : Color.white;
        if (DamageNumberManager.Instance != null)
            DamageNumberManager.Instance.ShowDamage(this.transform, dano, isCrit);
        else
            CriarTextoFlutuante(dano, cor, "", isCrit ? 32 : 24);

        if (isCrit) Impacto.Critico(); // game-feel: hit-stop (SP) + micro shake no crítico
    }

    private void MostrarDanoFatal(float danoFinal, bool isCrit)
    {
        if (!mostrarDanoAposMorte) return;

        // Usa a mesma lógica — vermelho se crit, branco se normal
        Color cor = isCrit ? Color.red : Color.white;
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.ShowDamage(this.transform, danoFinal, isCrit);
        }
        else
        {
            CriarTextoFlutuante(danoFinal, cor, "", isCrit ? 40 : 28);
        }
    }

    private void CriarTextoFlutuante(float valor, Color cor, string prefixo = "", int fontSize = 24)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TempCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject textObj = new GameObject("TextoFlutuante");
        textObj.transform.SetParent(canvas.transform, false);

        if (Camera.main == null) { Destroy(textObj); return; }
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z <= 0f) { Destroy(textObj); return; }
        screenPos.z  = 0f;
        screenPos.y += 80;
        screenPos.x  = Mathf.Clamp(screenPos.x, 10f, Screen.width  - 10f);
        screenPos.y  = Mathf.Clamp(screenPos.y, 10f, Screen.height - 10f);
        textObj.transform.position = screenPos;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = prefixo + Mathf.RoundToInt(valor).ToString();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = cor;
        text.fontStyle = FontStyles.Bold;

        textObj.AddComponent<AnimacaoTextoFlutuante>().Initialize(screenPos);
        Destroy(textObj, 1.5f);
    }

    private IEnumerator EfeitoVisualDano()
    {
        if (spriteRenderer == null) yield break;

        // Preserva só o alpha atual (BossCaveira muda alpha em runtime),
        // mas sempre restaura para corOriginal — nunca para cor de outro flash em andamento.
        float alphaAtual = spriteRenderer.color.a;
        spriteRenderer.color = new Color(1f, 0f, 0f, alphaAtual);
        yield return new WaitForSeconds(0.1f);

        if (!estaMorrendo && spriteRenderer != null)
            spriteRenderer.color = new Color(corOriginal.r, corOriginal.g, corOriginal.b, spriteRenderer.color.a);
        _flashDanoCoroutine = null;
    }

    private IEnumerator EfeitoVisualCura()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = corCura;
        yield return new WaitForSeconds(0.2f);

        if (!estaMorrendo)
        {
            spriteRenderer.color = corOriginal;
        }
    }

    private IEnumerator EfeitoVisualBuff()
    {
        if (spriteRenderer == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = new Color(0.5f, 0.5f, 1f, 1f);
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = corOriginal;
            yield return new WaitForSeconds(0.1f);
        }
    }

    public static event System.Action<InimigoController> OnPreMorte;

    public void Morrer()
    {
        if (estaMorrendo && vidaAtual <= 0)
        {
            OnPreMorte?.Invoke(this);

            if (suporteComponent != null)
            {
                suporteComponent.AtivarCura(false);
            }

            DroparOrbesXP();
            DroparPowerup();
            OnInimigoDerrotado?.Invoke();

            BossController        boss    = GetComponent<BossController>();
            BossPrincesa          princesa = GetComponent<BossPrincesa>();
            BossGuarda            guarda  = GetComponent<BossGuarda>();
            BossSlimeGuardaElite  elite   = GetComponent<BossSlimeGuardaElite>();
            BossCaveira           caveira = GetComponent<BossCaveira>();

            // Boss drop: token(s) de elemento ALEATÓRIO. SP = 1; MP = 2 (um pra cada player pegar).
            // Só o host/SP dropa (NetSpawn host-autoritativo) → replica pros clientes.
            bool ehBossDrop = boss != null || princesa != null || guarda != null || elite != null || caveira != null;
            if (ehBossDrop && NetSpawn.PodeSpawnar)
                ElementRegistry.Instance?.DroparTokenElemento(transform.position, NetSpawn.EmRede ? 2 : 1);

            if (ehBossDrop)
            {
                // Game-feel: morte de boss/elite é um momento — tremor (Tremer propaga em co-op)
                // + hit-stop maior (só SP). Mob comum NÃO entra aqui (horda viraria stutter).
                CameraShaker.Tremer(0.18f, 0.4f);
                Impacto.MorteBoss();
            }

            if      (boss    != null) boss.IniciarEfeitoMorte();
            else if (princesa != null) princesa.IniciarEfeitoMorte();
            else if (guarda  != null) guarda.IniciarEfeitoMorte();
            else if (elite   != null) elite.IniciarEfeitoMorte();
            else if (caveira != null) caveira.IniciarEfeitoMorte();
            else
            {
                // Kill juice: pop satisfatório na morte (SP toca local; co-op host toca + replica no P2).
                Color corMorte = spriteRenderer != null ? spriteRenderer.color : Color.white;
                float escMorte = Mathf.Abs(transform.localScale.x);
                KillPopVFX.Tocar(transform.position, corMorte, escMorte);
                GetComponent<EnemyNet>()?.BroadcastMorteVFX(transform.position, corMorte, escMorte);
                NetSpawn.Despawnar(gameObject); // host-autoritativo em rede
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ Tentativa de Morrer() bloqueada. estaMorrendo: {estaMorrendo}, vidaAtual: {vidaAtual}");
        }
    }

    // Se o ponto cair dentro de um obstáculo (camadaObstaculos do FlowField),
    // procura o ponto livre mais próximo em círculos crescentes ao redor.
    public static Vector3 AjustarPosicaoForaDeObstaculo(Vector3 pos)
    {
        var ff = FlowField.Instance;
        if (ff == null || !Physics2D.OverlapPoint(pos, ff.camadaObstaculos))
            return pos;

        const int direcoes = 12;
        for (float raio = 0.5f; raio <= 4f; raio += 0.5f)
        {
            for (int i = 0; i < direcoes; i++)
            {
                float ang = i * (360f / direcoes) * Mathf.Deg2Rad;
                Vector2 candidato = (Vector2)pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio;
                if (!Physics2D.OverlapPoint(candidato, ff.camadaObstaculos))
                    return candidato;
            }
        }
        return pos;
    }

    private void DroparOrbesXP()
    {
        if (xpOrbPrefab == null)
        {
            Debug.LogWarning("⚠️ xpOrbPrefab não configurado!");
            return;
        }

        int quantidadeOrbes = UnityEngine.Random.Range(minOrbs, maxOrbs + 1);
        Vector3 posDrop = AjustarPosicaoForaDeObstaculo(transform.position);

        for (int i = 0; i < quantidadeOrbes; i++)
        {
            // Co-op: host spawna como NetworkObject (os dois veem); cliente não spawna.
            GameObject orbe = NetSpawn.Spawnar(xpOrbPrefab, posDrop);
            if (orbe == null) continue;
            XPOrb xpOrb = orbe.GetComponent<XPOrb>();

            if (xpOrb != null)
            {
                xpOrb.xpValue = xpPorOrbe;
            }

            Rigidbody2D rb = orbe.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direcaoAleatoria = new Vector2(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                ).normalized;
                rb.AddForce(direcaoAleatoria * forcaDrop, ForceMode2D.Impulse);
            }
        }

    }

    private void DroparPowerup()
    {
        Vector3 posDrop = AjustarPosicaoForaDeObstaculo(transform.position);

        // Co-op: host spawna (drops com NetworkObject replicam; sem NetworkObject ficam locais
        // no host como antes); cliente não spawna. Em SP é Instantiate normal.
        foreach (var drop in drops)
        {
            if (drop.prefab == null) continue;
            if (UnityEngine.Random.value <= drop.chance)
                NetSpawn.Spawnar(drop.prefab, posDrop);
        }

        if (dadosInimigo != null && dadosInimigo.dropsPossiveis != null)
        {
            foreach (var drop in dadosInimigo.dropsPossiveis)
            {
                if (drop.prefab == null) continue;
                if (UnityEngine.Random.value <= drop.chance)
                    NetSpawn.Spawnar(drop.prefab, posDrop);
            }
        }

        var ge = GerenciadorEventos.Instance;
        if (ge != null && ge.dropsGlobais != null)
        {
            foreach (var drop in ge.dropsGlobais)
            {
                if (drop.prefab == null) continue;
                if (UnityEngine.Random.value <= drop.chance)
                    NetSpawn.Spawnar(drop.prefab, posDrop);
            }
        }
    }

    // ── Dano por contato (usado quando DanoInimigo não está presente) ──────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (danoInimigoComponent != null || estaMorrendo) return;
        if (other.CompareTag("Player")) AplicarDanoContato(other.GetComponent<PlayerStats>());
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (danoInimigoComponent != null || estaMorrendo || Time.time < proximoContato) return;
        if (other.CompareTag("Player")) AplicarDanoContato(other.GetComponent<PlayerStats>());
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (danoInimigoComponent != null || estaMorrendo) return;
        if (col.gameObject.CompareTag("Player")) AplicarDanoContato(col.gameObject.GetComponent<PlayerStats>());
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (danoInimigoComponent != null || estaMorrendo || Time.time < proximoContato) return;
        if (col.gameObject.CompareTag("Player")) AplicarDanoContato(col.gameObject.GetComponent<PlayerStats>());
    }

    void AplicarDanoContato(PlayerStats stats)
    {
        if (stats == null) return;
        stats.TakeDamage(danoAtual);
        proximoContato = Time.time + INTERVALO_CONTATO;
    }

    public float GetPorcentagemVida()
    {
        if (vidaMaxima <= 0) return 0f;
        return vidaAtual / vidaMaxima;
    }

    // 🆕 GETTERS para status
    public float GetVelocidadeAtual() => velocidadeAtual;
    public bool EstaAtordoado() => estaAtordoado;
    public float GetTempoAtordoado() => tempoAtordoado;
    public bool TemBuffDefesaAtivo() => temBuffDefesa;
    public float GetBonusDefesa() => bonusDefesa;
}

[System.Serializable]
public class DropEntry
{
    public GameObject prefab;
    [Range(0f, 1f)] public float chance = 0.1f;
}

// ✅ Classe de animação de texto mantida no mesmo arquivo
public class AnimacaoTextoFlutuante : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private Vector3 startPos;
    private float timer = 0f;

    public void Initialize(Vector3 startPosition)
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        startPos = startPosition;
    }

    void Update()
    {
        if (textMesh == null) return;

        timer += Time.deltaTime;

        float speed = 80f;
        transform.position = startPos + new Vector3(0, speed * timer, 0);

        Color color = textMesh.color;
        color.a = 1f - (timer / 1f);
        textMesh.color = color;

        if (timer >= 1f)
            Destroy(gameObject);
    }
}
