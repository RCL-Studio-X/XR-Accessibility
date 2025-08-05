using UnityEngine;

public class RockMover : MonoBehaviour
{
    public float speed = 10f;
    public Vector3 origin;

    [HideInInspector]
    public Vector3 moveDirection = Vector3.right;

    [HideInInspector]
    public RockSpawner spawner;

    void Update()
    {
        var direction = moveDirection.normalized;
        transform.position += direction * (speed * Time.deltaTime);

        if (spawner)
        {
            var distance = Vector3.Distance(transform.position, origin);
            var despawnDistance = spawner.despawnDistance;

            if (distance > despawnDistance)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log("Reference to spawner missing.");
        }
    }
}