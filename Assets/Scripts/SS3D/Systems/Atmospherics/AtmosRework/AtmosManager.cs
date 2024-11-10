﻿
using SS3D.Core;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    [ExecuteAlways]
    public class AtmosManager : MonoBehaviour
    {
        public bool ShowUpdate = true;
        public float UpdateRate = 0.5f;
       
        private TileSystem tileManager;
        private List<AtmosMap> atmosMaps;
        private List<AtmosJob> atmosJobs;
        private NativeArray<JobHandle> jobHandles;
        private float lastStep;
        private bool initCompleted = false;

        // Performance markers
        static ProfilerMarker s_PreparePerfMarker = new ProfilerMarker("Atmospherics.Initialize");
        static ProfilerMarker s_StepPerfMarker = new ProfilerMarker("Atmospherics.Step");

        // This is purely for debugging purposes. When false, the jobs execute all on the main threads, allowing easy debugging.
        [SerializeField]
        private bool _usesParallelComputation;

        private void Start()
        {
            tileManager = Subsystems.Get<TileSystem>();
            // Initialization is invoked by the tile manager
            tileManager.TileSystemLoaded += Initialize;
        }

        private void Awake()
        {
            initCompleted = false;
        }

        private void OnDestroy()
        {
            if (atmosMaps == null || atmosJobs == null)
                return;

            foreach (AtmosMap map in atmosMaps)
            {
                map.Clear();
            }

            atmosMaps.Clear();

            foreach (AtmosJob job in atmosJobs)
            {
                job.Destroy();
            }

            atmosJobs.Clear();
        }

        private void Update()
        {
            if (!initCompleted)
                return;

            if (Time.fixedTime >= lastStep + UpdateRate)
            {
                float dt = Time.fixedTime - lastStep;
                int tileCounter = StepAtmos(dt);

                if (ShowUpdate)
                    Debug.Log("Atmos loop took: " + (dt - UpdateRate) + " seconds, simulating " + tileCounter + " active atmos objects. Fixed update rate: " + UpdateRate);
                lastStep = Time.fixedTime;
            }
        }

        private void CreateAtmosMaps()
        {
            int chunkCounter = 0;
            atmosMaps.Clear();

            // Create identical atmos chunks for each tile chunk
            TileMap map = tileManager.CurrentMap;

            AtmosMap atmosMap = new AtmosMap(map, map.Name);

            List<TileChunk> tileChunks = map.Chunks;
            
            foreach (TileChunk chunk in tileChunks)
            {
                atmosMap.CreateChunkFromTileChunk(chunk.Key, chunk.Origin);
                chunkCounter++;
            }

            atmosMaps.Add(atmosMap);
        

            Debug.Log("AtmosManager: recreated " + chunkCounter + " chunks from the tilemap");
        }

        private void Initialize()
        {
            atmosMaps = new();
            atmosJobs = new();

            if (tileManager == null || tileManager.CurrentMap == null)
            {
                Debug.LogError("AtmosManager couldn't find the tilemanager or map.");
                return;
            }

            if (atmosJobs != null)
            {
                atmosJobs.ForEach(job => job.Destroy());
                atmosJobs.Clear();
            }

            s_PreparePerfMarker.Begin();

            CreateAtmosMaps();

            if (ShowUpdate)
                Debug.Log("AtmosManager: Initializing tiles");


            int initCounter = 0;
            foreach (AtmosMap map in atmosMaps)
            {
                
                List<TileAtmosObject> tiles = new();
                List<IAtmosLoop> devices = new();

                // Add tiles in chunk
                foreach (AtmosChunk chunk in map.GetAtmosChunks())
                {
                    var tileAtmosObjects = chunk.GetAllTileAtmosObjects();

                    // Initialize the atmos tiles. Cannot be done in the tilemap as it may still be creating tiles.
                    tileAtmosObjects.ForEach(tile => tile.Initialize(map.GetLinkedTileMap()));
                    tiles.AddRange(tileAtmosObjects);
                }

                devices.AddRange(tileManager.GetComponentsInChildren<IAtmosLoop>());

                AtmosJob atmosJob = new AtmosJob(map, tiles, devices);
                atmosJobs.Add(atmosJob);

                initCounter += tiles.Count; 
            }

            if (ShowUpdate)
                Debug.Log($"AtmosManager: Finished initializing {initCounter} tiles");

            initCompleted = true;
        }

        public void AddGas(Vector3 worldPosition, CoreAtmosGasses gas, float amount)
        {

            var tile = GetAtmosTile(worldPosition);
            if (tile != null)
            {
                // Update the gas amount. Keep in mind that this is a value type.
                var atmosObject = tile.GetAtmosObject();
                atmosObject.AddGas(gas, amount);
                tile.SetAtmosObject(atmosObject);

                // Indicate a refresh for the AtmosJob. TODO: Optimize to not loop everything
                atmosJobs.ForEach(job => job.Refresh());
            }
        }

        public void ClearAllGasses()
        {
            foreach (AtmosMap map in atmosMaps)
            {
                foreach (AtmosChunk atmosChunk in map.GetAtmosChunks())
                {
                    foreach (TileAtmosObject tile in atmosChunk.GetAllTileAtmosObjects())
                    {
                        var atmosObject = tile.GetAtmosObject();
                        atmosObject.ClearCoreGasses();
                        tile.SetAtmosObject(atmosObject);
                    }
                }
            }

            atmosJobs.ForEach(job => job.Refresh());
        }

        public void RemoveGas(Vector3 worldPosition, CoreAtmosGasses gas, float amount)
        {
            var tile = GetAtmosTile(worldPosition);
            if (tile != null)
            {
                // Update the gas amount. Keep in mind that this is a value type.
                var atmosObject = tile.GetAtmosObject();
                atmosObject.RemoveGas(gas, amount);
                tile.SetAtmosObject(atmosObject);

                // Indicate a refresh for the AtmosJob. TODO: Optimize to not loop everything
                atmosJobs.ForEach(job => job.Refresh());
            }
        }

        public void AddHeat(Vector3 worldPosition, float amount)
        {
            var tile = GetAtmosTile(worldPosition);
            if (tile != null)
            {
                // Update the gas amount. Keep in mind that this is a value type.
                var atmosObject = tile.GetAtmosObject();
                atmosObject.AddHeat(amount);
                tile.SetAtmosObject(atmosObject);

                // Indicate a refresh for the AtmosJob. TODO: Optimize to not loop everything
                atmosJobs.ForEach(job => job.Refresh());
            }
        }

        public void RemoveHeat(Vector3 worldPosition, float amount)
        {
            var tile = GetAtmosTile(worldPosition);
            if (tile != null)
            {
                // Update the gas amount. Keep in mind that this is a value type.
                var atmosObject = tile.GetAtmosObject();
                atmosObject.RemoveHeat(amount);
                tile.SetAtmosObject(atmosObject);

                // Indicate a refresh for the AtmosJob. TODO: Optimize to not loop everything
                atmosJobs.ForEach(job => job.Refresh());
            }
        }

        private int StepAtmos(float deltaTime)
        {
            s_StepPerfMarker.Begin();
            int counter = 0;

            List<JobHandle> jobHandlesList = new();

            // Step 0: Loop through every map
            foreach (AtmosJob atmosJob in atmosJobs)
            {
                // Step 1: Simulate tiles
                SimulateFluxJob simulateTilesJob = new SimulateFluxJob()
                {
                    TileObjectBuffer = atmosJob.NativeAtmosTiles,
                    MoleTransfers = atmosJob.MoleTransferArray,
                    DeltaTime = deltaTime,
                    ChunkSize = 16,
                    ChunkKeyBuffer = new (atmosJob.Map.GetAtmosChunks().Select(x => new int2(x.GetKey().x, x.GetKey().y)).ToArray(), Allocator.TempJob),
                };

                counter += atmosJob.CountActive();
                

                if (_usesParallelComputation)
                {
                    JobHandle simulateTilesHandle = simulateTilesJob.Schedule(atmosJob.AtmosTiles.Count, 100);
                    jobHandlesList.Add(simulateTilesHandle);
                }
                else
                {
                    simulateTilesJob.Run(atmosJob.AtmosTiles.Count);
                }
            }

            if (_usesParallelComputation)
            {
                jobHandles = new(jobHandlesList.ToArray(), Allocator.TempJob);
                JobHandle.CompleteAll(jobHandles);
                jobHandles.Dispose();
            }




            // Step 4: Write back the results
            for (int k=0; k < atmosJobs.Count; k++)
            {
                AtmosJob job = atmosJobs[k];
                NativeArray<AtmosObject> copyAtmosTiles = job.NativeAtmosTiles;

                for (int i = 0; i < job.MoleTransferArray.Length; i++)
                {
                    int atmosObjectFromIndex = job.MoleTransferArray[i].IndexFrom;

                    // check the copy, as the active state might be changed by the adding and removal of gasses 
                    if (copyAtmosTiles[atmosObjectFromIndex].State != AtmosState.Active)
                        continue;

                    AtmosObject atmosObject = job.NativeAtmosTiles[atmosObjectFromIndex];
                    atmosObject.RemoveCoreGasses(job.MoleTransferArray[i].TransferOne.Moles);
                    atmosObject.RemoveCoreGasses(job.MoleTransferArray[i].TransferTwo.Moles); 
                    atmosObject.RemoveCoreGasses(job.MoleTransferArray[i].TransferThree.Moles);
                    atmosObject.RemoveCoreGasses(job.MoleTransferArray[i].TransferFour.Moles);
                    job.NativeAtmosTiles[atmosObjectFromIndex] = atmosObject;

                    AtmosObject neighbourOne = job.NativeAtmosTiles[job.MoleTransferArray[i].TransferOne.IndexTo];
                    AtmosObject neighbourTwo = job.NativeAtmosTiles[job.MoleTransferArray[i].TransferTwo.IndexTo];
                    AtmosObject neighbourThree = job.NativeAtmosTiles[job.MoleTransferArray[i].TransferThree.IndexTo];
                    AtmosObject neighbourFour = job.NativeAtmosTiles[job.MoleTransferArray[i].TransferFour.IndexTo];

                    neighbourOne.AddCoreGasses(job.MoleTransferArray[i].TransferOne.Moles);
                    neighbourTwo.AddCoreGasses(job.MoleTransferArray[i].TransferTwo.Moles);
                    neighbourThree.AddCoreGasses(job.MoleTransferArray[i].TransferThree.Moles);
                    neighbourFour.AddCoreGasses(job.MoleTransferArray[i].TransferFour.Moles);

                    job.NativeAtmosTiles[job.MoleTransferArray[i].TransferOne.IndexTo] = neighbourOne;
                    job.NativeAtmosTiles[job.MoleTransferArray[i].TransferTwo.IndexTo] = neighbourTwo;
                    job.NativeAtmosTiles[job.MoleTransferArray[i].TransferThree.IndexTo] = neighbourThree;
                    job.NativeAtmosTiles[job.MoleTransferArray[i].TransferFour.IndexTo] = neighbourFour;
                }
                job.WriteResultsToList();
                
            }

            s_StepPerfMarker.End();

            return counter;
        }

        public TileAtmosObject GetAtmosTile(Vector3 worldPosition)
        {
            foreach (var map in atmosMaps)
            {
                var atmosTile = map.GetTileAtmosObject(worldPosition);
                if (atmosTile != null)
                    return atmosTile;
            }

            return null;
        }

        public List<AtmosJob> GetAtmosJobs()
        {
            return atmosJobs;
        }
    }
}