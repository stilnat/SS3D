﻿using Coimbra.Services.Events;
using SS3D.Systems.Entities;
using System.Collections.Generic;

namespace SS3D.Systems.PlayerControl.Events
{
    public partial struct OnlinePlayersChanged : IEvent
    {
        public readonly List<Player> OnlinePlayers;

        public readonly Player ChangedPlayer;

        public readonly string ChangedCkey;

        public readonly bool AsServer;

        public ChangeType ChangeType;

        public OnlinePlayersChanged(List<Player> onlinePlayers, ChangeType changeType, Player changed, string ckey, bool asServer)
        {
            OnlinePlayers = onlinePlayers;
            ChangeType = changeType;
            ChangedPlayer = changed;
            ChangedCkey = ckey;
            AsServer = asServer;
        }
    }
}
