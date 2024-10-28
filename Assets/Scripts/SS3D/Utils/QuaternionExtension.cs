using UnityEngine;

namespace SS3D.Utils
{
    public static class QuaternionExtension
    {
        /// <summary>
        /// Create a LookRotation for a non-standard 'forward' axis.
        /// </summary>
        public static Quaternion AltForwardLookRotation(Vector3 dir, Vector3 forwardAxis, Vector3 upAxis)
        {
            return Quaternion.LookRotation(dir) * Quaternion.Inverse(Quaternion.LookRotation(forwardAxis, upAxis));
        }
    }
}
