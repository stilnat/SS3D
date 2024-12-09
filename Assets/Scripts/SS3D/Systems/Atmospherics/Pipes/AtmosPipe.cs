using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Tile;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public class AtmosPipe : NetworkActor, IAtmosPipe
    {

        public AtmosObject AtmosObject { get; set; }

        public PlacedTileObject PlacedTileObject => GetComponent<PlacedTileObject>();

        public int PipeNetIndex { get; set; }

        public Vector2Int WorldOrigin { get; private set; }

        public TileLayer TileLayer { get; private set; }

        public override void OnStartServer()
        {
            base.OnStartServer();

            AtmosObject = new(new int2(0, 0), 0.5f);

            if (Subsystems.Get<PipeSystem>().IsSetUp)
            {
                Subsystems.Get<PipeSystem>().RegisterPipe(this);
            }
            else
            {
                Subsystems.Get<PipeSystem>().OnSystemSetUp += () => Subsystems.Get<PipeSystem>().RegisterPipe(this);
            }

            WorldOrigin = PlacedTileObject.WorldOrigin;
            TileLayer = PlacedTileObject.Layer;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            Subsystems.Get<PipeSystem>().RemovePipe(this);
        }
    }
}
