using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PortalController : MonoBehaviour
{
    [Header("Destino")]
    [SerializeField] private PortalController destinationPortal;

    [Header("Tempos")]
    [SerializeField] private float teleportDelay    = 5f;
    [SerializeField] private float cooldownDuration = 180f;

    [Header("Efeito de Carga")]
    [SerializeField] private float chargeSpinSpeed    = 180f;
    [SerializeField] private float chargeMaxSpinSpeed = 720f;
    [SerializeField] private float chargeBaseWidth    = 0.1f;
    [SerializeField] private float chargeMaxWidth     = 0.28f;
    [SerializeField] private Color chargeColorStart   = new Color(0f, 0.9f, 0.1f);
    [SerializeField] private Color chargeColorEnd     = new Color(0.8f, 1f, 0.8f);

    [Header("Efeito de Teleporte")]
    [SerializeField] private float shrinkDuration = 0.3f;
    [SerializeField] private float flightDuration = 0.7f;
    [SerializeField] private float expandDuration = 0.35f;
    [SerializeField] private float arcHeight      = 3f;
    [SerializeField] private Color energyColor    = new Color(0.1f, 1f, 0.3f);

    [Header("Indicador de Cooldown")]
    [SerializeField] private float  cdRingRadius = 1.9f;
    [SerializeField] private float  cdRingWidth  = 0.1f;
    [SerializeField] private Color  cdRingColor  = new Color(0f, 0.9f, 0.1f);

    private bool _onCooldown;
    private Coroutine _teleportCoroutine;

    private LineRenderer[] _lines;
    private Light2D _light;
    private float _baseIntensity;
    private float _baseRotation;

    private LineRenderer _cdRing;
    private const int CdSegments = 64;

    public bool IsOnCooldown => _onCooldown;
    public void SetDestination(PortalController dest) => destinationPortal = dest;

    private void Awake()
    {
        _lines = GetComponentsInChildren<LineRenderer>();
        _light = GetComponent<Light2D>();
        if (_light) _baseIntensity = _light.intensity;
        _baseRotation = transform.eulerAngles.z;

        BuildCooldownRing();
    }

    // ── Cooldown ring ──────────────────────────────────────────────────────

    private void BuildCooldownRing()
    {
        var go = new GameObject("cd_ring");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        _cdRing = go.AddComponent<LineRenderer>();
        _cdRing.useWorldSpace = false;
        _cdRing.loop = false;
        _cdRing.startWidth = cdRingWidth;
        _cdRing.endWidth   = cdRingWidth;
        _cdRing.startColor = cdRingColor;
        _cdRing.endColor   = cdRingColor;
        _cdRing.material   = new Material(Shader.Find("Sprites/Default"));
        _cdRing.sortingLayerName = "Default";
        _cdRing.sortingOrder = 4;
        _cdRing.positionCount = 0;
        go.SetActive(false);
    }

    private void SetCooldownArc(float fraction)
    {
        // fraction 1 = círculo completo, 0 = nada
        int pts = Mathf.RoundToInt(fraction * CdSegments) + 1;
        pts = Mathf.Clamp(pts, 0, CdSegments + 1);

        if (pts < 2) { _cdRing.positionCount = 0; return; }

        _cdRing.positionCount = pts;
        for (int i = 0; i < pts; i++)
        {
            // começa no topo e vai no sentido horário
            float angle = -Mathf.PI / 2f + 2f * Mathf.PI * i / CdSegments;
            _cdRing.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * cdRingRadius,
                Mathf.Sin(angle) * cdRingRadius, 0f));
        }
    }

    // ── Trigger ────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_onCooldown || _teleportCoroutine != null) return;
        if (!other.CompareTag("Player")) return;
        _teleportCoroutine = StartCoroutine(TeleportAfterDelay(other.transform));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || _teleportCoroutine == null) return;
        StopCoroutine(_teleportCoroutine);
        _teleportCoroutine = null;
        ResetVisuals();
    }

    // ── Countdown ──────────────────────────────────────────────────────────

    private IEnumerator TeleportAfterDelay(Transform player)
    {
        float elapsed = 0f;
        float currentAngle = _baseRotation;

        while (elapsed < teleportDelay)
        {
            float t = elapsed / teleportDelay;

            float spin = Mathf.Lerp(chargeSpinSpeed, chargeMaxSpinSpeed, t);
            currentAngle += spin * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

            float w = Mathf.Lerp(chargeBaseWidth, chargeMaxWidth, t);
            Color c = Color.Lerp(chargeColorStart, chargeColorEnd, t);
            foreach (var lr in _lines) { lr.startWidth = w; lr.endWidth = w; lr.startColor = c; lr.endColor = c; }

            if (_light)
                _light.intensity = _baseIntensity + Mathf.Lerp(0f, 2.5f, t) + 0.4f * Mathf.Sin(elapsed * 8f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (_light) _light.intensity = _baseIntensity + 4f;
        foreach (var lr in _lines) { lr.startColor = lr.endColor = Color.white; }
        yield return new WaitForSeconds(0.08f);

        ResetVisuals();
        _teleportCoroutine = null;

        if (destinationPortal != null)
            yield return StartCoroutine(TeleportEffect(player));

        EnterCooldown();
        if (destinationPortal != null) destinationPortal.EnterCooldown();
    }

    // ── Efeito de teleporte ────────────────────────────────────────────────

    private IEnumerator TeleportEffect(Transform player)
    {
        float totalDuration = shrinkDuration + flightDuration + expandDuration;

        var movement = player.GetComponent<moviment_player2>();
        movement?.Imobilizar(totalDuration + 0.2f);

        var renderers     = player.GetComponentsInChildren<SpriteRenderer>();
        var rb            = player.GetComponent<Rigidbody2D>();
        Vector3 origScale = player.localScale;
        var origColors    = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) origColors[i] = renderers[i].color;

        if (rb) { rb.linearVelocity = Vector2.zero; rb.isKinematic = true; }

        yield return AnimateScale(player, renderers, origScale, origColors[0],
                                  origScale * 0.08f, energyColor, shrinkDuration);

        Vector3 origin = player.position;
        Vector3 dest   = destinationPortal.transform.position;
        Vector3 mid    = (origin + dest) * 0.5f + Vector3.up * arcHeight;

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Clamp01(t + Time.deltaTime / flightDuration);
            Vector3 a = Vector3.Lerp(origin, mid, t);
            Vector3 b = Vector3.Lerp(mid,   dest, t);
            player.position = Vector3.Lerp(a, b, t);
            float pulse = 1f + 0.3f * Mathf.Sin(t * Mathf.PI * 4f);
            player.localScale = origScale * 0.08f * pulse;
            yield return null;
        }
        player.position = dest;

        yield return AnimateScale(player, renderers, origScale * 0.08f, energyColor,
                                  origScale, origColors[0], expandDuration);

        player.localScale = origScale;
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = origColors[i];
        if (rb) rb.isKinematic = false;
    }

    private IEnumerator AnimateScale(Transform target, SpriteRenderer[] renderers,
                                     Vector3 fromScale, Color fromColor,
                                     Vector3 toScale,   Color toColor, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Clamp01(t + Time.deltaTime / duration);
            float ease = t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
            target.localScale = Vector3.Lerp(fromScale, toScale, ease);
            Color c = Color.Lerp(fromColor, toColor, ease);
            foreach (var r in renderers) r.color = c;
            yield return null;
        }
    }

    // ── Visuais ────────────────────────────────────────────────────────────

    private void ResetVisuals()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, _baseRotation);
        foreach (var lr in _lines)
        {
            lr.startWidth = chargeBaseWidth; lr.endWidth = chargeBaseWidth;
            lr.startColor = chargeColorStart; lr.endColor = chargeColorStart;
        }
        if (_light) _light.intensity = _baseIntensity;
    }

    // ── Cooldown ───────────────────────────────────────────────────────────

    public void EnterCooldown()
    {
        if (!_onCooldown) StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _onCooldown = true;
        _cdRing.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < cooldownDuration)
        {
            float remaining = 1f - elapsed / cooldownDuration;
            SetCooldownArc(remaining);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetCooldownArc(0f);
        _cdRing.gameObject.SetActive(false);
        _onCooldown = false;
    }
}
