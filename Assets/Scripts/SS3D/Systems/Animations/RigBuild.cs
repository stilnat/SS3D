using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;


    /// <summary>
    /// Necessary class to initialize the rig builder, because some target for the rigs need to be taken
    /// out of the Human prefab, as they should not depend on the player movement, but also it's convenient to
    /// pack them in the human prefab.
    /// </summary>
    public class RigBuild : MonoBehaviour 
    {
        [SerializeField]
        private Transform _rightPickupTargetLocker;

        [SerializeField]
        private Transform _leftPickupTargetLocker;

        [SerializeField]
        private Transform _rightHoldTargetLocker;

        [SerializeField]
        private Transform _leftHoldTargetLocker;

        [SerializeField]
        private Transform _lookAtTargetLocker;

        [SerializeField]
        private Transform _leftPlaceTarget;

        [SerializeField]
        private Transform _rightPlaceTarget;

        // Start is called before the first frame update
        protected void Start()
        {
            _rightPickupTargetLocker.transform.parent = null;
            _leftPickupTargetLocker.transform.parent = null;
            _rightHoldTargetLocker.transform.parent = null;
            _leftHoldTargetLocker.transform.parent = null;
            _lookAtTargetLocker.transform.parent = null;
            _leftPlaceTarget.transform.parent = null;
            _rightPlaceTarget.transform.parent = null;

            Animator animator = GetComponent<Animator>();
            RigBuilder rigBuilder = GetComponent<RigBuilder>();

            rigBuilder.Build();
            animator.Rebind();
        }
    }
