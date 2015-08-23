using UnityEngine;
using System.Collections;

public class CameraFollower : MonoBehaviour 
{
    public Rect m_boundaries;
    public Transform m_target;

	// Use this for initialization
	void Start () 
    {
	
	}
	
	// Update is called once per frame
	void Update () 
    {
        Camera.main.transform.position = new Vector3(m_target.position.x, m_target.position.y, Camera.main.transform.position.z);
        Vector3 camPos = Camera.main.transform.position;
        float hheight = Camera.main.orthographicSize;
        float hwidth = hheight * Camera.main.aspect;
        
        Rect bounds = GameplayManager.Instance.Boundaries;
        if (camPos.x - hwidth < bounds.x)
        {
            camPos.x = bounds.x + hwidth;
        }
        else if (camPos.x + hwidth > bounds.x + bounds.width)
        {
            camPos.x = bounds.x + bounds.width - hwidth;
        }
        
        if (camPos.y + hheight > bounds.y)
        {
            camPos.y = bounds.y - hheight;
        }
        else if (camPos.y - hheight < bounds.y - bounds.height)
        {
            camPos.y = bounds.y - bounds.height + hheight;
        }
        Camera.main.transform.position = camPos;
    }
}
