using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// "Property" effects are never really triggered in the usual sense.  This base effect handles behavior common to them all
/// </summary>
abstract class BaseEffectProperty : BaseEffect, IEffectProperty
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } }  //property effects are not targeted
    [Hide] public override EffectType effectType       { get { return EffectType.property; } } //effect type

    public override bool triggersAs(EffectType triggerType)
    {
        return base.triggersAs(triggerType) || (triggerType == EffectType.property);
    }
}

class EffectReturnsToTopOfDeck : BaseEffectProperty
{
    [Hide] public override string Name { get { return "put this card on top of the deck"; } } //returns name and strength
    [Show] public override string XMLName { get { return "returnsToTopOfDeck"; } } //name used to refer to this effect in XML.
}

//tower lifespan does not decrease and is displayed as ∞.
class EffectInfiniteTowerLifespan : BaseEffectProperty
{
    [Hide] public override string Name { get { return null; } } //returns name and strength
    [Show] public override string XMLName { get { return "infiniteTowerLifespan"; } } //name used to refer to this effect in XML.
}

//colorizes attacks associated with this effect
class EffectAttackColor : BaseEffectProperty
{
    [Hide] public override string Name { get { return null; } } //returns name and strength
    [Show] public override string XMLName { get { return "attackColor"; } } //name used to refer to this effect in XML.
}

//tower can only fire X times before disappearing
class EffectLimitedAmmo : BaseEffectProperty
{
    [Hide] public override string Name { get { return null; } } //returns name and strength
    [Show] public override string XMLName { get { return "limitedAmmo"; } } //name used to refer to this effect in XML.

    private float maxStrength = float.MinValue;

    [Show] public override float strength
    {
        get { return base.strength; }
        set { base.strength = value; maxStrength = Mathf.Max(maxStrength, value); }
    }

    //restores the effect strength to the highest value it has ever held
    public void reload()
    {
        strength = maxStrength;
    }
}

//tower only fires if clicked on 
class EffectManualFire : BaseEffectProperty
{
    [Hide] public override string Name { get { return "Manual Fire"; } } //returns name and strength
    [Show] public override string XMLName { get { return "manualFire"; } } //name used to refer to this effect in XML.
}

//upgrade does not cost an upgrade slot
[ForbidEffectContext(EffectContext.tower)]
class EffectNoUpgradeCost : BaseEffectProperty
{
    [Hide] public override string Name { get { return "Does not cost an upgrade slot."; } } //returns name and strength
    [Show] public override string XMLName { get { return "noUpgradeCost"; } } //name used to refer to this effect in XML.
}

//tower can have up to X points of overcharge
class EffectMaxOvercharge : BaseEffectProperty
{
    [Hide] public override string Name { get { return "overcharge: " + strength; } } //returns name and strength
    [Show, Display(1)] public override string XMLName { get { return "maxOvercharge"; } } //name used to refer to this effect in XML.
}

//attacks ignore armor
class EffectArmorPierce : BaseEffectProperty
{
    [Hide] public override string Name { get { return "attacks ignore armor"; } } //returns name and strength
    [Show] public override string XMLName { get { return "armorPierce"; } } //name used to refer to this effect in XML.
}

//tower cannot receive upgrades
class EffectUpgradesForbidden : BaseEffectProperty
{
    [Hide] public override string Name { get { return "Cannot be upgraded"; } } //returns name and strength
    [Show] public override string XMLName { get { return "upgradesForbidden"; } } //name used to refer to this effect in XML.
}

//tower cannot be discarded
class EffectCannotBeDiscarded : BaseEffectProperty
{
    [Hide] public override string Name { get { return "Cannot be randomly discarded"; } } //returns name and strength
    [Show] public override string XMLName { get { return "cannotBeDiscarded"; } } //name used to refer to this effect in XML.
}