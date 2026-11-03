using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Angled camera that smoothly follows Pac-Man. Never rotates.
    /// ~63° down and far enough to see at least 5-6 cells in every direction, and it leads a little
    /// in the direction Pac-Man is moving (look-ahead) so you see where you are going.
    /// BuildingFader makes buildings in front of Pac-Man see-through.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        [Range(30f, 89f)] public float pitch = 63f;   // degrees down from horizontal
        public float distance = 11f;
        public float fieldOfView = 55f;
        public float smoothTime = 0.18f;
        [Tooltip("How far (world units) the camera leads in the movement direction")]
        public float lookAhead = 2f;
        public float lookAheadTime = 0.45f;

        Vector3 velocity, lead, leadVelocity;

        public Vector3 Offset => Quaternion.Euler(pitch, 0f, 0f) * new Vector3(0f, 0f, -distance);

        void Start()
        {
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            if (TryGetComponent(out Camera cam)) cam.fieldOfView = fieldOfView;
            if (target == null && PlayerMovement.Instance != null) target = PlayerMovement.Instance.transform;
            if (target != null) transform.position = target.position + Offset;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                if (PlayerMovement.Instance == null) return;
                target = PlayerMovement.Instance.transform;
            }

            // Lead toward the movement direction (grid y grows toward the camera, so world z = -y)
            var player = PlayerMovement.Instance;
            if (player != null && player.IsMoving)
            {
                var d = player.Direction;
                var wantLead = new Vector3(d.x, 0f, -d.y) * lookAhead;
                lead = Vector3.SmoothDamp(lead, wantLead, ref leadVelocity, lookAheadTime);
            }

            transform.position = Vector3.SmoothDamp(transform.position, target.position + lead + Offset, ref velocity, smoothTime);
        }
    }
}
