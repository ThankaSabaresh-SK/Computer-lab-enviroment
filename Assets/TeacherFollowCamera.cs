using UnityEngine;

/// <summary>
/// Third-person follow camera for Male_Teacher in the computer laboratory.
/// Follows smoothly behind the character based on his facing direction,
/// frames the upper body, and avoids clipping through walls and lab obstacles.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class TeacherFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Target character Transform to follow (Male_Teacher). If unassigned, automatically finds Male_Teacher.")]
    public Transform target;

    [Header("Distance & Framing")]
    [Tooltip("Horizontal distance behind the teacher in meters.")]
    [Range(1.5f, 7f)]
    public float distance = 3.5f;

    [Tooltip("Camera height above the teacher's feet in meters.")]
    [Range(1f, 3.5f)]
    public float height = 1.8f;

    [Tooltip("Height of the focus point on the teacher (frames upper body/chest instead of feet).")]
    [Range(0.5f, 2.2f)]
    public float targetHeightOffset = 1.35f;

    [Tooltip("Model facing offset in degrees. Set to 180 for characters whose rig/bones face -Z.")]
    [Range(0f, 360f)]
    public float characterFacingOffset = 180f;

    [Header("Smoothness")]
    [Tooltip("Smooth time for position following in seconds.")]
    [Range(0.01f, 0.5f)]
    public float positionSmoothTime = 0.12f;

    [Tooltip("Smooth time for camera rotation/orbit in seconds.")]
    [Range(0.01f, 0.5f)]
    public float rotationSmoothTime = 0.18f;

    [Header("Obstacle Collision Avoidance")]
    [Tooltip("Prevent the camera from clipping inside walls, tables, and obstacles.")]
    public bool enableCollision = true;

    [Tooltip("Layers to consider obstacles for camera collision (default: all layers).")]
    public LayerMask collisionLayers = ~0;

    [Tooltip("Sphere cast radius for collision checking.")]
    [Range(0.05f, 0.5f)]
    public float collisionRadius = 0.2f;

    [Tooltip("Padding distance to maintain between camera and collided obstacles.")]
    [Range(0.05f, 0.5f)]
    public float collisionPadding = 0.15f;

    [Tooltip("Minimum allowable distance from the teacher when obstructed.")]
    [Range(0.3f, 2f)]
    public float minDistance = 0.8f;

    private float currentYaw;
    private float yawVelocity;
    private Vector3 positionVelocity;

    private void Awake()
    {
        // Auto-detect target if not explicitly assigned
        if (target == null)
        {
            var teacher = GameObject.Find("Male_Teacher");
            if (teacher != null)
            {
                target = teacher.transform;
            }
            else
            {
                var wasd = Object.FindAnyObjectByType<TeacherWASDController>();
                if (wasd != null)
                {
                    target = wasd.transform;
                }
            }
        }

        // Disable competing FreeCameraController if present on the Main Camera
        var freeCam = GetComponent<FreeCameraController>();
        if (freeCam != null && freeCam.enabled)
        {
            freeCam.enabled = false;
            Debug.Log("[TeacherFollowCamera] Disabled FreeCameraController to prevent conflicting camera controls.");
        }
    }

    private void Start()
    {
        if (target != null)
        {
            // Initialize yaw and snap immediately behind target on start to avoid initial camera travel jump
            currentYaw = target.eulerAngles.y + characterFacingOffset;
            Vector3 focusPoint = target.position + Vector3.up * targetHeightOffset;
            Vector3 backDir = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.back;
            Vector3 startPos = focusPoint + backDir * distance + Vector3.up * (height - targetHeightOffset);

            if (enableCollision)
            {
                startPos = ResolveCollision(focusPoint, startPos);
            }

            transform.position = startPos;
            transform.LookAt(focusPoint);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculate focus point on the character's upper body
        Vector3 focusPoint = target.position + Vector3.up * targetHeightOffset;

        // Smoothly interpolate the camera's yaw angle to match the teacher's facing direction
        float targetYaw = target.eulerAngles.y + characterFacingOffset;
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);

        // Compute desired camera position behind the character
        Vector3 backDirection = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.back;
        Vector3 desiredPosition = focusPoint + backDirection * distance + Vector3.up * (height - targetHeightOffset);

        // Smoothly interpolate camera position
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

        // Prevent camera from clipping through walls/obstacles
        if (enableCollision)
        {
            Vector3 clearPosition = ResolveCollision(focusPoint, smoothedPosition);

            // While an obstacle holds the camera the smoothing builds up outward speed it cannot use,
            // then releases it all at once. Clearing only that part leaves the sideways follow as it was.
            if (clearPosition != smoothedPosition)
            {
                Vector3 blockedDirection = (smoothedPosition - focusPoint).normalized;
                positionVelocity -= blockedDirection * Mathf.Max(Vector3.Dot(positionVelocity, blockedDirection), 0f);
            }

            smoothedPosition = clearPosition;
        }

        transform.position = smoothedPosition;

        // Always smoothly frame the upper body focus point
        Vector3 lookDir = focusPoint - transform.position;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    private Vector3 ResolveCollision(Vector3 fromFocus, Vector3 desiredPosition)
    {
        Vector3 castRay = desiredPosition - fromFocus;
        float maxDistance = castRay.magnitude;

        if (maxDistance <= 0.001f) return desiredPosition;

        Vector3 castDirection = castRay / maxDistance;

        // Reach one padding past the camera. The camera rests that far short of an obstacle,
        // so a shorter cast loses the obstacle every other frame and the camera shakes.
        float castDistance = maxDistance + collisionPadding;

        RaycastHit[] hits = Physics.SphereCastAll(
            fromFocus,
            collisionRadius,
            castDirection,
            castDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestValidDistance = castDistance;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            // Ignore hits on the target character or its children
            if (hit.transform == target || hit.transform.IsChildOf(target))
                continue;

            // Ignore triggers or zero-distance hits
            if (hit.distance < 0.001f)
                continue;

            if (hit.distance < closestValidDistance)
            {
                closestValidDistance = hit.distance;
            }
        }

        if (closestValidDistance < castDistance)
        {
            // The minimum distance only applies while it keeps the camera on the near side of the obstacle
            float nearestAllowed = Mathf.Min(minDistance, closestValidDistance + collisionPadding);
            float safeDistance = Mathf.Clamp(closestValidDistance - collisionPadding, nearestAllowed, maxDistance);
            return fromFocus + castDirection * safeDistance;
        }

        return desiredPosition;
    }
}
