using UnityEngine;
using System.Collections.Generic;

public class SirenSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<Transform> spawnPoints;     // Possible spawn locations
    public List<GameObject> sirenPrefabs;   // Prefabs to spawn
    public int numberOfSirensToSpawn = 1;   // Total sirens to spawn

    private RockMover referenceMover;

    private void Start()
    {
        referenceMover = GetComponent<RockMover>();
        if (referenceMover == null)
        {
            Debug.LogError("RockMover component is missing on the SirenSpawner.");
            return;
        }

        SpawnSirens();
    }

    private void SpawnSirens()
    {
        int spawnCount = Mathf.Min(numberOfSirensToSpawn, spawnPoints.Count);
        List<Transform> availableSpawnPoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < spawnCount; i++)
        {
            int spawnIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnPoint = availableSpawnPoints[spawnIndex];
            availableSpawnPoints.RemoveAt(spawnIndex);

            GameObject selectedPrefab = sirenPrefabs[Random.Range(0, sirenPrefabs.Count)];
            GameObject instance = Instantiate(selectedPrefab, spawnPoint.position, spawnPoint.rotation);

            RockMover mover = instance.GetComponent<RockMover>();
            if (mover != null)
            {
                mover.speed = referenceMover.speed;
                mover.moveDirection = referenceMover.moveDirection;
                mover.origin = spawnPoint.position;
                mover.spawner = referenceMover.spawner;
            }
            else
            {
                Debug.LogWarning($"Spawned siren prefab '{selectedPrefab.name}' does not have a RockMover component.");
            }
        }
    }
}