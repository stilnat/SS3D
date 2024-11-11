using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace SS3D.Engine.AtmosphericsRework
{
    /// <summary>
    /// Structure used to interface between the atmos map, and the jobs that do the computation for the simulation.
    /// </summary>
    public struct AtmosJobPersistentData
    {
        public readonly AtmosMap Map;

        public readonly List<TileAtmosObject> AtmosTiles;

        public NativeArray<AtmosObject> NativeAtmosTiles;

        public NativeArray<MoleTransferToNeighbours> MoleTransferArray;

        public AtmosJobPersistentData(AtmosMap map, List<TileAtmosObject> atmosTiles, List<IAtmosLoop> atmosDevices)
        {
            Map = map;
            AtmosTiles = atmosTiles;
            NativeAtmosTiles = new(atmosTiles.Count, Allocator.Persistent);
            MoleTransferArray = new(atmosTiles.Count, Allocator.Persistent);

            LoadNativeArrays();
        }

        public void Destroy()
        {
            NativeAtmosTiles.Dispose();
        }

        public int CountActive()
        {
            return NativeAtmosTiles.Count(atmosObject => atmosObject.State == AtmosState.Active || atmosObject.State == AtmosState.Semiactive);
        }

        /// <summary>
        /// Refreshes the calculation array. Must be called when gas is added/removed from the system.
        /// </summary>
        public void Refresh()
        {
            LoadNativeArrays();
        }

        /// <summary>
        /// Writes back the results from the NativeContainers to the lists.
        /// </summary>
        public void WriteResultsToList()
        {
            for (int i = 0; i < NativeAtmosTiles.Length; i++)
            {
                AtmosTiles[i].SetAtmosObject(NativeAtmosTiles[i]);
            }
        }

        private void LoadNativeArrays()
        {
            for (int i = 0; i < AtmosTiles.Count; i++)
            {
                NativeAtmosTiles[i] = AtmosTiles[i].GetAtmosObject();
            }
        }
    }

}