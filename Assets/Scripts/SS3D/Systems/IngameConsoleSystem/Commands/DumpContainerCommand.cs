﻿using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class DumpContainerCommand : Command
    {
        private record CalculatedValues(AttachedContainer Container) : ICalculatedValues;

        public override string LongDescription => "Dump the content of all containers on the game object";

        public override string ShortDescription => "Dump the content of a container";

        public override string Usage => "(container's game object name)";

        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;

        public override CommandType Type => CommandType.Server;

        public override string Perform(string[] args, NetworkConnection conn)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            values.Container.Dump();
            return "Container content dumped";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = default;

            if (args.Length != 1)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            GameObject containerGo = GameObject.Find(args[0]);

            if (containerGo == null)
            {
                return response.MakeInvalid("This container doesn't exist");
            }

            AttachedContainer container = containerGo.GetComponentInChildren<AttachedContainer>();

            if (container == null)
            {
                return response.MakeInvalid("No container on this game object");
            }

            response.IsValid = true;
            return response.MakeValid(new CalculatedValues(container));
        }
    }
}
