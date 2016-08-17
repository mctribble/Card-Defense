using UnityEngine;
using Vexe.Runtime.Types;

//all effects in this file take place instantly, and target the card which contains them

//adds x charges to the card
internal class EffectAddCharges : IEffectSelf
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.self; } }            //this is a discard effect
    [Show, Display(2)] public float strength { get; set; }                             //how strong this effect is.  (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "Card gains " + strength + " charges when cast."; } } //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "addCharges"; } } //name used to refer to this effect in XML

    public void trigger(ref Card card, GameObject card_gameObject)
    {
        card.charges += Mathf.RoundToInt(strength);
    }
}

//discards x random cards from the hand in addition to this one.  They return to the bottom of the deck without damage
public class EffectDiscardRandom : IEffectSelf
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.self; } }            //this is an instant effect
    [Show, Display(2)] public float strength { get; set; }                             //how strong this effect is.  (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "Discard up to " + strength + " random cards"; } } //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "discardRandomCard"; } } //name used to refer to this effect in XML

    public void trigger(ref Card card, GameObject card_gameObject)
    {
        HandScript handRef = GameObject.FindGameObjectWithTag("Hand").GetComponent<HandScript>();
        handRef.StartCoroutine(handRef.discardRandomCards(card_gameObject, Mathf.FloorToInt(strength)));
    }
}