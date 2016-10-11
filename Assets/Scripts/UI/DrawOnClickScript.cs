using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using UnityEngine.EventSystems;
using System;

public class DrawOnClickScript : BaseBehaviour, IPointerClickHandler
{
    public HandFaction handToDraw; //the hand that should draw when the object with this script is clicked
    public bool oncePerRound; //if true, this can only be done once/round

    private bool drawnThisRound; //if true, we have drawn this round

    //event handling for updating drawnThisRound
    public void Start () { LevelManagerScript.instance.RoundOverEvent += roundOverHandler; } //register event
    public void roundOverHandler() { drawnThisRound = false; } //handle event

    public void OnPointerClick(PointerEventData eventData)
    {
        if ((oncePerRound == false) || (drawnThisRound == false))
        {
            switch (handToDraw)
            {
                case HandFaction.player:
                    HandScript.playerHand.drawCard();
                    drawnThisRound = true;
                    break;
                case HandFaction.enemy:
                    if (HandScript.enemyHand.isFull == false)
                    {
                        //if an enemy card is drawn successfully, the player gets a score bonus and two extra cards in hand
                        int oldCount = HandScript.enemyHand.currentHandSize;
                        HandScript.enemyHand.drawCard();

                        if (oldCount < HandScript.enemyHand.currentHandSize)
                        {
                            ScoreManagerScript.instance.enemyCardsDrawn++;
                            HandScript.playerHand.drawCard();
                            HandScript.playerHand.drawCard();
                            drawnThisRound = true;
                        }
                    }
                    break;
                default:
                    MessageHandlerScript.Warning("DrawOnClickScript doesnt know how to draw to a hand of that type");
                    break;
            }
        }
    }
}
