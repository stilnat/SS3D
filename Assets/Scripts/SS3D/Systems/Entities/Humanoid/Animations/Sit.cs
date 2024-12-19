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
    public sealed class Sit : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private readonly Sittable _sit;

        public Sit(float interactionTime, ProceduralAnimationController controller, NetworkObject sit)
            : base(interactionTime, controller)
        {
            _sit = sit.GetComponent<Sittable>();
        }

        public override void ClientPlay()
        {
            if (Controller.PositionController.PositionType == PositionType.Sitting)
            {
                StopSitting();
            }
            else
            {
                AnimateSit(_sit.transform);
            }
        }

        public override void Cancel()
        {
             StopSitting();
        }

        private void AnimateSit(Transform sit)
        {
            Controller.MovementController.enabled = false;

            Controller.PositionController.TrySit();

            InteractionSequence.Join(Controller.transform.DOMove(sit.position, 0.5f));
            InteractionSequence.Join(Controller.transform.DORotate(sit.rotation.eulerAngles, 0.5f));

            InteractionSequence.OnComplete(() => OnCompletion?.Invoke(this));
        }

        private void StopSitting()
        {
            Controller.MovementController.enabled = true;
            Controller.PositionController.TryToGetToPreviousPosition();
            Controller.PositionController.TryToStandUp();
        }
    }
}
