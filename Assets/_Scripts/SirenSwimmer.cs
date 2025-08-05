using UnityEngine;

public class SirenSwimmer : MonoBehaviour
{
    [Header("Trigger Settings")]
    public float targetX = -10f;
    public float tolerance = 0.0f;

    [Header("Idle Motion Settings")]
    public float swayAmplitude = 0.5f;
    public float swaySpeed = 1f;

    [Header("Rotation Oscillation Settings")]
    public float rotationAmplitude = 8f;
    public float rotationSpeed = 2f;

    private bool isIdle = false;
    private Vector3 idleOrigin;

    void Update()
    {
        HandleRotationOscillation();

        if (!isIdle)
        {
            if (Mathf.Abs(transform.position.x - targetX) <= tolerance)
            {
                RockMover mover = GetComponent<RockMover>();
                if (mover)
                {
                    mover.speed = 0f;
                    idleOrigin = transform.position;
                    isIdle = true;
                }
                else
                {
                    Debug.LogWarning("SirenSwimmer: No RockMover component found.");
                }
            }
        }
        else
        {
            OscillatePosition();
        }
    }

    void HandleRotationOscillation()
    {
        float xRotation = Mathf.Sin(Time.time * rotationSpeed) * rotationAmplitude;
        Vector3 currentEuler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(xRotation, currentEuler.y, currentEuler.z);
    }

    void OscillatePosition()
    {
        float offset = Mathf.Sin(Time.time * swaySpeed) * swayAmplitude;
        Vector3 pos = transform.position;
        transform.position = new Vector3(idleOrigin.x + offset, pos.y, pos.z);
    }
}