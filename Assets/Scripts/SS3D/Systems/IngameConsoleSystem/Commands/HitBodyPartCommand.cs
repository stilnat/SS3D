﻿using FishNet.Connection;
using FishNet.Object;
using SS3D.Permissions;
using SS3D.Systems.Health;
using System;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class HitBodyPartCommand : Command
    {
        private record CalculatedValues(BodyPart BodyPart, BodyLayerType BodyLayerType, DamageType DamageType, int DamageAmount) : ICalculatedValues;

        public override string LongDescription => "Hurt a given body part and body layer unattached to a player";

        public override string ShortDescription => "Hit me daddy";

        public override string Usage => "(game object name) (BodyLayerType) (DamageType) (amount of damage)";

        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;

        public override CommandType Type => CommandType.Server;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            if (args[1] != "all")
            {
                if (!values.BodyPart.TryInflictDamage(values.BodyLayerType, new(values.DamageType, values.DamageAmount)))
                {
                    return response.MakeInvalid("can't inflict damage on bodypart").InvalidArgs;
                }
            }
            else
            {
                values.BodyPart.InflictDamageToAllLayer(new(values.DamageType, values.DamageAmount));
            }

            return "BodyPart hurt";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = default;

            if (args.Length != 4)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            string gameObjectName = args[0];
            string bodyLayerName = args[1];
            string damageTypeName = args[2];
            string damageAmountString = args[3];

            if (!int.TryParse(damageAmountString, out int damageAmount))
            {
                return response.MakeInvalid("Invalid damage amount");
            }

            if (!Enum.TryParse(bodyLayerName, true, out BodyLayerType bodyLayerType) && bodyLayerName != "all")
            {
                return response.MakeInvalid("Provide a valid body layer type name");
            }

            if (!Enum.TryParse(damageTypeName, true, out DamageType damageType))
            {
                return response.MakeInvalid("Invalid damage type");
            }

            GameObject go = GameObject.Find(gameObjectName);

            if (go == null)
            {
                return response.MakeInvalid("No bodypart with this name");
            }

            BodyPart[] bodyParts = go.GetComponentsInChildren<BodyPart>().Where(x => x.gameObject.name == gameObjectName).ToArray();

            if (bodyParts.Length == 0)
            {
                return response.MakeInvalid("No bodypart with this name");
            }

            if (bodyParts.Length != 1)
            {
                return response.MakeInvalid("Multiple body parts with the same name, ambiguous command");
            }

            BodyPart bodyPart = bodyParts[0];

            if (!bodyPart.ContainsLayer(bodyLayerType))
            {
                return response.MakeInvalid("body layer not present on the bodypart");
            }

            response.IsValid = true;
            return response.MakeValid(new CalculatedValues(bodyPart, bodyLayerType, damageType, damageAmount));
        }
    }
}
