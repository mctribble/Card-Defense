using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

public class LevelSelectButtonScript : BaseBehaviour
{
    public FileInfo levelFile;  //level file attached to this button
    public Text     buttonText; //text of this button

    // Use this for initialization
    private void Awake()
    {
        buttonText.text = "???";
        levelFile = null;
    }

    // Update is called once per frame
    private void Update()
    {
    }

    //sets the level file associated with this button
    private void setLevel(FileInfo file)
    {
        levelFile = file;   //set file name
        buttonText.text = file.Name;    //set button text
        buttonText.text = buttonText.text.Remove(buttonText.text.Length - 4); //remove the '.xml' from the button text
    }

    //sets the color for this button
    private void setColor(Color c)
    {
        GetComponent<Image>().color = c;
    }

    //tells the manager to load the level associated with this button
    private void loadLevel()
    {
        LevelManagerScript.instance.SendMessage("loadLevel", levelFile.FullName);
    }
}