using UnityEngine;

using JetBrains.Annotations;

using F32   = System.Single;
using F32x2 = Unity.Mathematics.float2;
using F32x3 = Unity.Mathematics.float3;

using I32   = System.Int32;

namespace Game.Movement
{
    public sealed class AnimationHandler : MonoBehaviour
    {
        #region Variables

        [SerializeField] private Transform characterTransform;
        
        [SerializeField, HideInInspector] private Animator animator;
        
        private static readonly I32 forward = Animator.StringToHash(name: "Forward");
        private static readonly I32 right   = Animator.StringToHash(name: "Right");

        #endregion

        #region Methods

        #if UNITY_EDITOR
        
        private void Reset()
        {
            FindAnimatorReference();
            
            FindCharacterTransform();
        }
        
        private void OnValidate()
        {
            if (animator == null)
            {
                FindAnimatorReference();
            }
            
            if (characterTransform == null)
            {
                FindCharacterTransform();
            }
        }
        
        private void FindAnimatorReference()
        {
            if (!TryGetComponent(out animator))
            {
                Debug.LogWarning(message: $"No Animator component found on {name}!", context: this);
            }
        }

        private void FindCharacterTransform()
        {
            characterTransform = transform.parent;
        }
        #endif
        
        
        [PublicAPI]
        public void SetMoveVector(F32x3 moveVector)
        {
            //Rotate the moveVector to match the character's rotation
            //moveVector = (Quaternion.Inverse(transform.rotation) * moveVector);
            
            Vector3 __localMoveVector = characterTransform.InverseTransformDirection(moveVector);

            // Transform the input vector based on the character's rotation
            Vector3 __rotatedMoveVector = Quaternion.Euler(0, characterTransform.rotation.eulerAngles.y, 0) * __localMoveVector;
            
            animator.SetFloat(id: right,   value: __rotatedMoveVector.x);
            animator.SetFloat(id: forward, value: __rotatedMoveVector.z);
        }

        #endregion
    }
}
