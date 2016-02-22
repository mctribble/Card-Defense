using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.IO;

public class LevelSelectButtonScript : MonoBehaviour {

	public FileInfo levelFile; 	//level file attached to this button
	public Text		buttonText; //text of this button

	// Use this for initialization
	void Awake () {
		buttonText.text = "???";
		levelFile = null;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	//sets the level file associated with this button
	void setLevel (FileInfo file) {
		levelFile = file;	//set file name
		buttonText.text = file.Name;	//set button text
		buttonText.text = buttonText.text.Remove (buttonText.text.Length - 4); //remove the '.xml' from the button text
	}

	//sets the color for this button
	void setColor (Color c) {
		GetComponent<Image> ().color = c;
	}

	//tells the manager to load the level associated with this button
	void loadLevel() {
		LevelManagerScript.instance.SendMessage("loadLevel", levelFile.FullName);
	}
}
