
using SS3D.Core;
using SS3D.Systems.Tile;
using System;
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
    public class AtmosManager : MonoBehaviour
    {
        public bool ShowUpdate = true;
        public float UpdateRate = 0.5f;
       
        private TileSystem tileManager;
        private List<AtmosMap> atmosMaps;
        private List<AtmosJob> atmosJobs;
        
        private float lastStep;
        private bool initCompleted = false;

        private readonly List<IDisposable> _nativeStructureToDispose = new(); 
        private NativeList<JobHandle> _jobHandles;

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
            _jobHandles = new(1, Allocator.Persistent);
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

        private void LateUpdate()
        {
            CompleteJobs();
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

        private void CompleteJobs()
        {
            if (_usesParallelComputation)
            {
                JobHandle.CompleteAll(_jobHandles);
                _jobHandles.Clear();
            }

            // Write back the results
            atmosJobs.ForEach(x => x.WriteResultsToList());

            // Clean up
            //_jobHandles.Dispose();
            foreach (IDisposable disposable in _nativeStructureToDispose)
            {
                disposable.Dispose();
            }
            _nativeStructureToDispose.Clear();
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



        private int StepAtmos(float deltaTime)
        {
            s_StepPerfMarker.Begin();
            int counter = atmosJobs.Sum(x => x.CountActive());
            ScheduleJobs(deltaTime);
            s_StepPerfMarker.End();

            return counter;
        }

        private void ScheduleJobs(float deltaTime)
        {
            // Loop through every map
            foreach (AtmosJob atmosJob in atmosJobs)
            {
                NativeArray<int2> chunkKeyBuffer = new(atmosJob.Map.GetAtmosChunks().Select(x => new int2(x.GetKey().x, x.GetKey().y)).ToArray(), Allocator.TempJob);

                NativeHashMap<int2, int> chunkKeyHashMap = new(chunkKeyBuffer.Length, Allocator.TempJob);
                for (int i = 0; i < chunkKeyBuffer.Length; i++)
                {
                    chunkKeyHashMap.Add(chunkKeyBuffer[i], i);
                }

                _nativeStructureToDispose.Add(chunkKeyHashMap);
                _nativeStructureToDispose.Add(chunkKeyBuffer);

                SimulateFluxJob simulateTilesJob = new (atmosJob.NativeAtmosTiles, chunkKeyHashMap, atmosJob.MoleTransferArray, 16, deltaTime);
                TransferGasJob transferGasJob = new (atmosJob.MoleTransferArray, atmosJob.NativeAtmosTiles);

                if (_usesParallelComputation)
                {
                    JobHandle simulateTilesHandle = simulateTilesJob.Schedule(atmosJob.AtmosTiles.Count, 100);
                    JobHandle transferGasHandle = transferGasJob.Schedule(simulateTilesHandle);
                    _jobHandles.Add(simulateTilesHandle);
                    _jobHandles.Add(transferGasHandle);
                }
                else
                {
                    simulateTilesJob.Run(atmosJob.AtmosTiles.Count);
                    transferGasJob.Run();
                }
            }


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
    }
}