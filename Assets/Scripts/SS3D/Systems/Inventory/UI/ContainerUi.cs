using Coimbra;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.UI
{
    public class ContainerUi : MonoBehaviour
    {
        [SerializeField]
        private ItemGrid _grid;

        [SerializeField]
        private TMP_Text _containerName;

        private AttachedContainer _attachedContainer;

        public IInventory Inventory
        {
            get => _grid.Inventory;
            set => _grid.Inventory = value;
        }

        public AttachedContainer AttachedContainer
        {
            get => _attachedContainer;

            set
            {
                _attachedContainer = value;
                UpdateContainer(value);
            }
        }

        public void Close()
        {
            Inventory.ContainerViewer.CmdContainerClose(_attachedContainer);
            gameObject.Dispose(true);
        }

        protected void OnDestroy()
        {
            _grid.OnPointerClickSlot -= HandleGridPointerClickSlot;
            _grid.OnItemDrop -= HandleGridItemDrop;
            _attachedContainer.OnContentsChanged -= ContainerOnContentsChanged;
        }

        private void UpdateContainer(AttachedContainer container)
        {
            if (container == null)
            {
                return;
            }

            container.ContainerUi = this;
            _grid.Init(_attachedContainer.Size, _attachedContainer.StoredItems);
            _grid.OnPointerClickSlot += HandleGridPointerClickSlot;
            _grid.OnItemDrop += HandleGridItemDrop;
            _attachedContainer.OnContentsChanged += ContainerOnContentsChanged;

            RectTransform rectTransform = _grid.GetComponent<RectTransform>();
            Vector2 gridDimensions = _grid.GetGridDimensions();
            float width = rectTransform.offsetMin.x + Math.Abs(rectTransform.offsetMax.x) + gridDimensions.x + 1;
            float height = rectTransform.offsetMin.y + Math.Abs(rectTransform.offsetMax.y) + gridDimensions.y;
            RectTransform rect = transform.GetChild(0).GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            // Set the text inside the containerUI to be the name of the container
            _containerName.text = _attachedContainer.ContainerName;

            // Position the text correctly inside the UI.
            Vector3[] v = new Vector3[4];
            rect.GetLocalCorners(v);
            _containerName.transform.localPosition = v[1] + new Vector3(0.03f * width, -0.02f * height, 0);
        }

        private void HandleGridItemDrop(Item item, Vector2Int slotPosition)
        {
            if (!AttachedContainer.CanContainItemAtPosition(item, slotPosition))
            {
                return;
            }

            if (item.Container != null && !item.Container.CanRemoveItem(item))
            {
                return;
            }

            Inventory.ClientTransferItem(item, slotPosition, AttachedContainer);
        }

        private void HandleGridPointerClickSlot(Vector2Int slotPosition)
        {
            Inventory.ClientInteractWithContainerSlot(AttachedContainer, slotPosition);
        }

        /// <summary>
        /// When the container change, change the display of items inside it.
        /// Either add a display, remove a display or move a display to another slot.
        /// </summary>
        private void ContainerOnContentsChanged(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            switch (type)
            {
                case ContainerChangeType.Add:
                {
                    _grid.CreateItemDisplay(newItem, container.PositionOf(newItem));
                    break;
                }

                case ContainerChangeType.Remove:
                {
                    _grid.RemoveItemDisplay(oldItem);
                    break;
                }

                case ContainerChangeType.Move:
                {
                    _grid.MoveItemDisplay(newItem, container.PositionOf(newItem));
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
