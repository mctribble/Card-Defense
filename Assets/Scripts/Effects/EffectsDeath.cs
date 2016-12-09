using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// death effects take place when the tower/enemy is killed.  This base effect handles behavior common to them all
/// </summary>
abstract class BaseEffectDeath : BaseEffect, IEffectDeath
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } } //this effect is not targeted (since it effects the enemy/tower it is attached to)
    [Hide] public override EffectType    effectType    { get { return EffectType.death;   } } //effect type

    public abstract void onEnemyDeath(EnemyScript e);
    public abstract void onTowerDeath(TowerScript t);
}

//spawns X enemies of type Y.  On enemies, it spawns where the enemy died.  On towers, this is unsupported
class EffectSpawnEnemyOnDeath : BaseEffectDeath
{
    [Hide] public override string Name     { get { return "Carrying " + spawnWave.ToShortString(); } } //returns name and strength
    [Show] public override string XMLName  { get { return "spawnEnemyOnDeath"; } } //name used to refer to this effect in XML

    //wave that will be spawned when this unit dies
    private WaveData spawnWave;

    //constructor initializes the wave
    public EffectSpawnEnemyOnDeath()
    {
        spawnWave = new WaveData();
        spawnWave.time = 2.0f;
    }

    //override strength to update the wave
    public override float strength
    {
        get { return base.strength; }

        set
        {
            base.strength = value;
            spawnWave.budget = Mathf.FloorToInt(strength * spawnWave.enemyData.baseSpawnCost); //update budget instead of using forcedSpawnCount so the wave can rank up normally
            spawnWave.recalculateRank(); //make sure to recalculate the rank also
        }
    }

    //override argument to update the wave
    public override string argument
    {
        get { return base.argument; }

        set
        {
            base.argument = value;
            spawnWave.budget = Mathf.FloorToInt(strength * spawnWave.enemyData.baseSpawnCost); //update budget instead of using forcedSpawnCount so the wave can rank up normally
            spawnWave.recalculateRank(); //make sure to recalculate the rank also
        }
    }

    public override void onEnemyDeath(EnemyScript e)
    {
        //if the enemy died after reaching the goal, cancel to avoid throwing pathing exceptions
        if (e.currentDestination == e.path.Count)
            return;

        //otherwise, spawn the wave
        LevelManagerScript.instance.StartCoroutine(LevelManagerScript.instance.spawnWaveAt(spawnWave, e.transform.position, e.path[e.currentDestination]));
    }

    public override void onTowerDeath(TowerScript t)
    {
        MessageHandlerScript.Warning("spawnEnemyOnDeath does not support towers.");
    }
}
