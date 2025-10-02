using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    [Header("Configurações de Spawn")]
    public GameObject prefab;        // Prefab que será spawnado
    public float intervalo = 2f;     // Tempo entre cada spawn
    public int maximo = 10;          // Quantidade máxima de objetos (0 = infinito)
    public Vector3 area = new Vector3(10,0, 10);// Área de spawn em X e Z

    private int contador = 0;

    void Start()
    {
        StartCoroutine(LoopSpawn());
    }

    IEnumerator LoopSpawn()
    {
        while (true)
        {
            if (maximo == 0 || contador < maximo)
                Spawnar();

            yield return new WaitForSeconds(intervalo);
        }
    }

    void Spawnar()
    {
        Vector3 posicao = transform.position + new Vector3(
            Random.Range(-area.x / 2f, area.x / 2f),
            area.y,
            Random.Range(-area.z / 2f, area.z / 2f)
        );

        Instantiate(prefab, posicao, Quaternion.identity);
        contador++;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(transform.position + new Vector3(0, area.y, 0), area);
    }
}
