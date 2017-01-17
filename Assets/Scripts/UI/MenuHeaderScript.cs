using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//stub used for menu header objects
public class MenuHeaderScript : MonoBehaviour
{
    public Text headerText;
	
	public string text { get { return headerText.text; } set { headerText.text = value; } }
}
