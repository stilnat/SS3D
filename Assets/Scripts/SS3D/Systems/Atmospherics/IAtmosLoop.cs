
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAtmosLoop
{
    void Initialize();
    void Step();
    void SetTileNeighbour(PlacedTileObject tile, int index);
    void SetAtmosNeighbours();
}