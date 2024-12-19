using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Dragging;
using UnityEngine;

/// <summary>
/// For furnitures that can be dragged
/// </summary>
public sealed class DraggableFurniture : Draggable
{
    public override bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

    public override bool MoveToGrabber => false;
}
