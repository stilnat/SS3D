using DG.Tweening;
using FishNet.Component.Animating;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Component for character's gameobject, that controlls ragdoll
    /// </summary>
	public class Ragdoll : NetworkBehaviour
	{

        [SerializeField]
		private Transform _armatureRoot;

        [SerializeField]
        private Collider _characterCollider;

        [SerializeField]
        private Rigidbody _characterRigidBody;

        [SerializeField]
        private Transform _hips;

        [SerializeField]
        private Transform _character;

        [SerializeField]
        private byte _ragdollPartSyncInterval;

        [SerializeField]
        private PositionController _positionController;

        private Transform[] _ragdollParts;
        /// <summary>
        /// If knockdown is supposed to expire
        /// </summary>
        private bool _isKnockdownTimed;

        private readonly float _timeToResetBones = 0.7f;
        /// <summary>
        /// Determines how much higher than the lowest point character will be during AlignToHips(). This var prevent character from getting stuck in the floor 
        /// </summary>
        private const float AlignmentYDelta = 0.0051f;

        private class BoneTransform
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }
        /// <summary>
        /// Bones Transforms (position and rotation) in the first frame of StandUp animation
        /// </summary>
        private BoneTransform[] _standUpBones;

        /// <summary>
        /// Bones Transforms (position and rotation) during the Ragdoll state
        /// </summary>
        private BoneTransform[] _ragdollBones;

        public bool IsFacingDown { get; private set; }


        public override void OnStartNetwork()
		{
			base.OnStartNetwork();
            _ragdollParts = (from part in GetComponentsInChildren<RagdollPart>() select part.transform.GetComponent<Transform>()).ToArray();
            _standUpBones = new BoneTransform[_ragdollParts.Length];
            _ragdollBones = new BoneTransform[_ragdollParts.Length];

            for (int boneIndex = 0; boneIndex < _ragdollParts.Length; boneIndex++)
            {
                _standUpBones[boneIndex] = new();
                _ragdollBones[boneIndex] = new();
            }

            // All rigid bodies are kinematic at start, only the owner should be able to change that afterwards.
			SetRagdollPhysic(false);
            ToggleSyncRagdoll(false);
            _positionController.ChangedPosition += HandleChangedPosition;
        }

        private void HandleChangedPosition(PositionType position, float recoverTime)
        {
            if (position == PositionType.ResetBones)
            {
                AnimationClip recoverAnimation = _positionController.GetRecoverFromRagdollClip();
                BonesReset(recoverAnimation, recoverTime);
            }
            else if (position == PositionType.Ragdoll)
            {
                StartRagdoll();
            }
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            //SetRagdollPhysic(IsOwner);
        }
        

        private void StartRagdoll()
        {
            if (!IsOwner && Owner.ClientId != -1)
                return;
            
            SetRagdollPhysic(true);
            // put that in its own method
            /*foreach (Transform part in _ragdollParts)
            {
                part.GetComponent<Rigidbody>().AddForce(movement, ForceMode.VelocityChange);
            }  */
        }

        /// <summary>
        /// Adjust player's position and rotation. Character's x and z coords equals hips coords, y is at lowest positon.
        /// Character's y rotation is aligned with hips forwards direction.
        /// </summary>
        private void AlignToHips()
        {
            IsFacingDown = _hips.transform.forward.y < 0;
            Vector3 originalHipsPosition = _hips.position;
            Vector3 newPosition = _hips.position;
            // Get the lowest position
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo))
            {
                newPosition.y = hitInfo.point.y + AlignmentYDelta;
            }
            _character.position = newPosition;
            _hips.position = originalHipsPosition;
            
            Vector3 desiredDirection = _hips.up * (IsFacingDown ? 1 : -1);
            desiredDirection.y = 0;
            desiredDirection.Normalize();
            Quaternion originalHipsRotation = _hips.rotation;
            Vector3 rotationDifference = Quaternion.FromToRotation(transform.forward, desiredDirection).eulerAngles;
            // Make sure that rotation is only around Y axis
            rotationDifference.x = 0;
            rotationDifference.z = 0;
            transform.rotation *= Quaternion.Euler(rotationDifference);
            _hips.rotation = originalHipsRotation;
        }

        /// <summary>
        /// Switch to BonesReset state and prepare for BonesResetBehavior
        /// </summary>
        private void BonesReset(AnimationClip clip, float timeToResetBones)
        {
            SetRagdollPhysic(false);
            PopulateStandUpPartsTransforms(clip);
            for (int partIndex = 0; partIndex < _ragdollParts.Length; partIndex++)
            {
                _ragdollParts[partIndex].DOLocalMove(_standUpBones[partIndex].Position, timeToResetBones);
                _ragdollParts[partIndex].DOLocalRotate(_standUpBones[partIndex].Rotation.eulerAngles, timeToResetBones);
            }
        }

        /// <summary>
        /// Copy current ragdoll parts local positions and rotations to array.
        /// </summary>
        /// <param name="partsTransforms">Array, that receives ragdoll parts positions</param>
        private void PopulatePartsTransforms(BoneTransform[] partsTransforms)
        {
            for (int partIndex = 0; partIndex < _ragdollParts.Length; partIndex++)
            {
                partsTransforms[partIndex].Position = _ragdollParts[partIndex].localPosition;
                partsTransforms[partIndex].Rotation = _ragdollParts[partIndex].localRotation;
            }
        }

        /// <summary>
        /// Copy ragdoll parts position in first frame of StandUp animation to array
        /// </summary>
        /// <param name = "partsTransforms">Array, that receives ragdoll parts positions</param>
        /// <param name="animationClip"></param>
        private void PopulateStandUpPartsTransforms(AnimationClip animationClip)
        {
            // Copy into the _ragdollBones list the local position and rotation of bones, while in the current ragdoll position
            PopulatePartsTransforms(_ragdollBones);

            // Register some position and rotation before switching to the first frame of getting up animation
            Vector3 originalArmaturePosition = _armatureRoot.localPosition;
            Quaternion originalArmatureRotation = _armatureRoot.localRotation;
            Vector3 originalPosition = _character.position;
            Quaternion originalRotation = _character.rotation;

            // Put character into first frame of animation
            animationClip.SampleAnimation(gameObject, 0f);


            // Put back character at the right place as the animation moved it
            _character.position = originalPosition;
            _character.rotation = originalRotation;

            // Register hips position and rotation as moving the armature will messes with the hips position
            Vector3 originalHipsPosition = _hips.position;
            Quaternion originalHipsRotation = _hips.rotation;

            // Put back armature at the right place as the animation moved it
            _armatureRoot.localPosition = originalArmaturePosition;
            _armatureRoot.localRotation = originalArmatureRotation;

            // When moving the armature, it messes with the hips position, so we also move the hips back
            _hips.position = originalHipsPosition;
            _hips.rotation = originalHipsRotation;

            // Populate the bones local position and rotation when in first frame of animation
            PopulatePartsTransforms(_standUpBones);
            
            // Move bones back to the ragdolled position they were in
            for (int partIndex = 0; partIndex < _ragdollParts.Length; partIndex++)
            {
                _ragdollParts[partIndex].localPosition = _ragdollBones[partIndex].Position;
                _ragdollParts[partIndex].localRotation = _ragdollBones[partIndex].Rotation;
            }
        }

        /// <summary>
        /// Toggle the network transform syncing of the ragdoll parts, to save up on those sweet bytes.
        /// </summary>
        /// <param name="isActive"> true if the network transform of the ragdoll parts should sync</param>
        /// <returns></returns>
        private void ToggleSyncRagdoll(bool isActive)
        {
            foreach (Transform part in _ragdollParts)
            {
                part.GetComponent<NetworkTransform>().SetInterval(_ragdollPartSyncInterval);
                part.GetComponent<NetworkTransform>().SetSynchronizePosition(isActive);
                part.GetComponent<NetworkTransform>().SetSynchronizeRotation(isActive);
            }
        }

        private void SetRagdollPhysic(bool isOn)
        {
            ToggleSyncRagdoll(isOn);

            _characterRigidBody.isKinematic = isOn;

            foreach (Transform part in _ragdollParts)
            {
                part.GetComponent<Rigidbody>().isKinematic = !isOn;
            }

            _characterCollider.isTrigger = isOn;

            foreach (Transform part in _ragdollParts)
            {
                part.GetComponent<Collider>().isTrigger = !isOn;
            }
        }
    }
}
