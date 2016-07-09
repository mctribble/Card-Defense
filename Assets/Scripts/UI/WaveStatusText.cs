using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vexe.Runtime.Types;

public class WaveStatusText : BaseBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Text text;
    private bool mousedOver;

    // Use this for initialization
    private void Start()
    {
        text = GetComponent<Text>();
        mousedOver = false;
        LevelManagerScript.instance.LevelLoadedEvent += LevelLoadedHandler;
    }

    // event handler for LevelManagerScript.instance.LevelLoadedEvent
    private void LevelLoadedHandler()
    {
        text.raycastTarget = true; //alloww this object to capture mouse events only after the level has loaded
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

        //show enemy stats instead of wave stats on mouse over
        if (mousedOver)
        {
            showEnemyStats();
            return;
        }
            
        //get variables
        int curWave = LevelManagerScript.instance.currentWave;
        WaveData curWaveData = LevelManagerScript.instance.data.waves[curWave];

        //first line is always Wave ??/?? (?????)
        text.text = "Wave " + (curWave + 1) + "/" + LevelManagerScript.instance.data.waves.Count +
            " (<color=#" + curWaveData.getEnemyData().unitColor.toHex() + ">"  + curWaveData.type + "</color>)\n";

        //if the wave is still spawning or has not yet started, second line is ??? incoming over ??? seconds
        if (LevelManagerScript.instance.SpawnCount != 0)
        {
            text.text += LevelManagerScript.instance.SpawnCount +
                " incoming over " + curWaveData.time.ToString("F1") + " seconds";
        }
        else //if the wave has finished spawning, second line is ??? remaining with ?????? health
        {
            text.text += (Mathf.RoundToInt((float)curWaveData.budget / (float)curWaveData.getEnemyData().spawnCost) - LevelManagerScript.instance.deadThisWave) +
                " remaining with " + LevelManagerScript.instance.WaveTotalRemainingHealth + " health";
        }

        //if the game speed is not 1.0, add text to show what it is
        if (Time.timeScale != 1.0f)
        {
            //decide the color of the game speed indicator based on whether or not we are below the desired speed
            if (Time.timeScale < 1.0f)
                text.text += "<color=red>";
            else if (Time.timeScale < LevelManagerScript.instance.desiredTimeScale)
                text.text += "<color=yellow>";
            else
                text.text += "<color=black>";

            text.text += "\n(speed x" + Time.timeScale.ToString("F1") + ")</color>";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mousedOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mousedOver = false;
    }

    private void showEnemyStats()
    {
        WaveData curWaveData = LevelManagerScript.instance.data.waves[LevelManagerScript.instance.currentWave];
        text.text = "<color=#" + curWaveData.getEnemyData().unitColor.toHex() + ">"  + curWaveData.type + "</color>: " + "\n" +
            "Health: " + curWaveData.getEnemyData().maxHealth + " Attack: " + curWaveData.getEnemyData().damage + " Speed: " + curWaveData.getEnemyData().unitSpeed;

        if ((curWaveData.getEnemyData().effectData != null) && (curWaveData.getEnemyData().effectData.effects.Count > 0))
            foreach (IEffect e in curWaveData.getEnemyData().effectData.effects)
                text.text += "\n" + e.Name;
    }
}