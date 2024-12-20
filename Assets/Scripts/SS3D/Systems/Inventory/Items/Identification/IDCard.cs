﻿using FishNet.Object.Synchronizing;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Roles;
using SS3D.Traits;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// The honking device used by the clown on honking purposes
    /// </summary>
    public class IDCard : Item, IIdentification
    {
        [SyncObject]
        private readonly SyncList<IDPermission> _permissions = new();

        private string _ownerName;

        private string _roleName;

        public string OwnerName
        {
            get => _ownerName;
            set => _ownerName = value;
        }

        public string RoleName
        {
            get => _roleName;
            set => _roleName = value;
        }

        public bool HasPermission(IDPermission permission)
        {
            return permission == null || _permissions.Contains(permission);
        }

        public void AddPermission(IDPermission permission)
        {
            _permissions.Add(permission);
            _permissions.Dirty(permission);
        }

        public void RemovePermission(IDPermission permission)
        {
            _permissions.Remove(permission);
            _permissions.Dirty(permission);
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.CreateTargetInteractions(interactionEvent).ToList();

            return interactions.ToArray();
        }
    }
}
