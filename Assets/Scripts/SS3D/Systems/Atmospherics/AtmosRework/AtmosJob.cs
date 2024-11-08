using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public struct AtmosJob
    {
        public AtmosMap map;
        public List<TileAtmosObject> atmosTiles;
        public List<IAtmosLoop> atmosDevices;
        public NativeArray<AtmosObject> nativeAtmosTiles;
        public NativeArray<AtmosObject> nativeAtmosDevices;

        public AtmosJob(AtmosMap map, List<TileAtmosObject> atmosTiles, List<IAtmosLoop> atmosDevices)
        {
            this.map = map;
            this.atmosTiles = atmosTiles;
            this.atmosDevices = atmosDevices;

            nativeAtmosTiles = new NativeArray<AtmosObject>(atmosTiles.Count, Allocator.Persistent);
            nativeAtmosDevices = new NativeArray<AtmosObject>(atmosDevices.Count, Allocator.Persistent);

            LoadNativeArrays();
            LoadNeighboursToArray();
        }

        public void Destroy()
        {
            nativeAtmosTiles.Dispose();
            nativeAtmosDevices.Dispose();
        }

        public int CountActive()
        {
            int counter = 0;

            foreach (var atmosObject in nativeAtmosTiles)
            {
                if (atmosObject.atmosObject.state == AtmosState.Active ||
                    atmosObject.atmosObject.state == AtmosState.Semiactive)
                    counter++;
            }

            return counter;
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

            for (int i = 0; i < nativeAtmosTiles.Length; i++)
            {
                atmosTiles[i].SetAtmosObject(nativeAtmosTiles[i]);
            }

            for (int i = 0; i < nativeAtmosDevices.Length; i++)
            {
                atmosDevices[i].SetAtmosObject(nativeAtmosDevices[i]);
            }
        }

        private void LoadNativeArrays()
        {
            for (int i = 0; i < atmosTiles.Count; i++)
            {
                nativeAtmosTiles[i] = atmosTiles[i].GetAtmosObject();
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
            for (int tileIndex = 0; tileIndex < atmosTiles.Count; tileIndex++)
            {
                // Retrieve the neighbours that were set before
                TileAtmosObject[] neighbours = atmosTiles[tileIndex].GetNeighbours();

                // For each neighbour
                for (int neighbourIndex = 0; neighbourIndex < neighbours.Length; neighbourIndex++)
                {
                    // Find index
                    int foundIndex = atmosTiles.FindIndex(tileObject => tileObject == neighbours[neighbourIndex]);

                    // Get corresponding atmos object
                    AtmosObject atmosObject = nativeAtmosTiles[tileIndex];

                    // Set index for object
                    atmosObject.SetNeighbourIndex(neighbourIndex, foundIndex);

                    // Write back info into native array
                    nativeAtmosTiles[tileIndex] = atmosObject;
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

    //[BurstCompile(CompileSynchronously = true)]
    struct SimulateFluxJob : IJob
    {
        public NativeArray<AtmosObject> buffer;
        public float dt;

        /// <summary>
        /// Set the internal neighbour state based on the neighbour
        /// </summary>
        /// <param name="index"></param>
        private void LoadNeighbour(int ownIndex, int neighbourIndex, int neighbourOffset)
        {
            AtmosObjectInfo info = new AtmosObjectInfo()
            {
                state = buffer[neighbourIndex].atmosObject.state,
                container = buffer[neighbourIndex].atmosObject.container,
                bufferIndex = neighbourIndex,
                velocity = buffer[neighbourIndex].atmosObject.velocity,
            };

            AtmosObject writeObject = buffer[ownIndex];
            writeObject.SetNeighbour(info, neighbourOffset);
            buffer[ownIndex] = writeObject;
        }

        /// <summary>
        /// Modify the neighbour based on the internal update
        /// </summary>
        /// <param name="index"></param>
        private void SetNeighbour(int ownIndex, int neighbourIndex, int neighbourOffset)
        {
            AtmosObject writeObject = buffer[neighbourIndex];
            writeObject.atmosObject = buffer[ownIndex].GetNeighbour(neighbourOffset);
            buffer[neighbourIndex] = writeObject;
        }

        public void Execute()
        {
            for (int index = 0; index < buffer.Length; index++)
            {
                if (buffer[index].atmosObject.state == AtmosState.Active || buffer[index].atmosObject.state == AtmosState.Semiactive)
                {
                    // Load neighbour
                    for (int i = 0; i < 4; i++)
                    {
                        int neighbourIndex = buffer[index].GetNeighbourIndex(i);

                        if (neighbourIndex != -1)
                        {
                            LoadNeighbour(index, neighbourIndex, i);
                        }
                    }

                    // Do actual work
                    buffer[index] = AtmosCalculator.SimulateFlux(buffer[index], dt);

                    // Set neighbour
                    for (int i = 0; i < 4; i++)
                    {
                        int neighbourIndex = buffer[index].GetNeighbourIndex(i);

                        if (neighbourIndex != -1)
                        {
                            SetNeighbour(index, neighbourIndex, i);
                        }
                    }
                }
            }
        }
    }
}