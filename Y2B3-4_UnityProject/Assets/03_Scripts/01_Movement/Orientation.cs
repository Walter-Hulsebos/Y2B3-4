using System;

using UnityEngine;
using static Unity.Mathematics.math;

using EasyCharacterMovement;
using ProjectDawn.Geometry3D;

using F32   = System.Single;
using F32x2 = Unity.Mathematics.float2;
using F32x3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;
using Ray = UnityEngine.Ray;

namespace Game.Movement
{
    using Game.Inputs;
    using Component  = Game.Shared.Component;
    using Extensions = Game.Utils.Extensions;
    
    public sealed class Orientation : Component
    {
        #region Variables

        [SerializeField] private F32x3 aimOffset           = new F32x3(x: 0, y: 1, z: 0.75f);
        [SerializeField] private F32   minAimingDistance   = 2f;
        [SerializeField] private F32   minAimingDistanceSq = 2f * 2f;
        [SerializeField] private F32   maxAimingDistance   = 20f;
        //[SerializeField] private F32   maxAimingDistanceSq = 20f * 20f;
        [SerializeField] private F32   orientationSpeed    = 15f;
        
        /// <summary> Cached InputHandler component. </summary>
        [SerializeField, HideInInspector] private InputHandler inputHandler;
        /// <summary> Cached CharacterMovement component. </summary>
        [SerializeField, HideInInspector] private CharacterMotor motor;
        /// <summary> Cached Camera Transform. </summary>
        [SerializeField, HideInInspector] private new Camera camera;
        
        #endregion

        #region Methods

        #if UNITY_EDITOR
        private void Reset()
        {
            minAimingDistanceSq = minAimingDistance * minAimingDistance;
            //maxAimingDistanceSq = maxAimingDistance * maxAimingDistance;
            
            FindInputHandlerReference();
            
            FindMotorReference();

            FindCameraReference();
        }

        private void OnValidate()
        {
            minAimingDistanceSq = minAimingDistance * minAimingDistance;
            //maxAimingDistanceSq = maxAimingDistance * maxAimingDistance;
            
            if (inputHandler == null)
            {
                FindInputHandlerReference();
            }
            
            if (motor == null)
            {
                FindMotorReference();
            }
            
            if (camera == null)
            {
                FindCameraReference();
            }
        }

        private void FindInputHandlerReference()
        {
            inputHandler = GetComponent<InputHandler>();
        }
        
        private void FindMotorReference()
        {
            // Cache CharacterMovement component
            motor = GetComponent<CharacterMotor>();

            // Enable default physic interactions
            motor.enablePhysicsInteraction = true;
        }

        private void FindCameraReference()
        {
            Camera __mainCamera = Camera.main;
            if(__mainCamera == null)
            {
                Boolean __foundUnTaggedCamera = Extensions.TryFindObjectOfType(out __mainCamera);
                if (__foundUnTaggedCamera)
                {
                    Debug.LogWarning(message: "There was a Camera found in the scene, but it's not tagged as \"MainCamera\", if there is supposed to be one, tag it correctly.", context: this);
                    camera = __mainCamera;
                }
                else
                {
                    Debug.LogError(message: $"No Camera found in the scene. Please add one to the scene. and Reset this {nameof(Orientation)}", context: this);
                }
            }
            else
            {
                camera = __mainCamera;
            }
        }

        private void OnDrawGizmos()
        {
            DrawAimingGizmos();    
        }
        #endif
        
        /// <summary>
        /// Draw gizmos visualising the point to aim at in the scene view
        /// </summary>
        private void DrawAimingGizmos()
        {
            F32x3 __aimOrigin = AimOrigin;
            F32x3 __aimPoint  = AimPoint;

            Gizmos.color = Color.red;
            Debug.DrawLine(start: __aimOrigin, end: __aimPoint, color: Color.red);
            Gizmos.DrawWireSphere(center: __aimPoint, radius: 0.2f);
        }

        
        private F32x3 _cachedLookPosition = F32x3.zero;
        private F32x3 LookPosition
        {
            get
            {
                //Get Mouse Position Screen-Space
                Vector3 __mouseScreenPosition = inputHandler.MouseScreenPosition;
                
                //Create ray from the camera to the mouse position
                Ray __ray = camera.ScreenPointToRay(pos: __mouseScreenPosition);
                
                //Cast ray to the ground plane
                Plane __groundPlane3D = new Plane(inNormal: Vector3.up, d: 0);

                Boolean __rayHasHit = __groundPlane3D.Raycast(ray: __ray, enter: out F32 __hitDistance);
                
                Debug.DrawRay(start: __ray.origin, dir: __ray.direction * min(__hitDistance, 10), Color.yellow);
                
                if (__rayHasHit)
                {
                    _cachedLookPosition = __ray.GetPoint(distance: __hitDistance);
                }

                return _cachedLookPosition;
            }
        }

        private void Update()
        {
            OrientTowards(LookPosition);
        }

        /// <summary>
        /// Returns the point to aim at. A raycast is shot from the aim origin in the forward direction of the player. The hit point is used as the point to aim at.
        /// </summary>
        private F32x3 AimPoint
        {
            get
            {
                F32x3 __aimOrigin = AimOrigin;

                if (!Physics.Raycast(origin: __aimOrigin, direction: Forward, hitInfo: out RaycastHit __hitInfo, maxDistance: maxAimingDistance))
                {
                    return (AimOrigin + Forward * maxAimingDistance);
                }
                
                F32x3 __hitPoint = __hitInfo.point;
                
                F32   __distanceToHitSq = distancesq(__aimOrigin, __hitPoint);

                Boolean __isPastMinAimingDistance = (__distanceToHitSq >= minAimingDistanceSq);

                if (__isPastMinAimingDistance)
                {
                    return __hitPoint;
                }

                return (AimOrigin + Forward * minAimingDistance);
            }
        }
        
        /// <summary>
        /// The origin point used to determine the point to aim at
        /// </summary>
        /// <returns></returns>
        private F32x3 AimOrigin
        {
            get
            {
                F32x3 __offset = (Right * aimOffset.x) + (Up * aimOffset.y) + (Forward * aimOffset.z);

                return WorldPos + __offset;   
            }
        }
        
        /// <summary>
        /// Orients the player towards the given position
        /// </summary>
        /// <param name="lookPosition"></param>
        public void OrientTowards(F32x3 lookPosition)
        {
            Plane3D __plane3D = new Plane3D(normal: up(), distance: 0);
            
            F32x3 __lookDirection = (lookPosition - (F32x3)transform.position);
            
            F32x3 __projectedLookDirection = normalize(__plane3D.Projection(point: __lookDirection));
            
            //if(lengthsq(__projectedLookDirection) == 0) return;
            if (all(__projectedLookDirection == F32x3.zero)) return;
            
            quaternion __targetRotation = quaternion.LookRotation(forward: __projectedLookDirection, up: up());

            transform.rotation = slerp(q1: transform.rotation, q2: __targetRotation, t: orientationSpeed * Time.deltaTime);
            
            //motor.RotateTowards(worldDirection: MovementDirection, maxDegreesDelta: rotationRate * Time.deltaTime);
        }
        
        #endregion
        
    }
}
