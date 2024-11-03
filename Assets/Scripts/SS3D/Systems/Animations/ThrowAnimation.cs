using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    public class ThrowAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private readonly AbstractHoldable _holdable;
        private  readonly Hand _mainHand;
        private  readonly Hand _secondaryHand;
        private readonly Transform _rootTransform;

        public ThrowAnimation(float interactionTime, ProceduralAnimationController controller, AbstractHoldable holdable, Hand mainHand, Hand secondaryHand)
            : base(interactionTime, controller)
        {
            _holdable = holdable;
            _mainHand = mainHand;
            _secondaryHand = secondaryHand;
            _rootTransform = controller.transform;
        }

        public override void ClientPlay()
        {

            _mainHand.Hold.ItemPositionConstraint.weight = 0f;

           bool isRight = _mainHand.HandType == HandType.RightHand;
           int deviationRightOrLeft = isRight ? 1 : -1;

            Vector3 initialPosition = _mainHand.Hold.ItemPositionTargetLocker.transform.position;
            Vector3 initialPositionInRoot = _rootTransform.InverseTransformPoint(initialPosition);
            Vector3 middle = initialPositionInRoot + 0.15f * Vector3.up - 0.15f * Vector3.forward + deviationRightOrLeft * 0.1f * Vector3.right;
            Vector3 end = initialPositionInRoot - 0.3f * Vector3.forward;


            Vector3[] path =
            {
                initialPositionInRoot,
                middle,
                end
            };

            // do a little back and forth path
            InteractionSequence.Join(_mainHand.Hold.ItemPositionTargetLocker.transform.DOLocalPath(path, InteractionTime/2)
                .SetLoops(2, LoopType.Yoyo));  

            InteractionSequence.OnComplete(() => { 
                
                _mainHand.Hold.HoldIkConstraint.weight = 0f;
                _mainHand.Hold.PickupIkConstraint.weight = 0f;

                // remove all IK constraint on second hand if needed
                if (_holdable.CanHoldTwoHand && _secondaryHand)
                {
                    _secondaryHand.Hold.ItemPositionConstraint.weight = 0f;
                    _secondaryHand.Hold.HoldIkConstraint.weight = 0f;
                    _secondaryHand.Hold.PickupIkConstraint.weight = 0f;
                }

                _holdable.GameObject.transform.parent = null;

            });

            // Ignore collision between thrown item and player for a short while
            Physics.IgnoreCollision(_holdable.GetComponent<Collider>(), _rootTransform.GetComponent<Collider>(), true);

            WaitToRestoreCollision();
        }



        private async Task WaitToRestoreCollision()
        {
            await Task.Delay(300);
            // Allow back collision between thrown item and player
            Physics.IgnoreCollision(_holdable.GetComponent<Collider>(), _rootTransform.GetComponent<Collider>(), false);
        }

        public override void Cancel()
        {
            
        }
    }
}
