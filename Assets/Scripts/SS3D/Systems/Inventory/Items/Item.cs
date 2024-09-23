﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Coimbra;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Attributes;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Interactions;
using SS3D.Systems.Selection;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using AssetDatabase = UnityEditor.AssetDatabase;
using UnityEditor;
#endif

namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// An item describes what is held in a container.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(Selectable))]
    [RequiredLayer("Items")]
    public class Item : InteractionSource, IInteractionTarget, IWorldObjectAsset
    {
        [SerializeField]
#if UNITY_EDITOR
        [ReadOnly]
        [Header("This field is filled automatically by the AssetData system.")]
#endif
        private WorldObjectAssetReference _asset;

        #region Item
        [Header("Item settings")]
        [FormerlySerializedAs("Name")]
        [SerializeField] private string _name;

        [FormerlySerializedAs("Weight")]
        [SerializeField] private float _weight;

        [FormerlySerializedAs("Traits")]
        [SerializeField] private List<Trait> _startingTraits;

        [SerializeField] private Rigidbody _rigidbody;

        private Sprite _sprite;

        [Header("Attachment settings")]

        [Tooltip("a point we use to know how the item should be oriented when held in a hand")]
        public Transform AttachmentPoint;

        [Tooltip("same point but for the left hand, in cases where it's needed")]
        public Transform AttachmentPointAlt;

        /// <summary>
        /// The list of characteristics this Item has
        /// </summary>
        [SyncObject]
        private readonly SyncList<Trait> _traits = new();

        /// <summary>
        /// Where the item is stored
        /// </summary>
        [SyncVar]
        private AttachedContainer _container;

        public string Name => _name;

        public Holdable Holdable { get; private set; }

        public ReadOnlyCollection<Trait> Traits => ((List<Trait>) _traits.Collection).AsReadOnly();

        /// <summary>
        /// Where the item is stored
        /// </summary>
        public AttachedContainer Container => _container;

        private bool _initialised = false;
        
        /// <summary>
        /// All colliders, related to the item, except of colliders, related to stored items
        /// </summary>
        private Collider[] _nativeColliders;
        /// <summary>
        /// All colliders, related to the item, except of colliders, related to stored items
        /// </summary>
        public Collider[] NativeColliders
        {
            get
            {
                if (_nativeColliders == null)
                {
                    _nativeColliders = GetNativeColliders();
                }
                return _nativeColliders;
            }
            set => _nativeColliders = value;
        }

        public WorldObjectAssetReference Asset
        {
            get => _asset;
            set
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Serilog.Log.Warning($"Field {nameof(Asset)} is being modified in runtime. This should not happen in normal conditions.");
                }
                _asset = value;
            }
        }

        public Item Prefab => Asset.Get<Item>();

        /// <summary>
        /// Initialise this item fields. Can only be called once.
        /// </summary>
        public void Init(string itemName, float weight,  List<Trait> traits)
        {
            if (_initialised)
            {
                Log.Error(this, "Item already initialised, returning");
                return;
            }
            _name = itemName ?? string.Empty;
            _weight = weight;
            _traits.AddRange(traits);
            _initialised = true;
        }

        /// <summary>
        /// The sprite that is shown in the container slot
        /// </summary>
        public Sprite ItemSprite
        {
            get => InventorySprite();
            set => _sprite = value;
        }
        
        protected override void OnStart()
        {
            base.OnStart();

            Holdable = GetComponent<Holdable>();

            foreach (Animator animator in GetComponents<Animator>())
            {
                animator.keepAnimatorControllerStateOnDisable = true;
            }

            // Clients don't need to calculate physics for rigidbodies as this is handled by the server
            if (_rigidbody != null && IsClientOnly)
            {
                _rigidbody.isKinematic = true;
            }

            _nativeColliders ??= GetNativeColliders();
            Debug.Log("Start " + name);
        }

        /// <summary>
        /// Get all colliders, related to the item, except of colliders, related to stored items
        /// </summary>
        private Collider[] GetNativeColliders()
        {
            List<Collider> collidersToExcept = new();
            AttachedContainer[] containers = GetComponentsInChildren<AttachedContainer>();
            foreach (Item item in containers.SelectMany(container => container.Items))
            {
                collidersToExcept.AddRange(item.GetComponentsInChildren<Collider>());
            }
            return GetComponentsInChildren<Collider>().Except(collidersToExcept).ToArray();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _traits.AddRange(_startingTraits);
        }


        [ServerOrClient]
        private Sprite InventorySprite()
        {
            if (_sprite == null)
            {
                _sprite = GenerateIcon();
            }
            return _sprite;
        }

        /// <summary>
        /// Destroys this item
        /// </summary>
        [Server]
        public void Delete()
        {
            Container.RemoveItem(this);

            if (GameObject != null)
            {
                ServerManager.Despawn(GameObject);
            }
        }

        /// <summary>
        /// Freezes the item, making it not move or collide
        /// </summary>
        [ServerOrClient]
        public void Freeze()
        {
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }
            ToggleCollider(false);
        }

        /// <summary>
        /// Unfreezes the item, restoring normal functionality
        /// </summary>
        [ServerOrClient]
        public void Unfreeze()
        {
            if (_rigidbody != null)
            {
                if (IsServer)
                    _rigidbody.isKinematic = false;
            }
            ToggleCollider(true);
        }
        
        /// <summary>
        /// Enable or disable all colliders related to the item. Does not touch any colliders that would belong to stored items (if there are any).
        /// TODO : might want to replace GetComponentsInChildren with a manual setup of the container list.
        /// </summary>
        [ServerOrClient]
        protected virtual void ToggleCollider(bool isEnable)
        {
            foreach (Collider collider in NativeColliders) 
            { 
                collider.enabled = isEnable;
            }
        }
        
        /// <param name="visible">Should the item be visible</param>
        [ServerOrClient]
        public void SetVisibility(bool visible)
        {
            // TODO: Make this handle multiple renderers, with different states
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer childRenderer in renderers)
            {
                childRenderer.enabled = visible;
            }
        }

        public bool IsVisible()
        {
            // TODO: Make this handle multiple renderers
            Renderer component = GetComponent<Renderer>();
            return component != null && component.enabled;
        }

        public virtual IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new PickupInteraction { Icon = null } };
        }

        // this creates the base interactions for an item, in this case, the drop interaction
        public override void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            base.CreateSourceInteractions(targets, interactions);
            DropInteraction dropInteraction = new();

            interactions.Add(new InteractionEntry(null, dropInteraction));
        }

        /// <summary>
        /// Checks if the item is currently stored in a container
        /// </summary>
        /// <returns></returns>
        [ServerOrClient]
        public bool IsInContainer()
        {
            return _container != null;
        }

        /// <summary>
        /// Describe this item properties.
        /// </summary>
        [ServerOrClient]
        public string Describe()
        {
            string traits = "";
            foreach (Trait trait in _traits)
            {
                traits += trait.Name + " ";
            }
            return $"{Name}, weight = {_weight}, traits = {traits}, container is {_container?.ContainerName}";
        }

        /// <summary>
        /// Checks if the item has a specific trait
        /// </summary>
        [ServerOrClient]
        public bool HasTrait(Trait trait)
        {
            return _traits.Contains(trait);
        }

        /// <summary>
        /// Modify the container of this item, can pass null to make this item not depending on any container.
        /// </summary>
        [Server]
        public void SetContainer(AttachedContainer newContainer)
        {
            _container = newContainer;
        }

       

        // Generate preview of the same object, but without stored items.
        [ServerOrClient]
        public Sprite GenerateIcon()
        {
            RuntimePreviewGenerator.BackgroundColor = new Color(0, 0, 0, 0);
            RuntimePreviewGenerator.OrthographicMode = true;
            // Find stored items
            AttachedContainer[] containers = GetComponentsInChildren<AttachedContainer>();
            // If stored items are found, temporarily set their parents to null,
            // so RuntimePreviewGenerator won't generate stored items
            Dictionary<Transform, Transform> storedItemsWithParents = new Dictionary<Transform, Transform>();
            foreach (AttachedContainer attachedContainer in containers)
            {
                
                IEnumerable<Item> storedItems = attachedContainer.Items;
                foreach (Item item in storedItems)
                {
                    Transform itemTransform = item.transform;
                    storedItemsWithParents.Add(itemTransform, itemTransform.parent);
                }
            }
            foreach (Transform storedItem in storedItemsWithParents.Keys)
            {
                storedItem.parent = null;
            }

            Transform previewObject = Instantiate(transform, null, false);
            previewObject.gameObject.hideFlags = HideFlags.HideAndDontSave;
            previewObject.GetComponent<Item>().SetVisibility(true);
            Sprite icon;
            try
            {
                Texture2D texture = RuntimePreviewGenerator.GenerateModelPreviewWithShader(previewObject,
                    Shader.Find("Legacy Shaders/Diffuse"), null, 128, 128);
                icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), 
                    new Vector2(0.5f, 0.5f), 100);
                icon.name = transform.name;
            }
            catch (NullReferenceException)
            {
                Log.Warning(this, "Can't generate icon for " + name + ".");
                icon = null;
            }
            // Return stored items back to their parents
            previewObject.gameObject.Dispose(false);
            foreach (KeyValuePair<Transform, Transform> storedItemWithParent in storedItemsWithParents)
            {
                storedItemWithParent.Key.parent = storedItemWithParent.Value;
            }
            return icon;
        }

        /// <summary>
        /// Add a new trait to this and sync it
        /// </summary>
        [Server]
        public void AddTrait(Trait trait)
        {
            if (_traits.Contains(trait))
            {
                Log.Warning(this, "item already contains trait {trait}", Logs.Generic, trait.Name);
                return;
            }
            _traits.Add(trait);
        }

        /// <summary>
        /// Remove a trait from this item.
        /// </summary>
        [Server]
        public void RemoveTraits(Trait trait)
        {
             _traits.Remove(trait);
        }

        #endregion

        #region Editor
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Make sure gizmo only draws in prefab mode
            if (EditorApplication.isPlaying || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                return;
            }

            Mesh handGuide = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Art/Models/Other/HoldGizmo.fbx", typeof(Mesh));

            // Don't even have to check without attachment
            if (AttachmentPoint != null)
            {
                Gizmos.color = new Color32(255, 120, 20, 170);
                Quaternion localRotation = AttachmentPoint.localRotation;
                Vector3 eulerAngles = localRotation.eulerAngles;
                Vector3 parentPosition = AttachmentPoint.parent.position;
                Vector3 position = AttachmentPoint.localPosition;
                // Draw a wire mesh of the rotated model
                Vector3 rotatedPoint = RotatePointAround(parentPosition, position, eulerAngles);
                rotatedPoint += new Vector3(0, position.z, position.y);
                Gizmos.DrawWireMesh(handGuide, AttachmentPoint.position, localRotation);
            }

            // Same for the Left Hand
            if (AttachmentPointAlt != null)
            {
                Gizmos.color = new Color32(255, 120, 20, 170);
                Quaternion localRotation = AttachmentPointAlt.localRotation;
                Vector3 eulerAngles = localRotation.eulerAngles;
                Vector3 parentPosition = AttachmentPointAlt.parent.position;
                Vector3 position = AttachmentPointAlt.localPosition;
                // Draw a wire mesh of the rotated model
                Vector3 rotatedPoint = RotatePointAround(parentPosition, position, eulerAngles);
                rotatedPoint += new Vector3(0, position.z, position.y);
                Gizmos.DrawWireMesh(handGuide, AttachmentPointAlt.position, localRotation);
            }
        }

        private static Vector3 RotatePointAround(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot);
        }
#endif
        #endregion
    }
}
