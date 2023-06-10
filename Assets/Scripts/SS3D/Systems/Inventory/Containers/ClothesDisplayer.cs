
using System.Collections.Generic;
using System.Linq;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using FishNet.Object.Synchronizing;
using UnityEditor;
using FishNet.Object;

namespace SS3D.Systems.Inventory.Containers
{
    public class ClothesDisplayer : NetworkActor
    {
        private struct ClothDisplayData
        {
            public ClothDisplayData(NetworkObject bodyPart, bool display, Item clothToDisplay)
            {
                _bodyPart= bodyPart;
                _display= display;
                _clothToDisplay= clothToDisplay;
            }
            public NetworkObject _bodyPart;
            public bool _display;
            public Item _clothToDisplay;
        }

        public HumanInventory _inventory;

        public Transform ClothesRoot;

        // Game objects on the human prefab to display clothes.
        public NetworkObject Hat;
        public NetworkObject Eyes;
        public NetworkObject Jumpsuit;

        public NetworkObject HandLeft;
        public NetworkObject HandRight;
        public NetworkObject FootLeft;
        public NetworkObject FootRight;
        public NetworkObject Identification;
        public NetworkObject Backpack;

        [SyncVar(OnChange = nameof(OnChange))]
        private ClothDisplayData _hatData;

        [SyncVar(OnChange = nameof(OnChange))]
        private ClothDisplayData _eyesData;

        [SyncVar(OnChange = nameof(OnChange))]
        private ClothDisplayData _jumpsuitData;

        [SyncVar(OnChange = nameof(OnChange))]
        private ClothDisplayData _handLeftData;

        [SyncVar(OnChange = nameof(OnChange))]
        private ClothDisplayData _handRightData;

        [SyncVar(OnChange = nameof(OnChange))]
        private ClothDisplayData _footLeftData;

        [SyncVar(OnChange = nameof(OnChange))]
        private ClothDisplayData _footRightData;

        [SyncVar(OnChange = nameof(OnChange))]
        private ClothDisplayData _identificationData;

        [SyncVar(OnChange = nameof(OnChange))]
        private ClothDisplayData _backpackData;



        private void OnChange(ClothDisplayData oldValue, ClothDisplayData newValue, bool asServer)
        {

            if (asServer) return;
            Debug.Log("jumpsuit changed");
            Debug.Log("display ? " + newValue._display + "item : " + newValue._clothToDisplay + "on body part : " + newValue._bodyPart );


            bool display = newValue._display;
            var bodyPart = newValue._bodyPart;
            var item = newValue._clothToDisplay;

            if (!bodyPart.TryGetComponent(out SkinnedMeshRenderer renderer))
            {
                Punpun.Warning(this, $"no skinned mesh renderer on game object {bodyPart}, can't display cloth");
                return;
            }

            if (display)
            {
                bodyPart.gameObject.SetActive(true);
                renderer.sharedMesh = item.gameObject.GetComponentInChildren<MeshFilter>().sharedMesh;
            }
            else
            {
                bodyPart.gameObject.SetActive(false);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
        }

        protected override void OnStart()
        {
            base.OnStart();
            _inventory.OnContainerContentChanged += ContainerContentChanged;
        }

        public void ContainerContentChanged(Container container, IEnumerable<Item> oldItems, IEnumerable<Item> newItems, ContainerChangeType type)
        {
            // If it's not a cloth type container.
            // It'd be probably better to just create "cloth container" inheriting from container to easily test that.
            if(container.ContainerType < ContainerType.Bag)
            {
                return;
            }

            // TODO : check that the change include a single item.
            // Also maybe don't check only new item for remove ?
            var item = newItems.FirstOrDefault();

            switch(type)
            {
                case ContainerChangeType.Add:
                    ShowCloth(container, item, true);
                    break;
                case ContainerChangeType.Remove:
                    ShowCloth(container, item, false);
                    break;
            }
        }

        // TODO complete with missing stuff (mask, etc..)
        public void ShowCloth(Container container, Item item, bool display)
        {
            switch (container.ContainerType)
            {
                case ContainerType.Identification:
                    _identificationData = new ClothDisplayData(Identification, display, item);
                    break;

                case ContainerType.Glasses:
                    _eyesData= new ClothDisplayData(Eyes, display, item);
                    break;

                case ContainerType.Mask:
                    break;

                case ContainerType.Head:
                    _hatData = new ClothDisplayData(Hat, display, item);
                    break;

                case ContainerType.ExoSuit:
                    break;

                case ContainerType.Jumpsuit:
                    _jumpsuitData = new ClothDisplayData(Jumpsuit, display, item);
                    break;

                case ContainerType.ShoeLeft:
                    _footLeftData = new ClothDisplayData(FootLeft, display, item);
                    break;

                case ContainerType.ShoeRight:
                    _footRightData = new ClothDisplayData(FootRight, display, item);
                    break;

                case ContainerType.GloveLeft:
                    _handLeftData = new ClothDisplayData(HandLeft, display, item);
                    break;

                case ContainerType.GloveRight:
                    _handRightData = new ClothDisplayData(HandRight, display, item);
                    break;
            }
        }
    }
}