using UnityEngine;

namespace SS3D.Interactions.Interfaces
{
    public interface IInteractionPointProvider
    {
        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point);
    }
}
