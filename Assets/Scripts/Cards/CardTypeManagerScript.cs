using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;
using System.Linq;

/// <summary>
/// maintains the collection of card types, including saving/loading to XML and some inspector-only controls
/// </summary>
[XmlRoot("CardTypes")]
[System.Serializable]
public class CardTypeCollection
{
    //list of different card types
    [XmlArray("Cards")]
    [XmlArrayItem("Card")]
    [Display(Seq.GuiBox | Seq.PerItemDuplicate | Seq.PerItemRemove | Seq.Filter)]
    public List<PlayerCardData> cardTypes = new List<PlayerCardData>();

    //the file this collection was populated from.  For use in error reporting
    [XmlIgnore] public string filePath { get; set; }
    [XmlIgnore] public string fileName { get { return Path.GetFileNameWithoutExtension(filePath); } }

    //comma separated lists of mod files that this file is Dependant on, if any
    [XmlAttribute("enemyFileDependencies")][DefaultValue("")][Hide] public string enemyDependencies;
    [XmlAttribute( "cardFileDependencies")][DefaultValue("")][Hide] public string  cardDependencies;

    /// <summary>
    /// saves the collection to XML.  Cards marked as modded are not saved, since they are likely not part of the original collection
    /// </summary>
    /// <param name="path">where to save the collection</param>
    public void Save(string path)
    {
        //temporarily remove all modded cards
        List<PlayerCardData> temp = new List<PlayerCardData>(cardTypes);
        cardTypes.RemoveAll(ct => ct.isModded);

        //save normally
        XmlSerializer serializer = new XmlSerializer(typeof(CardTypeCollection));

        using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
        {
            serializer.Serialize(stream, this);
        }

        //put the list back
        cardTypes = temp;
    }

    /// <summary>
    /// returns a new CardTypeCollection loaded from the provided stream, and stores the given path into it
    /// </summary>
    public static CardTypeCollection Load(Stream stream, string filePath)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(CardTypeCollection));
        CardTypeCollection result = serializer.Deserialize(stream) as CardTypeCollection;
        result.filePath = filePath;
        return result;
    }

    /// <summary>
    /// returns true if the collection has cards in it
    /// </summary>
    [Hide] public bool cardTypesLoaded() { return (cardTypes != null) && (cardTypes.Count > 0); }

    /// <summary>
    /// sorts the list of card types using the same comparer as the deck editor default.
    /// This sorts by type, then name.
    /// </summary>
    [Show][VisibleWhen("cardTypesLoaded")] private void sortCardTypes ()
    {
        cardTypes.Sort(new CardTypeComparer());
    }
}

/// <summary>
/// responsible for saving/loading card types to/from XML and provides several utility functions for locating card types.
/// </summary>
public class CardTypeManagerScript : BaseBehaviour
{
    //manager settings: only shown outside of gameplay
    [Hide] private bool shouldShowSettings() { return !Application.isPlaying; }
    [VisibleWhen("shouldShowSettings")] public static CardTypeManagerScript instance; //singleton instance
    [VisibleWhen("shouldShowSettings")] public string path;                           //path of base game cards
    [VisibleWhen("shouldShowSettings")] public string modPath;                        //path of modded cards

    //contains all card types.  Only shown if it has data
    [Hide] public bool areTypesLoaded() { return (types != null) && (types.cardTypes != null) && (types.cardTypes.Count > 0); }
    [VisibleWhen("areTypesLoaded")] public CardTypeCollection types;

    //event for when types get reloaded
    public delegate void CardTypesReloadedHandler(CardTypeCollection newTypes);
    public event CardTypesReloadedHandler cardTypesReloadedEvent;

    // Use this for initialization
    private void Awake()
    {
        instance = this;
        StartCoroutine(loadCardTypes());
    }

    //reloads card definitions
    [Show] public void reload() { StartCoroutine(reloadCoroutine()); } //hide coroutine-ness since callers have no reason to care
    private IEnumerator reloadCoroutine()
    {
        yield return StartCoroutine(loadCardTypes());

        if (cardTypesReloadedEvent != null)
            cardTypesReloadedEvent.Invoke(types);
    }

    /// <summary>
    /// [COROUTINE] loads the card types.  This version is works for any supported build
    /// </summary>
    private IEnumerator loadCardTypes()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            yield return StartCoroutine(loadCardTypesWeb());
        else
            yield return StartCoroutine(loadCardTypesPC());
    }

    /// <summary>
    /// [COROUTINE] loads card typeson web builds.  
    /// </summary>
    private IEnumerator loadCardTypesWeb()
    {
        //form the web request
        string filePath = Application.streamingAssetsPath + '/' + path;
        //while (filePath.StartsWith("/")) filePath = filePath.Substring(1); //remove any leading /'s
        WWW request = new WWW(filePath);

        //wait for the request to load
        yield return request;

        //show error if there was one
        if (request.error != null)
        {
            MessageHandlerScript.Error("Error loading card types:\n" + request.error);
            yield break;
        }

        //or, if we were successful, create a new stream and fill it with the contents of the web request:
        using (MemoryStream cardTypesStream = new MemoryStream())    //create the stream
        {
            StreamWriter writer = new StreamWriter(cardTypesStream); //used to write to it
            writer.Write(request.text);                               //write contents of the request
            writer.Flush();                                           //make sure it gets processed
            cardTypesStream.Position = 0;                            //send the stream back to the start

            //now we can finally load the decks
            types = CardTypeCollection.Load(cardTypesStream, filePath);
        }

        Debug.Log(types.cardTypes.Count + " card types loaded.");
    }

    /// <summary>
    /// [COROUTINE] loads the card types.  This version is for PC builds
    /// </summary>
    private IEnumerator loadCardTypesPC()
    {
        //wait for the dependency manager to exist before we do this
        while (DependencyManagerScript.instance == null)
            yield return null;

        //also wait for enemies to be loaded
        while (DependencyManagerScript.instance.enemyDepenciesHandled == false)
            yield return null;

        //load base game cards
        string filePath = Path.Combine(Application.streamingAssetsPath, path);
        using (FileStream stream = new FileStream(filePath, FileMode.Open))
            types = CardTypeCollection.Load(stream, filePath);

        foreach (PlayerCardData baseCard in types.cardTypes)
            baseCard.isModded = false; //flag base game cards as being from the base game

        //find mod files
        DirectoryInfo modDir =  new DirectoryInfo (Path.Combine (Application.streamingAssetsPath, modPath));   //mod folder
        FileInfo[] modFiles = modDir.GetFiles ("*.xml");                                            //file list

        //load the files
        List<CardTypeCollection> modCardCollections = new List<CardTypeCollection>();
        foreach (FileInfo f in modFiles)
        {
            Debug.Log("found card mod file: " + f.Name);
            using (FileStream stream = new FileStream(f.FullName, FileMode.Open))
                modCardCollections.Add(CardTypeCollection.Load(stream, f.FullName));
        }

        //get the dependency manager to sort/cull the list
        modCardCollections = DependencyManagerScript.instance.handleCardFileDependencies(modCardCollections);

        foreach (CardTypeCollection modTypes in modCardCollections)
        {
            foreach (PlayerCardData moddedCard in modTypes.cardTypes)
            {
                //mark the definition as modded
                moddedCard.isModded = true;

                //find the existing version of this enemy
                PlayerCardData existingCard = null;
                foreach (PlayerCardData baseCard in types.cardTypes)
                {
                    if (baseCard.cardName == moddedCard.cardName)
                    {
                        existingCard = baseCard;
                        break;
                    }
                }

                //replace the enemy if it exists already, and add it if it doesnt
                if (existingCard != null)
                {
                    types.cardTypes.Remove(existingCard);
                    types.cardTypes.Add(moddedCard);
                    Debug.Log("Overwriting card: " + existingCard.cardName);
                }
                else {
                    types.cardTypes.Add(moddedCard);
                }
            }
        }

        yield break;
    }

    //Dev: shows a button in the inspector to save the card types, provided there are some loaded
    [Show][VisibleWhen("areTypesLoaded")] private System.Collections.IEnumerator saveCardChanges()
    {
        yield return StartCoroutine(MessageHandlerScript.PromptYesNo("Are you sure you want to overwrite the card definitions?"));
        if (MessageHandlerScript.responseToLastPrompt == "Yes")
        {
            types.Save(Path.Combine(Application.streamingAssetsPath, path));
            Debug.Log("Card changes saved. <UNMODDED CARDS ONLY!>");
        }
    }

    //returns a random card type from the database
    public PlayerCardData getRandomCardType()
    {
        //get random index
        int index = Mathf.RoundToInt (Random.Range (0.0f, types.cardTypes.Count-1));

        //return card at that index
        return types.cardTypes[index];
    }

    /// <summary>
    /// returns a random card type from the database, with the given PlayerCardType, that is neither modded nor a token
    /// </summary>
    /// <param name="type">card type (tower, spell, etc.) to return</param>
    /// <returns>the located type</returns>
    public PlayerCardData getRandomCardType(PlayerCardType type)
    {
        //get a subset of the type list
        IEnumerable<PlayerCardData> typeOptions = types.cardTypes.Where(pcd => (pcd.cardType == type) && (pcd.isModded == false) && (pcd.isToken == false));

        //get random index
        int index = Random.Range(0, typeOptions.Count());

        //return card at that index
        return typeOptions.ElementAt(index);
    }

    //returns the card from the database with the given name
    public PlayerCardData getCardByName(string name)
    {
        foreach (PlayerCardData cd in types.cardTypes)
        {
            if (cd.cardName.Equals(name))
            {
                return cd;
            }
        }

        MessageHandlerScript.Error("Could not find card type " + name + ".");
        return null;
    }

    //returns the names of all available cards
    public string[] getCardNames()
    {
        List<string> names = new List<string>();

        foreach (PlayerCardData cd in types.cardTypes)
            names.Add(cd.cardName);

        names.Sort();
        return names.ToArray();
    }

    //returns the names of all available tower cards
    public string[] getTowerNames()
    {
        List<string> names = new List<string>();

        foreach (PlayerCardData cd in types.cardTypes)
            if (cd.cardType == PlayerCardType.tower)
                names.Add(cd.cardName);

        names.Sort();
        return names.ToArray();
    }

    //returns the names of all available upgrade cards
    public string[] getUpgradeNames()
    {
        List<string> names = new List<string>();

        foreach (PlayerCardData cd in types.cardTypes)
            if (cd.cardType == PlayerCardType.upgrade)
                names.Add(cd.cardName);

        names.Sort();
        return names.ToArray();
    }

    //returns the names of all available spell cards
    public string[] getSpellNames()
    {
        List<string> names = new List<string>();

        foreach (PlayerCardData cd in types.cardTypes)
            if (cd.cardType == PlayerCardType.spell)
                names.Add(cd.cardName);

        names.Sort();
        return names.ToArray();
    }
}