using Coimbra;
using SS3D.Systems.Inventory.Containers;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.UI
{
    public class ContainerUi : MonoBehaviour
    {
        [FormerlySerializedAs("Grid")]
        [SerializeField]
        private ItemGrid _grid;

        private TMP_Text _containerName;

        private AttachedContainer _attachedContainer;

        public IInventory Inventory
        {
            get => _grid.Inventory;
            set => _grid.Inventory = value;
        }

        public AttachedContainer AttachedContainer
        {
            set
            {
                _attachedContainer = value;
                _grid.AttachedContainer = value;
                UpdateContainer(value);
            }
        }

        public void Close()
        {
            Inventory.ContainerViewer.CmdContainerClose(_attachedContainer);
            gameObject.Dispose(true);
        }

        private void UpdateContainer(AttachedContainer container)
        {
            if (container == null)
            {
                return;
            }

            container.ContainerUi = this;

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
    }
}
