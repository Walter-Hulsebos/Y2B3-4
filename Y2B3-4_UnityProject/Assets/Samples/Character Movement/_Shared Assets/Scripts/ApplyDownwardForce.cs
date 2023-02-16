using UnityEngine;

namespace EasyCharacterMovement.CharacterMovementDemo
{
    public class ApplyDownwardForce : MonoBehaviour
    {
        public float forceMagnitude = 1f;

        private CharacterMovement _characterMovement;
        private bool _isCharacterMovementNull;

        private void Awake()
        {
            _characterMovement = GetComponent<CharacterMovement>();
            _isCharacterMovementNull = _characterMovement == null;
        }

        private void FixedUpdate()
        {
            if (_isCharacterMovementNull)
                return;

            bool hasLanded = !_characterMovement.wasOnGround && _characterMovement.isGrounded;
            if (hasLanded)
            {
                // On Landed add landing force

                Rigidbody groundRigidbody = _characterMovement.groundRigidbody;
                if (groundRigidbody)
                {
                    groundRigidbody.AddForceAtPosition(_characterMovement.landedVelocity * Physics.gravity.magnitude,
                        _characterMovement.position);
                }
            }
            else if (_characterMovement.isGrounded)
            {
                // If standing, apply downward force

                Rigidbody groundRigidbody = _characterMovement.groundRigidbody;
                if (groundRigidbody)
                    groundRigidbody.AddForceAtPosition(Vector3.down * forceMagnitude, _characterMovement.position);
            }
        }
    }
}
