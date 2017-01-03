using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;

public class SelectionHandScript : HandScript
{
    public GameObject  previewCardPrefab;//prefab used to spawn a new card preview

    [Hide] public static SelectionHandScript instance; //singleton instance

    protected override IEnumerator Start()
    {
        yield return base.Start();
        instance = this;
    }

    /// <summary>
    /// prompts the user to choose a card in this hand with the given message.  
    /// options are the contents of the hand.
    /// </summary>
    /// <param name="exception">GameObject to exclude from the list of valid choices</param>
    /// <param name="prompt">message to show the player during the selection (not yet supported)</param>
    /// <returns></returns>
    public IEnumerator selectCard(GameObject exception, string prompt)
    {
        selectedCard = null;

        //if the hand is empty, bail now
        if (currentHandSize == 0)
            yield break;

        //if the hand has only one card, just act as if that was the selection and return immediately
        if (currentHandSize == 1)
        {
            selectedCard = cards[0];
            yield break;
        }

        //wait for a selection to be made and return it
        while (selectedCard == null)
            yield return null;

        yield break;
    }

    //if a cardPreviewScript in this hand gets clicked on, store it as the selected card for the selectCard() coroutine
    private void cardPreviewClicked(CardScript card) { selectedCard = card; }
}