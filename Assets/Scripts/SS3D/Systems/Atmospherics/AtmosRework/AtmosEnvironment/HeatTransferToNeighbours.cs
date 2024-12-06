
using Unity.Mathematics;

namespace SS3D.Engine.AtmosphericsRework
{
    public readonly struct HeatTransferToNeighbours
    {
        public readonly int IndexFrom;
        public readonly float4 TransferHeat;

        public HeatTransferToNeighbours(int indexFrom, float4 transferHeat)
        {
            IndexFrom = indexFrom;
            TransferHeat = transferHeat;
        }
    }
}