using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DummyRagdoll : MonoBehaviour
{
        private Transform _character;
        private Animator _animator; 

        private Transform[] _ragdollParts;
     
        private void Start()
        {

            _animator = GetComponent<Animator>();
            _ragdollParts = (from part in GetComponentsInChildren<RagdollPart>() select part.transform.GetComponent<Transform>()).ToArray();

            // All rigid bodies are kinematic at start, only the owner should be able to change that afterwards.
			ToggleKinematic(true);
		}
    

        private void Update()
		{
            if (Input.GetKeyDown(KeyCode.K))
            {
                Knockdown();
            }
        }

        private void Knockdown()
        {
            ToggleAnimator(false);
            ToggleKinematic(false);
            ToggleTrigger(false);
        }

    
        private void ToggleTrigger(bool isTrigger)
        {
            foreach (Transform part in _ragdollParts)
            {
                part.GetComponent<Collider>().isTrigger = isTrigger;
            }
        }
        
        
		/// <summary>
		/// Switch isKinematic for each ragdoll part
		/// </summary>
		private void ToggleKinematic(bool isKinematic)
		{
			foreach (Transform part in _ragdollParts)
			{
				part.GetComponent<Rigidbody>().isKinematic = isKinematic;
			}
		}
        
        private void ToggleAnimator(bool enable)
        {
            // Speed=0 prevents animator from choosing Walking animations after enabling it
            if (!enable)
                _animator.SetFloat("Speed", 0);

            if (_animator != null)
            {
                _animator.enabled = enable;
            }
        }
    
}
