using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public class ProceduralAnimationController : NetworkActor
    {

        [field: SerializeField]
        public Transform LookAtTargetLocker { get; private set; }

        [field: SerializeField]
        public HoldController HoldController { get; private set; }

        [field: SerializeField]
        public MultiAimConstraint LookAtConstraint {get; private set; }

        [field: SerializeField]
        public PositionController PositionController {get; private set; }

        [field: SerializeField]
        public HumanoidAnimatorController AnimatorController {get; private set; }

        [field: SerializeField]
        public Hands Hands {get; private set; }

        // We can't have more than one procedural animation running at the same time per hand
        private List<Tuple<Hand, IProceduralAnimation>> _animations = new();

        [Server]
        public void PlayAnimation(InteractionType interactionType, Hand hand,  NetworkObject target, Vector3 targetPosition, float time, float delay = 0f)
        {
            Hands.TryGetOppositeHand(hand, out Hand oppositeHand);
            ObserversPlayAnimation(interactionType, hand, oppositeHand, target, targetPosition, time, delay);
        }

        [ObserversRpc]
        public void ObserversPlayAnimation(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkObject target, Vector3 targetPosition, float time, float delay = 0f)
        {
            IProceduralAnimation proceduralAnimation;
            switch (interactionType)
            {
                  case InteractionType.Pickup:
                      proceduralAnimation = new PickUpAnimation();
                      break;
                  case InteractionType.Place:
                      proceduralAnimation = new PlaceAnimation();
                      break;
                  case InteractionType.Screw:
                      proceduralAnimation = new InteractAnimations();
                      break;
                  default:
                      return;

            } 
            
            _animations.Add(new(mainHand, proceduralAnimation));

            proceduralAnimation.ClientPlay(interactionType, mainHand, secondaryHand, target, targetPosition, this, time, delay);
            proceduralAnimation.OnCompletion += RemoveAnimation;
        }

        [Server]
        public void CancelAnimation(Hand hand)
        {
            ObserverCancelAnimation(hand);
        }

        [ObserversRpc]
        private void ObserverCancelAnimation(Hand hand)
        {
            _animations.FirstOrDefault(x => x.Item1 == hand)?.Item2.Cancel();
            _animations.Remove(_animations.FirstOrDefault(x => x.Item1 == hand));
        }

        private void RemoveAnimation(IProceduralAnimation animation)
        {
            _animations.Remove(_animations.FirstOrDefault(x => x.Item2 == animation));
        }


    }
}
