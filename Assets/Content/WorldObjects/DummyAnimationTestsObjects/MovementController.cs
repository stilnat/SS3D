using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public class MovementController : MonoBehaviour
    {
        public float moveSpeed = 5f;

        private Rigidbody rb;
        
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

            Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
            Vector3 moveVelocity = moveDirection * moveSpeed;
            
            OnSpeedChangeEvent?.Invoke(moveDirection.magnitude);

            rb.velocity = new Vector3(moveVelocity.x, rb.velocity.y, moveVelocity.z);
        }
    }
}
