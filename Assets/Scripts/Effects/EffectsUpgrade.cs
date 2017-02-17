using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;

//this file is for upgrade effects, which are applied to towers on upgrade instead of copied to them.  They are called from TowerScript.AddEffects()

[ForbidEffectContext(EffectContext.tower)] //this may look misleading: these effects TARGET towers, but cannot be ON towers.
public abstract class BaseEffectUpgrade : BaseEffect, IEffectUpgrade
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.tower; } } //cards with this effect must be targeted at towers
    [Hide] public override EffectType effectType       { get { return EffectType.upgrade; } } //effect type

    public override bool triggersAs(EffectType triggerType)
    {
        return base.triggersAs(triggerType) || (triggerType == EffectType.upgrade);
    }

    public abstract void upgradeTower(TowerScript tower);
}

//sets the range of the tower to X
public class EffectSetRange : BaseEffectUpgrade
{
    [Show] public override string Name { get { return "Range becomes " + strength; } }
    [Show] public override string XMLName { get { return "setRange"; } }

    public override void upgradeTower(TowerScript tower)
    {
        tower.range = strength;
        tower.updateRangeImage();
        tower.UpdateTooltipText();
    }
}

//sets the tower's ammo to the highest value it has had, effectively restoring it to maximum
public class EffectReloadAmmo : BaseEffectUpgrade
{
    [Show] public override string Name { get { return "If the tower has limited ammo, reloads it"; } }
    [Show] public override string XMLName { get { return "reloadAmmo"; } }

    public override void upgradeTower(TowerScript tower)
    {
        if (tower.effects != null)
            if (tower.effects.propertyEffects.limitedAmmo != null)
                foreach (IEffect ie in tower.effects.effects)
                    if (ie.XMLName == "limitedAmmo")
                        ((EffectLimitedAmmo)ie).reload();

    }
}

//tower stats change by X% per round.  RechargeTime change is inverted so that negative values are always bad and positive values are always good.
public class EffectStatPercentChangePerRound : BaseEffectUpgrade
{
    [Show] public override string Name
    {
        get
        {
            string result = "Stats ";

            if (strength < 0)
                result += "degrade ";
            else
                result += "improve ";

            result += "by " + Mathf.Abs(strength) + "% per round";

            return result;
        }
    }
    [Show] public override string XMLName { get { return "statPercentChangePerRound"; } }

    private TowerScript targetTower;

    public override void upgradeTower(TowerScript tower)
    {
        targetTower = tower; //save reference

        //inject the effect description onto the tower so it shows on the tooltip
        EffectCustomDescription e = new EffectCustomDescription();
        e.argument = Name;
        tower.effects.Add(e);

        //register for event
        LevelManagerScript.instance.RoundOverEvent += doStatChanges;
    }

    //performs the actual stat changes at the end of each round
    private void doStatChanges()
    {
        targetTower.attackPower  *= (1 + (strength / 100));
        targetTower.range        *= (1 + (strength / 100));
        targetTower.rechargeTime *= (1 + (-strength / 100));

        targetTower.UpdateTooltipText();
        targetTower.updateRangeImage();
    }
}