using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;

//this file is for upgrade effects, which are applied to towers on upgrade instead of copied to them.  They are called from TowerScript.AddEffects()
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