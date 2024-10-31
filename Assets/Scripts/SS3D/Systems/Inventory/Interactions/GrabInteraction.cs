using FishNet.Connection;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabInteraction : ContinuousInteraction
{
    private GrabbableBodyPart _grabbedBodyPart;

    private NetworkConnection _previousOwner;

    public float TimeToMoveBackHand { get; private set; }

    public float TimeToReachGrabPlace { get; private set; }

    public GrabInteraction(float timeToMoveBackHand, float timeToReachGrabPlace)
    {
        TimeToMoveBackHand = timeToMoveBackHand;
        TimeToReachGrabPlace = timeToReachGrabPlace;
        Delay = TimeToMoveBackHand + TimeToReachGrabPlace;
    }

    public override string GetName(InteractionEvent interactionEvent) => "Grab";

    public override string GetGenericName() => "Grab";

    public override InteractionType InteractionType => InteractionType.Grab;

    public override bool CanInteract(InteractionEvent interactionEvent)
    {
        // Can only grab with hand
        if (interactionEvent.Source.GetRootSource() is not Hand hand)
        {
            return false;
        }

        if (interactionEvent.Target is not GrabbableBodyPart grabbable)
        {
            return false;
        }

        // check that our hand is empty
        if (!hand.IsEmpty())
        {
            return false;
        }

        Entity entity = interactionEvent.Source.GetComponentInParent<Entity>();
        if (!entity)
        {
            return false;
        }

        return true;
    }

    public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
    {
        base.Start(interactionEvent, reference);

        Hand hand = interactionEvent.Source.GetRootSource() as Hand;
        GrabbableBodyPart grabbable = interactionEvent.Target as GrabbableBodyPart;
        
        hand.GetComponentInParent<ProceduralAnimationController>().PlayAnimation(InteractionType, hand, grabbable, grabbable.GameObject.transform.position, 3*Delay);
                
        return true;
    }

    protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
    {
        Hand hand = interactionEvent.Source.GetRootSource() as Hand;

        _grabbedBodyPart = interactionEvent.Target as GrabbableBodyPart;
        _previousOwner = _grabbedBodyPart.Owner;
        _grabbedBodyPart.NetworkObject.GiveOwnership(hand.Owner);

    }
    

    public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
    {
        Hand hand = interactionEvent.Source.GetRootSource() as Hand;
        hand.GetComponentInParent<ProceduralAnimationController>().CancelAnimation(hand);

        if (_grabbedBodyPart != null)
        {
            _grabbedBodyPart.GiveOwnership(_previousOwner);
        }
    }

    protected override bool CanKeepInteracting(InteractionEvent interactionEvent, InteractionReference reference)
    {
        return true;
    }
}
