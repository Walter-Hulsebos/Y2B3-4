using System.Collections;
using System.Collections.Generic;
using EasyCharacterMovement;
using UnityEngine;

namespace Game.Movement
{
    public sealed class Locomotion : MonoBehaviour
    {
        #region Variables
        
        [Tooltip("The Player following camera.")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Change in rotation per second (Deg / s).")]
        [SerializeField] private float rotationRate = 540.0f;

        [Space(15f)]
        [Tooltip("The character's maximum speed.")]
        [SerializeField] private float maxSpeed = 5.0f;

        [Tooltip("Max Acceleration (rate of change of velocity).")]
        [SerializeField] private float maxAcceleration = 20.0f;

        [Tooltip("Setting that affects movement control. Higher values allow faster changes in direction.")]
        [SerializeField] private float groundFriction = 8.0f;

        [Space(15f)]
        [Tooltip("Initial velocity (instantaneous vertical velocity) when jumping.")]
        [SerializeField] private float jumpImpulse = 6.5f;

        [Tooltip("Friction to apply when falling.")]
        [SerializeField] private float airFriction = 0.1f;

        [Range(0.0f, 1.0f)]
        [Tooltip("When falling, amount of horizontal movement control available to the character.\n" +
                 "0 = no control, 1 = full control at max acceleration.")]
        [SerializeField] private float airControl = 0.3f;

        [Tooltip("The character's gravity.")]
        [SerializeField] private Vector3 gravity = Vector3.down * 9.81f;

        [Space(15f)]
        [Tooltip("Character's height when standing.")]
        [SerializeField] private float standingHeight = 2.0f;

        private Coroutine _lateFixedUpdateCoroutine;
        
        /// <summary>
        /// Cached CharacterMovement component.
        /// </summary>
        public CharacterMotor CharacterMotor { get; private set; }

        /// <summary>
        /// Desired movement direction vector in world-space.
        /// </summary>
        public Vector3 movementDirection { get; set; }

        /// <summary>
        /// Dash input.
        /// </summary>
        public bool dash { get; set; }
        
        #endregion

        #region EVENT HANDLERS

        /// <summary>
        /// Collided event handler.
        /// </summary>

        private void OnCollided(ref CollisionResult inHit)
        {
            Debug.Log($"{name} collided with: {inHit.collider.name}");
        }

        /// <summary>
        /// FoundGround event handler.
        /// </summary>

        private void OnFoundGround(ref FindGroundResult foundGround)
        {
            Debug.Log("Found ground...");

            // Determine if the character has landed

            if (!CharacterMotor.wasOnGround && foundGround.isWalkableGround)
            {
                Debug.Log("Landed!");
            }
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Handle Player input.
        /// </summary>

        private void HandleInput()
        {
            // Read Input values

            float horizontal = Input.GetAxisRaw($"Horizontal");
            float vertical = Input.GetAxisRaw($"Vertical");

            // Create a Movement direction vector (in world space)

            movementDirection = Vector3.zero;

            movementDirection += Vector3.forward * vertical;
            movementDirection += Vector3.right * horizontal;

            // Make Sure it won't move faster diagonally

            movementDirection = Vector3.ClampMagnitude(movementDirection, 1.0f);

            // Make movementDirection relative to camera view direction
            movementDirection = movementDirection.relativeTo(playerCamera.transform);

            // Jump input
            dash = Input.GetButton($"Jump");
        }

        /// <summary>
        /// Update the character's rotation.
        /// </summary>

        private void UpdateRotation()
        {
            // Rotate towards character's movement direction

            CharacterMotor.RotateTowards(movementDirection, rotationRate * Time.deltaTime);
        }

        /// <summary>
        /// Move the character when on walkable ground.
        /// </summary>

        private void GroundedMovement(Vector3 desiredVelocity)
        {
            CharacterMotor.velocity = Vector3.Lerp(CharacterMotor.velocity, desiredVelocity,
                1f - Mathf.Exp(-groundFriction * Time.deltaTime));
        }

        /// <summary>
        /// Move the character when falling or on not-walkable ground.
        /// </summary>

        private void NotGroundedMovement(Vector3 desiredVelocity)
        {
            // Current character's velocity

            Vector3 velocity = CharacterMotor.velocity;

            // If moving into non-walkable ground, limit its contribution.
            // Allow movement parallel, but not into it because that may push us up.
            
            if (CharacterMotor.isOnGround && Vector3.Dot(desiredVelocity, CharacterMotor.groundNormal) < 0.0f)
            {
                Vector3 groundNormal = CharacterMotor.groundNormal;
                Vector3 groundNormal2D = groundNormal.onlyXZ().normalized;

                desiredVelocity = desiredVelocity.projectedOnPlane(groundNormal2D);
            }

            // If moving...

            if (desiredVelocity != Vector3.zero)
            {
                // Accelerate horizontal velocity towards desired velocity

                Vector3 horizontalVelocity = Vector3.MoveTowards(velocity.onlyXZ(), desiredVelocity,
                    maxAcceleration * airControl * Time.deltaTime);

                // Update velocity preserving gravity effects (vertical velocity)
                
                velocity = horizontalVelocity + velocity.onlyY();
            }

            // Apply gravity

            velocity += gravity * Time.deltaTime;

            // Apply Air friction (Drag)

            velocity -= velocity * airFriction * Time.deltaTime;

            // Update character's velocity

            CharacterMotor.velocity = velocity;
        }

        /// <summary>
        /// Handle jumping state.
        /// </summary>
        private void Jumping()
        {
            if (dash && CharacterMotor.isGrounded)
            {
                // Pause ground constraint so character can jump off ground

                CharacterMotor.PauseGroundConstraint();

                // perform the jump

                Vector3 jumpVelocity = Vector3.up * jumpImpulse;

                CharacterMotor.LaunchCharacter(jumpVelocity, true);
            }
        }

        /// <summary>
        /// Perform character movement.
        /// </summary>

        private void Move()
        {
            Vector3 desiredVelocity = movementDirection * maxSpeed;

            // Update character’s velocity based on its grounding status

            if (CharacterMotor.isGrounded)
            {
                GroundedMovement(desiredVelocity);
            }
            else
            {
                NotGroundedMovement(desiredVelocity);
            }

            // Handle jumping state

            Jumping();

            // Perform movement using character's current velocity

            CharacterMotor.Move();
        }

        /// <summary>
        /// Post-Physics update, used to sync our character with physics.
        /// </summary>

        private void OnLateFixedUpdate()
        {
            UpdateRotation();
            Move();
        }

        #endregion

        #region MONOBEHAVIOR

        private void Awake()
        {
            // Cache CharacterMovement component

            CharacterMotor = GetComponent<CharacterMotor>();

            // Enable default physic interactions

            CharacterMotor.enablePhysicsInteraction = true;
        }

        private void OnEnable()
        {
            // Start LateFixedUpdate coroutine

            if (_lateFixedUpdateCoroutine != null)
                StopCoroutine(_lateFixedUpdateCoroutine);

            _lateFixedUpdateCoroutine = StartCoroutine(LateFixedUpdate());

            // Subscribe to CharacterMovement events

            CharacterMotor.FoundGround += OnFoundGround;
            CharacterMotor.Collided += OnCollided;
        }

        private void OnDisable()
        {
            // Ends LateFixedUpdate coroutine

            if (_lateFixedUpdateCoroutine != null)
                StopCoroutine(_lateFixedUpdateCoroutine);

            // Un-Subscribe from CharacterMovement events

            CharacterMotor.FoundGround -= OnFoundGround;
            CharacterMotor.Collided -= OnCollided;
        }

        private IEnumerator LateFixedUpdate()
        {
            WaitForFixedUpdate waitTime = new WaitForFixedUpdate();

            while (true)
            {
                yield return waitTime;

                OnLateFixedUpdate();
            }
        }

        private void Update()
        {
            HandleInput();
        }

        #endregion
    }
}
