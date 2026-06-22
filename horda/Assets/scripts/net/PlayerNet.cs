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

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        baseScale = transform.localScale;
    }

    bool ultReadyAnterior;

    void Update()
    {
        if (!IsSpawned) return;
        if (IsOwner)
        {
            // o moviment_player2 já virou o localScale.x; só publicamos o lado.
            facingLeft.Value = transform.localScale.x < 0f;

            // Detecta o cast do ultimate (ready true→false) e transmite pro colega ver o visual.
            bool r = stats != null && stats.ultimateReady;
            if (ultReadyAnterior && !r) UltimateCastServerRpc();
            ultReadyAnterior = r;

            // Publica o brilho ATIVO da luz (0 fora da segunda fase) pro fantoche brilhar igual.
            if (stats != null)
            {
                float p = stats.luzGlowPct;
                if (Mathf.Abs(p - luzPct.Value) > 0.01f) luzPct.Value = p;
            }
        }
        else
        {
            // cópia remota: aplica o lado instantâneo (snap), sem passar por 0.
            float mag = Mathf.Abs(baseScale.x);
            var s = transform.localScale;
            s.x = facingLeft.Value ? -mag : mag;
            transform.localScale = s;

            // cópia remota: reflete a luz do dono (brilho da segunda fase).
            var pcl = GetComponent<PlayerCollectLight>();
            if (pcl != null) pcl.AtualizarPorPercentual(luzPct.Value);
        }

        if (IsServer) MonitorarHost();
    }

    // ── SP2c: downed / revive / game over (host-autoritativo) ──────────

    // Dano de inimigo (no host) aplicado no dono.
    [Rpc(SendTo.Owner)]
    public void TomarDanoOwnerRpc(float dano) { stats.TakeDamage(dano); }

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

    [Rpc(SendTo.Owner)]
    public void DashChargeOwnerRpc() { if (stats != null) stats.AddDashCharge(); }

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
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = caido ? new Color(0.5f, 0.25f, 0.25f, 0.9f) : Color.white;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            PlayerStats.SetLocal(stats);
            charIndex.Value = PlayerPrefs.GetInt("SelectedCharacter", 0);
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
        }

        // Aplica o personagem correto em TODAS as cópias (dono e remotas).
        stats.ApplyCharacterData(charIndex.Value);

        // Reaplica se o valor chegar/mudar depois (ordem de sincronização).
        charIndex.OnValueChanged += AoMudarPersonagem;

        // Tint visual ao cair/voltar.
        downed.OnValueChanged += AoMudarDowned;
        AoMudarDowned(false, downed.Value);
    }

    public override void OnNetworkDespawn()
    {
        charIndex.OnValueChanged -= AoMudarPersonagem;
        downed.OnValueChanged -= AoMudarDowned;
        PlayerStats.ClearLocal(stats);
    }

    void AoMudarPersonagem(int anterior, int novo)
    {
        stats.ApplyCharacterData(novo);
    }
}
