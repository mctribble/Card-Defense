using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System;

//effects in this file apply when a tower makes an attack and has ta least 1 full point of overcharge
//they alter the given damage event and take effect before enemyDamaged effects

//tower deals (1+X) times as much damage when overcharged
class EffectOverchargeDamage : IEffectOvercharge
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //overcharge effects are not targeted
    [Hide] public EffectType effectType { get { return EffectType.overcharge; } }      //effect type
    [Hide] public float strength { get; set; }                                         //damage multiplier
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)
    
    //this effect
    [Hide] public string Name { get { return "deals an extra " + (strength * 100) + "% more damage per point of overcharge"; } } //returns name and strength
    [Show] public string XMLName { get { return "overchargeDamage"; } } //name used to refer to this effect in XML.

    public void trigger(ref DamageEventData d, int pointsOfOvercharge)
    {
        Debug.Log(d.rawDamage);
        d.rawDamage = (1 + (strength * pointsOfOvercharge)) * d.rawDamage;
        Debug.Log(d.rawDamage);
    }
}


