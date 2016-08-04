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
    [Hide] public string Name { get { return "put this card on top of deck"; } } //returns name and strength
    [Show] public string XMLName { get { return "returnsToTopOfDeck"; } } //name used to refer to this effect in XML.  This should never happen for this effect since it is a placeholder
}
