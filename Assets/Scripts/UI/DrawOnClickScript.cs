using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using UnityEngine.EventSystems;
using System;

public class DrawOnClickScript : BaseBehaviour, IPointerClickHandler
{
    public HandFaction handToDraw; //the hand that should draw when the object with this script is clicked

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (handToDraw)
        {
            case HandFaction.player:
                HandScript.playerHand.drawCard();
                break;
            case HandFaction.enemy:
                if (HandScript.enemyHand.isFull == false)
                {
                    HandScript.enemyHand.drawCard();
                    ScoreManagerScript.instance.enemyCardsDrawn++;
                }
                break;
            default:
                MessageHandlerScript.Warning("DrawOnClickScript doesnt know how to draw to a hand of that type");
                break;
        }
    }
}
