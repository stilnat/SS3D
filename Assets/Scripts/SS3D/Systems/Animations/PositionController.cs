using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace SS3D.Systems.Animations
{
    public class PositionController : NetworkBehaviour
    {
        [SyncVar]
        private PositionType _position;

        public PositionType Position
        {
            get => _position;
            set => _position = value;
        }

        protected void Start()
        {
            Position = PositionType.Standing;
        }
    }
}
