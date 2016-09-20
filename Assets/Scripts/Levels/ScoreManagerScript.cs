using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;

public class ScoreManagerScript : BaseBehaviour
{
    [Hide] public static ScoreManagerScript instance;

    //scoring constants
    public int CLEAR_VALUE;      //for winning
    public int WAVE_VALUE;       //for clearing a wave
    public int ENEMY_DRAW_VALUE; //for drawing an enemy card
    public int FLAWLESS_VALUE;   //for winning without taking damage from enemies

    //score tracking
    public int  wavesCleared;    //score from wave clears
    public int  enemyCardsDrawn; //score from drawing enemies
    public int  bonusPoints;     //score from cards with the score effect
    public bool flawless;        //set to false if the player is damaged by an enemy

	//Use this for initialization
	void Awake()
    {
        instance = this;
        wavesCleared = 0;
        enemyCardsDrawn = 0;
        flawless = true;
	}
	
    //returns a string of the score report.  PlayerWon indicates whether or not hte player won the level
    public string report(bool PlayerWon)
    {
        string result = "";
        int totalScore = 0;

        if (PlayerWon)
        {
            result += "Victory!:   " + CLEAR_VALUE + '\n';
            totalScore += CLEAR_VALUE;
        }
        else
        {
            result += "Defeat!:    " + 0 + '\n';
        }

        result +=     "Wave bonus:   " + wavesCleared * WAVE_VALUE + '\n';
        totalScore += wavesCleared * WAVE_VALUE;

        result +=     "Draw bonus:   " + enemyCardsDrawn * ENEMY_DRAW_VALUE + '\n';
        totalScore += enemyCardsDrawn * ENEMY_DRAW_VALUE;

        if (flawless)
        {
            result += "Flawless!:    " + FLAWLESS_VALUE + '\n';
            totalScore += FLAWLESS_VALUE;
        }

        if (bonusPoints != 0)
        {
            result += "Bonus points: " + bonusPoints + '\n';
            totalScore += bonusPoints;
        }

        result +=     "=================\n";
        result +=     "Total:      " + totalScore;

        return result;
    }
}
