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
    public class ThrowAnimation : IProceduralAnimation
    {
        public event Action<IProceduralAnimation> OnCompletion;

        private ProceduralAnimationController _controller;

        public void ClientPlay(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkBehaviour target, Vector3 targetPosition, ProceduralAnimationController proceduralAnimationController, float time, float delay)
        {
            _controller = proceduralAnimationController;

            // remove all IK constraint
            Item item = target as Item;
            AbstractHoldable holdable = item.Holdable;

            Sequence throwAnimation = DOTween.Sequence();
            Quaternion holdRotation = mainHand.Hold.HandHoldTargetLocker.transform.rotation;
            mainHand.Hold.ItemPositionConstraint.weight = 0f;
           // item.transform.parent = mainHand.Hold.HoldTransform;
           // mainHand.Hold.HandHoldTargetLocker.transform.rotation = holdRotation;

            Transform hold = holdable.GetHold(true, mainHand.HandType);
            Vector3 initialPosition = mainHand.Hold.ItemPositionTargetLocker.transform.position;
            Vector3 middle = initialPosition + 0.15f * Vector3.up - 0.15f * _controller.Forward;
            Vector3 end = initialPosition - 0.3f * _controller.Forward;

            Vector3[] path =
            {
                initialPosition,
                middle,
                end
            };

        // do a little back and forth rotation
            throwAnimation.Join(mainHand.Hold.ItemPositionTargetLocker.transform.DOPath(path, time/2)
                .SetLoops(2, LoopType.Yoyo));  

            throwAnimation.OnComplete(() => { 
                
                mainHand.Hold.HoldIkConstraint.weight = 0f;
                mainHand.Hold.PickupIkConstraint.weight = 0f;

                // remove all IK constraint on second hand if needed
                if (holdable.CanHoldTwoHand && secondaryHand)
                {
                    secondaryHand.Hold.ItemPositionConstraint.weight = 0f;
                    secondaryHand.Hold.HoldIkConstraint.weight = 0f;
                    secondaryHand.Hold.PickupIkConstraint.weight = 0f;
                }

                item.GameObject.transform.parent = null;

            });


            

            // Ignore collision between thrown item and player for a short while
            Physics.IgnoreCollision(item.GetComponent<Collider>(), _controller.GetComponent<Collider>(), true);

            
            WaitToRestoreCollision(item, _controller.transform);
        }



        private async Task WaitToRestoreCollision(Item item, Transform playerRoot)
        {
            await Task.Delay(300);
            // Allow back collision between thrown item and player
            Physics.IgnoreCollision(item.GetComponent<Collider>(), playerRoot.GetComponent<Collider>(), false);
        }

        public void Cancel()
        {
            
        }
    }
}
