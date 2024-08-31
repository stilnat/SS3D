using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DummyStuff
{
    public class DummyPositionController : MonoBehaviour
    {
        public PositionType Position { get; set; }

        protected void Start()
        {
            Position = PositionType.Standing;
        }
    }
}
