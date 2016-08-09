using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;

//contains "Property" effects that are never really triggered in the usual sense


class EffectReturnsToTopOfDeck : IEffectProperty
{
    //generic interface
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //property effects are not targeted
    [Hide] public EffectType effectType { get { return EffectType.property; } }        //effect type
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)
    
    //this effect
    [Hide] public string Name { get { return "put this card on top of the deck"; } } //returns name and strength
    [Show] public string XMLName { get { return "returnsToTopOfDeck"; } } //name used to refer to this effect in XML.
}

//tower lifespan does not decrease and is displayed as ∞.
class EffectInfiniteTowerLifespan : IEffectProperty
{
    //generic interface
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //property effects are not targeted
    [Hide] public EffectType effectType { get { return EffectType.property; } }        //effect type
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)
    
    //this effect
    [Hide] public string Name { get { return "tower does not decay"; } } //returns name and strength
    [Show] public string XMLName { get { return "infiniteTowerLifespan"; } } //name used to refer to this effect in XML.
}

//colorizes attacks associated with this effect
class EffectAttackColor : IEffectProperty
{
    //generic interface
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //property effects are not targeted
    [Hide] public EffectType effectType { get { return EffectType.property; } }        //effect type
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (HTML color)
    
    //this effect
    [Hide] public string Name { get { return "Attack uses <color=" + argument + "> this</color> color"; } } //returns name and strength
    [Show] public string XMLName { get { return "attackColor"; } } //name used to refer to this effect in XML.
}

//tower can only fire X times before disappearing
class EffectLimitedAmmo : IEffectProperty
{
    //generic interface
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //property effects are not targeted
    [Hide] public EffectType effectType { get { return EffectType.property; } }        //effect type
    [Hide] public float strength { get; set; }                                         //number of shots the tower has
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)
    
    //this effect
    [Hide] public string Name { get { return "Ammo: " + strength; } } //returns name and strength
    [Show] public string XMLName { get { return "limitedAmmo"; } } //name used to refer to this effect in XML.
}

//tower only fires if clicked on 
class EffectManualFire : IEffectProperty
{
    //generic interface
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //property effects are not targeted
    [Hide] public EffectType effectType { get { return EffectType.property; } }        //effect type
    [Hide] public float strength { get; set; }                                         //number of shots the tower has
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)
    
    //this effect
    [Hide] public string Name { get { return "Manual Fire"; } } //returns name and strength
    [Show] public string XMLName { get { return "manualFire"; } } //name used to refer to this effect in XML.
}

//tower can have up to X points of overcharge
class EffectMaxOvercharge : IEffectProperty
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //property effects are not targeted
    [Hide] public EffectType effectType { get { return EffectType.property; } }        //effect type
    [Hide] public float strength { get; set; }                                         //max points of overcharge
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)
    
    //this effect
    [Hide] public string Name { get { return "overcharge: " + strength; } } //returns name and strength
    [Show] public string XMLName { get { return "maxOvercharge"; } } //name used to refer to this effect in XML.
}