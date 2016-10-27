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
    [Hide] public override string Name     { get { return "[On Death] Spawn " + strength + " " + argument; } } //returns name and strength
    [Show] public override string XMLName  { get { return "spawnEnemyOnDeath"; } } //name used to refer to this effect in XML

    public override void onEnemyDeath(EnemyScript e)
    {
        WaveData newWave = new WaveData();
        newWave.type = argument;
        newWave.forcedSpawnCount = Mathf.FloorToInt(strength);
        newWave.time = 1.0f;

        //if the enemy died after reaching the goal, cancel to avoid throwing pathing exceptions
        if (e.currentDestination == e.path.Count)
            return;

        LevelManagerScript.instance.StartCoroutine(LevelManagerScript.instance.spawnWaveAt(newWave, e.transform.position, e.path[e.currentDestination]));
    }

    public override void onTowerDeath(TowerScript t)
    {
        MessageHandlerScript.Warning("spawnEnemyOnDeath does not support towers.");
    }
}
