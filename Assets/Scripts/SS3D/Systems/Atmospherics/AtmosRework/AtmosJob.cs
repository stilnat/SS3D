using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SS3D.Engine.AtmosphericsRework
{
    public struct AtmosJob
    {
        public AtmosMap Map;

        public List<TileAtmosObject> AtmosTiles;

        public List<IAtmosLoop> AtmosDevices;

        public NativeArray<AtmosObject> NativeAtmosTiles;

        public NativeArray<AtmosObject> ResultNativeAtmosTiles;

        public NativeArray<AtmosObject> NativeAtmosDevices;

        public AtmosJob(AtmosMap map, List<TileAtmosObject> atmosTiles, List<IAtmosLoop> atmosDevices)
        {
            Map = map;
            AtmosTiles = atmosTiles;
            AtmosDevices = atmosDevices;

            NativeAtmosTiles = new(atmosTiles.Count, Allocator.Persistent);
            NativeAtmosDevices = new(atmosDevices.Count, Allocator.Persistent);

            LoadNativeArrays();
            LoadNeighboursToArray();
        }

        public void Destroy()
        {
            NativeAtmosTiles.Dispose();
            NativeAtmosDevices.Dispose();
        }

        public int CountActive()
        {
            return NativeAtmosTiles.Count(atmosObject => atmosObject.atmosObject.State == AtmosState.Active || atmosObject.atmosObject.State == AtmosState.Semiactive);
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

            for (int i = 0; i < NativeAtmosDevices.Length; i++)
            {
                AtmosDevices[i].SetAtmosObject(NativeAtmosDevices[i]);
            }
        }

        private void LoadNativeArrays()
        {
            for (int i = 0; i < AtmosTiles.Count; i++)
            {
                NativeAtmosTiles[i] = AtmosTiles[i].GetAtmosObject();
            }

            /*
            for (int i = 0; i < atmosDevices.Count; i++)
            {
                nativeAtmosDevices[i] = atmosDevices[i];
            }
            */
        }

        private void LoadNeighboursToArray()
        {
            // For all tiles
            for (int tileIndex = 0; tileIndex < AtmosTiles.Count; tileIndex++)
            {
                // Retrieve the neighbours that were set before
                TileAtmosObject[] neighbours = AtmosTiles[tileIndex].GetNeighbours();

                // For each neighbour
                for (int neighbourIndex = 0; neighbourIndex < neighbours.Length; neighbourIndex++)
                {
                    // Find index
                    int foundIndex = AtmosTiles.FindIndex(tileObject => tileObject == neighbours[neighbourIndex]);

                    // Get corresponding atmos object
                    AtmosObject atmosObject = NativeAtmosTiles[tileIndex];

                    // Set index for object
                    atmosObject.SetNeighbourIndex(neighbourIndex, foundIndex);

                    // Write back info into native array
                    NativeAtmosTiles[tileIndex] = atmosObject;
                }
            }

            /*
            // For all devices and pipes
            for (int deviceIndex = 0; deviceIndex < atmosDevices.Count; deviceIndex++)
            {
                // Retrieve the neighbours that were set before
                TileAtmosObject[] neighbours = atmosDevices[deviceIndex].GetNeighbours();

                // For each neighbour
                for (int neighbourIndex = 0; neighbourIndex < neighbours.Length; neighbourIndex++)
                {
                    // Find index
                    int foundIndex = atmosDevices.FindIndex(tileObject => tileObject == neighbours[neighbourIndex]);

                    // Get corresponding atmos object
                    AtmosObject atmosObject = nativeAtmosTiles[deviceIndex];

                    // Set index for object
                    atmosObject.SetNeighbourIndex(neighbourIndex, foundIndex);

                    // Write back info into native array
                    nativeAtmosTiles[deviceIndex] = atmosObject;
                }
            }
            */
        }
    }
    
    //[BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    struct SimulateFluxJob : IJobParallelFor
    {

        // todo : using NativeDisableParallelForRestriction is a dirty trick to avoid doing proper code. This might lead to race conditions.
        // The issue is that each jobs need to access atmos tile outside its set of indexes
        // Unity recommends using a so called double buffering methods.
        // https://github.com/korzen/Unity3D-JobsSystemAndBurstSamples/blob/master/Assets/JobsAndBurst/Scripts/DoubleBufferingBasics.cs
        [NativeDisableParallelForRestriction]
        public NativeArray<AtmosObject> Buffer;

        [NativeDisableParallelForRestriction]
        public NativeArray<AtmosObject> ResultBuffer;

        public float DeltaTime;

        /// <summary>
        /// Set the internal neighbour state based on the neighbour
        /// </summary>
        private void LoadNeighbour(int ownIndex, int neighbourIndex, int neighbourOffset)
        {
            AtmosObjectInfo info = new (Buffer[neighbourIndex].atmosObject.State, Buffer[neighbourIndex].atmosObject.Container, Buffer[neighbourIndex].atmosObject.Velocity, neighbourIndex);

            AtmosObject writeObject = Buffer[ownIndex];
            writeObject.SetNeighbour(info, neighbourOffset);
            Buffer[ownIndex] = writeObject;
        }

        /// <summary>
        /// Modify the neighbour based on the internal update
        /// </summary>
        private void SetNeighbour(int ownIndex, int neighbourIndex, int neighbourOffset)
        {
            AtmosObject writeObject = Buffer[neighbourIndex];
            writeObject.atmosObject = Buffer[ownIndex].GetNeighbour(neighbourOffset);
            Buffer[neighbourIndex] = writeObject;
        }

        public void Execute(int index)
        {
            // TODO : We might need to set velocity of inactive atmosObject to 0 here ? Or maybe elsewhere, but velocity stays stuck sometimes on inactive atmosObject
            if (Buffer[index].atmosObject.State != AtmosState.Active && Buffer[index].atmosObject.State != AtmosState.Semiactive)
            {
                return;
            }

            // Load neighbour
            for (int i = 0; i < 4; i++)
            {
                int neighbourIndex = Buffer[index].GetNeighbourIndex(i);

                if (neighbourIndex != -1)
                {
                    LoadNeighbour(index, neighbourIndex, i);
                }
            }

            // TODO : Is it correct to rewrite to the same buffer the result of the computation ? One issue might be that doing so, the neighbours will take into account 
            // TODO : the state of the new atmos object, when it should do computation using the state before it was modified. 
            // Do actual work
            Buffer[index] = AtmosCalculator.SimulateFlux(Buffer[index], DeltaTime);

            // Set neighbour
            for (int i = 0; i < 4; i++)
            {
                int neighbourIndex = Buffer[index].GetNeighbourIndex(i);

                if (neighbourIndex != -1)
                {
                    SetNeighbour(index, neighbourIndex, i);
                }
            }
        }
    }
}