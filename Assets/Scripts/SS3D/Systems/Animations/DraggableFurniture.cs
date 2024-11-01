using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For furnitures that can be dragged
/// </summary>
public class DraggableFurniture : Draggable
{
    public override bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

    public override bool MoveToGrabber => false;
}
