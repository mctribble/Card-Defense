using UnityEngine;
using System.Collections;

//all effects in this file take place instantly, and target the card which contains them

//adds x charges to the card
class EffectAddCharges : IEffectSelf
{
    //generic interface
    public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    public EffectType effectType { get { return EffectType.self; } }            //this is a discard effect
    public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    public string Name { get { return "Card gains " + strength + "charges."; } }        //returns name and strength
    public void trigger(ref Card card, GameObject card_gameObject)
    {
        card.charges += Mathf.RoundToInt(strength);
    }
}

//discards x random cards from the hand in addition to this one.  They return to the bottom of the deck without damage
public class EffectDiscardRandom : IEffectSelf
{
    //generic interface
    public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    public EffectType effectType { get { return EffectType.self; } }            //this is an instant effect
    public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    public string Name { get { return "Discard " + strength + " random cards"; } }        //returns name and strength
    public void trigger(ref Card card, GameObject card_gameObject)
    {
        for (int i = 0; i < strength; i++)
        {
            GameObject.FindGameObjectWithTag("Hand").SendMessage("discardRandom", card_gameObject);
        }
    }
}