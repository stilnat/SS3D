using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public class ViewContainerInteraction : IInteraction
    {
        private readonly AttachedContainer _attachedContainer;

        public ViewContainerInteraction(AttachedContainer attachedContainer)
        {
            _attachedContainer = attachedContainer;
        }

        public InteractionType InteractionType => InteractionType.None;

        public float MaxDistance { get; set; }

        public string GetGenericName() => "View Container";

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        public string GetName(InteractionEvent interactionEvent) => "View " + _attachedContainer.ContainerName;

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Open;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            if (_attachedContainer == null)
            {
                return false;
            }

            ContainerViewer containerViewer = interactionEvent.Source.GetComponentInParent<ContainerViewer>();
            if (containerViewer == null)
            {
                return false;
            }

            return !containerViewer.HasContainer(_attachedContainer);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            ContainerViewer containerViewer = interactionEvent.Source.GetComponentInParent<ContainerViewer>();

            containerViewer.ShowContainerUI(_attachedContainer);

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
