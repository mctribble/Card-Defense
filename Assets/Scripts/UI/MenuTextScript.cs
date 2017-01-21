using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//stub used for menu header objects
public class MenuTextScript : MonoBehaviour
{
    public Text textComponent;
	
	public string text { get { return textComponent.text; } set { textComponent.text = value; } }
}
