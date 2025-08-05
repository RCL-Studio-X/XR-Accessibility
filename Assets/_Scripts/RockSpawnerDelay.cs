using UnityEngine;

public class RockSpawnerDelay : MonoBehaviour
{
    [Tooltip("Time in seconds to wait after parent is active before enabling RockSpawner.")]
    public float delayTime = 5f;

    [Tooltip("Optional: Assign RockSpawner manually, otherwise it searches this GameObject.")]
    public RockSpawner targetSpawner;

    private bool delayStarted = false;
    private float parentActiveTime = -1f;

    void Update()
    {
        // Wait until parent is active in hierarchy
        if (!delayStarted && gameObject.activeInHierarchy)
        {
            parentActiveTime = Time.time;
            delayStarted = true;
        }

        // Begin delay countdown
        if (delayStarted && Time.time >= parentActiveTime + delayTime)
        {
            if (targetSpawner == null)
                targetSpawner = GetComponent<RockSpawner>();

            if (targetSpawner != null)
            {
                targetSpawner.enabled = true;
                enabled = false; // Disable this script after enabling spawner
            }
            else
            {
                Debug.LogWarning("RockSpawnerDelay: No RockSpawner found.");
                enabled = false;
            }
        }
    }
}