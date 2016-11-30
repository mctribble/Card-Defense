using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;

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
    private List<MenuButtonScript> menuButtons;

    //temp storage for player menu selections
    private LevelData chosenLevel;

    // Use this for initialization
    private void Start()
    {
        //force menu to be at least as tall as the UI canvas
        //we can't just use Screen.height because that is the height of the window itself and doesnt account for scaling
        //this is especially true of playing in the editor
        float canvasHeight = Screen.height / transform.root.gameObject.GetComponent<Canvas>().transform.localScale.y;
        gameObject.GetComponentInParent<UnityEngine.UI.LayoutElement>().minHeight = canvasHeight;

        //create an empty list to hold the buttons in
        menuButtons = new List<MenuButtonScript>();

        //start on a level select prompt
        StartCoroutine(setupLevelButtons());
    }

    /// <summary>
    /// [COROUTINE] creates buttons to be used as a level select by calling one of the other forms of this function.  
    /// This version works regardless of platform.
    /// </summary>
    private IEnumerator setupLevelButtons()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            //this is a web build.  Run that version
            yield return StartCoroutine(setupLevelButtonsWeb());
        }
        else
        {
            //this is a PC build.  Run the appropriate coroutine...
            yield return StartCoroutine(setupLevelButtonsPC());
        
            //and also create/update the manifest for web builds
            updateLevelManifest();
        }
    }

    /// <summary>
    /// [COROUTINE] creates buttons to be used as a level select.  This version is for PC builds
    /// </summary>
    private IEnumerator setupLevelButtonsPC()
    {
        //base game levels
        DirectoryInfo dir = new DirectoryInfo (Path.Combine (Application.streamingAssetsPath, levelDir));  //find level folder
        FileInfo[] files = dir.GetFiles ("*.xml");                                              //get list of .xml files from it
        foreach (FileInfo f in files)                           //for each level file...
        {
            MenuButtonScript fButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>();     //create a new button
            fButton.SendMessage("setLevel", f);                 //tell it what level it belongs to
            fButton.SendMessage("setColor", baseLevelColor);    //set the button color
            fButton.transform.SetParent(this.transform, false); //add it to the menu without altering scaling settings
            menuButtons.Add(fButton);                           //and add it to the list of buttons
        }

        //modded levels
        dir = new DirectoryInfo(Path.Combine(Application.streamingAssetsPath, modLevelDir));   //find level folder
        files = dir.GetFiles("*.xml");                                              //get list of .xml files from it
        foreach (FileInfo f in files)                           //for each level file
        {
            MenuButtonScript fButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>();     //create a new button
            fButton.SendMessage("setLevel", f);                 //tell it what level it belongs to
            fButton.SendMessage("setColor", modLevelColor);     //set the button color
            fButton.transform.SetParent(this.transform, false); //add it to the menu without altering scaling settings
            menuButtons.Add(fButton);                           //and add it to the list of buttons
        }

        //also have a button to choose a random level with
        MenuButtonScript rButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>();       //create a new button
        rButton.SendMessage("setButtonText", "Random Level"); //set the text
        rButton.SendMessage("setColor", menuButtonColor);     //and the color
        rButton.transform.SetParent(this.transform, false);   //and it to the menu
        menuButtons.Add(rButton);                             //and add it to the list of buttons

        //throw in a "quit" button also to exit the game with, if we are not in the editor or a web build (both of which ignore Application.Quit() anyway)
        if ((Application.isEditor == false) && (Application.platform == RuntimePlatform.WebGLPlayer == false))
        {
            MenuButtonScript qButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>();     //create a new button
            qButton.SendMessage("setButtonText", "Quit");       //set the text
            qButton.SendMessage("setColor", menuButtonColor);   //and the color
            qButton.transform.SetParent(this.transform, false); //and it to the menu
            menuButtons.Add(qButton);                           //and add it to the list of buttons
        }

        yield return null; //give the scrollRect a frame to catch up
        gameObject.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1; //scroll the menu to the top after adding all these buttons
    }

    /// <summary>
    /// [COROUTINE] creates buttons to be used as a level select.  This version is for web builds
    /// </summary>
    private IEnumerator setupLevelButtonsWeb()
    {
        //fetch the manifest
        string manifestPath = Application.streamingAssetsPath + '/' + levelDir + "levelManifest.txt";
        //while (manifestPath.StartsWith("/")) manifestPath = manifestPath.Substring(1); //remove any leading /'s
        Debug.Log("Looking for level manifest at " + manifestPath);
        WWW request = new WWW(manifestPath);
        yield return request;

        //error check
        if (request.error != null)
            Debug.LogError("error loading manifest: " + request.error);

        //read the manifest and create a button for each level
        string[] manifestLines = request.text.Split('\n');
        foreach(string line in manifestLines)
        {
            string levelName = line.Trim(); //read the next line on the manifest and trim off the whitespace

            //skip if the name we are left with is blank
            if (levelName == "")
                continue;

            MenuButtonScript lButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
            string levelPath = Application.streamingAssetsPath + '/' + levelDir + levelName;       //path of the level file
            Debug.Log("requesting level " + levelPath);                                            //log the request
            StartCoroutine(lButton.setLevel(new WWW(levelPath)));                                  //set the level by sending the button request for the level file
            lButton.setColor(baseLevelColor);                                                      //set the button color
            lButton.transform.SetParent(this.transform, false);                                    //add it to the menu
            menuButtons.Add(lButton);                                                              //add it to the list of buttons
        }

        //also have a button to choose a random level with
        MenuButtonScript rButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
        rButton.SendMessage("setButtonText", "Random Level"); //set the text
        rButton.SendMessage("setColor", menuButtonColor);     //and the color
        rButton.transform.SetParent(this.transform, false);   //and it to the menu
        menuButtons.Add(rButton);                             //and add it to the list of buttons

        //throw in a "quit" button also to exit the game with, if we are not in the editor or a web build (both of which ignore Application.Quit() anyway)
        if ((Application.isEditor == false) && (Application.platform == RuntimePlatform.WebGLPlayer == false))
        {
            MenuButtonScript qButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
            qButton.SendMessage("setButtonText", "Quit");       //set the text
            qButton.SendMessage("setColor", menuButtonColor);   //and the color
            qButton.transform.SetParent(this.transform, false); //and it to the menu
            menuButtons.Add(qButton);                           //and add it to the list of buttons
        }

        yield return null; //give the scrollRect a frame to catch up
        gameObject.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1; //scroll the menu to the top after adding all these buttons
    }

    /// <summary>
    /// updates (or creates) the file levelManifest.txt in the level directory that contains a listing of all available level files there
    /// this is necessarry for web builds, which have no equivalent to .getFiles().
    /// </summary>
    [Show] private void updateLevelManifest()
    {
        //find levels
        DirectoryInfo dir = new DirectoryInfo (Path.Combine (Application.streamingAssetsPath, levelDir));  //find level folder
        FileInfo[] files = dir.GetFiles ("*.xml");                                              //get list of .xml files from it

        //where the manifest file should be
        string manifestPath = Path.Combine(dir.FullName, "levelManifest.txt");

        //delete manifest if it already exists
        if (File.Exists(manifestPath))
            File.Delete(manifestPath);

        //create a file for the manifest
        using (StreamWriter manifest = new StreamWriter(manifestPath))
        {
            //for each level file, create a line in the manifest
            foreach (FileInfo file in files)
                manifest.WriteLine(file.Name);
        }   
    }

    /// <summary>
    /// [COROUTINE] creates buttons to be used as a deck select
    /// </summary>
    private IEnumerator setupDeckButtons()
    {
        //create a button for using the default level deck
        MenuButtonScript ldButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
        ldButton.SendMessage("setButtonText", "Default Level Deck"); //set the text
        ldButton.SendMessage("setColor", levelDeckColor);            //and the color
        ldButton.transform.SetParent(this.transform, false);         //add it to the menu
        menuButtons.Add(ldButton);                                   //and add it to the list of buttons
        
        //buttons for all the player decks
        foreach (XMLDeck pd in DeckManagerScript.instance.playerDecks.decks)
        {
            MenuButtonScript pdButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
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
            MenuButtonScript pdButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
            pdButton.SendMessage("setDeck", pd);                 //set the deck
            pdButton.SendMessage("setColor", premadeDeckColor);  //and the color
            pdButton.transform.SetParent(this.transform, false); //and add it to the menu
            menuButtons.Add(pdButton);                           //and add it to the list of buttons
        }

        //random deck buttons...
        MenuButtonScript rButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
        rButton.SendMessage("setButtonText", "random existing deck"); //set the text
        rButton.SendMessage("setColor", menuButtonColor);             //and the color
        rButton.transform.SetParent(this.transform, false);           //and add it to the menu for returning to the level select
        menuButtons.Add(rButton);                                     //and add it to the list of buttons

        MenuButtonScript vrButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
        vrButton.SendMessage("setButtonText", "create random deck");  //set the text
        vrButton.SendMessage("setColor", menuButtonColor);            //and the color
        vrButton.transform.SetParent(this.transform, false);          //and add it to the menu for returning to the level select
        menuButtons.Add(vrButton);                                    //and add it to the list of buttons

        //a button to open the editor...
        MenuButtonScript eButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
        eButton.SendMessage("setButtonText", "Deck Editor"); //set the text
        eButton.SendMessage("setColor", menuButtonColor);    //and the color
        eButton.transform.SetParent(this.transform, false);  //and add it to the menu for returning to the level select
        menuButtons.Add(eButton);                            //and add it to the list of buttons

        //and a back button 
        MenuButtonScript backButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
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
        foreach (MenuButtonScript button in menuButtons)
            Destroy(button.gameObject);
        menuButtons.Clear();
    }

    /// <summary>
    /// callback from level buttons.  Selects the given level and prompts for a deck
    /// </summary>
    private void LevelSelected(LevelData data)
    {
        chosenLevel = data;                    //save the chosen level
        destroyButtons();                      //get rid of the level menu
        StartCoroutine(setupDeckButtons());    //present the deck menu
        infoImage.gameObject.SetActive(false); //hide the info image since the deck menu doesnt need it
    }

    /// <summary>
    /// [COROUTINE] called when a level is hovered over.  Shows information about it in the text box
    /// </summary>
    /// <param name="levelFile"></param>
    private IEnumerator LevelHovered(LevelData data)
    {
        //start with the level file name, but replace the file extension with a newline
        infoText.text = data.fileName.Replace(".xml", ":\n");

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

        //yes, I know its awkward, but we're loading the level thumbnail with WWW, even on a PC build
        string thumbnailPath = "";
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            thumbnailPath = "file:///";

        thumbnailPath += Path.Combine(Application.streamingAssetsPath, thumbnailDir);
        WWW www = new WWW( Path.Combine( thumbnailPath, data.fileName.Replace(".xml", ".png") ) ); //thumbnail has the same name as the level, but is a .png
        yield return www;

        if (www.error == null)
        {
            infoImage.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            infoImage.sprite = Resources.Load<Sprite>("Sprites/Error");
            Debug.LogError("could not load level thumbnail (" + www.error + ")");
        }

        infoImage.type = Image.Type.Sliced;
    }

    /// <summary>
    /// callback from deck buttons.  Selects the given deck and loads the level
    /// </summary>
    private void DeckSelected(XMLDeck deck)
    {
        DeckManagerScript.instance.SendMessage("SetDeck", deck); //send deck manager the chosen deck
        DeckManagerScript.instance.Shuffle(); //always shuffle the deck, regardless of what the level file says, if the deck did not come from the level file
        LevelManagerScript.instance.SendMessage("loadLevel", chosenLevel); //load the previously chosen level
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
            case "Random Level":
                //find all level buttons currently available
                MenuButtonScript[] levelButtons = menuButtons.Where(mb => mb.buttonType == MenuButtonType.level).ToArray();

                //choose one of them at random and treat it as if that button was clicked on
                int buttonIndex = Random.Range(0,levelButtons.Length);
                LevelSelected(levelButtons[buttonIndex].level);

                break;

            case "random existing deck":
                //chooses a deck at random from the player and behave as if that button was clicked on
                IEnumerable<XMLDeck> deckOptions = DeckManagerScript.instance.playerDecks.decks.Where(xd => xd.isModded() == false); //choose from un-modded player decks...
                deckOptions.Concat( DeckManagerScript.instance.premadeDecks.decks ); //and all premade decks
                int deckIndex = Random.Range(0, deckOptions.Count());
                DeckSelected(deckOptions.ElementAt(deckIndex));
                break;

            case "create random deck":
                //randomly generates a deck for the player to use
                DeckSelected(DeckManagerScript.instance.generateRandomDeck());
                break;

            case "Deck Editor":
                //player wants to load the deck editor
                SceneManager.LoadScene("Deck Editor");
                break;

            case "Default Level Deck":
                //player wants to use the predefined deck for this level.  Load the level immediately and then let the level manager load the deck for us when it sees we haven't.
                LevelManagerScript.instance.SendMessage("loadLevel", chosenLevel);
                Destroy(menuRoot); //we are done with this menu.  Destroy it.
                break;

            case "Quit":
                //player wants to quit.
                Application.Quit();
                break;

            case "Back":
                //player wants to go back to beginning
                chosenLevel = null;
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
    private IEnumerator TextButtonHovered(string buttonText)
    {
        switch(buttonText)
        {
            case "Random Level":
                //shows a special image and description to explain what this button does
                
                //yes, I know its awkward, but we're loading the random level thumbnail with WWW, since this is the Unity method for runtime asset loading
                string thumbnailPath = "file:///" + Path.Combine(Application.streamingAssetsPath, thumbnailDir); //path where the file is
                WWW www = new WWW( Path.Combine(thumbnailPath, "Random_Level.png") ); //file name
                yield return www; //wait for it to load
                infoImage.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f)); //create a sprite with it and set it on the image

                infoText.text = "Chooses a level at random!";

                break;

            case "Random Deck(from list)":
                //show text to explain the button
                infoText.text = "Chooses randomly from premade decks and those you have designed.";
                break;

            case "create random deck":
                //show text to explain the button
                infoText.text = "randomly chooses cards to create a brand new deck.  Warning: deck quality will vary wildly!";
                break;

            case "Default Level Deck":
                //treat the default level deck button as if it were a reference to the level deck
                if ((chosenLevel.premadeDeckName == null) || (chosenLevel.premadeDeckName == ""))
                    DeckHovered(chosenLevel.levelDeck);
                else
                    DeckHovered(DeckManagerScript.instance.premadeDecks.getDeckByName(chosenLevel.premadeDeckName));
                break;

            case "Deck Editor":
                //show text to explain the button
                infoText.text = "Go to the deck editor to create or edit a deck of your own!";
                break;

            case "Back":
                //show text to explain the button
                infoText.text = "Back to level select";
                break;

            default:
                //in all other cases, just blank out the text
                infoText.text = "";
                break;
        }

        yield break;
    }
}