using DG.Tweening;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Furniture;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Simple animation allowing to place correctly the human before playing the sit animation.
    /// I'm not sure the sitting stuff really has its place here, maybe more in humanoid movement controller
    /// </summary>
    public class Sit : IProceduralAnimation
    {
        public event Action<IProceduralAnimation> OnCompletion;

        private ProceduralAnimationController _controller;

        private Sequence _sitSequence;

        public void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _controller = proceduralAnimationController;

            if (_controller.PositionController.Position == PositionType.Sitting)
            {
                StopSitting();
            }
            else
            {
                AnimateSit(target.transform);
            }
        }

        public void Cancel()
        {
             StopSitting();
        }

        private void AnimateSit(Transform sit)
        {
            _controller.MovementController.enabled = false;

            _controller.AnimatorController.Sit(true);

            _sitSequence = DOTween.Sequence();

            _sitSequence.Join(_controller.transform.DOMove(sit.position, 0.5f));
            _sitSequence.Join(_controller.transform.DORotate(sit.rotation.eulerAngles, 0.5f));

            _controller.PositionController.TrySit();

            _sitSequence.OnComplete(() => OnCompletion?.Invoke(this));
        }

        private void StopSitting()
        {
            _controller.MovementController.enabled = true;
            _controller.AnimatorController.Sit(false);
            _controller.PositionController.TryToStandUp();
        }
    }
}
