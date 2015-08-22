using UnityEngine;
using System.Collections.Generic;

public class DepthAdjuster : MonoBehaviour {

    private Renderer m_renderer;
	// Use this for initialization
	void Start () 
    {
        m_renderer = GetComponent<Renderer>();        
	}
	
	// Update is called once per frame
	void Update () 
    {
        m_renderer.sortingOrder = -(int)(transform.position.y * 10);
	}
}
