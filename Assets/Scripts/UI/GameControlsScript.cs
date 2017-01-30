using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControlsScript : MonoBehaviour
{
    //references
    public MenuButtonScript startButton;
    public MenuButtonScript pauseButton;
    public MenuButtonScript Speed1Button;
    public MenuButtonScript Speed2Button;
    public MenuButtonScript Speed3Button;

    //speed settings
    public float speed1;                //time scale for the '>' button
    public float speed2;                //time scale for the '>>' button
    public float speed3;                //time scale for the '>>>' button
    public float forceSlowDownBelowFPS; //time slows down on its own if framerate dips below this
    public float allowSpeedUpAboveFPS;  //game speeds back up if the player wants it to and framerate is above this

    //color settings
    public Color defaultColor;          //button can be clicked on and is inactive
    public Color selectedColor;         //button is currently selected
    public Color forcedColor;           //this is the current speed because the game cant keep up
    public Color unusableColor;         //this button cannot be selected because the game cant keep up
    public Color unusableSelectedColor; //this is the player's desired speed, but it is not active because the game cant keep up

    //current status, shown only if the game is running
    public float desiredTimeScale; //the game speed the player wants to play at

    //for unpausing
    private float speedWhenLastPaused;

    // Use this for initialization
    void Start ()
    {
        //start at speed 1, and not in a wave
        startButton.setColor(defaultColor);
        pauseButton.setColor(defaultColor);
        Speed1Button.setColor(selectedColor);
        desiredTimeScale = speed1;
        Speed2Button.setColor(defaultColor);
        Speed3Button.setColor(defaultColor);
    }
	
	//adjusts speed if the game cant keep up and handle the wave status indicator
	void Update ()
    {
        //respond to keyboard controls by behaving as we would for button clicks
        if (Input.GetButtonDown("Start Wave"))
            TextButtonSelected(startButton.buttonText.text);
        if (Input.GetButtonDown("Pause"))
            TextButtonSelected(pauseButton.buttonText.text);
        if (Input.GetButtonDown("Speed 1"))
            TextButtonSelected(Speed1Button.buttonText.text);
        if (Input.GetButtonDown("Speed 2"))
            TextButtonSelected(Speed2Button.buttonText.text);
        if (Input.GetButtonDown("Speed 3"))
            TextButtonSelected(Speed3Button.buttonText.text);
        if (Input.GetButtonDown("Cycle Speed"))
        {
            if ((desiredTimeScale == 0.0f) || (desiredTimeScale == speed3))
                TextButtonSelected(Speed1Button.buttonText.text);
            else if (desiredTimeScale == speed1)
                TextButtonSelected(Speed2Button.buttonText.text);
            else if (desiredTimeScale == speed2)
                TextButtonSelected(Speed3Button.buttonText.text);
            else
                Debug.LogWarning("Couldn't Cycle Speed: no case for our current speed");
        }

        //attempt to regulate timeScale so the game slows down if the framerate tanks but then speeds back up when things settle down
        //the time scale will go down if frame rate is below the reduce threshold, and up if frame rate is above the increase threshold
        float timeScaleReduceThreshold   = (1.0f / forceSlowDownBelowFPS); 
        float timeScaleIncreaseThreshold = (1.0f / allowSpeedUpAboveFPS);  

        if (Time.timeScale > desiredTimeScale) //if we are going faster than the player wants...
            Time.timeScale = desiredTimeScale; //then slow down!

        float unscaledSmoothDeltaTime = Time.smoothDeltaTime / Time.timeScale;  //smooth delta time scales by the sim speed, so we have to undo that for framerate calculations

        //force slow down if we cant keep up
        if (unscaledSmoothDeltaTime > timeScaleReduceThreshold) //if frame rate is below the threshold
        {
            //drop by one speed setting, if we can
            if (Time.timeScale == speed3)
            {
                Time.timeScale = speed2;
                updateSpeedButtons();
            }
            else if (Time.timeScale == speed2)
            {
                Time.timeScale = speed1;
                updateSpeedButtons();
            }
        }

        //allow speed to go back up once frame rate recovers
        if (unscaledSmoothDeltaTime < timeScaleIncreaseThreshold) //if the frame rate is doing well...
        {
            if (Time.timeScale < desiredTimeScale) //and the player wants a higher sim speed...
            {
                //go up to the next setting
                if (Time.timeScale == speed1)
                {
                    Time.timeScale = speed2;
                    updateSpeedButtons();
                }
                else if (Time.timeScale == speed2)
                {
                    Time.timeScale = speed3;
                    updateSpeedButtons();
                }
            }
        }

        //update the wave button text
        int stillToSpawn = LevelManagerScript.instance.SpawnCount - LevelManagerScript.instance.totalSpawnedThisWave;
        int enemiesRemaining = EnemyManagerScript.instance.activeEnemies.Count;

        if (LevelManagerScript.instance.wavesSpawning > 0)
        {
            //enemies are currently spawning
            startButton.setButtonText("incoming: " + stillToSpawn);
        }
        else if (enemiesRemaining > 0)
        {
            //enemies are done spawning, but some are still alive
            startButton.setButtonText(enemiesRemaining + " remain");
        }
        else
        {
            //we are between waves
            startButton.setButtonText("Start Wave");
            startButton.setColor(defaultColor);
        }
    }

    //updates speed setting buttons
    public void updateSpeedButtons()
    {
        // ||
        if (desiredTimeScale == 0.0f)
        {
            //game is paused
            pauseButton.setColor(selectedColor);
        }
        else
        {
            //game is not paused
            pauseButton.setColor(defaultColor);
        }

        // >
        if (desiredTimeScale == speed1)
        {
            //button is selected
            Speed1Button.setColor(selectedColor);
        }
        else
        {
            if (Time.timeScale == speed1)
            {
                //button is not selected, but the game is forced to this speed
                Speed1Button.setColor(forcedColor);
            }
            else
            {
                //button is neither selected or forced to this speed
                Speed1Button.setColor(defaultColor);
            }
        }

        // >>
        if (desiredTimeScale == speed2)
        {
            if (Time.timeScale == speed2)
            {
                //button is selected, and game is moving this speed
                Speed2Button.setColor(selectedColor);
            }
            else
            {
                //button is selected, but the game is not moving this speed because it cant keep up
                Speed2Button.setColor(unusableSelectedColor);
            }
        }
        else
        {
            if (Time.timeScale == speed2)
            {
                //button is not selected, but the game is forced to this speed
                Speed2Button.setColor(forcedColor);
            }
            else if ( (Time.timeScale < speed2) && (Time.timeScale < desiredTimeScale) )
            {
                //button is not selected, and the game is forced to move slower than this
                Speed2Button.setColor(unusableColor);
            }
            else
            {
                //button is not selected, and the game is moving faster than this
                Speed2Button.setColor(defaultColor);
            }
        }

        // >>>
        if (desiredTimeScale == speed3)
        {
            if (Time.timeScale == speed3)
            {
                //button is selected, and game is moving this speed
                Speed3Button.setColor(selectedColor);
            }
            else if (Time.timeScale < speed3)
            {
                //button is selected, but the game is not moving this speed because it cant keep up
                Speed3Button.setColor(unusableSelectedColor);
            }
        }
        else
        {
            if ((Time.timeScale < speed3) && (Time.timeScale < desiredTimeScale))
            {
                //button is not selected, and the game is forced to move slower than this
                Speed2Button.setColor(unusableColor);
            }
            else
            {
                //button is neither selected or forced to this speed
                Speed3Button.setColor(defaultColor);
            }
        }
    }

    //a button was clicked on
    public void TextButtonSelected(string text)
    {
        switch(text)
        {
            case "||":
                if (desiredTimeScale == 0.0f)
                {
                    desiredTimeScale = speedWhenLastPaused;
                }
                else
                {
                    speedWhenLastPaused = desiredTimeScale;
                    desiredTimeScale = 0.0f;
                }

                updateSpeedButtons();
                break;

            case ">":
                desiredTimeScale = speed1;
                updateSpeedButtons();
                break;

            case ">>":
                desiredTimeScale = speed2;
                updateSpeedButtons();
                break;

            case ">>>":
                desiredTimeScale = speed3;
                updateSpeedButtons();
                break;

            case "Start Wave":
                LevelManagerScript.instance.startRound();
                startButton.setColor(unusableColor);
                break;
        }

        //if the time scale is currently 0, bump it up to speed1 to get things moving again
        if (Time.timeScale == 0.0f)
            Time.timeScale = speed1;
    }
}
