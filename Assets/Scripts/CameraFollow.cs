// DISABLED: classic Pac-Man prototype (Step 1). Replaced by PAC-MAN: NEXUS in Assets/Scripts/Core, Player, World, UI.
// A copy is in Demo_Backup/ClassicStep1/. To turn it back on, add PACMAN_CLASSIC to Scripting Define Symbols.
#if PACMAN_CLASSIC
using UnityEngine;

// Kamera e pjerrët 55°: ndjek Pac-Man butë, por nuk rrotullohet kurrë.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float pitch = 55f;       // këndi poshtë
    public float distance = 13f;    // largësia nga Pac-Man
    public float smoothTime = 0.2f;

    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;
        transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        Vector3 desired = target.position - transform.forward * distance;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    // Kërcen menjëherë te Pac-Man (në fillim dhe pas rifillimit)
    public void SnapToTarget()
    {
        if (target == null) return;
        transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.position = target.position - transform.forward * distance;
        velocity = Vector3.zero;
    }
}

#endif
