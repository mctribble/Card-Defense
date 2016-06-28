using UnityEngine;
using System.Collections;
using System.IO;

public class LevelSelectScript : MonoBehaviour {

	public string 		levelDir; 		//directory levels are stored in
	public string 		modLevelDir; 		//directory mod levels are stored in
	public GameObject 	buttonPrefab; 	//prefab used to create buttons
	public Color		baseLevelColor;	//color for base game levels
	public Color 		modLevelColor;	//color for modded game levels

	// Use this for initialization
	void Start () {
        //the menu image is disabled to hide it in the editor, but we want it to be visible in game
        //so we turn it on again at runtime
        gameObject.GetComponent<UnityEngine.UI.Image>().enabled = true;

		//base game levels
		DirectoryInfo dir = new DirectoryInfo (Path.Combine (Application.dataPath, levelDir));	//find level folder
		FileInfo[] files = dir.GetFiles ("*.xml");												//get list of .xml files from it
		foreach (FileInfo f in files) {															//for each level file
			GameObject fButton = Instantiate(buttonPrefab);										//create a new button
			fButton.SendMessage("setLevel", f);													//tell it what level it belongs to
			fButton.SendMessage("setColor", baseLevelColor);									//set the button color
			fButton.transform.SetParent(this.transform, false); 								//and add it to the menu without altering scaling settings
		}

		//modded levels
		dir = new DirectoryInfo (Path.Combine (Application.dataPath, modLevelDir));	//find level folder
		files = dir.GetFiles ("*.xml");												//get list of .xml files from it
		foreach (FileInfo f in files) {															//for each level file
			GameObject fButton = Instantiate(buttonPrefab);										//create a new button
			fButton.SendMessage("setLevel", f);													//tell it what level it belongs to
			fButton.SendMessage("setColor", modLevelColor);									//set the button color
			fButton.transform.SetParent(this.transform, false); 								//and add it to the menu without altering scaling settings
		}
	}
	
	// Update is called once per frame
	void Update () {
		//delete self once level is loaded
		if (LevelManagerScript.instance.levelLoaded) {
			Destroy (this.gameObject);
		}
	}
}
