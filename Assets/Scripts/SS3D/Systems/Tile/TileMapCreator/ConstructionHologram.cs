﻿using Coimbra;
using SS3D.Data.Generated;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Represent a hologram tile object used for construction, through the tilemap menu.
    /// </summary>
    public class ConstructionHologram
    {
        [SerializeField]
        private GameObject _hologram;

        private Vector3 _targetPosition;

        private Direction _direction;

        /// <summary>
        /// Build a new hologram
        /// </summary>
        /// <param name="ghostObject"> the game object we want to make a hologram from.</param>
        /// <param name="targetTargetPosition"> the initial position of the hologram in space.</param>
        /// <param name="dir"> the expected original direction. Note that not all directions are compatible with
        /// all tile objects. If it's not, it will choose another available direction.</param>
        public ConstructionHologram(GameObject ghostObject, Vector3 targetPosition, Direction dir, ConstructionMode mode = ConstructionMode.Valid)
        {
            DisableBehaviours(ghostObject);

            _hologram = ghostObject;
            _targetPosition = targetPosition;
            _direction = dir;

            if (ghostObject.TryGetComponent(out ICustomGhostRotation customRotationComponent)
                && !customRotationComponent.GetAllowedRotations().Contains(dir))
            {
                _direction = customRotationComponent.DefaultDirection;
            }

            if (ghostObject.TryGetComponent(out Rigidbody ghostRigidbody))
            {
                ghostRigidbody.useGravity = false;
                ghostRigidbody.isKinematic = true;
            }

            Collider[] colliders = ghostObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            ChangeHologramColor(mode);
        }

        public Direction Direction => _direction;

        public bool ActiveSelf => _hologram.activeSelf;

        public bool SetActive { set => _hologram.SetActive(value); }

        public Vector3 TargetPosition { get => _targetPosition; set => _targetPosition = value; }

        public GameObject Hologram => _hologram;

        /// <summary>
        /// Chooses which material to set on the ghost based on which mode we are building.
        /// </summary>
        /// <param name="mode"></param>
        public void ChangeHologramColor(ConstructionMode mode)
        {
            Material ghostMat = mode switch
            {
                ConstructionMode.Valid => Materials.ValidConstruction,
                ConstructionMode.Invalid => Materials.InvalidConstruction,
                ConstructionMode.Delete => Materials.DeleteConstruction,
                _ => null,
            };

            foreach (MeshRenderer mr in _hologram.GetComponentsInChildren<MeshRenderer>())
            {
                Material[] materials = mr.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = ghostMat;
                }

                mr.materials = materials;
            }

            foreach (SkinnedMeshRenderer smr in _hologram.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Material[] materials = smr.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = ghostMat;
                }

                smr.materials = materials;
            }
        }

        /// <summary>
        /// Smoothly change rotation and position for better visual effects.
        /// </summary>
        public void UpdateRotationAndPosition()
        {
            // Small offset is added so that meshes don't overlap with already placed objects.
            _hologram.transform.SetPositionAndRotation(
                Vector3.Lerp(_hologram.transform.position, _targetPosition + new Vector3(0, 0.1f, 0), Time.deltaTime * 15f),
                Quaternion.Lerp(_hologram.transform.rotation, Quaternion.Euler(0, TileHelper.GetRotationAngle(_direction), 0), Time.deltaTime * 15f));
        }

        /// <summary>
        /// Set the next allowed rotation, depends on the tile object.
        /// </summary>
        public void SetNextRotation()
        {
            if (_hologram.TryGetComponent(out ICustomGhostRotation customRotationComponent))
            {
                _direction = customRotationComponent.GetNextDirection(_direction);
            }
            else
            {
                _direction = TileHelper.GetNextCardinalDir(_direction);
            }
        }

        public void Destroy()
        {
            _hologram.Dispose(true);
            _hologram = null;
        }

        private void DisableBehaviours(GameObject ghostObject)
        {
            List<MonoBehaviour> components = ghostObject.GetComponentsInChildren<MonoBehaviour>()
                .Where(x => x is not ICustomGhostRotation).ToList();

            components.ForEach(x => x.enabled = false);
        }
    }
}
