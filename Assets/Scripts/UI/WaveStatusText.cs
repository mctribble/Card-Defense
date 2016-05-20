﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class WaveStatusText : MonoBehaviour {

	Text text;

	// Use this for initialization
	void Start () {
		text = GetComponent<Text> ();
	}
	
	// Update is called once per frame
	void Update () {
		//dont update if paused or there is no level loaded
		if ((Time.timeScale == 0.0f) || (LevelManagerScript.instance.levelLoaded == false))
			return;

		//also dont update if there are no waves yet
		if (LevelManagerScript.instance.data.waves.Count == 0)
			return;

        //get variables
		int curWave = LevelManagerScript.instance.currentWave;
		WaveData curWaveData = LevelManagerScript.instance.data.waves[curWave];

        //first line is always Wave ??/?? (?????)
        text.text = "Wave " + (curWave + 1) + "/" + LevelManagerScript.instance.data.waves.Count + " (" + curWaveData.type + ")\n";

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
        
	}
}
