using UnityEngine;
using Mirror;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] public GameObject enemyPrefab;
    [SerializeField] public Transform spawnPoint;

    public override void OnStartServer()
    {
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        NetworkServer.Spawn(enemy);
    }
}
