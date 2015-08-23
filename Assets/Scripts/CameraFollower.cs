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
	}
}
