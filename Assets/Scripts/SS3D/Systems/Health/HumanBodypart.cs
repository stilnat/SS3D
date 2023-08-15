﻿using Coimbra;
using SS3D.Substances;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Represent a generic body part, containing muscles, bones, nerves and veins.
/// Can be used for arms, foot, legs ... Should not be used for organs.
/// </summary>
public class HumanBodypart : BodyPart
{

    public override void Init(string name = "")
    {
        base.Init(name);
    }

    public override void Init(BodyPart parent, string name = "")
    {
        base.Init(parent, name);
    }

    protected override void AddInitialLayers()
    {
        TryAddBodyLayer(new MuscleLayer(this));
        TryAddBodyLayer(new BoneLayer(this));
        TryAddBodyLayer(new CirculatoryLayer(this));
        TryAddBodyLayer(new NerveLayer(this));
    }

	protected override void RemoveBodyPart()
	{
		base.RemoveBodyPart();
	}
}