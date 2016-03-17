using UnityEngine;
using System.Collections;

//all effects in this file happen when the card is discarded (whether there are charges remaining or not)

//trigger() for these effects returns true if the card no longer needs to be discarded afterwards

//this card returns to the top of the deck instead of the bottom
class EffectReturnsToTopOfDeck : IEffectDiscard
{
    //generic interface
    public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    public EffectType effectType { get { return EffectType.discard; } }         //this is a discard effect
    public float strength { get; set; }                                         //how strong this effect is

    //this effect
    public string Name { get { return "Returns to top of deck."; } }        //returns name and strength
    public bool trigger(ref Card c)
    {
        DeckManagerScript.instance.addCardAtTop(c);
        return true; //tell the card it no longer needs to be discarded
    }
}

//adds x charges to the card
class EffectAddCharges : IEffectDiscard
{
    //generic interface
    public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    public EffectType effectType { get { return EffectType.discard; } }         //this is a discard effect
    public float strength { get; set; }                                         //how strong this effect is

    //this effect
    public string Name { get { return "Card gains " + strength + "charges."; } }        //returns name and strength
    public bool trigger(ref Card c)
    {
        Debug.Log(c.charges);
        c.charges += Mathf.RoundToInt(strength);
        Debug.Log(c.charges);
        return false; //the card should be discarded as normal
    }
}