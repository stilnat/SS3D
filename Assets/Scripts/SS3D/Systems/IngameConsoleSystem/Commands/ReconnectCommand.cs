﻿using FishNet.Connection;
using SS3D.Permissions;
using System.Diagnostics;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class ReconnectCommand : Command
    {
        private record CalculatedValues : ICalculatedValues;

        public override string ShortDescription => "Restart app";

        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;

        public override CommandType Type => CommandType.Offline;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            Process.Start(UnityEngine.Application.dataPath.Replace("_Data", ".exe"));
            UnityEngine.Application.Quit();
            return "Done";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = default;

            if (args.Length != 0)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            return response.MakeValid(new CalculatedValues());
        }
    }
}
