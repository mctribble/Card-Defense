using System;
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

//discards up to x random cards from the hand IN ADDITION TO this one, then draws a card for each card discarded EXCEPT FOR this one (therefore, if player has 7 cards and the effect replaces up to 7, their hand will be emptied and they will draw six)
public class EffectReplaceRandomCard : BaseEffectSelf
{
    [Hide] public override string Name { get { return "Discard up to " + strength + " cards at random, and draw new ones to replace them"; } } //returns name and strength
    [Show] public override string XMLName { get { return "replaceRandomCard"; } } //name used to refer to this effect in XML

    public override void trigger(ref Card card, GameObject card_gameObject)
    {
        int toReplace = Mathf.Min( Mathf.RoundToInt(strength), HandScript.playerHand.currentHandSize-1);  //how many cards are being replaced

        //if we are replacing the entire hand, do them all at once with no delay
        bool applyDelay = true;
        if (toReplace == HandScript.playerHand.currentHandSize - 1)
            applyDelay = false;

        HandScript.playerHand.StartCoroutine(HandScript.playerHand.discardRandomCards(card_gameObject, toReplace, applyDelay)); //discard toReplace random cards that are NOT this one (this card will be discarded regardless, since it was just played)
        HandScript.playerHand.StartCoroutine(HandScript.playerHand.drawCards(toReplace, applyDelay)); //draw new cards to replace them
    }
}