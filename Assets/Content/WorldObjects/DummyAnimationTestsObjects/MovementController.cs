using System;
using UnityEngine;

namespace DummyStuff
{
    public class MovementController : MonoBehaviour
    {
        public event Action<float> OnSpeedChangeEvent;

        [SerializeField]
        private float _moveSpeed = 5f;

        private Rigidbody _rb;

        [SerializeField]
        private Transform _camera;

        protected void Start()
        {
            _rb = GetComponent<Rigidbody>();
        }

        protected void LateUpdate()
        {
            // Movement
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            Vector3 moveDirection = (verticalInput * Vector3.Cross(_camera.transform.right, Vector3.up).normalized) +
                (horizontalInput * Vector3.Cross(Vector3.up, _camera.transform.forward).normalized);

            Vector3 moveVelocity = moveDirection * _moveSpeed;

            OnSpeedChangeEvent?.Invoke(moveDirection.magnitude);

            _rb.velocity = new Vector3(moveVelocity.x, _rb.velocity.y, moveVelocity.z);

            if (_rb.velocity.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(_rb.velocity.normalized, Vector3.up);
            }
        }
    }
}
