using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// handles the level select menu
/// </summary>
public class LevelSelectScript : BaseBehaviour
{
    public string       levelDir;     //directory levels are stored in
    public string       modLevelDir;  //directory mod levels are stored in
    public string       thumbnailDir; // where the level thumbnails are stored
    public GameObject   buttonPrefab; //prefab used to create buttons
    public GameObject   menuRoot;     //object to be destroyed when the menu is no longer needed
    public Image        infoImage;    //image object to use for showing level information
    public Text         infoText;     //text object to use for showing level information

    //colors to be used on various types of buttons
    public Color        menuButtonColor;  //misc. menu buttons such as back, quit, etc.
    public Color        baseLevelColor;   //base game levels
    public Color        modLevelColor;    //modded game levels
    public Color        levelDeckColor;   //the level deck
    public Color        moddedDeckColor;  //modded decks
    public Color        premadeDeckColor; //premade decks
    public Color        playerDeckColor;  //player decks

    //list of created menu buttons
    private List<GameObject> menuButtons;

    //temp storage for player menu selections
    private FileInfo chosenLevelFile;

    // Use this for initialization
    private void Start()
    {
        //force menu to be at least as tall as the UI canvas
        //we can't just use Screen.height because that is the height of the window itself and doesnt account for scaling
        //this is especially true of playing in the editor
        float canvasHeight = Screen.height / transform.root.gameObject.GetComponent<Canvas>().transform.localScale.y;
        gameObject.GetComponentInParent<UnityEngine.UI.LayoutElement>().minHeight = canvasHeight;

        //create an empty list to hold the buttons in
        menuButtons = new List<GameObject>();

        //start on a level select prompt
        StartCoroutine(setupLevelButtons());
    }

    /// <summary>
    /// [COROUTINE] creates buttons to be used as a level select
    /// </summary>
    private IEnumerator setupLevelButtons()
    {
        //base game levels
        DirectoryInfo dir = new DirectoryInfo (Path.Combine (Application.dataPath, levelDir));  //find level folder
        FileInfo[] files = dir.GetFiles ("*.xml");                                              //get list of .xml files from it
        foreach (FileInfo f in files)                           //for each level file...
        {
            GameObject fButton = Instantiate(buttonPrefab);     //create a new button
            fButton.SendMessage("setLevel", f);                 //tell it what level it belongs to
            fButton.SendMessage("setColor", baseLevelColor);    //set the button color
            fButton.transform.SetParent(this.transform, false); //add it to the menu without altering scaling settings
            menuButtons.Add(fButton);                           //and add it to the list of buttons
        }

        //modded levels
        dir = new DirectoryInfo(Path.Combine(Application.dataPath, modLevelDir));   //find level folder
        files = dir.GetFiles("*.xml");                                              //get list of .xml files from it
        foreach (FileInfo f in files)                           //for each level file
        {
            GameObject fButton = Instantiate(buttonPrefab);     //create a new button
            fButton.SendMessage("setLevel", f);                 //tell it what level it belongs to
            fButton.SendMessage("setColor", modLevelColor);     //set the button color
            fButton.transform.SetParent(this.transform, false); //add it to the menu without altering scaling settings
            menuButtons.Add(fButton);                           //and add it to the list of buttons
        }

        //throw in a "quit" button also to exit the game with, if we are not in the editor or a web build (both of which ignore Application.Quit() anyway)
        if ((Application.isEditor == false) && (Application.isWebPlayer == false))
        {
            GameObject qButton = Instantiate(buttonPrefab);     //create a new button
            qButton.SendMessage("setButtonText", "Quit");       //set the text
            qButton.SendMessage("setColor", menuButtonColor);   //and the color
            qButton.transform.SetParent(this.transform, false); //and it to the menu
            menuButtons.Add(qButton);                           //and add it to the list of buttons
        }

        yield return null; //give the scrollRect a frame to catch up
        gameObject.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1; //scroll the menu to the top after adding all these buttons
    }

    /// <summary>
    /// [COROUTINE] creates buttons to be used as a deck select
    /// </summary>
    private IEnumerator setupDeckButtons()
    {
        //create a button for using the default level deck
        GameObject ldButton = Instantiate(buttonPrefab);             //create a new button
        ldButton.SendMessage("setButtonText", "Default Level Deck"); //set the text
        ldButton.SendMessage("setColor", levelDeckColor);            //and the color
        ldButton.transform.SetParent(this.transform, false);         //add it to the menu
        menuButtons.Add(ldButton);                                   //and add it to the list of buttons
        
        //buttons for all the player decks
        foreach (XMLDeck pd in DeckManagerScript.instance.playerDecks.decks)
        {
            GameObject pdButton = Instantiate(buttonPrefab);     //create a new button
            pdButton.SendMessage("setDeck", pd);                 //set the deck

            //and the color (varies based on modded/not modded
            if (pd.isModded())
                pdButton.SendMessage("setColor", moddedDeckColor);
            else
                pdButton.SendMessage("setColor", playerDeckColor);   


            pdButton.transform.SetParent(this.transform, false); //and add it to the menu
            menuButtons.Add(pdButton);                           //and add it to the list of buttons
        }

        //buttons for all the premade decks
        foreach (XMLDeck pd in DeckManagerScript.instance.premadeDecks.decks)
        {
            GameObject pdButton = Instantiate(buttonPrefab);     //create a new button
            pdButton.SendMessage("setDeck", pd);                 //set the deck
            pdButton.SendMessage("setColor", premadeDeckColor);  //and the color
            pdButton.transform.SetParent(this.transform, false); //and add it to the menu
            menuButtons.Add(pdButton);                           //and add it to the list of buttons
        }

        //a button to open the editor...
        GameObject eButton = Instantiate(buttonPrefab);      //create a new button
        eButton.SendMessage("setButtonText", "Deck Editor"); //set the text
        eButton.SendMessage("setColor", menuButtonColor);    //and the color
        eButton.transform.SetParent(this.transform, false);  //and add it to the menu for returning to the level select
        menuButtons.Add(eButton);                            //and add it to the list of buttons

        //and a back button 
        GameObject backButton = Instantiate(buttonPrefab);     //create a new button
        backButton.SendMessage("setButtonText", "Back");       //set the text
        backButton.SendMessage("setColor", menuButtonColor);   //and the color
        backButton.transform.SetParent(this.transform, false); //and add it to the menu for returning to the level select
        menuButtons.Add(backButton);                           //and add it to the list of buttons

        yield return null; //give the scrollRect a frame to catch up
        gameObject.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1; //scroll the menu to the top after adding all these buttons
    }

    /// <summary>
    /// destroys all the menu buttons so a different menu can be shown
    /// </summary>
    private void destroyButtons()
    {
        foreach (GameObject button in menuButtons)
            Destroy(button);
        menuButtons.Clear();
    }

    /// <summary>
    /// callback from level buttons.  Selects the given level and prompts for a deck
    /// </summary>
    private void LevelSelected(FileInfo levelFile)
    {
        chosenLevelFile = levelFile;           //save the chosen level
        destroyButtons();                      //get rid of the level menu
        StartCoroutine(setupDeckButtons());    //present the deck menu
        infoImage.gameObject.SetActive(false); //hide the info image since the deck menu doesnt need it
    }

    /// <summary>
    /// [COROUTINE] called when a level is hovered over.  Shows information about it in the text box
    /// </summary>
    /// <param name="levelFile"></param>
    private IEnumerator LevelHovered(FileInfo levelFile)
    {
        LevelData data = LevelData.Load(Path.Combine(Application.dataPath, levelFile.FullName));

        //start with the level file name, but replace the file extension with a newline
        infoText.text = levelFile.Name.Replace(levelFile.Extension, ":\n"); ;

        //these values we can simply use directly
        infoText.text +=
            data.description + '\n' + 
            '\n' +
            data.waves.Count + " predetermined waves\n" +
            data.randomWaveCount + " random waves\n" +
            "Start with " + data.towers.Count + " towers on the map\n" +
            data.spawners.Count + " spawn locations\n";

        //this intimidating-looking function call simply counts how many path segments there are that end at a spot that no segment begins
        //such segments are at the "end of the line", and as such are the points the player must defend
        infoText.text += data.pathSegments.FindAll(s => (data.pathSegments.Exists(ss => ss.startPos == s.endPos) == false)).Count + " points to defend";

        //yes, I know its awkward, but we're loading the level thumbnail with WWW.
        string thumbnailPath = "file:///" + Path.Combine(Application.dataPath, thumbnailDir);
        WWW www = new WWW( Path.Combine( thumbnailPath, levelFile.Name.Replace(levelFile.Extension, ".png") ) );
        yield return www;

        if (www.error == null)
            infoImage.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
        else
            infoImage.sprite = Resources.Load<Sprite>("Sprites/Error");

        infoImage.type = Image.Type.Sliced;
    }

    /// <summary>
    /// callback from deck buttons.  Selects the given deck and loads the level
    /// </summary>
    private void DeckSelected(XMLDeck deck)
    {
        DeckManagerScript.instance.SendMessage("SetDeck", deck); //send deck manager the chosen deck
        DeckManagerScript.instance.Shuffle(); //always shuffle the deck, regardless of what the level file says, if the deck did not come from the level file
        LevelManagerScript.instance.SendMessage("loadLevel", chosenLevelFile.FullName); //load the previously chosen level
        Destroy(menuRoot); //we are done with this menu.  Destroy it.
    }

    /// <summary>
    /// callback from deck buttons.  Shows info on the given deck
    /// </summary>
   private void DeckHovered(XMLDeck deck)
   {
        infoText.text = deck.name + ":\n";
        foreach (XMLDeckEntry entry in deck.contents)
            infoText.text += entry.ToString() + '\n';
   }

    /// <summary>
    /// callback from text buttons.  
    /// </summary>
    private void TextButtonSelected(string buttonText)
    {
        switch(buttonText)
        {
            case "Deck Editor":
                //player wants to load the deck editor
                SceneManager.LoadScene("Deck Editor");
                break;
            case "Default Level Deck":
                //player wants to use the predefined deck for this level.  Load the level immediately and then let the level manager load the deck for us when it sees we haven't.
                LevelManagerScript.instance.SendMessage("loadLevel", chosenLevelFile.FullName);
                Destroy(menuRoot); //we are done with this menu.  Destroy it.
                break;
            case "Quit":
                //player wants to quit.
                Application.Quit();
                break;
            case "Back":
                //player wants to go back to beginning
                chosenLevelFile = null;
                destroyButtons();
                StartCoroutine(setupLevelButtons());
                infoImage.gameObject.SetActive(true);
                break;
            default:
                MessageHandlerScript.Error("LevelSelectScript doesnt know how to handle this button!");
                break;
        }
    }

    /// <summary>
    /// callback from text buttons. 
    /// </summary>
    private void TextButtonHovered(string buttonText)
    {
        switch(buttonText)
        {
            case "Default Level Deck":
                //treat the default level deck button as ifit were a reference to the level deck
                LevelData data = LevelData.Load(Path.Combine(Application.dataPath, chosenLevelFile.FullName));
                if ((data.premadeDeckName == null) || (data.premadeDeckName == ""))
                    DeckHovered(data.levelDeck);
                else
                    DeckHovered(DeckManagerScript.instance.premadeDecks.getDeckByName(data.premadeDeckName));
                break;

            default:
                //in all other cases, just blank out the text
                infoText.text = "";
                break;
        }
    }
}