using UnityEngine;

// Tilted 55-degree camera: follows the player smoothly but never rotates.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float pitch = 55f;      // angle looking down
    public float distance = 9f;    // distance from the player
    public float smoothTime = 0.15f;

    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;

        transform.rotation = Quaternion.Euler(pitch, 0f, 0f); // fixed, no rotation
        Vector3 desired = target.position - transform.forward * distance;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    // Jump straight to the target (used when the game is rebuilt)
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.position = target.position - transform.forward * distance;
        velocity = Vector3.zero;
    }
}
