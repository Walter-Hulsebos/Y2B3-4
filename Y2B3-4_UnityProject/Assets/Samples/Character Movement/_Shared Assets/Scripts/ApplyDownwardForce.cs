using UnityEngine;

namespace EasyCharacterMovement.CharacterMovementDemo
{
    public class ApplyDownwardForce : MonoBehaviour
    {
        public float forceMagnitude = 1f;

        private CharacterMotor _characterMotor;
        private bool _isCharacterMovementNull;

        private void Awake()
        {
            _characterMotor = GetComponent<CharacterMotor>();
            _isCharacterMovementNull = _characterMotor == null;
        }

        private void FixedUpdate()
        {
            if (_isCharacterMovementNull)
                return;

            bool hasLanded = !_characterMotor.wasOnGround && _characterMotor.isGrounded;
            if (hasLanded)
            {
                // On Landed add landing force

                Rigidbody groundRigidbody = _characterMotor.groundRigidbody;
                if (groundRigidbody)
                {
                    groundRigidbody.AddForceAtPosition(_characterMotor.landedVelocity * Physics.gravity.magnitude,
                        _characterMotor.position);
                }
            }
            else if (_characterMotor.isGrounded)
            {
                // If standing, apply downward force

                Rigidbody groundRigidbody = _characterMotor.groundRigidbody;
                if (groundRigidbody)
                    groundRigidbody.AddForceAtPosition(Vector3.down * forceMagnitude, _characterMotor.position);
            }
        }
    }
}
