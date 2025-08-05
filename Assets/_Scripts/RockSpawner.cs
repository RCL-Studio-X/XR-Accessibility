using System.Collections.Generic;
using UnityEngine;

public class RockSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<GameObject> rockPrefabs;

    [Range(0.01f, 100f)]
    public float spawnInterval = 1f;

    [Range(1, 500)]
    public int maxConcurrentRocks = 50;

    private float nextSpawnTime;

    [Header("Spawn Area")]
    [Tooltip("Defines the box area around the spawner where rocks can spawn (Width, Height, Depth)")]
    public Vector3 spawnRange = new Vector3(0, 20, 50);

    [Header("Rock Properties")]
    [Range(0f, 5000f)]
    public float minRockScale = 500f;

    [Range(0f, 5000f)]
    public float maxRockScale = 2000f;

    [Tooltip("Direction rocks will move in. Default is Vector3.right (1, 0, 0).")]
    public Vector3 movementDirection = Vector3.right;
    
    [Range(1f, 50f)]
    public float rockMoveSpeed = 10f;

    [Header("Rotation Settings")]
    public Vector3 prefabBaseRotation = Vector3.zero;
    public bool randomRotation = true;
    public bool yAxisOnlyRotation = false;
    
    [Header("Despawn Settings")]
    [Range(10f, 1000f)]
    public float despawnDistance = 100f;

    private List<GameObject> activeRocks = new List<GameObject>();

    void Update()
    {
        if (Time.time >= nextSpawnTime && activeRocks.Count < maxConcurrentRocks)
        {
            SpawnRock();
            nextSpawnTime = Time.time + spawnInterval;
        }

        MoveAndCullRocks();
    }

    void SpawnRock()
    {
        if (rockPrefabs.Count == 0) return;

        GameObject prefab = rockPrefabs[Random.Range(0, rockPrefabs.Count)];

        Vector3 spawnPos = transform.position + new Vector3(
            Random.Range(-spawnRange.x / 2f, spawnRange.x / 2f),
            Random.Range(-spawnRange.y / 2f, spawnRange.y / 2f),
            Random.Range(-spawnRange.z / 2f, spawnRange.z / 2f)
        );

        Quaternion baseRotation = Quaternion.Euler(prefabBaseRotation);
        GameObject rock = Instantiate(prefab, spawnPos, baseRotation);

        float scale = Random.Range(minRockScale, maxRockScale);
        rock.transform.localScale = Vector3.one * scale;

        if (randomRotation)
        {
            if (yAxisOnlyRotation)
            {
                float randomY = Random.Range(0f, 360f);
                rock.transform.rotation *= Quaternion.Euler(0f, randomY, 0f);
            }
            else
            {
                rock.transform.rotation *= Random.rotation;
            }
        }

        // Updated movement component
        RockMover mover = rock.GetComponent<RockMover>();
        if (!mover)
            mover = rock.AddComponent<RockMover>();
        mover.speed = rockMoveSpeed;
        mover.origin = spawnPos;
        mover.spawner = this;
        mover.moveDirection = movementDirection;

        activeRocks.Add(rock);
    }


    void MoveAndCullRocks()
    {
        for (int i = activeRocks.Count - 1; i >= 0; i--)
        {
            if (activeRocks[i] == null)
            {
                activeRocks.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(activeRocks[i].transform.position, transform.position);
            if (distance > despawnDistance)
            {
                Destroy(activeRocks[i]);
                activeRocks.RemoveAt(i);
            }
        }
    }
}
