using System;
using UnityEngine;

namespace DummyStuff
{
    public class MovementController : MonoBehaviour
    {
        public float moveSpeed = 5f;

        private Rigidbody rb;

        [SerializeField]
        private Transform _camera;

        
        public event Action<float> OnSpeedChangeEvent;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void LateUpdate()
        {
            // Movement
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            
            Vector3 moveDirection = verticalInput * Vector3.Cross(_camera.transform.right, Vector3.up).normalized + 
                horizontalInput * Vector3.Cross(Vector3.up, _camera.transform.forward).normalized;
            
            Vector3 moveVelocity = moveDirection * moveSpeed;
            
            OnSpeedChangeEvent?.Invoke(moveDirection.magnitude);

            rb.velocity = new Vector3(moveVelocity.x, rb.velocity.y, moveVelocity.z);
            
            if (rb.velocity.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(rb.velocity.normalized, Vector3.up);
            }
        }
    }
}
