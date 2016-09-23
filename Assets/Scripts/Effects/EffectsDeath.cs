using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System;

//effects in this file take place when the tower/enemy is killed

//spawns X enemies of type Y.  On enemies, it spawns where the enemy died.  On towers, this is unsupported
class EffectSpawnEnemyOnDeath : IEffectDeath
{
    [Hide] public string cardName { get; set; }                                      //name of the card containing this effect
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } } //this effect is not targeted (since it effects the enemy/tower it is attached to)
    [Hide] public EffectType effectType { get { return EffectType.death; } }         //effect type
    [Show, Display(2)] public float strength { get; set; }                           //how many enemies to spawn
    [Show, Display(3)] public string argument { get; set; }                          //which enemy type to spawn

    [Hide] public string Name { get { return "On Death: Spawn " + strength + " " + argument; } } //returns name and strength
    [Show, Display(1)] public string XMLName { get { return "spawnEnemyOnDeath"; } } //name used to refer to this effect in XML

    public void onEnemyDeath(EnemyScript e)
    {
        WaveData newWave = new WaveData();
        newWave.type = argument;
        newWave.forcedSpawnCount = Mathf.FloorToInt(strength);
        newWave.time = 1.0f;

        LevelManagerScript.instance.StartCoroutine(LevelManagerScript.instance.spawnWaveAt(newWave, e.transform.position, e.path[e.currentDestination]));
    }

    public void onTowerDeath(TowerScript t)
    {
        MessageHandlerScript.Warning("spawnEnemyOnDeath does not support towers.");
    }
}
