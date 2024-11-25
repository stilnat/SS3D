
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
    public class AtmosManager : Core.Behaviours.System
    {
        public Action AtmosTick;

        public float UpdateRate = 0.1f;

        private TileSystem _tileManager;

        private float _lastStep;

        private bool _initCompleted = false;

        // This list track job handles to complete jobs after a while.
        private NativeList<JobHandle> _jobHandles;

        // This is purely for debugging purposes. When false, the jobs execute all on the main thread, allowing easy debugging.
        [SerializeField]
        private bool _usesParallelComputation;

        // True when jobs are scheduled, false after making sure they completed.
        private bool _jobsScheduled;

        private List<IAtmosLoop> _atmosDevices = new();

        [SerializeField]
        private float _viscosity = 1f;
        
        public AtmosMap Map { get; private set; }


        private void Start()
        {
            _tileManager = Subsystems.Get<TileSystem>();

            // Initialization is invoked by the tile manager
            _tileManager.TileSystemLoaded += Initialize;
            _jobHandles = new(1, Allocator.Persistent);
        }

        private void Update()
        {
            if (!_initCompleted)
            {
                return;
            }

            if (Time.fixedTime >= _lastStep + UpdateRate)
            {
                float dt = Time.fixedTime - _lastStep;

                _lastStep = Time.fixedTime;
                
                Map.Simulate(dt, _viscosity);

                //AtmosTick?.Invoke();
            }
        }

        public void RemoveAtmosDevice(IAtmosLoop atmosDevice)
        {
            _atmosDevices.Remove(atmosDevice);
        }

        public void RegisterAtmosDevice(IAtmosLoop atmosDevice)
        {
            _atmosDevices.Add(atmosDevice);
        }
        
        private void Initialize()
        {
            if (_tileManager == null || _tileManager.CurrentMap == null)
            {
                Debug.LogError("AtmosManager couldn't find the tilemanager or map.");

                return;
            }

            TileMap map = _tileManager.CurrentMap;
            Map = new(map, map.Name, 301);
            _initCompleted = true;
        }
    }
}