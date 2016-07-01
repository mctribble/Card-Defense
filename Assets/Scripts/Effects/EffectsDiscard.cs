//all effects in this file happen when the card is discarded (whether there are charges remaining or not)

//trigger() for these effects returns true if the card no longer needs to be discarded afterwards

//this card returns to the top of the deck instead of the bottom
using Vexe.Runtime.Types;

internal class EffectReturnsToTopOfDeck : IEffectDiscard
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //this effect doesnt need a target
    [Hide] public EffectType effectType { get { return EffectType.discard; } }         //this is a discard effect
    [Hide] public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "Returns to top of deck."; } } //returns name and strength

    [Show] public string XMLName { get { return "returnsToTopOfDeck"; } } //name used to refer to this effect in XML

    public bool trigger(ref Card c)
    {
        DeckManagerScript.instance.addCardAtTop(c);
        return true; //tell the card it no longer needs to be discarded
    }
}