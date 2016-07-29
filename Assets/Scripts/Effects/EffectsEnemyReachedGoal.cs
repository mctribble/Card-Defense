using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System;

//effects in this file take effect when the enemy reaches its goal

public class EffectinvScaleAttackWithHealth: IEffectEnemyReachedGoal
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }    //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.enemyReachedGoal; } } //effect type
    [Show, Display(2)] public float strength { get; set; }                              //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                         //effect argument (unused in this effect)

    [Hide] public string Name
    {
        get
        {
            return "attack decreases as the enemy gets damaged.";
        }
    }

    [Show, Display(1)] public string XMLName { get { return "invScaleAttackWithHealth"; } }

    public void trigger(EnemyScript enemy)
    {
        enemy.damage = Mathf.CeilToInt(enemy.damage * (((float)enemy.curHealth) / ((float)enemy.maxHealth)));
    }
}