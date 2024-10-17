using System.Linq;
using UnityEngine;

namespace SS3D.Interactions
{
    public class InteractionPointsProvider : MonoBehaviour
    {
        [SerializeField]
        private Transform[] _interactionPoints;

        public Transform[] InteractionPoints => _interactionPoints;

        public Vector3 GetClosestPointFromSource(Vector3 source)
        {
            return _interactionPoints
                .Select(x => x.position)                  
                .OrderBy(p => Vector3.Distance(p, source))
                .First();  
        }

    }
}
