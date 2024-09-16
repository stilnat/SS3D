using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public class DummyPositionController : NetworkBehaviour
    {
        [SyncVar]
        private PositionType _position;

        public PositionType Position
        {
            get => _position;
            set => _position = value;
        }

        protected void Start()
        {
            Position = PositionType.Standing;
        }
    }
}
