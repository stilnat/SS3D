using SS3D.Core;
using SS3D.Core.Behaviours;

namespace SS3D.Systems.Atmospherics.AtmosRework.Machinery
{
    public abstract class BasicAtmosDevice : NetworkActor, IAtmosDevice
    {
        public override void OnStartServer()
        {
            base.OnStartServer();
            if (Subsystems.Get<PipeSystem>().IsSetUp)
            {
                Subsystems.Get<PipeSystem>().RegisterAtmosDevice(this);
            }
            else
            {
                Subsystems.Get<PipeSystem>().OnSystemSetUp += () => Subsystems.Get<PipeSystem>().RegisterAtmosDevice(this);
            }
        }

        protected void OnDestroy()
        {
            Subsystems.Get<PipeSystem>().RemoveAtmosDevice(this); 
        }

        public abstract void StepAtmos(float dt);
    }
}
