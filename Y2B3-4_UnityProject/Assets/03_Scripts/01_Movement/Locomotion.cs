using System.Collections;

using UnityEngine;
using static Unity.Mathematics.math;

using EasyCharacterMovement;
using JetBrains.Annotations;
using UltEvents;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using static ProjectDawn.Mathematics.math2;

using Component = Game.Shared.Component;

using F32   = System.Single;
using F32x2 = Unity.Mathematics.float2;
using F32x3 = Unity.Mathematics.float3;

namespace Game.Movement
{
    using Game.Inputs;
    
    public sealed class Locomotion : Component
    {
        #region Variables
        
        [Tooltip(tooltip: "The Player following camera.")]
        [SerializeField] private Camera playerCamera;

        [Space(height: 15f)]
        [Tooltip(tooltip: "The character's maximum speed. (m/s)")]
        #if ODIN_INSPECTOR
        [SuffixLabel(label: "m/s", overlay: true)]
        #endif
        [SerializeField] private F32 maxSpeed = 5.0f;

        [Tooltip(tooltip: "Max Acceleration (rate of change of velocity).")]
        [SerializeField] private F32 maxAcceleration = 20.0f;

        [Tooltip(tooltip: "Setting that affects movement control. Higher values allow faster changes in direction.")]
        [SerializeField] private F32 groundFriction = 8.0f;

        [Tooltip(tooltip: "Friction to apply when falling.")]
        [SerializeField] private F32 airFriction = 0.1f;

        [Range(min: 0.0f, max: 1.0f)]
        [Tooltip(tooltip: "When falling, amount of horizontal movement control available to the character.\n" +
                          "0 = no control, 1 = full control at max acceleration.")]
        [SerializeField] private F32 airControl = 0.3f;

        [Tooltip(tooltip: "The character's gravity.")]
        [SerializeField] private F32x3 gravity = new F32x3(x: 0, y: -20, z: 0);

        private Coroutine _lateFixedUpdateCoroutine;

        /// <summary> Cached InputHandler component. </summary>
        [SerializeField, HideInInspector] private InputHandler inputHandler;
        /// <summary> Cached CharacterMovement component. </summary>
        [SerializeField, HideInInspector] private CharacterMotor motor;

        #endregion

        #region Events

        #if ODIN_INSPECTOR
        [field: FoldoutGroup(groupName: "Events", expanded: false)]
        #endif
        [field: SerializeField] public UltEvent OnLanded { get; [UsedImplicitly] private set; } = new UltEvent();
        
        #if ODIN_INSPECTOR
        [field: FoldoutGroup(groupName: "Events", expanded: false)]
        #endif
        [field: SerializeField] public UltEvent<F32x3> OnMove { get; [UsedImplicitly] private set; } = new UltEvent<F32x3>();

        #endregion

        #region EVENT HANDLERS

        /// <summary>
        /// Collided event handler.
        /// </summary>
        private void OnCollided(ref CollisionResult inHit)
        {
            //Debug.Log(message: $"{name} collided with: {inHit.collider.name}");
        }

        /// <summary>
        /// FoundGround event handler.
        /// </summary>

        private void OnFoundGround(ref FindGroundResult foundGround)
        {
            //Debug.Log(message: "Found ground...");

            // Determine if the character has landed
            if (!motor.wasOnGround && foundGround.isWalkableGround)
            {
                OnLanded?.Invoke();
            }
        }

        #endregion

        #region Methods

        #if UNITY_EDITOR
        private void Reset()
        {
            playerCamera = Camera.main;

            inputHandler = GetComponent<InputHandler>();

            // Cache CharacterMovement component
            motor = GetComponent<CharacterMotor>();

            // Enable default physic interactions
            motor.enablePhysicsInteraction = true;
        }

        private void OnValidate()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
            
            if (inputHandler == null)
            {
                inputHandler = GetComponent<InputHandler>();
            }
            
            if (motor == null)
            {
                // Cache CharacterMovement component
                motor = GetComponent<CharacterMotor>();

                // Enable default physic interactions
                motor.enablePhysicsInteraction = true;
            }
        }
        #endif

        private void OnEnable()
        {
            EnableLateFixedUpdate();

            // Subscribe to CharacterMovement events
            motor.FoundGround += OnFoundGround;
            motor.Collided    += OnCollided;
        }

        private void EnableLateFixedUpdate()
        {
            if (_lateFixedUpdateCoroutine != null)
            {
                StopCoroutine(routine: _lateFixedUpdateCoroutine);
            }
            _lateFixedUpdateCoroutine = StartCoroutine(LateFixedUpdate());
        }

        private void OnDisable()
        {
            DisableLateFixedUpdate();

            // Un-Subscribe from CharacterMovement events
            motor.FoundGround -= OnFoundGround;
            motor.Collided    -= OnCollided;
        }

        private void DisableLateFixedUpdate()
        {
            if (_lateFixedUpdateCoroutine != null)
            {
                StopCoroutine(routine: _lateFixedUpdateCoroutine);
            }
        }

        private readonly WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();
        private IEnumerator LateFixedUpdate()
        {
            while (true)
            {
                yield return _waitForFixedUpdate;

                OnLateFixedUpdate();
            }
        }

        /// <summary>
        /// Move the character when on walkable ground.
        /// </summary>

        private void GroundedMovement(Vector3 desiredVelocity)
        {
            motor.velocity = Vector3.Lerp(a: motor.velocity, b: desiredVelocity,
                t: 1f - Mathf.Exp(power: -groundFriction * Time.deltaTime));
        }

        /// <summary>
        /// Move the character when falling or on not-walkable ground.
        /// </summary>

        private void NotGroundedMovement(F32x3 desiredVelocity)
        {
            // Current character's velocity

            F32x3 __velocity = motor.velocity;

            // If moving into non-walkable ground, limit its contribution.
            // Allow movement parallel, but not into it because that may push us up.
            if (motor.isOnGround && dot(desiredVelocity, motor.groundNormal) < 0.0f)
            {
                F32x3 __groundNormal   = motor.groundNormal;

                F32x3 __planeNormal = normalize(new F32x3(x: __groundNormal.x, y: 0, z: __groundNormal.y));

                desiredVelocity = desiredVelocity.ProjectedOnPlane(planeNormal: __planeNormal);
            }

            // If moving...
            if (all(desiredVelocity != F32x3.zero))
            {
                F32x3 __flatVelocity = new F32x3(x: __velocity.x, y: 0,            z: __velocity.z);
                F32x3 __verVelocity   = new F32x3(x: 0,            y: __velocity.y, z: 0);

                // Accelerate horizontal velocity towards desired velocity
                F32x3 __horizontalVelocity = Vector3.MoveTowards(
                    current: __flatVelocity, 
                    target: desiredVelocity,
                    maxDistanceDelta: maxAcceleration * airControl * Time.deltaTime);

                // Update velocity preserving gravity effects (vertical velocity)
                __velocity = __horizontalVelocity + __verVelocity;
            }

            // Apply gravity
            __velocity += gravity * Time.deltaTime;

            // Apply Air friction (Drag)
            __velocity -= __velocity * airFriction * Time.deltaTime;

            // Update character's velocity
            motor.velocity = __velocity;
        }

        /// <summary>
        /// Perform character movement.
        /// </summary>
        private void Move()
        {
            // Create a Movement direction vector (in world space)
            F32x3 __moveDirection = F32x3.zero;
            __moveDirection.xz = inputHandler.MoveInput;
            
            // Make Sure it won't move faster diagonally
            __moveDirection.SetMaxLength(1.0f);
            
            // Make movementDirection relative to camera view direction
            F32x3 __moveDirectionRelativeToCamera = __moveDirection.RelativeTo(playerCamera.transform);
            
            Vector3 __desiredVelocity = (__moveDirectionRelativeToCamera * maxSpeed);

            // Update character’s velocity based on its grounding status
            if (motor.isGrounded)
            {
                GroundedMovement(desiredVelocity: __desiredVelocity);
            }
            else
            {
                NotGroundedMovement(desiredVelocity: __desiredVelocity);
            }
            
            OnMove?.Invoke(__moveDirection);
            
            // Perform movement using character's current velocity
            motor.Move();
        }

        /// <summary>
        /// Post-Physics update, used to sync our character with physics.
        /// </summary>
        private void OnLateFixedUpdate()
        {
            //UpdateRotation();
            Move();
        }

        #endregion
    }
}
