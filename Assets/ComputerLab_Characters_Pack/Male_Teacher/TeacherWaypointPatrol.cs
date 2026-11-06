using UnityEngine;

namespace ComputerLab.Characters
{
    /// <summary>
    /// Controls automatic back-and-forth waypoint walking for the Teacher character.
    /// Moves strictly upright in a straight line, turns 180 degrees smoothly at each end,
    /// and repeats the loop indefinitely without player input.
    /// Compensates for 3D model/rig orientation so the character always faces the direction of travel.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class TeacherWaypointPatrol : MonoBehaviour
    {
        [Header("Waypoints (Optional - auto-generated if unassigned)")]
        [Tooltip("Starting waypoint. If empty, the character's initial position is used.")]
        [SerializeField] private Transform waypointStart;

        [Tooltip("Endpoint waypoint. If empty, a point 'Patrol Distance' units along the patrol heading is used.")]
        [SerializeField] private Transform waypointEnd;

        [Header("Movement Settings")]
        [Tooltip("Walking speed in meters per second.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float walkSpeed = 1.4f;

        [Tooltip("Rotation speed in degrees per second when turning 180 degrees.")]
        [Range(30f, 360f)]
        [SerializeField] private float turnSpeed = 180f;

        [Tooltip("Distance threshold to consider the waypoint reached.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float arrivalDistance = 0.25f;

        [Tooltip("If waypoints are unassigned, how far the teacher walks before turning back.")]
        [SerializeField] private float defaultPatrolDistance = 6.0f;

        [Tooltip("Brief pause (in seconds) after reaching an endpoint before turning back.")]
        [Range(0f, 3f)]
        [SerializeField] private float pauseAtEndpoint = 0.2f;

        [Header("Model Facing Correction")]
        [Tooltip("Yaw offset in degrees to align the 3D model/animation front with the travel direction. Use 180 for characters whose model/bones face -Z (standard for this character pack).")]
        [SerializeField] private float modelFacingOffset = 180f;

        [Header("Ground & Stability")]
        [Tooltip("Lock the teacher's Y position to the initial floor level to prevent falling, floating, or sinking.")]
        [SerializeField] private bool lockToFloorHeight = true;

        [Tooltip("Snap down to floor colliders via raycasting.")]
        [SerializeField] private bool snapToFloorCollider = true;

        [Tooltip("Layers recognized as floor.")]
        [SerializeField] private LayerMask floorLayers = ~0;

        [Header("Animator Parameter Names")]
        [SerializeField] private string isWalkingBoolParam = "IsWalking";
        [SerializeField] private string speedFloatParam = "Speed";

        // Patrol State
        private enum PatrolState
        {
            Walking,
            PausingAtEndpoint,
            Turning
        }

        private PatrolState currentState = PatrolState.Walking;
        private Vector3 startPos;
        private Vector3 endPos;
        private Vector3 targetPos;
        private bool movingTowardsEnd = true;

        private float fixedFloorY;
        private float pauseTimer = 0f;
        private Quaternion targetTurnRotation;
        private Vector3 initialForward;

        private Animator anim;
        private int isWalkingHash;
        private int speedHash;
        private bool hasIsWalking;
        private bool hasSpeed;

        public Transform WaypointStart
        {
            get => waypointStart;
            set { waypointStart = value; RecalculateWaypoints(); }
        }

        public Transform WaypointEnd
        {
            get => waypointEnd;
            set { waypointEnd = value; RecalculateWaypoints(); }
        }

        public float WalkSpeed
        {
            get => walkSpeed;
            set => walkSpeed = Mathf.Max(0.1f, value);
        }

        public float TurnSpeed
        {
            get => turnSpeed;
            set => turnSpeed = Mathf.Max(10f, value);
        }

        public float ModelFacingOffset
        {
            get => modelFacingOffset;
            set => modelFacingOffset = value;
        }

        /// <summary>
        /// Calculates the required Transform rotation so the visual 3D model faces the specified direction.
        /// </summary>
        public Quaternion GetFacingRotation(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return transform.rotation;
            direction.y = 0f;
            return Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(0f, modelFacingOffset, 0f);
        }

        private void Awake()
        {
            anim = GetComponent<Animator>();

            // Disable root motion so script controls movement and orientation deterministically
            if (anim != null)
            {
                anim.applyRootMotion = false;

                // Cache animator parameter hashes
                isWalkingHash = Animator.StringToHash(isWalkingBoolParam);
                speedHash = Animator.StringToHash(speedFloatParam);

                if (anim.runtimeAnimatorController != null)
                {
                    foreach (var p in anim.parameters)
                    {
                        if (p.nameHash == isWalkingHash && p.type == AnimatorControllerParameterType.Bool)
                            hasIsWalking = true;
                        if (p.nameHash == speedHash && p.type == AnimatorControllerParameterType.Float)
                            hasSpeed = true;
                    }
                }
            }

            initialForward = transform.forward;

            // Ensure character stands upright
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

            fixedFloorY = transform.position.y;
            RecalculateWaypoints();
        }

        private void Start()
        {
            // Position character at starting point
            transform.position = new Vector3(startPos.x, GetFloorY(startPos), startPos.z);

            targetPos = endPos;
            movingTowardsEnd = true;

            // Orient model towards the first target (endpoint) so he faces the direction of travel
            Vector3 dir = (endPos - startPos);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = GetFacingRotation(dir);
            }

            currentState = PatrolState.Walking;
            SetAnimatorWalking(true, walkSpeed);
        }

        private void Update()
        {
            switch (currentState)
            {
                case PatrolState.Walking:
                    UpdateWalking();
                    break;

                case PatrolState.PausingAtEndpoint:
                    UpdatePausing();
                    break;

                case PatrolState.Turning:
                    UpdateTurning();
                    break;
            }

            // Maintain strict upright posture (no tilting or falling)
            Vector3 euler = transform.eulerAngles;
            if (Mathf.Abs(euler.x) > 0.01f || Mathf.Abs(euler.z) > 0.01f)
            {
                transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            }
        }

        private void UpdateWalking()
        {
            SetAnimatorWalking(true, walkSpeed);

            Vector3 currentPos = transform.position;
            Vector3 toTarget = targetPos - currentPos;
            toTarget.y = 0f; // Only consider horizontal distance

            float dist = toTarget.magnitude;

            if (dist <= arrivalDistance)
            {
                // Snap horizontally to target to avoid overshoot
                transform.position = new Vector3(targetPos.x, GetFloorY(targetPos), targetPos.z);

                // Switch target for the return trip
                movingTowardsEnd = !movingTowardsEnd;
                targetPos = movingTowardsEnd ? endPos : startPos;

                Vector3 newDir = (targetPos - transform.position);
                newDir.y = 0f;
                if (newDir.sqrMagnitude > 0.001f)
                {
                    targetTurnRotation = GetFacingRotation(newDir);
                }
                else
                {
                    targetTurnRotation = transform.rotation * Quaternion.Euler(0f, 180f, 0f);
                }

                if (pauseAtEndpoint > 0f)
                {
                    pauseTimer = pauseAtEndpoint;
                    currentState = PatrolState.PausingAtEndpoint;
                    SetAnimatorWalking(false, 0f);
                }
                else
                {
                    currentState = PatrolState.Turning;
                    SetAnimatorWalking(false, 0f);
                }
                return;
            }

            // Direction of travel towards target
            Vector3 moveDir = toTarget.normalized;

            // Smoothly align the visual model facing with the travel direction
            Quaternion desiredRot = GetFacingRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRot, turnSpeed * Time.deltaTime);

            // Move straight forward towards target
            Vector3 nextPos = transform.position + moveDir * (walkSpeed * Time.deltaTime);
            nextPos.y = GetFloorY(nextPos);
            transform.position = nextPos;
        }

        private void UpdatePausing()
        {
            pauseTimer -= Time.deltaTime;
            SetAnimatorWalking(false, 0f);

            if (pauseTimer <= 0f)
            {
                currentState = PatrolState.Turning;
            }
        }

        private void UpdateTurning()
        {
            // Smoothly rotate 180 degrees towards next destination
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetTurnRotation, turnSpeed * Time.deltaTime);

            float remainingAngle = Quaternion.Angle(transform.rotation, targetTurnRotation);
            if (remainingAngle < 2.0f)
            {
                transform.rotation = targetTurnRotation;
                currentState = PatrolState.Walking;
                SetAnimatorWalking(true, walkSpeed);
            }
        }

        private void SetAnimatorWalking(bool isWalking, float speedValue)
        {
            if (anim == null) return;

            if (hasIsWalking)
                anim.SetBool(isWalkingHash, isWalking);

            if (hasSpeed)
                anim.SetFloat(speedHash, speedValue);
        }

        private float GetFloorY(Vector3 pos)
        {
            if (snapToFloorCollider)
            {
                // Raycast downwards from slightly above the feet to detect floor collider
                Vector3 rayOrigin = new Vector3(pos.x, pos.y + 0.5f, pos.z);
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 2.0f, floorLayers, QueryTriggerInteraction.Ignore))
                {
                    return hit.point.y;
                }
            }

            return lockToFloorHeight ? fixedFloorY : pos.y;
        }

        public void RecalculateWaypoints()
        {
            if (waypointStart != null)
            {
                startPos = waypointStart.position;
            }
            else
            {
                startPos = transform.position;
            }

            if (waypointEnd != null)
            {
                endPos = waypointEnd.position;
            }
            else
            {
                // In the scene, the teacher was placed oriented along the aisle
                Vector3 heading = (initialForward.sqrMagnitude > 0.001f) ? initialForward : transform.forward;
                heading.y = 0f;
                if (heading.sqrMagnitude < 0.001f) heading = Vector3.forward;
                endPos = startPos + heading.normalized * defaultPatrolDistance;
            }

            startPos.y = fixedFloorY;
            endPos.y = fixedFloorY;
        }

        private void OnDrawGizmos()
        {
            Vector3 heading = (initialForward.sqrMagnitude > 0.001f) ? initialForward : transform.forward;
            Vector3 pStart = waypointStart != null ? waypointStart.position : (Application.isPlaying ? startPos : transform.position);
            Vector3 pEnd = waypointEnd != null ? waypointEnd.position : (Application.isPlaying ? endPos : pStart + heading.normalized * defaultPatrolDistance);

            // Waypoint Spheres
            Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.8f);
            Gizmos.DrawSphere(pStart, 0.25f);
            Gizmos.DrawWireSphere(pStart, 0.35f);

            Gizmos.color = new Color(0.9f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawSphere(pEnd, 0.25f);
            Gizmos.DrawWireSphere(pEnd, 0.35f);

            // Patrol path line
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pStart, pEnd);

            // Direction arrow showing visual model facing
            Vector3 facing = transform.rotation * Quaternion.Euler(0f, -modelFacingOffset, 0f) * Vector3.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, facing * 1.2f);
        }
    }
}
