using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// updates information about incoming enemies on the status bar
/// </summary>
public class WaveStatusText : BaseBehaviour
{
    public Text enemyDeckText;

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
        if (LevelManagerScript.instance.endurance)
            enemyDeckText.text = "∞";
        else
            enemyDeckText.text = LevelManagerScript.instance.wavesInDeck + "/" + LevelManagerScript.instance.data.waves.Count;
    }
}