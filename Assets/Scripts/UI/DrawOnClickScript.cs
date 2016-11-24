using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;

/// <summary>
/// draws from handToDraw whenever the player clicks on this object.  
/// If oncePerRound is true, they can only do this once per round and the object fades out if drawing is unavailable
/// </summary>
public class DrawOnClickScript : BaseBehaviour, IPointerClickHandler
{
    public HandFaction handToDraw; //the hand that should draw when the object with this script is clicked
    public bool oncePerRound; //if true, this can only be done once/round

    public Image fadeImage;   //if not null, this image will be grayed out when drawing is forbidden
    public Color fadeColor;   //color to use when faded out
    public Color normalColor; //color to use normally

    private bool drawnThisRound; //if true, we have drawn this round

    //event handling for updating drawnThisRound
    public void Start () { LevelManagerScript.instance.RoundOverEvent += roundOverHandler;  LevelManagerScript.instance.LevelLoadedEvent += levelLoadedHandler; } //register events
    public void roundOverHandler() { drawnThisRound = false; } //handle event
    public void levelLoadedHandler() { drawnThisRound = false; } //handle event

    //check every few frames to see if we need to fade/unfade the image
    private void Update()
    {
        if ( (fadeImage != null) && (oncePerRound == true) )
        {
            if (Time.frameCount % 8 == 0) //every eight frames
            {
                if (drawnThisRound)
                    fadeImage.color = fadeColor;
                else
                    fadeImage.color = normalColor;
            }
        }
    }

    //click handler
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
