using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;

/// <summary>
/// handles user input to control the camera
/// </summary>
public class CameraControlScript : BaseBehaviour
{
    public float cameraSpeed;  //speed of the camera
    public Rect  cameraBounds; //region the camera is allowed to move in

    public float zoomSpeed; //camera zoom speed
    public float minSize;   //min size of the camera
    public float maxSize;   //max size of the camera

    private Camera cameraRef;

    //init
    private void Start()
    {
        cameraRef = this.GetComponent<Camera>();
    }

    //allows player to move the camera
	private void Update()
    {
        Vector3 newPos = transform.position; //fetch current position

        //update it from keyboard input
        newPos.x += Input.GetAxis("Horizontal") * Time.deltaTime * cameraSpeed * cameraRef.orthographicSize;
        newPos.y += Input.GetAxis("Vertical")   * Time.deltaTime * cameraSpeed * cameraRef.orthographicSize;

        //clamp it to the bounds
        newPos.x = Mathf.Clamp(newPos.x, cameraBounds.xMin, cameraBounds.xMax);
        newPos.y = Mathf.Clamp(newPos.y, cameraBounds.yMin, cameraBounds.yMax);

        transform.position = newPos; //update the transform

        //zoom controls
        cameraRef.orthographicSize -= Input.GetAxis("Zoom") * Time.deltaTime * zoomSpeed;
        cameraRef.orthographicSize  = Mathf.Clamp(cameraRef.orthographicSize, minSize, maxSize);
    }
	
}
