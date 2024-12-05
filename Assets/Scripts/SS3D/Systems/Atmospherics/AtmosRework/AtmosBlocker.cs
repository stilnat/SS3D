using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Engine.AtmosphericsRework;

public class AtmosBlocker : NetworkActor
{
    public override void OnStartServer(){
        base.OnStartServer();
        Subsystems.Get<AtmosManager>().ChangeState(Position, AtmosState.Blocked);
    }
}