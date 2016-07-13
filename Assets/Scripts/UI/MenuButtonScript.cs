using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

public enum MenuButtonType
{
    level, //button is associated with a level
    deck,
    text
}

public class MenuButtonScript : BaseBehaviour
{
    public Text           buttonText; //text of this button
    public MenuButtonType buttonType; //enum that represents how this menu button is being used

    //each of these may be null based on button type
    public FileInfo levelFile;  //level file attached to this button, if any
    public XMLDeck  xDeck;      //deck attached to this button, if any

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
        buttonType = MenuButtonType.level;
    }

    //sets the deck associated with this button
    private void setDeck(XMLDeck newXDeck)
    {
        xDeck = newXDeck; //set deck
        buttonText.text = xDeck.name;
        buttonType = MenuButtonType.deck;
    }

    //sets the text of the button (note: only use on text buttons, as the other types set the text automatically)
    private void setButtonText(string text)
    {
        buttonText.text = text;
        buttonType = MenuButtonType.text;
    }

    //sets the color for this button
    private void setColor(Color c)
    {
        GetComponent<Image>().color = c;
    }

    //reports back to the parent object in a slightly different way for each button type
    private void buttonClicked()
    {
        switch (buttonType)
        {
            case MenuButtonType.level:
                SendMessageUpwards("LevelSelected", levelFile);
                break;
            case MenuButtonType.deck:
                SendMessageUpwards("DeckSelected", xDeck);
                break;
            case MenuButtonType.text:
                SendMessageUpwards("TextButtonSelected", buttonText.text);
                break;
            default:
                Debug.LogError("MenuButtonScript cant handle this button type!");
                break;
        }
    }
}