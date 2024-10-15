using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabInteraction : GradualInteraction
{
    private bool _hasGrabbedItem;

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

    public override bool CanInteract(InteractionEvent interactionEvent)
    {
        // Can only grab with hand
        if (interactionEvent.Source.GetRootSource() is not Hand hand)
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

        //bool isInRange = InteractionExtensions.RangeCheck(interactionEvent);

        return true;
    }

    public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
    {
        base.Start(interactionEvent, reference);

        Hand hand = interactionEvent.Source.GetRootSource() as Hand;
        GrabbableBodyPart grabbable = interactionEvent.Target as GrabbableBodyPart;
        
        hand.GetComponentInParent<GrabAnimation>().Grab(grabbable, hand.Owner, TimeToMoveBackHand, TimeToReachGrabPlace);
                
        return true;
    }

    public override bool Update(InteractionEvent interactionEvent, InteractionReference reference)
    {

        if (StartTime + TimeToMoveBackHand >= Time.time || !HasStarted || _hasGrabbedItem)
        {
            return base.Update(interactionEvent, reference);
        }

        // After time to grab has passed, tell the hand that it grabbed something
        _hasGrabbedItem = true;

        Hand hand = interactionEvent.Source.GetRootSource() as Hand;
        GrabbableBodyPart grabbable = interactionEvent.Target as GrabbableBodyPart;

        // Grabbed thing is now owned by the hand's client, as we move thing based on client authority currently. 
        grabbable.GiveOwnership(hand.Owner);
        hand.IsGrabbing = true;

        return base.Update(interactionEvent, reference);
    }

    public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
    {
        Hand hand = interactionEvent.Source.GetRootSource() as Hand;
        hand.GetComponentInParent<GrabAnimation>().CancelGrab();
    }
}