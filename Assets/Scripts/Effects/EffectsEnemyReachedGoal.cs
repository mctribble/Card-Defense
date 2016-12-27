using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// EnemyReachedGoal effects take effect when the enemy reaches its goal.  This base effect handles behavior common to them all
/// </summary>
public abstract class BaseEffectEnemyReachedGoal : BaseEffect, IEffectEnemyReachedGoal
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } }    //this effect doesnt need a target
    [Hide] public override EffectType effectType { get { return EffectType.enemyReachedGoal; } } //effect type

    public abstract void trigger(EnemyScript enemy);
}

//monster deals more damage as it gets injured.  Increases proportionally if x = 1.  Higher/lower values cause it to increase faster/slower, respectively.
public class EffectscaleAttackWithDamage: BaseEffectEnemyReachedGoal
{
    [Hide] public override string Name { get { return "attack increases as the enemy gets damaged."; } }
    [Show] public override string XMLName { get { return "scaleAttackWithDamage"; } }

    public override void trigger(EnemyScript enemy)
    {
        float scaleRatio = 1 - ((float)enemy.curHealth / (float)enemy.maxHealth); //ratio we are scaling by
        float scaleFactor = ((scaleRatio -1 ) * strength) + 1;                    //factor to use for scaling
        enemy.damage = Mathf.RoundToInt(scaleFactor * enemy.damage);              //scale
    }
}

//enemy deals less damage as it gets injured.  Decreases proportionally if x = 1.  Higher/lower values cause it to decrease faster/slower, respectively.
public class EffectinvScaleAttackWithDamage: BaseEffectEnemyReachedGoal
{
    [Hide] public override string Name { get { return "attack decreases as the enemy gets damaged."; } }
    [Show] public override string XMLName { get { return "invScaleAttackWithDamage"; } }

    public override void trigger(EnemyScript enemy)
    {
        float healthRatio = (float)enemy.curHealth / (float)enemy.maxHealth;        //amount of health reamining (0: dead, 1: full health)
        enemy.damage = Mathf.CeilToInt(enemy.damage * (healthRatio / strength) );   //scale
        enemy.damage = Mathf.Max(enemy.damage, 0);                                  //enfore minimum
    }
}
