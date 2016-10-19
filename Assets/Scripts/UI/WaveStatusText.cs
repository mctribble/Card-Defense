using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vexe.Runtime.Types;

public class WaveStatusText : BaseBehaviour
{
    public Text enemyStatText;
    public Text enemyDeckText;

    // Use this for initialization
    private void Start()
    {
        LevelManagerScript.instance.LevelLoadedEvent += LevelLoadedHandler;
    }

    // event handler for LevelManagerScript.instance.LevelLoadedEvent
    private void LevelLoadedHandler()
    {
        enemyStatText.raycastTarget = true; //allow this object to capture mouse events only after the level has loaded
    }

    // Update is called once per frame
    private void Update()
    {
        //dont update if paused or there is no level loaded
        if ((Time.timeScale == 0.0f) || (LevelManagerScript.instance.levelLoaded == false))
            return;

        //also dont update if there are no waves yet
        if (LevelManagerScript.instance.data.waves.Count == 0)
            return;

        //enemy deck counter
        enemyDeckText.text = LevelManagerScript.instance.wavesInDeck + "/" + LevelManagerScript.instance.data.waves.Count;

        //wave stats
        int stillToSpawn = LevelManagerScript.instance.SpawnCount - LevelManagerScript.instance.totalSpawnedThisWave;
        if (stillToSpawn > 0)
        {
            //wave has not started or is still spawning: "?????? incoming (???s)"
            enemyStatText.text = stillToSpawn + " incoming (" +
            HandScript.enemyHand.longestTime.ToString("F1") + "s)";
        }
        else
        {
            //wave has finished spawning: "???? remain (????? health)"
            enemyStatText.text = (LevelManagerScript.instance.SpawnCount - LevelManagerScript.instance.deadThisWave) + " remain (" + LevelManagerScript.instance.totalRemainingHealth + " health)";
        }

        //if the game speed is not 1.0, add text to show what it is
        if (Time.timeScale != 1.0f)
        {
            //decide the color of the game speed indicator based on whether or not we are below the desired speed
            if (Time.timeScale < 1.0f)
                enemyStatText.text += "<color=red>";
            else if (Time.timeScale < LevelManagerScript.instance.desiredTimeScale)
                enemyStatText.text += "<color=yellow>";
            else
                enemyStatText.text += "<color=green>";

            enemyStatText.text += "\n(speed x" + Time.timeScale.ToString("F1") + ")</color>";
        }
    }
}