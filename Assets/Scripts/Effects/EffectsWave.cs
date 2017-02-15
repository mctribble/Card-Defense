using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// all wave effects should derive from this
/// </summary>
public abstract class BaseEffectWave : BaseEffect, IEffectWave
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } } //wave effects dont need a target
    [Hide] public override EffectType    effectType    { get { return EffectType.wave; } }    //this is a wave effect

    public abstract WaveData alteredWaveData(WaveData currentWaveData);
}

//alters wave budget by x%
public class EffectBudgetPercentageChange : BaseEffectWave
{
    [Hide] public override string Name //returns name and strength
    {
        get
        {
            if (strength >= 0)
                return "wave strength: +" + strength + '%';
            else
                return "wave strength: " + -strength + '%';
        }
    }

    [Show] public override string XMLName { get { return "budgetPercentageChange"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        currentWaveData.budget = Mathf.RoundToInt(currentWaveData.budget * (1.0f + (strength / 100.0f)));
        return currentWaveData;
    }
}

//sets the enemy type to argument
public class EffectChangeWaveType : BaseEffectWave
{
    [Hide] private string m_argument;
    [Show] public override string argument
    {
        get { return m_argument; }
        set
        {
            m_argument = value;

            //validate argument: must be an existing enemy type
            try
            {
                EnemyTypeManagerScript.instance.getEnemyTypeByName(m_argument);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                Debug.LogWarning("<" + cardName + ">Could not find an enemy type named " + m_argument + ": defaulting to Standard.");
                m_argument = "Standard";
            }
        }
    }

    [Hide] public override string Name { get { return "change type of incoming waves to " + argument + "."; } }   //returns name and strength
    [Show] public override string XMLName { get { return "changeWaveType"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newWave =  new WaveData(EnemyTypeManagerScript.instance.getEnemyTypeByName(argument), currentWaveData.budget, currentWaveData.time);

        //apply wave effects and update ranks
        if (newWave.enemyData.effectData != null)
            foreach (IEffect e in newWave.enemyData.effectData.effects)
                if (e.triggersAs(EffectType.wave))
                    newWave = ((IEffectWave)e).alteredWaveData(newWave);

        newWave.recalculateRank();

        return newWave;
    }
}

//adjusts the wave spawn time by X%
public class EffectTimePercentageChange : BaseEffectWave
{
    [Hide] public override string Name //returns name and strength
    {
        get
        {
            if (strength >= 0)
                return "wave spawns " + strength + "% slower.";
            else
                return "wave spawns " + -strength + "% faster.";
        }
    }

    [Show] public override string XMLName { get { return "timePercentageChange"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        currentWaveData.time = currentWaveData.time * (1.0f + (strength / 100.0f));
        return currentWaveData;
    }
}

//waves of this enemy type always spawn exactly X enemies.  Best when used with budget scaling effects, so stronger waves can continue getting stronger.
public class EffectFixedSpawnCount : BaseEffectWave
{
    [Hide] public override string Name { get { return "Always spawns " + strength + " per card"; } } //returns name and strength
    [Show] public override string XMLName { get { return "fixedSpawnCount"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.forcedSpawnCount = Mathf.RoundToInt(strength);
        return newData;
    }
}

//enemy attack scales up with wave budget.  Increases proportionally if x = 1.  Higher/lower values cause it to decrease faster/slower, respectively.
public class EffectScaleAttackWithBudget : BaseEffectWave
{
    [Hide] public override string Name { get { return "attack increases on tougher waves"; } } //returns name and strength
    [Show] public override string XMLName { get { return "scaleAttackWithBudget"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        float scaleRatio = (float)newData.budget / (float)newData.enemyData.baseSpawnCost;            //ratio we are scaling by
        float scaleFactor = ((scaleRatio -1) * strength) + 1;                                         //factor to use for scaling
        newData.enemyData.baseAttack = Mathf.RoundToInt( scaleFactor * newData.enemyData.baseAttack); //scale
        return newData;
    }
}

//enemy health scales up with wave budget.  Increases proportionally if x = 1.  Higher/lower values cause it to increase faster/slower, respectively.
public class EffectScaleHealthWithBudget : BaseEffectWave
{
    [Hide] public override string Name { get { return "health increases on tougher waves"; } } 
    [Show] public override string XMLName { get { return "scaleHealthWithBudget"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        float scaleRatio = (float)newData.budget / (float)newData.enemyData.baseSpawnCost;                 //ratio we are scaling by
        float scaleFactor = ((scaleRatio -1) * strength) + 1;                                              //factor to use for scaling
        newData.enemyData.baseMaxHealth = Mathf.RoundToInt(scaleFactor * newData.enemyData.baseMaxHealth); //scale
        return newData;
    }
}

//enemy speed  scales up with wave budget.  Increases proportionally if x = 1.  Higher/lower values cause it to increase faster/slower, respectively.
public class EffectScaleSpeedWithBudget : BaseEffectWave
{
    [Hide] public override string Name { get { return "speed increases on tougher waves"; } }
    [Show] public override string XMLName { get { return "scaleSpeedWithBudget"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        float scaleRatio = (float)newData.budget / (float)newData.enemyData.baseSpawnCost;                    //ratio we are scaling by
        float scaleFactor = ((scaleRatio -1) * strength) + 1;                                                 //factor to use for scaling
        newData.enemyData.baseUnitSpeed = Mathf.RoundToInt(scaleFactor * newData.enemyData.currentUnitSpeed); //scale
        return newData;
    }
}