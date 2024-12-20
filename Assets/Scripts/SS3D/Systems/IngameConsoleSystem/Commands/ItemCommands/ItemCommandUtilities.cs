using FishNet.Connection;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public static class ItemCommandUtilities
    {
        public static Item GetItemInHand(NetworkConnection conn = null)
        {
            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
            Player player = playerSystem.GetPlayer(conn);
            Entity playerEntity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);

            if (playerEntity == null)
            {
                return null;
            }

            Hands hands = playerEntity.GetComponentInParent<HumanInventory>().Hands;
            return hands.SelectedHand.ItemHeld;
        }
    }
}
