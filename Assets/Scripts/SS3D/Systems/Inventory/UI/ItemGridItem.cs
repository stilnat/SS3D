using Coimbra;
using UnityEngine.UI;

namespace SS3D.Systems.Inventory.UI
{
    public class ItemGridItem : ItemDisplay
    {
        protected override void OnDropAccepted()
        {
            base.OnDropAccepted();
            MakeVisible(false);
        }
    }
}
