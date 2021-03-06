﻿using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// what kind of information is associated with the button
/// </summary>
public enum MenuButtonType
{
    level, 
    deck,
    card,
    text
}

/// <summary>
/// a versatile menu button that sends a message back to its parent when clicked or hovered over
/// </summary>
public class MenuButtonScript : BaseBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public Text           buttonText; //text of this button
    public MenuButtonType buttonType; //enum that represents how this menu button is being used
    public Image          buttonImage;//image of this button

    public AudioClip[] clickSounds; //one of these is played at random when the button is clicked

    //each of these may be null based on button type
    public LevelData      level; //level associated with this button, if any
    public XMLDeck        xDeck; //deck attached to this button, if any
    public PlayerCardData card;  //card type attached to this button, if any

    /// <summary>
    /// the button is set up to correspond to the given LOCAL level file
    /// </summary>
    public void setLevel(FileInfo file)
    {
        //load the levelData
        using (FileStream stream = new FileStream(file.FullName, FileMode.Open))
            level = LevelData.Load(stream, file.Name, file.FullName); 

        buttonText.text = file.Name;       //set button text
        levelButtonText();                 //set text for this button
        buttonType = MenuButtonType.level; //this is now a level button
    }

    /// <summary>
    /// [COROUTINE] the button is set up to correspond to the given REMOTE level file.  The web request does not need to be complete before calling.
    /// while loading, this will be a text button.  Once loading is complete, it becomes a level button.
    /// </summary>
    public IEnumerator setLevel(WWW request)
    {
        //set up placeholder info during loading
        buttonText.text = "Loading...(" + request.url + ")";
        buttonType = MenuButtonType.text;

        //wait for the request to load
        yield return request;

        //show error if there was one
        if (request.error != null)
        {
            Debug.LogError(request.error);
            buttonText.text = request.error;
            buttonText.color = Color.white;
            setColor(Color.red);
            yield break;
        }

        //or, if we were successful, create a new stream and fill it with the contents of the web request:
        using (MemoryStream levelStream = new MemoryStream())     //create the stream
        {
            StreamWriter writer = new StreamWriter(levelStream); //used to write to it
            writer.Write(request.text);                          //write contents of the request
            writer.Flush();                                      //make sure it gets processed
            levelStream.Position = 0;                            //send the stream back to the start

            //figure out the file name
            string fileName = "";
            if (request.url.Contains("file://"))
            {
                //special, simplified handling for access of web player through file:///
                fileName = Path.GetFileName(request.url);
            }
            else
            {
                //usual handling for access of web player through the web or a connection to localhost
                System.Uri address = new System.Uri(request.url); //fetch address from the web request
                fileName = Path.GetFileName(address.LocalPath);   //set button text to the file name (we know it's a file already, or we would have errored earlier)
                fileName = fileName.Replace("%20", " ");          //replace URL special sequence "%20" with the ' ' it is meant to represent.
            }

            //now we can finally setup the level button
            level = LevelData.Load(levelStream, fileName, request.url); //load the levelData
            levelButtonText();                             //set text for this button
            buttonType = MenuButtonType.level;             //this is now a usable level button
        }
    }

    //sets text for a level button
    private void levelButtonText()
    {
        buttonText.alignment = TextAnchor.MiddleLeft;
        buttonText.text = "(" +
                          (level.waves.Count + level.randomWaveCount).ToString("00") + " waves, " +
                          level.towers.Count.ToString("00") + " towers" +
                          ") " + level.fileName.Replace(".xml","");
    }

    /// <summary>
    /// the button is set up to correspond to the given XMLDeck
    /// </summary>
    public void setDeck(XMLDeck newXDeck)
    {
        xDeck = newXDeck; //set deck
        buttonText.text = xDeck.name;
        buttonType = MenuButtonType.deck;
    }

    /// <summary>
    /// the button is set up to correspond to the given PlayerCardData
    /// </summary>
    private void setCard(PlayerCardData newCard)
    {
        card = newCard;
        buttonText.text = newCard.cardName;
        buttonType = MenuButtonType.card;
    }

    /// <summary>
    /// sets up the button by setting the text directly (note: only use on text buttons, as the other types set the text automatically)
    /// </summary>
    public void setButtonText(string text)
    {
        buttonText.text = text;
        buttonType = MenuButtonType.text;
    }

    /// <summary>
    /// sets the color for this button
    /// </summary>
    public void setColor(Color c)
    {
        if (buttonImage.color != c)
            buttonImage.color = c;
    }

    /// <summary>
    /// reports back to the parent object in a slightly different way for each button type
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        //play the random sound with thea udio source attached to the main camera since we dont want UI sounds to overlap and this button may cease to exist before the sound is done
        int soundToPlay = Random.Range(0, clickSounds.Length);
        Camera.main.GetComponent<AudioSource>().clip = clickSounds[soundToPlay];
        Camera.main.GetComponent<AudioSource>().volume = MessageHandlerScript.instance.SFXVolumeSetting;
        Camera.main.GetComponent<AudioSource>().Play();

        switch (buttonType)
        {
            case MenuButtonType.level:
                SendMessageUpwards("LevelSelected", level, SendMessageOptions.DontRequireReceiver);
                break;
            case MenuButtonType.deck:
                SendMessageUpwards("DeckSelected", xDeck, SendMessageOptions.DontRequireReceiver);
                break;
            case MenuButtonType.card:
                SendMessageUpwards("CardSelected", card, SendMessageOptions.DontRequireReceiver);
                break;
            case MenuButtonType.text:
                SendMessageUpwards("TextButtonSelected", buttonText.text, SendMessageOptions.DontRequireReceiver);
                break;
            default:
                Debug.LogError("MenuButtonScript cant handle this button type!");
                break;
        }
    }

    /// <summary>
    /// reports back to the parent object in a slightly different way for each button type
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        switch (buttonType)
        {
            case MenuButtonType.level:
                SendMessageUpwards("LevelHovered", level, SendMessageOptions.DontRequireReceiver);
                break;
            case MenuButtonType.deck:
                SendMessageUpwards("DeckHovered", xDeck, SendMessageOptions.DontRequireReceiver);
                break;
            case MenuButtonType.card:
                SendMessageUpwards("CardHovered", card, SendMessageOptions.DontRequireReceiver);
                break;
            case MenuButtonType.text:
                SendMessageUpwards("TextButtonHovered", buttonText.text, SendMessageOptions.DontRequireReceiver);
                break;
            default:
                Debug.LogError("MenuButtonScript cant handle this button type!");
                break;
        }
    }
}