using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Put that on body parts that can be grabbed
    /// </summary>
    public class GrabbableBodyPart : Draggable, IInteractionTarget
    {
        public override bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point)
        {
            point = gameObject.transform.position;

            if (gameObject.TryGetComponent(out Rigidbody rigibody))
            {
                point = rigibody.centerOfMass;
            }

            return true;
        }

        public override bool MoveToGrabber => true;
    }
}
