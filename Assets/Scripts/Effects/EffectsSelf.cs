using System;
using System.Collections;
using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// all self effects take place instantly, and target the card which contains them.  This base effect handles behavior common to them all
/// </summary>
public abstract class BaseEffectSelf : BaseEffect, IEffectSelf
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } } //this effect doesnt need a target
    [Hide] public override EffectType effectType       { get { return EffectType.self; } }    //this is a discard effect

    public abstract void trigger(ref PlayerCard card, GameObject card_gameObject);
}

//adds x charges to the card
public class EffectAddCharges : BaseEffectSelf
{
    [Hide] public override string Name
    {
        get
        {
            if (strength < 0)
                return "Costs " + (strength - 1) * -1 + " charges to cast";

            if (strength == 0)
                return null;

            if (strength == 1.0f)
                return "Does not cost a charge to cast";

            return "Card gains " + (strength - 1) + " charges when cast.";
        }
    } 

    [Show] public override string XMLName { get { return "addCharges"; } } //name used to refer to this effect in XML

    public override void trigger(ref PlayerCard card, GameObject card_gameObject)
    {
        card.charges += Mathf.RoundToInt(strength);
    }
}

//discards x chosen cards from the hand in addition to this one.  They return to the bottom of the deck without damage
public class EffectDiscardChosen : BaseEffectSelf
{
    [Hide] public override string Name
    {
        get
        {
            if (strength == 1)
                return "Choose a card to discard";
            else
                return "Choose up to " + strength + " cards to discard";
        }
    } 
    [Show] public override string XMLName { get { return "discardChosenCard"; } }

    //this effect relies on user input, so it starts a coroutine
    public override void trigger(ref PlayerCard card, GameObject card_gameObject)
    {
        SelectionHandScript.instance.StartCoroutine(effectCoroutine(card_gameObject));
    }

    //asks player which cards to discard, then discards them
    private IEnumerator effectCoroutine(GameObject exception)
    {
        int count = Mathf.Min( (Mathf.RoundToInt(strength)), (PlayerHandScript.instance.currentHandSize-1) ); //number of cards to actually discard

        for (int i = 0; i < count; i++)
        {
            //wait for everything in the hand to stop moving around, in case other effects on the same card do something, like drawing/discarding cards, to affect how this should behave
            yield return PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.waitForReady());

            //have player select any card in the player hand except this one
            yield return PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.selectCard(exception, "Discard what? (" + (count - i) + " to go)"));
            CardScript selected = PlayerHandScript.instance.selectedCard;

            //discard the chosen card
            if (selected == null)
                Debug.LogWarning("HandScript failed to prompt user to pick a card!");
            else
                selected.StartCoroutine(selected.Discard());
        }
    }
}

//discards x random cards from the hand in addition to this one.  They return to the bottom of the deck without damage
public class EffectDiscardRandom : BaseEffectSelf
{
    [Hide] public override string Name { get { return "Discard " + strength + " random cards"; } } //returns name and strength
    [Show] public override string XMLName { get { return "discardRandomCard"; } } //name used to refer to this effect in XML

    public override void trigger(ref PlayerCard card, GameObject card_gameObject)
    {
        HandScript handRef = PlayerHandScript.instance;
        handRef.StartCoroutine(handRef.discardRandomCards(card_gameObject.GetComponent<CardScript>(), Mathf.FloorToInt(strength)));
    }
}

//discards up to x random cards from the hand IN ADDITION TO this one, then draws a card for each card discarded EXCEPT FOR this one (therefore, if player has 7 cards and the effect replaces up to 7, their hand will be emptied and they will draw six)
public class EffectReplaceRandomCard : BaseEffectSelf
{
    [Hide] public override string Name { get { return "Discard up to " + strength + " cards at random, and draw new ones to replace them"; } } //returns name and strength
    [Show] public override string XMLName { get { return "replaceRandomCard"; } } //name used to refer to this effect in XML

    public override void trigger(ref PlayerCard card, GameObject card_gameObject)
    {
        int toReplace = Mathf.Min( Mathf.RoundToInt(strength), PlayerHandScript.instance.discardableCardCount-1);  //how many cards are being replaced

        //if we are replacing the entire hand, do them all at once with no delay
        bool applyDelay = true;
        if (toReplace == PlayerHandScript.instance.currentHandSize - 1)
            applyDelay = false;

        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.discardRandomCards(card_gameObject.GetComponent<CardScript>(), toReplace, applyDelay)); //discard toReplace random cards that are NOT this one (this card will be discarded regardless, since it was just played)
        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards(toReplace, applyDelay)); //draw new cards to replace them
    }
}