using UnityEngine;

namespace Nexus
{
    /// <summary>Top-down camera with a slight angle that smoothly follows Pac-Man. Never rotates.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 9f, -4.5f);
        public float smoothTime = 0.18f;

        Vector3 velocity;

        void Start()
        {
            transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
            if (target == null && PlayerMovement.Instance != null) target = PlayerMovement.Instance.transform;
            if (target != null) transform.position = target.position + offset;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                if (PlayerMovement.Instance == null) return;
                target = PlayerMovement.Instance.transform;
            }
            transform.position = Vector3.SmoothDamp(transform.position, target.position + offset, ref velocity, smoothTime);
        }
    }
}
