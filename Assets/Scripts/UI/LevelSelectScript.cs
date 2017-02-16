using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Analytics;
using System;

/// <summary>
/// handles the level select menu
/// </summary>
public class LevelSelectScript : BaseBehaviour
{
    public string       levelDir;         //directory levels are stored in
    public string       modLevelDir;      //directory mod levels are stored in
    public string       thumbnailDir;     // where the level thumbnails are stored
    public GameObject   buttonPrefab;     //prefab used to create buttons
    public GameObject   menuHeaderPrefab; //prefab used to create menu headers
    public GameObject   menuTextPrefab;   //prefab used to create larger blocks of menu text
    public GameObject   menuRoot;         //object to be destroyed when the menu is no longer needed
    public Image        infoImage;        //image object to use for showing level information
    public Text         infoText;         //text object to use for showing level information
    public Text         menuText;         //text object to use for showing menu information

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
    private List<MenuTextScript> menuHeaders;

    //temp storage for player menu selections
    private LevelData chosenLevel;

    // Use this for initialization
    private void Start()
    {
        //force menu to be at least as tall as the UI canvas
        //we can't just use Screen.height because that is the height of the window itself and doesnt account for scaling
        //this is especially true of playing in the editor
        float canvasHeight = Screen.height / transform.root.gameObject.GetComponent<Canvas>().transform.localScale.y;
        gameObject.GetComponentInParent<UnityEngine.UI.LayoutElement>().minHeight = canvasHeight - 50; //-50 is to make room for the menu label

        //create empty lists to hold the menu items in
        menuButtons = new List<MenuButtonScript>();
        menuHeaders = new List<MenuTextScript>();

        //test if data persistence is working in webGL builds
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            try
            {
                if (PlayerPrefs.HasKey("saveTest"))
                    PlayerPrefs.DeleteKey("saveTest");

                int test = UnityEngine.Random.Range(0,500);
                PlayerPrefs.SetInt("saveTest", test);
                if (PlayerPrefs.HasKey("saveTest") == false)
                    MessageHandlerScript.ShowAndYield("Data saving doesn't seem to be working.  If you make a deck it may disappear when you reload the page.");
                else
                    if (PlayerPrefs.GetInt("saveTest") != test)
                    MessageHandlerScript.ShowAndYield("Data saving doesn't seem to be working.  If you make a deck it may disappear when you reload the page.");
            }
            catch (System.Exception e)
            {
                MessageHandlerScript.ShowAndYield("Data saving doesn't seem to be working.  If you make a deck it may disappear when you reload the page. \n(" + e.Message + ")");
            }
        }

        //start on a level select prompt
        StartCoroutine(setupLevelButtons());
    }

    /// <summary>
    /// [COROUTINE] creates buttons to be used as a level select by calling one of the other forms of this function.  
    /// This version works regardless of platform.
    /// </summary>
    private IEnumerator setupLevelButtons()
    {
        //random level button
        MenuButtonScript rButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
        rButton.setButtonText("Random Level");                                                 //set the text
        rButton.setColor(menuButtonColor);                                                     //and the color
        rButton.transform.SetParent(this.transform, false);                                    //add it to the menu
        menuButtons.Add(rButton);                                                              //and add it to the list of buttons

        //deck editor button
        MenuButtonScript eButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
        eButton.setButtonText("Deck Editor");                                                  //set the text
        eButton.setColor(menuButtonColor);                                                     //and the color
        eButton.transform.SetParent(this.transform, false);                                    //and add it to the menu for returning to the level select
        menuButtons.Add(eButton);                                                              //and add it to the list of buttons

        //help button
        MenuButtonScript hButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>();
        hButton.setButtonText("Help");
        hButton.setColor(menuButtonColor);
        hButton.transform.SetParent(this.transform, false);
        menuButtons.Add(hButton);

        //quit button, if we are not in the editor or a web build (both of which ignore Application.Quit() anyway)
        if ((Application.isEditor == false) && (Application.platform == RuntimePlatform.WebGLPlayer == false))
        {
            MenuButtonScript qButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
            qButton.setButtonText("Quit");                                                         //set the text
            qButton.setColor(menuButtonColor);                                                     //and the color
            qButton.transform.SetParent(this.transform, false);                                    //add it to the menu
            menuButtons.Add(qButton);                                                              //and add it to the list of buttons
        }

        //platform-specific logic to create the buttons
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            yield return StartCoroutine(setupLevelButtonsWeb()); //this is a web build.  This performs many web requests and updates menuText with the status as it goes.  It may take a while.
        }
        else
        {
            setupLevelButtonsPC(); //this is a PC build.  Makes buttons by loading files from disk and returns immediately
            updateLevelManifest(); //and also create/update the manifest for web builds
        }

        //level buttons are ready.  add them by difficulty
        //the buttons we need to add are everything but the menu buttons we made at the start
        List<MenuButtonScript> levelButtonsToAdd = menuButtons.SkipWhile(mbs => mbs.buttonType == MenuButtonType.text).ToList();

        //do categories that we know will exist
        string[] knownDifficulties = { "tutorials", "easy", "medium", "hard" };
        foreach (string diff in knownDifficulties)
        {
            MenuTextScript header = Instantiate(menuHeaderPrefab).GetComponent<MenuTextScript>(); //create a new header
            header.text = diff.ToUpper();                                                             //label it
            header.transform.SetParent(this.transform, false);                                        //add it to the menu
            menuHeaders.Add(header);                                                                  //add it to the list

            //add the level buttons
            foreach (MenuButtonScript levelButton in levelButtonsToAdd.Where(mbs => mbs.level.difficulty.ToUpper() == diff.ToUpper()))
                levelButton.transform.SetParent(this.transform, false);

            //take them out of the list
            levelButtonsToAdd.RemoveAll(mbs => mbs.level.difficulty.ToUpper() == diff.ToUpper());
        }

        //make new categories for everything else
        while (levelButtonsToAdd.Count > 0)
        {
            string diff = levelButtonsToAdd[0].level.difficulty;

            MenuTextScript header = Instantiate(menuHeaderPrefab).GetComponent<MenuTextScript>(); //create a new header
            header.text = diff.ToUpper();                                                             //label it
            header.transform.SetParent(this.transform, false);                                        //add it to the menu
            menuHeaders.Add(header);                                                                  //add it to the list

            //add the level buttons
            foreach (MenuButtonScript levelButton in levelButtonsToAdd.Where(mbs => mbs.level.difficulty.ToUpper() == diff.ToUpper()))
                levelButton.transform.SetParent(this.transform, false);

            //take them out of the list
            levelButtonsToAdd.RemoveAll(mbs => mbs.level.difficulty.ToUpper() == diff.ToUpper());
        }

        yield return null; //give the scrollRect a frame to catch up
        gameObject.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1; //scroll the menu to the top after adding all these buttons

        menuText.text = "Select a level. (hover for info)";
    }

    /// <summary>
    /// creates buttons to be used as a level select.  This version is for PC builds
    /// </summary>
    private void setupLevelButtonsPC()
    {
        Dictionary<FileInfo, bool> fileDict = new Dictionary<FileInfo, bool>();

        //look for base game level files
        DirectoryInfo dir = new DirectoryInfo (Path.Combine (Application.streamingAssetsPath, levelDir));  //find level folder
        FileInfo[] files = dir.GetFiles ("*.xml");                                                         //get list of .xml files from it
        foreach (FileInfo f in files)
            fileDict.Add(f, false);

        //look for modded level files
        dir = new DirectoryInfo(Path.Combine(Application.streamingAssetsPath, modLevelDir)); //find level folder
        files = dir.GetFiles("*.xml");                                                       //get list of .xml files from it
        foreach (FileInfo f in files)
            fileDict.Add(f, true);

        //create buttons for each
        foreach (KeyValuePair<FileInfo, bool> entry in fileDict) //for each level file
        {
            MenuButtonScript fButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
            fButton.setLevel(entry.Key); //tell it what level it belongs to

            //set the button color
            if (entry.Value)
                fButton.setColor(modLevelColor);
            else       
                fButton.setColor(baseLevelColor);

            menuButtons.Add(fButton); //and add it to the list of buttons
        }
    }

    /// <summary>
    /// [COROUTINE] creates buttons to be used as a level select.  This version is for web builds
    /// </summary>
    private IEnumerator setupLevelButtonsWeb()
    {
        menuText.text = "Loading...";

        //fetch the manifest
        string manifestPath = Application.streamingAssetsPath + '/' + levelDir + "levelManifest.txt";
        Debug.Log("Looking for level manifest at " + manifestPath);
        WWW request = new WWW(manifestPath);
        yield return request;

        //error check
        if (request.error != null)
        {
            Debug.LogError("error loading manifest: " + request.error);
            yield return StartCoroutine(MessageHandlerScript.ShowAndYield("error loading manifest: " + request.error));
            menuText.text = "Error.  Please refresh and try again.";
            yield break;
        }

        //read the manifest and create a button for each level
        int requestCount = 0; //number of level requests made
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
            StartCoroutine(lButton.setLevel(new WWW(levelPath)));                                  //set the level by sending the button a request for the level file
            requestCount++;                                                                        //keep count of how many requests we made for later
            lButton.setColor(baseLevelColor);                                                      //set the button color
            menuButtons.Add(lButton);                                                              //add it to the list of buttons
        }

        //wait for all the levels to be loaded
        int loadedCount = 0;
        int loopCount = 0;
        while (loadedCount < requestCount)
        {
            if (loopCount == 300)
            {
                Debug.LogWarning("Loading is taking a long time: ");
                foreach (MenuButtonScript button in menuButtons.Where(mbs => mbs.buttonType == MenuButtonType.text && mbs.buttonText.text.StartsWith("Loading")))
                    Debug.LogWarning(button.buttonText.text);
                Debug.LogWarning("Forcing continue");
                break;
            }

            loadedCount = menuButtons.Count(mbs => mbs.buttonType == MenuButtonType.level); //the buttons only become level buttons once they are actually loaded, so this works
            menuText.text = "Loading: " + loadedCount + "/" + requestCount;
            yield return new WaitForSeconds(0.1f);
            loopCount++;
        }
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
        menuText.text = "Choose a deck. (hover for info)";

        //create a button for using the Suggested Deck...
        MenuButtonScript ldButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
        ldButton.SendMessage("setButtonText", "Suggested Deck");                                //set the text
        ldButton.SendMessage("setColor", levelDeckColor);                                       //and the color
        ldButton.transform.SetParent(this.transform, false);                                    //add it to the menu
        menuButtons.Add(ldButton);                                                              //and add it to the list of buttons

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

        //and a back button 
        MenuButtonScript backButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
        backButton.SendMessage("setButtonText", "Back");       //set the text
        backButton.SendMessage("setColor", menuButtonColor);   //and the color
        backButton.transform.SetParent(this.transform, false); //and add it to the menu for returning to the level select
        menuButtons.Add(backButton);                           //and add it to the list of buttons

        //buttons for all the player decks
        if (DeckManagerScript.instance.playerDecks.decks.Count > 0)
        {
            MenuTextScript header = Instantiate(menuHeaderPrefab).GetComponent<MenuTextScript>();
            header.text = "Your Decks";
            header.transform.SetParent(this.transform);
            menuHeaders.Add(header);
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
        }

        //buttons for all the premade decks
        if (DeckManagerScript.instance.premadeDecks.decks.Count > 0)
        {
            MenuTextScript header = Instantiate(menuHeaderPrefab).GetComponent<MenuTextScript>();
            header.text = "Premade Decks";
            header.transform.SetParent(this.transform);
            menuHeaders.Add(header);
            foreach (XMLDeck pd in DeckManagerScript.instance.premadeDecks.decks)
            {
                MenuButtonScript pdButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>(); //create a new button
                pdButton.SendMessage("setDeck", pd);                 //set the deck
                pdButton.SendMessage("setColor", premadeDeckColor);  //and the color
                pdButton.transform.SetParent(this.transform, false); //and add it to the menu
                menuButtons.Add(pdButton);                           //and add it to the list of buttons
            }
        }

        yield return null; //give the scrollRect a frame to catch up
        gameObject.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1; //scroll the menu to the top after adding all these buttons
    }

    /// <summary>
    /// destroys all the menu buttons so a different menu can be shown
    /// </summary>
    private void clearMenu()
    {
        foreach (MenuButtonScript button in menuButtons)
            Destroy(button.gameObject);

        menuButtons.Clear();

        foreach (MenuTextScript header in menuHeaders)
            Destroy(header.gameObject);

        menuHeaders.Clear();
    }

    /// <summary>
    /// callback from level buttons.  Selects the given level and prompts for a deck
    /// </summary>
    private void LevelSelected(LevelData data)
    {
        chosenLevel = data;                    //save the chosen level
        clearMenu();                           //get rid of the level menu
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
        infoText.text += " Par Score: " + ScoreManagerScript.instance.parScore(data) + '\n';
        infoText.text += "High Score: " + ScoreManagerScript.instance.playerScores.getTopScoreForLevel(data.fileName) + '\n';

        //these values we can simply use directly
        infoText.text +=
            "difficulty: " + data.difficulty + '\n' +
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
            Debug.LogWarning("could not load level thumbnail (" + www.error + ")");
        }

        infoImage.type = Image.Type.Sliced;
    }

    /// <summary>
    /// callback from deck buttons.  Selects the given deck and loads the level
    /// </summary>
    private void DeckSelected(XMLDeck deck)
    {
        DeckManagerScript.instance.SetDeck(deck); //send deck manager the chosen deck
        DeckManagerScript.instance.Shuffle(); //always shuffle the deck, regardless of what the level file says, if the deck did not come from the level file
        LevelManagerScript.instance.SendMessage("loadLevel", chosenLevel); //load the previously chosen level

        //track it
        AnalyticsResult ar;

        if (DeckManagerScript.instance.playerDecks.decks.Contains(deck))
            ar = Analytics.CustomEvent("deckLoaded", new Dictionary<string, object> { { "deckName", "customDeck" } }); //player decks all get the label "customDeck" to lump them together in analytics
        else
            ar = Analytics.CustomEvent("deckLoaded", new Dictionary<string, object> { {"deckName", deck.name } }); //everything else uses the name of the deck

        if (ar != AnalyticsResult.Ok)
            Debug.LogWarning("failed to track deckLoaded: " + ar);

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
            case "Back to Help":
            case "Help":
                //purge the current menu and show the help screen instead
                clearMenu();
                menuText.text = "Help";
                infoImage.enabled = false;
                infoText.enabled = false;
                showHelpMenu();
                break;

            case "Random Level":
                //find all level buttons currently available
                MenuButtonScript[] levelButtons = menuButtons           //all menu buttons
                    .Where(mb => mb.buttonType == MenuButtonType.level) //that correspond to a level
                    .Where(mb => mb.level.difficulty != "Tutorials")    //that is not a tutorial
                    .Where(mb => mb.level.difficulty != "testing only") //and is not a dev map
                    .ToArray();                                         //returned as an array

                //choose one of them at random and treat it as if that button was clicked on
                int buttonIndex = UnityEngine.Random.Range(0,levelButtons.Length);
                LevelSelected(levelButtons[buttonIndex].level);

                break;

            case "random existing deck":
                //chooses a deck at random from the player and behave as if that button was clicked on
                List<XMLDeck> deckOptions = DeckManagerScript.instance.playerDecks.decks.Where(xd => xd.isModded() == false) //choose from un-modded player decks...
                              .Concat( DeckManagerScript.instance.premadeDecks.decks ).                                      //and all premade decks
                              ToList();                                                                                      //and save it as a list

                DeckSelected(deckOptions[UnityEngine.Random.Range(0, deckOptions.Count)]); //select one of them
                break;

            case "create random deck":
                //randomly generates a deck for the player to use
                DeckSelected(DeckManagerScript.instance.generateRandomDeck());
                break;

            case "Deck Editor":
                //player wants to load the deck editor
                SceneManager.LoadScene("Deck Editor");
                break;

            case "Suggested Deck":
                //player wants to use the predefined deck for this level.  
                LevelManagerScript.instance.SendMessage("loadLevel", chosenLevel); //Load the level (the level manager will load the deck for us when it sees we haven't).

                //track it
                AnalyticsResult ar = Analytics.CustomEvent("deckLoaded", new Dictionary<string, object> { {"deckName", "levelDefault" } }); //default level decks all get the label "levelDefault" to lump them together in analytics
                if (ar != AnalyticsResult.Ok)
                    Debug.LogWarning("failed to track deckLoaded: " + ar);

                Destroy(menuRoot); //we are done with this menu.  Destroy it.
                break;

            case "Quit":
                //player wants to quit.
                Application.Quit();
                break;

            case "Back":
                //player wants to go back to beginning
                chosenLevel = null;
                clearMenu();
                StartCoroutine(setupLevelButtons());
                infoImage.gameObject.SetActive(true);
                infoImage.transform.parent.gameObject.SetActive(true);
                break;

            //these buttons are for individual help screens.  These we delegate to showHelpScreen() because of their length
            case "The Basics":
            case "Controls":
            case "Cards":
            case "Towers":
            case "Effect Reference":
                clearMenu();
                showHelpScreen(buttonText);
                break;

            default:
                Debug.LogError("LevelSelectScript doesnt know how to handle this button!");
                break;
        }
    }

    /// <summary>
    /// shows the help menu
    /// </summary>
    private void showHelpMenu()
    {
        //hide unused UI elements
        infoImage.transform.parent.gameObject.SetActive(false);

        //create the buttons
        string[] menuButtonStrings = {"The Basics", "Controls", "Cards", "Towers", "Effect Reference", "Back to Help"};
        foreach (string s in menuButtonStrings)
        {
            MenuButtonScript mbs = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>();
            mbs.setButtonText(s);
            mbs.setColor(menuButtonColor);
            mbs.transform.SetParent(this.transform, false);
            menuButtons.Add(mbs);
        }
    }

    /// <summary>
    /// shows one of the help screens.  This is separate from the rest of TextButtonSelected() because of its length
    /// </summary>
    private void showHelpScreen(string screen)
    {
        //create the first bit of text
        MenuTextScript helpText = Instantiate(menuTextPrefab).GetComponent<MenuTextScript>();
        helpText.transform.SetParent(this.transform, false);
        menuHeaders.Add(helpText);

        //set the text, and possibly include pictures and/or additional text objects, based on which help screen it is
        switch (screen)
        {
            case "Controls":
                helpText.text = 
                    "Pan camera: arrow keys OR WASD\n" +
                    "Zoom camera: scroll wheel or Q/E\n" +
                    "Start wave: click 'Start Wave' or press space\n" +
                    "Pause: `\n" +
                    "Set speed: 1, 2, or 3\n" +
                    "Quit level: Esc";
                break;

            case "The Basics":
                helpText.text =
                    "In this game, your goal is to use your deck of cards to do battle with the enemy.  You want to deplete the enemy deck before they deplete yours.\n" + 
                    "Enemies come in waves.  The cards along the top of the screen show what is coming next, and the cards along the bottom are what you can do to stop them.\n" +
                    "Play cards from your hand to build and upgrade towers, then press the space bar when you feel you can defeat the enemy.  Be careful not to overbuild, as towers go away after a certain number of rounds and you will probably need them later.\n" + 
                    "After the wave is over, any enemies that are still alive return to the top of the screen.  Both you and the enemy draw a card, and the process repeats.\n" + 
                    "You also get a special card that doesn't come from your deck called 'Gather Power'.  This is a powerful card that returns to you at the start of the next round in addition to your normal draw.\n" +
                    "If you manage to defeat all enemies, then you are given the option to 'continue in endurance'.  If you do this, the game will endlessly throw enemies at you until you die.";
                    break;

            case "Cards":
                helpText.text =
                    "Cards represent actions you can take, and are also your lifeblood.  Each card has a limited number of 'charges', shown at the top.  The card loses a charge every time it is played, and cards in your deck can lose charges if attacked by the enemy.  If a card runs out of charges, it is destroyed.\n" +
                    "If you run out of cards in your hand, you cannot take any action besides starting the wave. If you run out of cards in your deck, and then get hit by ane nemy, you lose the game.\n" +
                    "\n" +
                    "There are three type of cards:\n" +
                    "Tower Cards: these are yellow, and are used to build towers.  The description will give you the stats of the tower that would be built.\n" +
                    "Upgrade Cards: these are green, and are used to improve towers that already exist.  Note that not all towers can be upgraded, and those that can have a limit on how many times it can be done.\n" +
                    "Spell Cards: these are blue, and can do a lot of different things.  Read the card to find out what it does.\n" +
                    "\n" +
                    "You also get a special card at the beginning of every round called 'Gather Power' if you don't already have it.  This card gives the enemy one more card but gives you TWO cards, so you should use it regularly if you can to keep a steady supply of cards in your hand.\n" + 
                    "Drawing more enemies also means that you will get to the end of their deck faster, so you wont have to worry as much about your towers decaying.\n" +
                    "\n" +
                    "When building your own deck, the rules are as follows:\n" +
                    "A deck can have 30-60 cards in total.  Larger decks can take more of a beating, but it is easier to draw something specific with a smaller deck.\n" + 
                    "A deck can contain no more than 10 copies of the same card.\n" +
                    "You are not technically required to have towers in your deck, but you probably won't get very far without them unless the level you play on already has several built for you.\n";
                break;

            case "Towers":
                helpText.text =
                    "Towers are built into the world to defend yourself against the enemy.  The following is a breakdown of their stats and what they mean:\n" +
                    "Max Upgrades: number of times you can play upgrade cards on this tower.  Upgrades which say 'does not cost an upgrade slot' are not counted against this cap.\n" +
                    "Damage: how much health an enemy loses when it gets hit by this tower.\n" +
                    "Attack speed: how frequently the tower can make an attack.\n" +
                    "Damage per second: this number, shown in paranthesees, is just a general measure of about how powerful a tower is. The bigger this number is, the better.  See below for a more thorough explanation\n" +
                    "range: how far away a tower can attack enemies from.  Unless specified otherwise, the tower looks at all enemies in range of it and attacks whichever is closest to hurting you.  If there are none in range, it will wait for one to get closer.\n" +
                    "lifespan, or waves remaining: how many waves the tower can fight before disappearing.  When this hits zero, the tower is lost permanently.  Some towers have an infinite lifespan, but these usually have another limitation, such as limited ammo.\n" +
                    "\n" +
                    "Damage Per Second (DPS):\n" +
                    "Damage per second, or DPS for short, is the amount of damage, on average, a tower will do per second that it is attacking enemies.  This number is useful as a milestone to compare different towers with each other.\n" +
                    "For example, let's compare 'Heavy Tower' and 'Fast Tower':\n" +
                    "Heavy tower: deals 100 damage per hit, and attacks every 3 seconds.\n" +
                    "Fast tower: deals 4 damage per hit, and attacks 8.33 times a second.\n" +
                    "The heavy tower does a lot of damage in one hit, but it is very slow.  The fast tower, on the other hand, does very little damage but can fire several shots a second.  Comparing these two just by looking can be confusing.\n" +
                    "This is what DPS is for:  it turns out both towers average out to 33.33 damage per second, and so they are just as strong as each other!\n" +
                    "\n" +
                    "fast towers or slow towers?:\n" +
                    "Continuing the above example, if both a 'Heavy Tower' and a 'Fast Tower' do about the same amount of damage, why would you use one over the other?  The answer is how that damage is distributed.\n" +
                    "slower towers deal a lot of damage at once, so they are good for enemies with armor (which reduce the damage from every incoming attack), or for enemies you want to kill very quickly (like anything that heals itself)\n" +
                    "on the other hand, if the tower attacks something with low health, then the extra damage is wasted.\n" +
                    "fast towers deal low damage, but they attack very fast.  This makes them ideal for tackling large groups of weak enemies, since they won't waste their damage by attacking an enemy for far more health than it actually has.  They also get more chances to apply any special effects they may have.\n" +
                    "on the other hand, if the enemy has a lot of armor, then the attacks will mostly be wasted.  Armor can't reduce the attack below 1 damage, so it will still help, but most of the damage will be wasted.\n" +
                    "\n" +
                    "special targeting:\n" +
                    "There are several different types of targeting effects in the game, which override how a tower decides what to attack.  Examples include:\n" +
                    "'Target: highest health': targets whichever enemy has the most health remaining.  Useful to stop powerful towers from wasting their attack on weak enemies.\n" +
                    "'Target: all in range': attacks EVERY enemy in range of the tower.  DPS on these towers is expressed per enemy, so don't be fooled by how little damage they do.  10 damage dealt to 100 enemies at a time is quite a big deal!\n" +
                    "'Target: orthogonal': this will make more sense when you see it in action, but if there is an enemy in range then all enemies above, below, left, or right of the tower, whether they are in range or not, will get attacked.\n" +
                    "'Target: mouse': towers with this effect attack enemies near your mouse, instead of enemies near themselves.  These towers effectively have infinite range, but require you to move your mouse around to follow the enemies.\n";
                break;

            case "Effect Reference":
                helpText.text =
                    "This section is for more detailed explanations of some of the more confusing effects you may see on cards in the game.  Feel free to ignore this section for now and come back later to look up something that confuses you.\n" +
                    "\n" +
                    "\n" +
                    "[Scaled]: this tag on an enemy effect means that it becomes more powerful on tougher enemy waves.  The numbers listed on the card are always correct for whatever group you are looking at.\n" +
                    "\n" +
                    "[Ranked]: like [Scaled], but the effect only gets stronger if the enemy ranks up (ex, a Tank III has more armor than a Tank II)\n" +
                    "\n" +
                    "Armor: for every point of armor, incoming attacks do one less damage.  However, armor cannot reduce an attack below 0 damage.  Even an enemy with 999 armor can be taken down with enough bullets, no matter how weak they are.\n" +
                    "\n" +
                    "Enemy loses % health: This is their max health, not their current!  Towers with this effect are VERY strong against giants, which can often end up with hundreds of thousands of health in the late game.\n" +
                    "\n" +
                    "<Resonant>: All towers with these effects get stronger for all other towers with the same effect.  For example, if you have two resonant towers and build a third, all three will become more powerful!\n" +
                    "\n" +
                    "secondary burst: in addition to the normal attack, the tower produces a small wave that hurts everything near the tower.\n" +
                    "\n" +
                    "splash damage: attacks create small explosions that damage anything caught in them.\n" +
                    "\n" +
                    "Ammo: towers with a limited supply of ammo spend one ammo per attack, regardless of how many enemies they hit with it.  When they run out of ammo, they disappear.\n" +
                    "\n" +
                    "fired manually: towers with this effect sparkle when they are ready to attack, but they wont actually do so unless you click on them.  Usually found on towers with limited ammo.\n" +
                    "\n" +
                    "Overcharge: if a tower with overcharge is ready to attack but nothing is in range, it can continue charging up and get bonus damage when an enemy gets close enough to attack.\n";
                break;

            default:
                Debug.LogWarning("showHelpScreen doesnt recognize screen \"" + screen + "\"");
                break;
        }

        //and put a back button at the bottom
        MenuButtonScript mbs = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>();
        mbs.setButtonText("Back");
        mbs.setColor(menuButtonColor);
        mbs.transform.SetParent(this.transform, false);
        menuButtons.Add(mbs);
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

            case "Help":
                //show text to explain the button
                infoText.text = "Confused? Find explanations and tutorials here, whether you need a little help or a lot.";
                break;

            case "Random Deck(from list)":
                //show text to explain the button
                infoText.text = "Chooses randomly from premade decks and those you have designed.";
                break;

            case "create random deck":
                //show text to explain the button
                infoText.text = "randomly chooses cards to create a brand new deck.  Warning: deck quality will vary wildly!";
                break;

            case "Suggested Deck":
                //treat the Suggested Deck button as if it were a reference to the level deck
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