using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vexe.Runtime.Types;

//contains effects which are triggered by multiple things and therefore don't really fit into another file
//note that there are compound effects in other files.  this one is just meant for effects that dont neatly fit elsewhere

//common base for all Resonant effects
public abstract class BaseEffectResonant : BaseEffect, IEffectOnSpawned, IEffectDeath
{
    private static Dictionary<string, List<TowerScript>> resonanceDict; //static container that tracks resonance effects across all towers

    //event to use for resonance changes
    public delegate void resonanceChangeHandler();
    public static event resonanceChangeHandler resonanceChangedEvent;

    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } }  //this effect should never be on a card, and thus should never be cast
    [Hide] public override EffectType    effectType    { get { return EffectType.misc; } } //effect type

    //convenience accessor to reference resonance across all towers with this argument and the same argument
    protected int resonance
    {
        get
        {
            List<TowerScript> towersWithSameArgument = null;
            resonanceDict.TryGetValue(argument, out towersWithSameArgument);

            if (towersWithSameArgument == null)
                return 0;
            else
                return towersWithSameArgument.Count;
        }
    }

    public BaseEffectResonant()
    {
        if (resonanceDict == null)
            resonanceDict = new Dictionary<string, List<TowerScript>>();
    }

    //add triggers for tower spawn/death to track how many towers with this effect are present
    public override bool triggersAs(EffectType triggerType)
    {
        return base.triggersAs(triggerType) || (triggerType == EffectType.spawn) || (triggerType == EffectType.death);
    }

    //track resonance for new towers
    public virtual void onTowerSpawned(TowerScript tower)
    {
        //list of all towers that already have this argument
        List<TowerScript> towerList = null;
        resonanceDict.TryGetValue(argument, out towerList);
        if (towerList == null)
            towerList = new List<TowerScript>();

        //add the new tower to it, if it isnt there already
        if (towerList.Contains(tower) == false)
            towerList.Add(tower);

        //make that list the new value in the dictionary
        resonanceDict.Remove(argument); //note that this does nothing if the item is already absent
        resonanceDict.Add(argument, towerList);

        //fire the event, if it has listeners
        if (resonanceChangedEvent != null)
            resonanceChangedEvent();
    }

    //track resonance for dying towers
    public virtual void onTowerDeath(TowerScript tower)
    {
        //list of all towers that already have this argument
        List<TowerScript> towerList = null;
        resonanceDict.TryGetValue(argument, out towerList);
        if (towerList == null)
            towerList = new List<TowerScript>();

        //remove this tower from it
        towerList.Remove(tower); //note that this does nothing if the item is already absent

        //make that list the new value in the dictionary
        resonanceDict.Remove(argument); //note that this does nothing if the item is already absent
        resonanceDict.Add(argument, towerList);

        //fire the event, if it has listeners
        if (resonanceChangedEvent != null)
            resonanceChangedEvent();
    }

    //we can ignore these triggers, since they dont apply
    public virtual void onEnemySpawned(EnemyScript enemy) { }
    public virtual void onEnemyDeath(EnemyScript e) { }
}

//tower attack gets X times stronger for each tower that has this effect with the same value for Y
public class EffectResonantTowerAttackMult : BaseEffectResonant
{
    public override string Name { get { return "<" + argument + ">: " + strength + "x damage per other <" + argument + "> tower"; } }
    public override string XMLName { get { return "ResonantTowerAttackMult"; } }

    //private tracking
    private TowerScript t;
    private float attackBonus = 0.0f;

    //add triggers for tower spawn/death to track how many towers with this effect are present
    public override bool triggersAs(EffectType triggerType)
    {
        return base.triggersAs(triggerType) || (triggerType == EffectType.spawn) || (triggerType == EffectType.death);
    }

    //store a reference to the tower when it gets spawned
    public override void onTowerSpawned(TowerScript tower)
    {
        t = tower;
        base.onTowerSpawned(tower);
    }

    //register to hear about resonance changes
    public EffectResonantTowerAttackMult() { resonanceChangedEvent += resonanceChanged; }

    //called when resonance changes to recalculate the range
    public void resonanceChanged()
    {
        //ignore if the tower has not spawned yet
        if (t == null)
            return;

        //remove the old bonus
        t.attackPower -= attackBonus;

        //calculate the new bonus
        float newAttackBonus = (t.attackPower * Mathf.Pow(strength, (resonance - 1))) - t.attackPower;

        //add the new bonus
        t.attackPower += newAttackBonus;

        //track the change
        attackBonus = newAttackBonus;

        //update the tower UI
        t.UpdateTooltipText();
    }
}

//tower attack increases by X for each tower that has this effect with the same value for Y
public class EffectResonantTowerAttackMod : BaseEffectResonant
{
    public override string Name { get { return "<" + argument + ">: +" + strength + " damage per other <" + argument + "> tower"; } }
    public override string XMLName { get { return "ResonantTowerAttackMod"; } }

    //private tracking
    private TowerScript t;
    private float attackBonus = 0.0f;

    //add triggers for tower spawn/death to track how many towers with this effect are present
    public override bool triggersAs(EffectType triggerType)
    {
        return base.triggersAs(triggerType) || (triggerType == EffectType.spawn) || (triggerType == EffectType.death);
    }

    //store a reference to the tower when it gets spawned
    public override void onTowerSpawned(TowerScript tower)
    {
        t = tower;
        base.onTowerSpawned(tower);
    }

    //register to hear about resonance changes
    public EffectResonantTowerAttackMod() { resonanceChangedEvent += resonanceChanged; }

    //called when resonance changes to recalculate the range
    public void resonanceChanged()
    {
        //ignore if the tower has not spawned yet
        if (t == null)
            return;

        //calculate the new range bonus
        float newAttackBonus = strength * (resonance - 1);

        //update range by removing the old bonus and adding the new one
        t.attackPower -= attackBonus;
        t.attackPower += newAttackBonus;

        //track the change
        attackBonus = newAttackBonus;

        //update the tower UI
        t.UpdateTooltipText();
    }
}

//tower range gets longer by X for each tower that has this effect with the same value for Y
public class EffectResonantTowerRangeMod : BaseEffectResonant
{
    public override string Name { get { return "<" + argument + ">: +" + strength + " range per other <" + argument + "> tower"; } }
    public override string XMLName { get { return "ResonantTowerRangeMod"; } }

    //private tracking
    private TowerScript t;
    private float rangeBonus = 0.0f;

    //add triggers for tower spawn/death to track how many towers with this effect are present
    public override bool triggersAs(EffectType triggerType)
    {
        return base.triggersAs(triggerType) || (triggerType == EffectType.spawn) || (triggerType == EffectType.death);
    }

    //store a reference to the tower when it gets spawned
    public override void onTowerSpawned(TowerScript tower)
    {
        t = tower;
        base.onTowerSpawned(tower);
    }

    //register to hear about resonance changes
    public EffectResonantTowerRangeMod() { resonanceChangedEvent += resonanceChanged; }

    //called when resonance changes to recalculate the range
    public void resonanceChanged()
    {
        //ignore if the tower has not spawned yet
        if (t == null)
            return;

        //calculate the new range bonus
        float newRangeBonus = strength * (resonance - 1);

        //update range by removing the old bonus and adding the new one
        t.range -= rangeBonus;
        t.range += newRangeBonus;

        //track the change
        rangeBonus = newRangeBonus;

        //update the tower UI
        t.UpdateTooltipText();
        t.updateRangeImage();
    }
}