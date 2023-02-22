using System;
using Drawing;
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

        //[SerializeField] private F32x3 aimOffset           = new F32x3(x: 0, y: 1, z: 0.75f);
        //[SerializeField] private F32   minAimingDistance   = 2f;
        //[SerializeField] private F32   minAimingDistanceSq = 2f * 2f;
        //[SerializeField] private F32   maxAimingDistance   = 20f;
        //[SerializeField] private F32   maxAimingDistanceSq = 20f * 20f;
        [SerializeField] private F32   orientationSpeed    = 25f;
        
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
            //minAimingDistanceSq = minAimingDistance * minAimingDistance;
            //maxAimingDistanceSq = maxAimingDistance * maxAimingDistance;
            
            FindInputHandlerReference();
            
            FindMotorReference();

            FindCameraReference();
        }

        private void OnValidate()
        {
            //minAimingDistanceSq = minAimingDistance * minAimingDistance;
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

        // private void OnDrawGizmos()
        // {
        //     DrawAimingGizmos();    
        // }
        //
        // /// <summary>
        // /// Draw gizmos visualising the point to aim at in the scene view
        // /// </summary>
        // private void DrawAimingGizmos()
        // {
        //     F32x3 __aimOrigin = AimOrigin;
        //     F32x3 __aimPoint  = AimPoint;
        //
        //     Gizmos.color = Color.red;
        //     Debug.DrawLine(start: __aimOrigin, end: __aimPoint, color: Color.red);
        //     Gizmos.DrawWireSphere(center: __aimPoint, radius: 0.2f);
        // }
        #endif


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
                Plane __groundPlane = new Plane(inNormal: Vector3.up, inPoint: WorldPos);

                Boolean __rayHasHit = __groundPlane.Raycast(ray: __ray, enter: out F32 __hitDistance);

                //Debug.DrawRay(start: __ray.origin, dir: __ray.direction * min(__hitDistance, 10), Color.yellow);
                
                if (__rayHasHit)
                {
                    _cachedLookPosition = __ray.GetPoint(distance: __hitDistance);
                }
                
                Draw.SolidCircleXZ(center: WorldPos,            radius: 0.25f, color: Color.yellow);
                Draw.SolidCircleXZ(center: _cachedLookPosition, radius: 0.25f, color: Color.yellow);
                Draw.Line(a: WorldPos, b: _cachedLookPosition, color: Color.yellow);
                
                //Debug.DrawLine(start: );

                return _cachedLookPosition;
            }
        }

        private void Update()
        {
            OrientTowardsPos(LookPosition);
        }

        // /// <summary>
        // /// Returns the point to aim at. A raycast is shot from the aim origin in the forward direction of the player. The hit point is used as the point to aim at.
        // /// </summary>
        // private F32x3 AimPoint
        // {
        //     get
        //     {
        //         F32x3 __aimOrigin = AimOrigin;
        //
        //         if (!Physics.Raycast(origin: __aimOrigin, direction: Forward, hitInfo: out RaycastHit __hitInfo, maxDistance: maxAimingDistance))
        //         {
        //             return (AimOrigin + Forward * maxAimingDistance);
        //         }
        //         
        //         F32x3 __hitPoint = __hitInfo.point;
        //         
        //         F32   __distanceToHitSq = distancesq(__aimOrigin, __hitPoint);
        //
        //         Boolean __isPastMinAimingDistance = (__distanceToHitSq >= minAimingDistanceSq);
        //
        //         if (__isPastMinAimingDistance)
        //         {
        //             return __hitPoint;
        //         }
        //
        //         return (AimOrigin + Forward * minAimingDistance);
        //     }
        // }
        //
        // /// <summary>
        // /// The origin point used to determine the point to aim at
        // /// </summary>
        // /// <returns></returns>
        // private F32x3 AimOrigin
        // {
        //     get
        //     {
        //         F32x3 __offset = (Right * aimOffset.x) + (Up * aimOffset.y) + (Forward * aimOffset.z);
        //
        //         return WorldPos + __offset;   
        //     }
        // }
        public void OrientTowardsPos(F32x3 lookPosition)
        {
            F32x3 __lookDirection = (lookPosition - WorldPos);
            
            OrientTowardsDir(lookDirection: __lookDirection);;
        }
        
        public void OrientTowardsDir(F32x3 lookDirection)
        {
            Plane3D __plane3D = new Plane3D(normal: up(), distance: 0);
            
            F32x3 __projectedLookDirection = normalize(__plane3D.Projection(point: lookDirection));
            
            if (lengthsq(__projectedLookDirection) == 0) return;
            //if (all(__projectedLookDirection == F32x3.zero)) return;
            
            quaternion __targetRotation = quaternion.LookRotation(forward: __projectedLookDirection, up: up());

            Rot = slerp(q1: Rot, q2: __targetRotation, t: orientationSpeed * Time.deltaTime);
            
            //motor.RotateTowards(worldDirection: MovementDirection, maxDegreesDelta: rotationRate * Time.deltaTime);
        }
        
        #endregion
        
    }
}
