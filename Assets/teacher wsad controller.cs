
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class TeacherWASDController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 1.6f;
    public float runSpeed = 3f;
    public float walkStartTime = 0.15f;

    [Header("Rotation")]
    public float modelFacingOffset = 180f;
    public float turnAngle = 90f;
    public float turnSpeed = 180f;

    [Header("Gravity")]
    public float gravity = -9.81f;
    public float groundedDownwardForce = -2f;

    [Header("Animator Parameters")]
    public string isWalkingParam = "IsWalking";
    public string speedParam = "Speed";
    public string talkingParam = "Talking";

    [Header("Animation")]
    public float baseStrideSpeed = 1.5f;

    private CharacterController controller;
    private Animator anim;

    private Vector3 verticalVelocity;
    private float walkStartBlend;

    private bool sWasHeld;
    private bool isTurningAround;

    private float turnProgress;
    private float currentTurnAngle;
    private Quaternion turnStartRotation;

    private int isWalkingHash;
    private int speedHash;
    private int talkingHash;

    private bool hasIsWalking;
    private bool hasSpeed;
    private bool hasTalking;

    private int walkStateHash;
    private bool hasWalkState;

    private bool isSteppingBackward;
    private float backwardPhase;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        anim = GetComponent<Animator>();

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (anim != null)
        {
            anim.applyRootMotion = false;

            isWalkingHash = Animator.StringToHash(isWalkingParam);
            speedHash = Animator.StringToHash(speedParam);
            talkingHash = Animator.StringToHash(talkingParam);

            walkStateHash = Animator.StringToHash("Walk");
            hasWalkState = anim.HasState(0, walkStateHash);

            foreach (var param in anim.parameters)
            {
                if (param.name == isWalkingParam)
                    hasIsWalking = true;

                if (param.name == speedParam)
                    hasSpeed = true;

                if (param.name == talkingParam)
                    hasTalking = true;
            }

            if (hasTalking)
                anim.SetBool(talkingHash, false);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        bool wHeld = keyboard.wKey.isPressed;
        bool sHeld = keyboard.sKey.isPressed;
        bool aHeld = keyboard.aKey.isPressed;
        bool dHeld = keyboard.dKey.isPressed;

        bool sPressed = sHeld && !sWasHeld;
        sWasHeld = sHeld;

        float walkDirection = 0f;

        // S: Start one smooth 180-degree turn.
        if (sPressed && !isTurningAround)
        {
            isTurningAround = true;
            turnProgress = 0f;
            currentTurnAngle = 0f;
            turnStartRotation = transform.rotation;
        }

        // S turn takes priority over A and D.
        if (isTurningAround)
        {
            turnProgress += turnSpeed * 2f * Time.deltaTime;

            float angle = Mathf.Min(turnProgress, 180f);

            transform.rotation =
                turnStartRotation *
                Quaternion.Euler(0f, angle, 0f);

            if (angle >= 180f)
            {
                isTurningAround = false;
            }
        }
        else
        {
            // A and D: Smooth turning while held.
            float turnInput = 0f;

            if (aHeld)
                turnInput -= 1f;

            if (dHeld)
                turnInput += 1f;

            if (turnInput != 0f)
            {
                transform.Rotate(
                    Vector3.up,
                    turnInput * turnSpeed * Time.deltaTime,
                    Space.World
                );
            }

            // Turning stops walking.
            if (turnInput == 0f)
            {
                if (wHeld)
                    walkDirection = 1f;
                else if (sHeld)
                    walkDirection = 1f;
            }
        }

        bool wantsToWalk = walkDirection != 0f;

        float currentSpeed = wantsToWalk
            ? (keyboard.leftShiftKey.isPressed ? runSpeed : walkSpeed)
            : 0f;

        walkStartBlend = wantsToWalk
            ? Mathf.MoveTowards(
                walkStartBlend,
                1f,
                Time.deltaTime / Mathf.Max(walkStartTime, 0.01f)
            )
            : 0f;

        // Use the actual visible model-facing direction.
        Vector3 facing =
            transform.rotation *
            Quaternion.Euler(0f, -modelFacingOffset, 0f) *
            Vector3.forward;

        Vector3 horizontalMovement =
            facing * walkDirection * currentSpeed * walkStartBlend;

        // Gravity.
        if (controller.isGrounded)
        {
            if (verticalVelocity.y < 0f)
                verticalVelocity.y = groundedDownwardForce;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        Vector3 displacement =
            (horizontalMovement + verticalVelocity) * Time.deltaTime;

        controller.Move(displacement);

        Vector3 travelled = controller.velocity;
        travelled.y = 0f;

        bool isWalking =
            wantsToWalk && travelled.sqrMagnitude > 0.01f;

        UpdateAnimator(
            isWalking,
            isWalking ? currentSpeed : 0f,
            false
        );
    }

    private void UpdateAnimator(
        bool isMoving,
        float currentSpeed,
        bool walkingBackward)
    {
        if (anim == null)
            return;

        if (hasIsWalking)
            anim.SetBool(isWalkingHash, isMoving);

        if (hasSpeed)
            anim.SetFloat(speedHash, currentSpeed);

        if (hasTalking && isMoving)
            anim.SetBool(talkingHash, false);

        if (isMoving && baseStrideSpeed > 0.01f)
        {
            float playbackRate = Mathf.Clamp(
                currentSpeed / baseStrideSpeed,
                0.75f,
                1.8f
            );

            anim.speed = playbackRate;
            isSteppingBackward = false;
        }
        else
        {
            anim.speed = 1f;
            isSteppingBackward = false;
        }
    }
}


