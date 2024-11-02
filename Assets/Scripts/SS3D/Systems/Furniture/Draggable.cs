using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Interactions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Put this script on things that can be dragged by a player, such as unbolted furnitures.
/// </summary>
public abstract class Draggable : NetworkActor, IInteractionTarget
{
    /// <summary>
    /// True if the object is currently being dragged.
    /// </summary>
    [SyncVar]
    private bool _dragged;

    public bool Dragged => _dragged;

    public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
    {
        return new IInteraction[]
        {
            new GrabInteraction(1f),
        };
    }

    public abstract bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point);

    /// <summary>
    /// If true, when grabbing, the draggable root transform should be moved toward the grabber, otherwise it's the grabber that moves toward the grabbed item.
    /// For heavy, rigid stuff this should be false, for stuff such as ragdoll this should be true.
    /// </summary>
    public abstract bool MoveToGrabber { get; }



}
