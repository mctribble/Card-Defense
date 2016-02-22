using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CastingTooltipScript : MonoBehaviour {

	public Color CastableColor;				//color of tooltip when cast is OK
	public Color UncastableColor;			//color of tooltip when cast is forbidden
	public Image rangeImage;				//reference to image for the range overlay

	private const float GRID_SCALE = 0.5f;	//size of grid to snap to. 
	private GameObject card;				//card that produced this tooltip
	private bool castable;					//whether or not the spell can be cast here
	private GameObject targetTower;			//the tower this card is targeting.  Applies only to upgrades
	private CardType type;					//type of card that owns this tooltip

	// Use this for initialization
	void Start () {
		castable = false;
		targetTower = null;
	}
	
	// Update is called once per frame
	void Update () {

		//get position of cursor in world space
		Vector2 mousePositionWorld = Camera.main.ScreenToWorldPoint (Input.mousePosition);

		//move to the mouse position, but snap to a grid of size GRID_SCALE
		transform.position = new Vector3(Mathf.Round (mousePositionWorld.x / GRID_SCALE) * GRID_SCALE,
		                                 Mathf.Round (mousePositionWorld.y / GRID_SCALE) * GRID_SCALE,
		                                 -3.0f);

		//check if this spot is free
		Collider2D collision = Physics2D.OverlapPoint (transform.position, LayerMask.GetMask("Obstacle"));

		//determine if the fcard can be cast
		if (type == CardType.tower) {
			//towers only castable if unobstructed
			if (collision)
				castable = false;
			else
				castable = true;
		} else if (type == CardType.upgrade) {
			//only castable if there is a tower here
			if (collision)
			{
				if (collision.GetComponent<Collider2D>().gameObject.tag.Equals("TowerImage")) { //test for TowerImage to only collide ith the tower itself and not its range
					targetTower = collision.GetComponent<Collider2D>().gameObject.transform.root.gameObject;
					castable = true;
				} else {
					castable = false;
				}
			} else {
				castable = false;
			}
		}

		//colorize accordingly
		if (castable) {
			GetComponentInChildren<Image> ().color = CastableColor;
			rangeImage.enabled = true;

			//TODO: figure out why event trigger wasnt working and replace this with that
			if (Input.GetMouseButtonUp(0))
				Cast ();
		} else {
			GetComponentInChildren<Image> ().color = UncastableColor;
			rangeImage.enabled = false;
		}
	}

	//called when a cast is attempted
	void Cast () {
		if (castable) {
			//error prevention: cancel cast if card no longer exists
			if (card != null) {
				if (type == CardType.tower)
					card.SendMessage ("SummonTower", transform.position);
				else if (type == CardType.upgrade)
					card.SendMessage ("UpgradeTower", targetTower);
			}
			Destroy(gameObject);
		}
	}

	//stores a reference to the card that created this tooltip
	void SetParent (GameObject parent){
		card = parent;
	}

	//sets the card type
	void SetCardType (CardType t){
		type = t;
	}

	//sets the size of the range overlay
	void SetRange (float r) {
		rangeImage.gameObject.GetComponent<RectTransform> ().localScale = new Vector3 (r, r, 1.0f);
	}

}
