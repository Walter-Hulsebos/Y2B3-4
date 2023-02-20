using System;
using UnityEngine;

namespace EasyCharacterMovement.CharacterMovementWalkthrough.Forces
{
    /// <summary>
    /// This example shows how to implement a ForceField for characters using the CharacterMovement component.
    /// </summary>

    public class ForceField : MonoBehaviour
    {
        public ForceMode forceMode = ForceMode.Force;
        public float forceMagnitude = 15.0f;

        // Cached CharacterMovement component

        private CharacterMotor CharacterMotor { get; set; }

        private void OnTriggerEnter(Collider other)
        {
            // Cache CharacterMovement (if any)

            CharacterMotor = other.GetComponent<CharacterMotor>();
        }

        private void OnTriggerExit(Collider other)
        {
            // If our cached character leaves the trigger, remove cached CharacterController

            if (other.TryGetComponent(out CharacterMotor component) &&
                CharacterMotor.gameObject == component.gameObject)
            {
                CharacterMotor = null;
            }
        }

        private void Update()
        {
            // If a character is inside ForceField trigger area, add force!

            if (CharacterMotor)
            {
                // If the character is grounded, pause ground constraint so it can leave the ground

                if (CharacterMotor.isGrounded)
                    CharacterMotor.PauseGroundConstraint();

                // Add continuous force
                
                CharacterMotor.AddForce(transform.up * forceMagnitude, forceMode);
            }
        }
    }
}
