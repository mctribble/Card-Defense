using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System;

//effects in this file apply when a tower makes an attack and has ta least 1 full point of overcharge
//they alter the given damage event and take effect before enemyDamaged effects
//This base effect handles behavior common to them all
abstract class BaseEffectOvercharge : BaseEffect, IEffectOvercharge
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } }    //overcharge effects are not targeted
    [Hide] public override EffectType    effectType    { get { return EffectType.overcharge; } } //effect type

    public abstract void trigger(ref DamageEventData d, int pointsOfOvercharge);
}

//tower deals (1+X) times as much damage when overcharged
class EffectOverchargeDamage : BaseEffectOvercharge
{
    [Hide] public override string Name { get { return "deals an extra " + (strength * 100) + "% more damage per point of overcharge"; } } //returns name and strength
    [Show] public override string XMLName { get { return "overchargeDamage"; } } //name used to refer to this effect in XML.

    public override void trigger(ref DamageEventData d, int pointsOfOvercharge)
    {
        d.rawDamage = (1 + (strength * pointsOfOvercharge)) * d.rawDamage;
    }
}


