using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System;

//effects in this file take effect when the enemy reaches its goal

//enemy attack increases as it takes damage (range: 1x to 2x, rounds down)
public class EffectScaleAttackWithHealth: IEffectEnemyReachedGoal
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }    //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.enemyReachedGoal; } } //effect type
    [Hide] public float strength { get; set; }                                          //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                         //effect argument (unused in this effect)

    [Hide] public string Name { get { return "attack increases as the enemy gets damaged."; } }
    [Show, Display(1)] public string XMLName { get { return "scaleAttackWithHealth"; } }

    public void trigger(EnemyScript enemy)
    {
        enemy.damage = enemy.damage + Mathf.FloorToInt(enemy.damage * (((float)enemy.curHealth) / ((float)enemy.maxHealth)));
    }
}

//enemy attack decreases as it takes damage (range: 1x to 0x, rounds up)
public class EffectInvScaleAttackWithHealth: IEffectEnemyReachedGoal
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }    //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.enemyReachedGoal; } } //effect type
    [Hide] public float strength { get; set; }                                          //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                         //effect argument (unused in this effect)

    [Hide] public string Name { get { return "attack decreases as the enemy gets damaged."; } }
    [Show, Display(1)] public string XMLName { get { return "invScaleAttackWithHealth"; } }

    public void trigger(EnemyScript enemy)
    {
        enemy.damage = Mathf.CeilToInt(enemy.damage * (((float)enemy.curHealth) / ((float)enemy.maxHealth)));
    }
}

//deals X damage to the players hand
public class EffectDamageHand : IEffectEnemyReachedGoal
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }    //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.enemyReachedGoal; } } //effect type
    [Hide] public float strength { get; set; }                                          //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                         //effect argument (unused in this effect)
    
    [Hide] public string Name { get { return "deals " + strength + " damage to the players hand"; } }
    [Show, Display(1)] public string XMLName { get { return "damageHand"; } }

    public void trigger(EnemyScript enemy)
    {
        DeckManagerScript.instance.DamageHand(Mathf.FloorToInt(strength));
    }
}

