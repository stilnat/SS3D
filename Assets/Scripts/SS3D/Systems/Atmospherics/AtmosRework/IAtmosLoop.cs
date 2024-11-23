using SS3D.Interactions.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    public interface IAtmosLoop : IGameObjectProvider
    {
        void Step();
    }
}