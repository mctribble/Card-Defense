using UnityEngine;
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

		int curWave = LevelManagerScript.instance.currentWave;
		WaveData curWaveData = LevelManagerScript.instance.data.waves[curWave];
		text.text = "Wave: " + (curWave + 1) + "/" + LevelManagerScript.instance.data.waves.Count + " Next: " + curWaveData.type + "\n" + 
			"  Count: " + (Mathf.RoundToInt((float)curWaveData.budget / (float)curWaveData.getEnemyData ().spawnCost) - LevelManagerScript.instance.deadThisWave) +
            " over " + curWaveData.time + " seconds";
	}
}
