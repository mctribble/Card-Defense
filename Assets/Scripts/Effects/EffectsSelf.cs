using UnityEngine;
using Vexe.Runtime.Types;

//all effects in this file take place instantly, and target the card which contains them.  This base effect handles behavior common to them all
public abstract class BaseEffectSelf : BaseEffect, IEffectSelf
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } } //this effect doesnt need a target
    [Hide] public override EffectType effectType       { get { return EffectType.self; } }    //this is a discard effect

    public abstract void trigger(ref Card card, GameObject card_gameObject);
}

//adds x charges to the card
public class EffectAddCharges : BaseEffectSelf
{
    [Hide] public override string Name { get { return "Card gains " + strength + " charges when cast."; } } //returns name and strength
    [Show] public override string XMLName { get { return "addCharges"; } } //name used to refer to this effect in XML

    public override void trigger(ref Card card, GameObject card_gameObject)
    {
        card.charges += Mathf.RoundToInt(strength);
    }
}

//discards x random cards from the hand in addition to this one.  They return to the bottom of the deck without damage
public class EffectDiscardRandom : BaseEffectSelf
{
    [Hide] public override string Name { get { return "Discard " + strength + " random cards"; } } //returns name and strength
    [Show] public override string XMLName { get { return "discardRandomCard"; } } //name used to refer to this effect in XML

    public override void trigger(ref Card card, GameObject card_gameObject)
    {
        HandScript handRef = HandScript.playerHand;
        handRef.StartCoroutine(handRef.discardRandomCards(card_gameObject, Mathf.FloorToInt(strength)));
    }
}