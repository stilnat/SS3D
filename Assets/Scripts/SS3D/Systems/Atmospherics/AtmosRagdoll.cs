using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    
    // This handles ragdolling by atmos wind, which I think should be moved elsewhere
    // makes the player ragdoll when there's strong wind
    [RequireComponent(typeof(Ragdoll))]
    public class AtmosRagdoll : NetworkActor
    {
        private float minVelocity = 10;
        private float knockdownTime = 3;
        private float checkInterval = 1f;
        
        [SerializeField]
        private Ragdoll ragdoll;

        private float lastCheck;

        private AtmosEnvironmentSystem atmosSystem;



        public override void OnStartServer()
        {
            base.OnStartServer();
            atmosSystem = Subsystems.Get<AtmosEnvironmentSystem>();
        }

        public void Update()
        {
            float time = Time.time;
            // Reduce check interval
            if (lastCheck + checkInterval < time)
            {
                lastCheck = time;

                // Get current tile position
                Vector3 position = transform.position;
                //AtmosObject atmosObject = atmosSystem.GetAtmosTile(position).AtmosObject;

                //ApplyVelocity(atmosObject.Velocity);
            }
        }

        private void ApplyVelocity(Vector2 velocity)
        {
            if (velocity.sqrMagnitude > minVelocity * minVelocity)
            {
               // ragdoll.Knockdown(knockdownTime);
            }
        }
    }
}
