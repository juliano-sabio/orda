using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ElementDropToken : MonoBehaviour
{
    [Header("Elemento")]
    public ElementType elementType = ElementType.Fogo;

    [Header("Animacao")]
    public float bobAmplitude = 0.12f;
    public float bobFrequency = 2.2f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var ui = ElementApplicationUI.Instance;
        if (ui == null)
        {
            Debug.LogWarning("[ElementDropToken] ElementApplicationUI.Instance e null.");
            Destroy(gameObject);
            return;
        }

        ui.Abrir(elementType);
        Destroy(gameObject);
    }
}
