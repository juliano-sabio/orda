using Unity.Netcode;
using UnityEngine;

// Vive SÓ na variante NetworkPlayer. Isola o NGO do player_stats.
// - sincroniza o índice de personagem (dono escreve; todos aplicam)
// - registra PlayerStats.Local no dono
// - implementa INetOwnership pro gating dual-mode
[RequireComponent(typeof(PlayerStats))]
public class PlayerNet : NetworkBehaviour, INetOwnership
{
    readonly NetworkVariable<int> charIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Co-op: ultimate/passiva POR JOGADOR (o lobby usava PlayerPrefs, que é compartilhado
    // na mesma máquina/MPPM → os dois ficavam com a mesma). -1 = usar PlayerPrefs (solo).
    readonly NetworkVariable<int> ultimateIdx = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<int> passivaIdx = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Lado que o player olha (facing). Sincronizado à parte porque o sprite
    // vira invertendo localScale.x — se isso fosse pelo NetworkTransform com
    // interpolação, a escala passaria por 0 (sprite encolhe/some ao virar).
    readonly NetworkVariable<bool> facingLeft = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Co-op: % da barra de luz (segunda fase) do dono → o brilho do PlayerCollectLight
    // aparece no fantoche na tela do colega (sem isto, só o dono via a própria luz).
    readonly NetworkVariable<float> luzPct = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // SP2c — downed/revive.
    readonly NetworkVariable<bool> downed = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<float> reviveProgresso = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] float reviveRaio = 2.5f;
    [SerializeField] float tempoRevive = 4f;
    [SerializeField] float fracaoRevive = 0.5f;
    bool gameOverDisparado;
    static bool voltandoAoLobby; // evita múltiplos LoadScene ao game over do grupo

    PlayerStats stats;
    Vector3 baseScale;

    // SP-lobby — estado de lobby.
    readonly NetworkVariable<bool> ready = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<Unity.Collections.FixedString32Bytes> playerName =
        new NetworkVariable<Unity.Collections.FixedString32Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool IsNetworked => IsSpawned;
    public bool IsLocalOwner => IsOwner;
    public bool Caido => downed.Value;
    public float ReviveProgresso => reviveProgresso.Value;
    public bool Pronto => ready.Value;
    public string Nome => playerName.Value.ToString();
    public int CharIndexLobby => charIndex.Value;

    public void SetPronto(bool v) { if (IsOwner) ready.Value = v; }
    public void SetChar(int idx) { if (IsOwner) charIndex.Value = idx; }

    // Co-op: índice de ultimate/passiva por jogador (-1 = usar PlayerPrefs no solo).
    public int UltimateIdx => ultimateIdx.Value;
    public int PassivaIdx  => passivaIdx.Value;
    public void SetUltimate(int idx) { if (IsOwner) ultimateIdx.Value = idx; }
    public void SetPassiva(int idx)  { if (IsOwner) passivaIdx.Value = idx; }

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        baseScale = transform.localScale;
    }

    bool ultReadyAnterior;

    bool acoesBloqueadas;

    void Update()
    {
        if (!IsSpawned) return;

        // Bloqueia skills (auto-fire) e ultimates quando CAÍDO ou no LOBBY — senão continuam
        // disparando (e tocando som) no lobby/após morte. Roda em todas as cópias (puppet idem).
        bool bloquear = downed.Value || LobbyState.EmLobby;
        if (bloquear != acoesBloqueadas) { acoesBloqueadas = bloquear; BloquearAcoesCaido(bloquear); }

        if (IsOwner)
        {
            // o moviment_player2 já virou o localScale.x; só publicamos o lado.
            facingLeft.Value = transform.localScale.x < 0f;

            // Detecta o cast do ultimate (ready true→false) e transmite pro colega ver o visual.
            bool r = stats != null && stats.ultimateReady;
            if (ultReadyAnterior && !r) UltimateCastServerRpc();
            ultReadyAnterior = r;

            // Publica a intensidade REAL do brilho (barra de luz OU pickup) pro fantoche brilhar igual.
            var pclDono = GetComponent<PlayerCollectLight>();
            if (pclDono != null)
            {
                float p = pclDono.IntensidadeNorm;
                if (Mathf.Abs(p - luzPct.Value) > 0.02f) luzPct.Value = p;
            }
        }
        else
        {
            // cópia remota: aplica o lado instantâneo (snap), sem passar por 0.
            float mag = Mathf.Abs(baseScale.x);
            var s = transform.localScale;
            s.x = facingLeft.Value ? -mag : mag;
            transform.localScale = s;

            // cópia remota: reflete o brilho do dono (barra de luz OU pickup).
            var pcl = GetComponent<PlayerCollectLight>();
            if (pcl != null) pcl.AplicarNorm(luzPct.Value);
        }

        if (IsServer) MonitorarHost();
    }

    // ── SP2c: downed / revive / game over (host-autoritativo) ──────────

    // Dano de inimigo (no host) aplicado no dono.
    [Rpc(SendTo.Owner)]
    public void TomarDanoOwnerRpc(float dano) { stats.TakeDamage(dano); }

    // [debug temp] Host força invulnerabilidade no dono (botão de debug do P2). Remover no release.
    [Rpc(SendTo.Owner)]
    public void DebugInvulneravelOwnerRpc(bool v) { if (stats != null) stats.invulneravel = v; }

    // Chamado pelo dono ao cair (health <= 0 em co-op).
    public void Cair() { if (IsOwner) downed.Value = true; }

    // Host manda o dono reviver.
    [Rpc(SendTo.Owner)]
    public void ReviverOwnerRpc(float fracao)
    {
        downed.Value = false;
        stats.ReviverCoop(fracao);
    }

    // VFX de revive em todos.
    [Rpc(SendTo.Everyone)]
    public void ReviveVfxRpc()
    {
        // Antes: SendMessage("Executar") batia no LevelUpEffect.Executar(int) (erro) e nem rodava
        // a corrotina. Agora chama o VFX direto (raio azul + luz), sem bloquear movimento.
        var ef = GetComponent<PlayerSpawnEffect>();
        if (ef != null) ef.Reproduzir();
    }

    // Game over de grupo em todos.
    [Rpc(SendTo.Everyone)]
    public void GameOverGrupoRpc() { GameOverUI.Mostrar(); }

    // Co-op: o host manda este player (no cliente dono) ofertar a própria evolução.
    [Rpc(SendTo.Owner)]
    public void OfertarEvolucaoOwnerRpc() { GerenciadorEventos.Instance?.OfertarEvolucaoLocal(); }

    // Co-op: o nível do grupo subiu; este player aplica o level-up (escolha individual).
    [Rpc(SendTo.Owner)]
    public void SubirNivelOwnerRpc(int novoNivel) { if (stats != null) stats.AplicarLevelUpLocal(novoNivel); }

    // Instâncias cosméticas do fantoche, por índice de skill (pra recolorir na infusão).
    readonly System.Collections.Generic.Dictionary<int, SkillBehavior> cosmeticasPorIdx =
        new System.Collections.Generic.Dictionary<int, SkillBehavior>();

    // Co-op: o dono adquiriu uma skill (suportada) → o fantoche do colega passa a rodar
    // a versão COSMÉTICA dela. idx = índice no skillsRegistro; elemento = appliedElement do dono.
    public void SincronizarSkillCosmetica(int idx, int elemento)
    {
        if (IsOwner && IsSpawned) AdicionarSkillCosmeticaServerRpc(idx, elemento);
    }

    [Rpc(SendTo.Server)]
    void AdicionarSkillCosmeticaServerRpc(int idx, int elemento) { AdicionarSkillCosmeticaRpc(idx, elemento); }

    [Rpc(SendTo.NotOwner)]
    void AdicionarSkillCosmeticaRpc(int idx, int elemento)
    {
        var fx = GetComponent<SkillFxNet>();
        if (fx == null || fx.skillsRegistro == null || idx < 0 || idx >= fx.skillsRegistro.Length) return;
        var b = SkillFxCosmetico.Adicionar(stats, fx.skillsRegistro[idx], elemento);
        if (b != null) cosmeticasPorIdx[idx] = b;
    }

    // Co-op: o dono infundiu uma skill → recolorir a cópia cosmética no fantoche.
    public void SincronizarInfusao(int idx, int elemento)
    {
        if (IsOwner && IsSpawned) InfundirSkillCosmeticaServerRpc(idx, elemento);
    }

    [Rpc(SendTo.Server)]
    void InfundirSkillCosmeticaServerRpc(int idx, int elemento) { InfundirSkillCosmeticaRpc(idx, elemento); }

    [Rpc(SendTo.NotOwner)]
    void InfundirSkillCosmeticaRpc(int idx, int elemento)
    {
        SkillBehavior b;
        if (cosmeticasPorIdx.TryGetValue(idx, out b) && b != null && b.skillData != null)
            b.skillData.appliedElement = (ElementType)elemento;
    }

    // Co-op: o dono usou o ultimate; o fantoche do colega roda SÓ o visual (sem dano).
    [Rpc(SendTo.Server)]
    void UltimateCastServerRpc() { UltimateCastRpc(); }

    [Rpc(SendTo.NotOwner)]
    void UltimateCastRpc()
    {
        var u = GetComponent<IUltimateCosmetico>();
        if (u != null) u.ExecutarCosmetico();
    }

    // Co-op: a Necropole spawna fantasmas em OnPreMorte (só no host) → sem isto o P2 não via
    // nenhum. O host avisa os clientes pra criarem o fantasma cosmético (sem dano) na posição.
    public void SincronizarNecropoleFantasma(Vector3 pos)
    {
        if (IsServer && IsSpawned) NecropoleFantasmaRpc(pos);
    }

    [Rpc(SendTo.NotServer)]
    void NecropoleFantasmaRpc(Vector3 pos)
    {
        GetComponent<NecropoleUltimate>()?.SpawnarFantasmaCosmeticoRemoto(pos);
    }

    // Co-op: o dono disparou uma defensiva (teia/fuga/instinto/segunda chance) → o fantoche
    // do colega reproduz SÓ o visual. tipo = (int)SpecificSkillType. Shield tem caminho próprio.
    public void SincronizarDefensiva(int tipo)
    {
        if (IsOwner && IsSpawned) DefensivaServerRpc(tipo);
    }

    [Rpc(SendTo.Server)]
    void DefensivaServerRpc(int tipo) { DefensivaRpc(tipo); }

    [Rpc(SendTo.NotOwner)]
    void DefensivaRpc(int tipo)
    {
        if (stats == null) return;
        var t = (SpecificSkillType)tipo;

        // Shield: a aura cosmética é filha do fantoche; quebra reproduz a animação.
        if (t == SpecificSkillType.Shield)
        {
            var sh = stats.GetComponentInChildren<ShieldAuraBehavior>();
            if (sh != null) sh.QuebrarCosmetico();
            return;
        }

        var behaviors = stats.GetComponents<SkillBehavior>();
        foreach (var b in behaviors)
        {
            if (b == null || !b.cosmetico || b.skillData == null) continue;
            if (b.skillData.specificType != t) continue;
            var dc = b as IDefensivaCosmetico;
            if (dc != null) { dc.ExecutarCosmetico(); break; }
        }
    }

    // Co-op: o dono equipou o Shield → o fantoche instancia a aura sustentada (prefab),
    // em modo cosmético (não bloqueia dano). idx = índice no skillsRegistro.
    public void SincronizarShieldEquip(int idx)
    {
        if (IsOwner && IsSpawned) ShieldEquipServerRpc(idx);
    }

    [Rpc(SendTo.Server)]
    void ShieldEquipServerRpc(int idx) { ShieldEquipRpc(idx); }

    [Rpc(SendTo.NotOwner)]
    void ShieldEquipRpc(int idx)
    {
        if (stats == null) return;
        var fx = GetComponent<SkillFxNet>();
        if (fx == null || fx.skillsRegistro == null || idx < 0 || idx >= fx.skillsRegistro.Length) return;
        var skill = fx.skillsRegistro[idx];
        if (skill == null || skill.visualEffect == null) return;
        if (stats.GetComponentInChildren<ShieldAuraBehavior>() != null) return; // já tem

        // Espelha PlayerStats.AddShieldAuraBehavior (mesma instância/offset), porém cosmético.
        var auraObj = Instantiate(skill.visualEffect);
        auraObj.transform.SetParent(stats.transform, false);
        auraObj.transform.localPosition = new Vector3(1.5f, 1.2f, 1.8f);
        auraObj.transform.localRotation = Quaternion.identity;
        auraObj.transform.localScale = Vector3.one;
        var beh = auraObj.GetComponent<ShieldAuraBehavior>();
        if (beh != null) { beh.cosmetico = true; beh.Initialize(stats); }
    }

    // Co-op: pickups coletados no host são aplicados no DONO do player que pegou
    // (vida/dash/boosts são owner-autoritativos).
    // Co-op: efeito de escuridão (projétil do boss) na tela do player ATINGIDO, não no host.
    [Rpc(SendTo.Owner)]
    public void EscuridaoOwnerRpc(float duracao, float raioTela)
    {
        ProjetilEspecialBoss.AplicarVisaoReduzida(duracao, raioTela);
    }

    [Rpc(SendTo.Owner)]
    public void CurarOwnerRpc(float quantia) { if (stats != null) stats.Heal(quantia); }

    // Co-op: efeitos de status aplicados pelo host vão pro DONO (gameplay + visual na tela dele).
    [Rpc(SendTo.Owner)]
    public void AplicarSlowOwnerRpc(float reducao, float duracao) { if (stats != null) stats.AplicarSlow(reducao, duracao); }

    [Rpc(SendTo.Owner)]
    public void AplicarVenenoOwnerRpc(float danoPorTick, float intervalo, float duracao) { if (stats != null) stats.AplicarVenenoPlayer(danoPorTick, intervalo, duracao); }

    [Rpc(SendTo.Owner)]
    public void AplicarQueimaduraOwnerRpc(float danoPorTick, float intervalo, float duracao) { if (stats != null) stats.AplicarQueimaduraPlayer(danoPorTick, intervalo, duracao); }

    [Rpc(SendTo.Owner)]
    public void AplicarParalisiaOwnerRpc(float duracao) { if (stats != null) stats.AplicarParalisiaPlayer(duracao); }

    // Co-op: lentidão do projétil de vinhas (slime_de_projetil) — o EfeitoLentidao mexe em
    // velocidade/tint locais do player, então precisa rodar na máquina do DONO atingido.
    [Rpc(SendTo.Owner)]
    public void LentidaoOwnerRpc(float duracao, float fator, float r, float g, float b)
    {
        if (stats == null) return;
        var efeito = stats.GetComponent<EfeitoLentidao>();
        if (efeito == null) efeito = stats.gameObject.AddComponent<EfeitoLentidao>();
        efeito.AplicarLentidao(duracao, fator, new Color(r, g, b));
    }

    [Rpc(SendTo.Owner)]
    public void BloquearUltimateOwnerRpc(float duracao) { if (stats != null) stats.BloquearUltimate(duracao); }

    [Rpc(SendTo.Owner)]
    public void DashChargeOwnerRpc() { if (stats != null) stats.AddDashCharge(); }

    // Co-op: replica o EFEITO visual do dash (rastro/partículas) no fantoche do colega —
    // o movimento já vem pelo NetworkTransform, mas o DashEffect é local; sem isto o dash
    // do outro player aparece "sem efeito" na sua tela.
    public void BroadcastDash(Vector2 dir)
    {
        if (IsOwner && IsSpawned) DashFxServerRpc(dir);
    }

    [Rpc(SendTo.Server)]
    void DashFxServerRpc(Vector2 dir) { DashFxClientRpc(dir); }

    [Rpc(SendTo.NotOwner)]
    void DashFxClientRpc(Vector2 dir)
    {
        SomSkill.Tocar(SomSkill.Tipo.DashUsar, transform.position, 0.5f); // co-op: ouvir o dash do colega
        var fx = GetComponent<DashEffect>();
        if (fx == null) return;
        fx.IniciarEfeito(dir);
        StartCoroutine(PararDashFx(fx));
    }

    System.Collections.IEnumerator PararDashFx(DashEffect fx)
    {
        yield return new WaitForSeconds(0.15f); // ~dashDuration
        if (fx != null) fx.PararEfeito();
    }

    // Co-op: espírito de luz (drop host-local) recarrega a barra de luz do DONO que coletou.
    [Rpc(SendTo.Owner)]
    public void AdicionarLuzOwnerRpc(float qtd) { if (stats != null) stats.AdicionarLuz(qtd); }

    // Co-op: LightPickup ativa o modo "coleta de luz" no DONO que coletou.
    [Rpc(SendTo.Owner)]
    public void ColetaLuzOwnerRpc(float dur) { if (stats != null) stats.GetComponent<PlayerCollectLight>()?.Ativar(dur); }

    // Co-op: ImaPickup aumenta o raio de coleta de XP do DONO que coletou.
    [Rpc(SendTo.Owner)]
    public void AumentarRaioXpOwnerRpc(float bonus) { if (stats != null) stats.xpCollectionRadius += bonus; }

    // Co-op: o token de elemento existe só no host; ele roteia a infusão pro DONO do player
    // que tocou, pra a escolha abrir na tela certa (não na do host/player 1).
    [Rpc(SendTo.Owner)]
    public void AbrirInfusaoOwnerRpc(int elemento)
    {
        var ui = ElementApplicationUI.Instance;
        if (ui != null) ui.Abrir((ElementType)elemento);
    }

    [Rpc(SendTo.Owner)]
    public void BoostColetaOwnerRpc(float raio, float mult, float dur)
    {
        if (stats == null) return;
        stats.BoostCollectionRadius(raio, dur);
        stats.BoostOrbSpeed(mult, dur);
    }

    void MonitorarHost()
    {
        // Enquanto no lobby: sem monitoramento de fase e rearma os guards pro próximo run
        // (game over do grupo + volta ao lobby) — os players persistem entre runs.
        if (LobbyState.EmLobby) { voltandoAoLobby = false; gameOverDisparado = false; return; }

        // Revive: se EU estou caído e há companheiro vivo no raio, enche a barra.
        if (downed.Value)
        {
            bool temReanimador = false;
            for (int i = 0; i < PlayerStats.All.Count; i++)
            {
                var p = PlayerStats.All[i];
                if (p == null || p == stats) continue;
                var pn = p.GetComponent<PlayerNet>();
                if (pn != null && pn.Caido) continue; // companheiro também caído
                float d2 = ((Vector2)p.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d2 <= reviveRaio * reviveRaio) { temReanimador = true; break; }
            }
            if (temReanimador)
            {
                reviveProgresso.Value += Time.deltaTime / tempoRevive;
                if (reviveProgresso.Value >= 1f)
                {
                    reviveProgresso.Value = 0f;
                    ReviverOwnerRpc(fracaoRevive);
                    ReviveVfxRpc();
                }
            }
            else if (reviveProgresso.Value > 0f)
            {
                reviveProgresso.Value = Mathf.Max(0f, reviveProgresso.Value - Time.deltaTime / tempoRevive);
            }
        }

        // Game over de grupo: todos os players caídos (uma vez).
        if (!gameOverDisparado && PlayerStats.All.Count > 0)
        {
            bool todosCaidos = true;
            for (int i = 0; i < PlayerStats.All.Count; i++)
            {
                var pn = PlayerStats.All[i] != null ? PlayerStats.All[i].GetComponent<PlayerNet>() : null;
                if (pn == null || !pn.Caido) { todosCaidos = false; break; }
            }
            if (todosCaidos)
            {
                gameOverDisparado = true;
                RunState.Desligar(); // desliga a fase na hora (para spawn/eventos/timer, congela inimigos)
                GameOverGrupoRpc();
                if (!voltandoAoLobby) { voltandoAoLobby = true; StartCoroutine(VoltarAoLobby()); }
            }
        }
    }

    // Game over do grupo em co-op: mostra a tela ~4s (tempo real, pois timeScale=0)
    // e o host devolve todos ao lobby. Sessão NGO continua viva → o grupo re-escolhe.
    System.Collections.IEnumerator VoltarAoLobby()
    {
        yield return new WaitForSecondsRealtime(4f);
        Time.timeScale = 1f; // não carregar o lobby congelado
        LobbyState.EmLobby = true;
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsServer)
            nm.SceneManager.LoadScene("lobby_mp", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    void AoMudarDowned(bool _, bool caido)
    {
        // Animação: o corpo some e a máscara cai no chão (persiste como marcador de revive em co-op).
        var mc = GetComponent<MascaraCaido>();
        if (mc == null) mc = gameObject.AddComponent<MascaraCaido>();
        if (caido) mc.Cair(true);
        else        mc.Levantar();
        // O bloqueio de skills/ultimates (caído OU lobby) é gerenciado central no Update.
    }

    // Desliga skills (auto-fire) e ultimates; usado p/ caído E lobby. Religa ao voltar.
    void BloquearAcoesCaido(bool caido)
    {
        foreach (var sb in GetComponentsInChildren<SkillBehavior>(true))
            sb.enabled = !caido;
        foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
            if (mb is IUltimateCosmetico) mb.enabled = !caido;

        // Player caído não pode ser empurrado por mobs/knockback: congela o corpo do DONO
        // (Kinematic + velocidade zero) e restaura ao reviver. As cópias remotas já são
        // Kinematic — só o dono tem corpo dinâmico, então só ele precisa disso.
        if (IsOwner)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = caido ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
                if (caido) rb.linearVelocity = Vector2.zero;
            }
        }
    }

    IndicadorSlime indicadorAliado; // co-op: seta apontando pro player remoto

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            PlayerStats.SetLocal(stats);

            // Co-op: no CLIENTE, o contato com inimigos é detectado localmente (hitbox preciso)
            // e o dano é pedido ao host. No host (P1) o DanoInimigo já detecta com precisão.
            if (!IsServer && GetComponent<ContatoInimigoNet>() == null)
                gameObject.AddComponent<ContatoInimigoNet>();

            charIndex.Value = PlayerPrefs.GetInt("SelectedCharacter", 0);
            // valor inicial do PlayerPrefs; o lobby sobrescreve POR JOGADOR via SetUltimate/SetPassiva.
            ultimateIdx.Value = PlayerPrefs.GetInt($"SelectedUltimate_{charIndex.Value}", 0);
            passivaIdx.Value  = PlayerPrefs.GetInt($"SelectedPassiva_{charIndex.Value}", 0);
            playerName.Value = new Unity.Collections.FixedString32Bytes("Jogador " + (OwnerClientId + 1));

            // Separa os players por cliente pra não nascerem sobrepostos.
            // Owner-authoritative: setar a posição aqui replica pros demais.
            float x = ((int)OwnerClientId - 0.5f) * 4f; // host(0) -> -2, client(1) -> +2
            transform.position = new Vector3(x, 0f, transform.position.z);
        }
        else
        {
            // Cópia remota = fantoche controlado pelo NetworkTransform.
            // Só um AudioListener e uma Câmera devem existir (os do dono local).
            var al = GetComponentInChildren<AudioListener>(true);
            if (al != null) al.enabled = false;

            // O player carrega a própria câmera (filha). Nas cópias remotas ela
            // é desligada — cada cliente renderiza só a câmera do SEU player.
            var camRemota = GetComponentInChildren<Camera>(true);
            if (camRemota != null) camRemota.enabled = false;

            // Rigidbody2D Kinematic: impede a física de brigar com o NetworkTransform
            // (sem isso, o corpo retém a última velocidade e arrasta/jittera).
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

            // Indicador (seta) apontando pro player remoto — some quando ele está visível na tela.
            indicadorAliado = IndicadorSlime.Criar(transform, new Color(0.3f, 1f, 0.55f), "Aliado", true, true);
        }

        // Aplica o personagem correto em TODAS as cópias (dono e remotas).
        stats.ApplyCharacterData(charIndex.Value);

        // Reaplica se o valor chegar/mudar depois (ordem de sincronização).
        charIndex.OnValueChanged += AoMudarPersonagem;
        // Reaplica quando a ultimate/passiva mudar (seleção no lobby).
        ultimateIdx.OnValueChanged += AoMudarBuild;
        passivaIdx.OnValueChanged  += AoMudarBuild;

        // Máscara ao cair/voltar.
        downed.OnValueChanged += AoMudarDowned;
        if (downed.Value) AoMudarDowned(false, true); // só age se já estiver caído ao spawnar (raro)
    }

    public override void OnNetworkDespawn()
    {
        charIndex.OnValueChanged -= AoMudarPersonagem;
        ultimateIdx.OnValueChanged -= AoMudarBuild;
        passivaIdx.OnValueChanged  -= AoMudarBuild;
        downed.OnValueChanged -= AoMudarDowned;
        if (indicadorAliado != null) Destroy(indicadorAliado.gameObject);
        PlayerStats.ClearLocal(stats);
    }

    void AoMudarPersonagem(int anterior, int novo)
    {
        stats.ApplyCharacterData(novo);
    }

    // Co-op: ultimate/passiva mudou (seleção no lobby) → reaplica o personagem com a nova build.
    void AoMudarBuild(int anterior, int novo)
    {
        // [diag temp] revela se cada player tem ultimate/passiva DISTINTAS (NetVar por-player)
        // ou iguais (artefato do MPPM = PlayerPrefs compartilhados). Remover no release.
        Debug.Log($"[BuildDiag] clientId={OwnerClientId} IsOwner={IsOwner} ultimateIdx={ultimateIdx.Value} passivaIdx={passivaIdx.Value} char={charIndex.Value}");
        stats.ApplyCharacterData(charIndex.Value);
    }

    // Co-op: reseta este player pra uma RUN NOVA. Chamado pelo FaseCoopBootstrap no load da fase,
    // em CADA máquina, pra TODOS os players. SkillManager/StatusCard/Evolution são DontDestroyOnLoad
    // e os players persistem (NGO) → sem isto as skills/stats/cartas da run anterior carregavam.
    public void ResetarParaNovaRun()
    {
        if (IsOwner)
        {
            SkillManager.Instance?.ClearAllSkills();           // skills reais + behaviors + bônus de stat
            SkillEvolutionManager.Instance?.Resetar();         // evoluções
            if (stats != null) stats.ApplyCharacterData(charIndex.Value); // stats/health base (zera cartas) + reaplica build
        }
        else
        {
            // fantoche do colega (objeto local nesta máquina): remove as cópias COSMÉTICAS
            // das skills da run anterior (senão acumulam/ficam stale na run nova).
            foreach (var b in GetComponents<SkillBehavior>())
                if (b != null && b.cosmetico) Destroy(b);
            cosmeticasPorIdx.Clear();
        }
    }
}
