using System;
using UnityEngine;
using static Unity.Mathematics.math;

using Component = Game.Shared.Component;
using Plane = ProjectDawn.Geometry3D.Plane;

using F32   = System.Single;
using F32x2 = Unity.Mathematics.float2;
using F32x3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;

namespace Game.Movement
{
    using Game.Inputs;
    
    public sealed class Orientation : Component
    {
        [SerializeField, HideInInspector] private InputHandler inputHandler;
        
        [SerializeField] private F32x3 aimOffset           = new F32x3(x: 0, y: 1, z: 0.75f);
        [SerializeField] private F32   minAimingDistance   = 2f;
        [SerializeField] private F32   minAimingDistanceSq = 2f * 2f;
        [SerializeField] private F32   maxAimingDistance   = 20f;
        //[SerializeField] private F32   maxAimingDistanceSq = 20f * 20f;
        [SerializeField] private F32   orientationSpeed    = 15f;

        #if UNITY_EDITOR
        private void Reset()
        {
            inputHandler = GetComponent<InputHandler>();

            minAimingDistanceSq = minAimingDistance * minAimingDistance;
            //maxAimingDistanceSq = maxAimingDistance * maxAimingDistance;
        }

        private void OnValidate()
        {
            minAimingDistanceSq = minAimingDistance * minAimingDistance;
            //maxAimingDistanceSq = maxAimingDistance * maxAimingDistance;
            
            if (inputHandler == null)
            {
                inputHandler = GetComponent<InputHandler>();
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

        private void Update()
        {
            
        }
        
        /// <summary>
        /// Rotates the player to the given look direction
        /// </summary>
        /// <param name="lookInput"></param>
        public void Rotate(Vector2 lookInput)
        {
            F32x3 __lookDirection = normalize(new F32x3(lookInput.x, 0, lookInput.y));

            if (all(__lookDirection != F32x3.zero))
            {
                OrientTowards(lookPosition: WorldPos + __lookDirection);
            }
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
            Plane __plane = new Plane(normal: up(), distance: 0);
            
            F32x3 __lookDirection = (lookPosition - (F32x3)transform.position);
            
            F32x3 __projectedLookDirection = normalize(__plane.Projection(point: __lookDirection));
            
            //if(lengthsq(__projectedLookDirection) == 0) return;
            if (all(__projectedLookDirection == F32x3.zero)) return;
            
            quaternion __targetRotation = quaternion.LookRotation(forward: __projectedLookDirection, up: up());

            transform.rotation = slerp(q1: transform.rotation, q2: __targetRotation, t: orientationSpeed * Time.deltaTime);
        }
        
    }
}
