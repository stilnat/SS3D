using SS3D.Substances;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CirculatoryLayer : BiologicalLayer
{
    public override float OxygenConsumptionRate { get => 0.5f; }

    public SubstanceContainer _bodyfluids;

    public override BodyLayerType LayerType
    {
        get { return BodyLayerType.Circulatory; }
    }
    public CirculatoryLayer(BodyPart bodyPart, SubstanceContainer bodyfluids) : base(bodyPart)
    {
        _bodyfluids = bodyfluids;
    }
    public CirculatoryLayer(BodyPart bodyPart, SubstanceContainer bodyfluids,
    List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances)
    : base(bodyPart, damages, susceptibilities, resistances)
    {
        _bodyfluids = bodyfluids;
    }

    protected override void SetSuceptibilities()
    {
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Slash, 2f));
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Puncture, 2f));
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Toxic, 1.5f));
    }

    public void Bleed()
    {

    }


}
