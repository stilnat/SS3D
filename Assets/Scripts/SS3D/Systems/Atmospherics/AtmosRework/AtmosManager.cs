
using SS3D.Core;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Random = Unity.Mathematics.Random;

namespace SS3D.Engine.AtmosphericsRework
{
    public class AtmosManager : MonoBehaviour
    {
        public bool ShowUpdate = true;
        public float UpdateRate = 0.5f;

        private TileSystem tileManager;
        private List<AtmosMap> atmosMaps;
        private List<AtmosJobPersistentData> atmosJobs;

        private float lastStep;
        private bool initCompleted = false;

        private readonly List<IDisposable> _nativeStructureToDispose = new();
        private NativeList<JobHandle> _jobHandles;

        // Performance markers
        static ProfilerMarker s_PreparePerfMarker = new ProfilerMarker("Atmospherics.Initialize");
        static ProfilerMarker s_StepPerfMarker = new ProfilerMarker("Atmospherics.Step");

        private static ProfilerMarker PipePerfMarker = new ProfilerMarker("Atmospherics.Pipes");

        private static ProfilerMarker BullshitPerfMarker = new ProfilerMarker("Atmospherics.Bullshit");

        // This is purely for debugging purposes. When false, the jobs execute all on the main thread, allowing easy debugging.
        [SerializeField]
        private bool _usesParallelComputation;

        // True when jobs are scheduled, false after making sure they completed.
        private bool _jobsScheduled;


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
                // If the time step is smaller than the number of frames waited by the DelayCompleteJob method, then its necessary to ensure jobs are complete before proceeding
                if (_jobsScheduled)
                {
                    CompleteJobs();
                }

                float dt = Time.fixedTime - lastStep;
                
                ScheduleJobs(dt);
                StartCoroutine(DelayCompleteJob());

                lastStep = Time.fixedTime;
            }
        }

        public AtmosContainer GetAtmosContainer(Vector3 worldPosition, TileLayer layer)
        {
            foreach (AtmosMap map in atmosMaps)
            {
                AtmosContainer atmos = map.GetTileAtmosObject(worldPosition, layer);

                if (atmos != null)
                    return atmos;
            }

            return null;
        }

        public List<AtmosJobPersistentData> GetAtmosJobs()
        {
            return atmosJobs;
        }


        public void AddGas(Vector3 worldPosition, CoreAtmosGasses gas, float amount, TileLayer layer)
        {
            AtmosContainer tile = GetAtmosContainer(worldPosition, layer);

            if (tile != null)
            {
                atmosJobs.FirstOrDefault(x => x.Map == tile.Map).AddGas(tile, gas, amount);
            }
        }

        public void RandomizeAllGasses(float maxAmount)
        {
            atmosJobs.ForEach(job => job.RandomizeAllGasses(maxAmount));
        }

        public void ClearAllGasses()
        {
            atmosJobs.ForEach(job => job.ClearAllGasses());
        }

        public void RemoveGas(Vector3 worldPosition, CoreAtmosGasses gas, float amount, TileLayer layer)
        {
            AtmosContainer tile = GetAtmosContainer(worldPosition, layer);

            if (tile != null)
            {
                atmosJobs.FirstOrDefault(x => x.Map == tile.Map).RemoveGas(tile, gas, amount);
            }
        }

        public void AddHeat(Vector3 worldPosition, float amount, TileLayer layer)
        {
            AtmosContainer tile = GetAtmosContainer(worldPosition, layer);

            if (tile != null)
            {
                // Update the gas amount. Keep in mind that this is a value type.
                AtmosObject atmosObject = tile.AtmosObject;
                atmosObject.AddHeat(amount);
                tile.AtmosObject = atmosObject;
            }
        }

        public void RemoveHeat(Vector3 worldPosition, float amount, TileLayer layer)
        {
            AtmosContainer tile = GetAtmosContainer(worldPosition, layer);

            if (tile != null)
            {
                // Update the gas amount. Keep in mind that this is a value type.
                AtmosObject atmosObject = tile.AtmosObject;
                atmosObject.RemoveHeat(amount);
                tile.AtmosObject = atmosObject;
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
            foreach (IDisposable disposable in _nativeStructureToDispose)
            {
                disposable.Dispose();
            }

            _nativeStructureToDispose.Clear();
            _jobsScheduled = false;
        }

        private void CreateAtmosMaps()
        {
            int chunkCounter = 0;
            atmosMaps.Clear();

            // Create identical atmos chunks for each tile chunk
            TileMap map = tileManager.CurrentMap;

            AtmosMap atmosMap = new(map, map.Name);

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

            CreateAtmosMaps();

            int initCounter = 0;

            foreach (AtmosMap map in atmosMaps)
            {

                List<AtmosContainer> tiles = new();
                List<AtmosContainer> pipesLeft = new();


                // Add tiles in chunk
                foreach (AtmosChunk chunk in map.GetAtmosChunks())
                {
                    List<AtmosContainer> tileAtmosObjects = chunk.GetAllTileAtmosObjects();
                    tileAtmosObjects.ForEach(tile => tile.Initialize());
                    tiles.AddRange(tileAtmosObjects);

                    List<AtmosContainer> pipeLeftAtmosObjects = chunk.GetAllPipeLeftAtmosObjects();
                    pipeLeftAtmosObjects.ForEach(tile => tile.Initialize());
                    pipesLeft.AddRange(pipeLeftAtmosObjects);
                }

                AtmosJobPersistentData atmosJob = new(map, tiles, pipesLeft);
                atmosJobs.Add(atmosJob);

                initCounter += tiles.Count;
            }

            if (ShowUpdate)
            {
                Debug.Log($"AtmosManager: Finished initializing {initCounter} tiles");
            }

            initCompleted = true;
        }



        /// <summary>
        /// Schedule all the atmos jobs
        /// </summary>
        private void ScheduleJobs(float deltaTime)
        {

            // Loop through every map
            foreach (AtmosJobPersistentData atmosJob in atmosJobs)
            {
                atmosJob.Refresh();
                ScheduleTileObjectJobs(atmosJob, deltaTime);
            }

            _jobsScheduled = true;
        }


        private void ScheduleTileObjectJobs(AtmosJobPersistentData atmosJob, float deltaTime)
        {
            // compute neighbour indexes of tile atmos objects. TODO :  No need to run this job unless a new chunk was created
            ComputeIndexesJob computeIndexesJob = new(atmosJob.NeighbourTileIndexes, atmosJob.NativeAtmosTiles, atmosJob.ChunkKeyHashMap, 16);

            SetActiveJob setActiveJob = new(atmosJob.NativeAtmosTiles, atmosJob.NeighbourTileIndexes, atmosJob.ActiveEnvironmentIndexes, atmosJob.SemiActiveEnvironmentIndexes);

            ComputeFluxesJob diffusionFluxJob = new(atmosJob.NativeAtmosTiles, atmosJob.MoleTransferArray, atmosJob.NeighbourTileIndexes, atmosJob.SemiActiveEnvironmentIndexes, deltaTime, false);
            TransferFluxesJob transferDiffusionFluxJob = new(atmosJob.MoleTransferArray, atmosJob.NativeAtmosTiles, atmosJob.NeighbourTileIndexes, atmosJob.ActiveTransferIndex, atmosJob.SemiActiveEnvironmentIndexes, true);

            ComputeFluxesJob activeFluxesJob = new(atmosJob.NativeAtmosTiles, atmosJob.MoleTransferArray, atmosJob.NeighbourTileIndexes, atmosJob.ActiveEnvironmentIndexes, deltaTime, true);
            TransferFluxesJob transferActiveFluxesJob = new(atmosJob.MoleTransferArray, atmosJob.NativeAtmosTiles,
                atmosJob.NeighbourTileIndexes, atmosJob.ActiveTransferIndex, atmosJob.ActiveEnvironmentIndexes, false);

            ComputeVelocityJob velocityJob = new(atmosJob.NativeAtmosTiles, atmosJob.MoleTransferArray);

            // Pipe stuff
            ComputeFluxesJob pipeActiveFluxJob = new(atmosJob.NativeAtmosPipesLeft, atmosJob.PipeMoleTransferArray, atmosJob.NeighbourTileIndexes, atmosJob.ActiveLeftPipeIndexes, deltaTime, true);
            TransferFluxesJob transferPipeFluxesJob = new(atmosJob.PipeMoleTransferArray, atmosJob.NativeAtmosPipesLeft,
                atmosJob.NeighbourTileIndexes, atmosJob.PipeActiveTransferIndex, atmosJob.ActiveLeftPipeIndexes, false);

            if (_usesParallelComputation)
            {
                JobHandle computeIndexesHandle = computeIndexesJob.Schedule(atmosJob.AtmosTiles.Count, 64);
                JobHandle setActiveHandle = setActiveJob.Schedule(atmosJob.AtmosTiles.Count, 64, computeIndexesHandle);
                JobHandle diffusionFluxHandle = diffusionFluxJob.Schedule(atmosJob.SemiActiveEnvironmentIndexes.Length, 64, setActiveHandle);
                JobHandle transferDiffusionFluxHandle = transferDiffusionFluxJob.Schedule(diffusionFluxHandle);
                JobHandle activeFluxHandle = activeFluxesJob.Schedule(atmosJob.ActiveEnvironmentIndexes.Length, 64, transferDiffusionFluxHandle);
                JobHandle transferActiveFluxHandle = transferActiveFluxesJob.Schedule(activeFluxHandle);
                JobHandle computeVelocityHandle = velocityJob.Schedule(atmosJob.AtmosTiles.Count, 64, transferActiveFluxHandle);

                JobHandle pipeActiveFluxHandle = pipeActiveFluxJob.Schedule(atmosJob.ActiveLeftPipeIndexes.Length, 64, computeIndexesHandle);
                JobHandle transferPipeActiveFluxHandle = transferPipeFluxesJob.Schedule(pipeActiveFluxHandle);

                _jobHandles.Add(computeIndexesHandle);
                _jobHandles.Add(setActiveHandle); 
                _jobHandles.Add(diffusionFluxHandle);
                _jobHandles.Add(transferDiffusionFluxHandle);
                _jobHandles.Add(activeFluxHandle);
                _jobHandles.Add(transferActiveFluxHandle);
                _jobHandles.Add(computeVelocityHandle);


                _jobHandles.Add(pipeActiveFluxHandle);
                _jobHandles.Add(transferPipeActiveFluxHandle);
            }
            else
            {
                computeIndexesJob.Run(atmosJob.AtmosTiles.Count);
                setActiveJob.Run(atmosJob.AtmosTiles.Count);
                diffusionFluxJob.Run(atmosJob.SemiActiveEnvironmentIndexes.Length);
                transferDiffusionFluxJob.Run();
                activeFluxesJob.Run(atmosJob.ActiveEnvironmentIndexes.Length);
                transferActiveFluxesJob.Run();
                velocityJob.Run(atmosJob.AtmosTiles.Count);

                pipeActiveFluxJob.Run(atmosJob.ActiveLeftPipeIndexes.Length);
                transferPipeFluxesJob.Run();
            }
        }

        /// <summary>
        /// Delay as much as possible the job main thread completion, as the maximum allowed for temp allocation is 4 frame. Delaying allows
        /// spreading the jobs computation as much as possible, as we do not wish to have them all executed on a single frame, that can cause lag spikes.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DelayCompleteJob()
        {
            yield return null;
            yield return null;
            yield return null;

            if (_jobsScheduled)
            {
                CompleteJobs();
            }
        }

        private void OnDestroy()
        {
            if (atmosMaps == null || atmosJobs == null)
            {
                return;
            }

            foreach (AtmosMap map in atmosMaps)
            {
                map.Clear();
            }

            atmosMaps.Clear();

            foreach (AtmosJobPersistentData job in atmosJobs)
            {
                job.Destroy();
            }

            atmosJobs.Clear();
        }
    }
}