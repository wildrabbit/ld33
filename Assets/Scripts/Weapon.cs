using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour 
{
    private PlayerControl m_parent;

    private SpriteRenderer m_parentRenderer;
    private SpriteRenderer m_renderer;
	// Use this for initialization
	void Start () 
    {
        m_parent = GetComponentInParent<PlayerControl>();
        m_parentRenderer = m_parent.GetComponent<SpriteRenderer>();
        m_renderer = GetComponent<SpriteRenderer>();
	}
	
	// Update is called once per frame
	void Update () 
    {
        m_renderer.sortingOrder = m_parentRenderer.sortingOrder + 1;
        float angle = m_parent.Orientation;
        Debug.LogFormat("Angle: {0}", angle);
        Vector2 localScale = transform.localScale;
        float absAngle = Mathf.Abs(angle);
        if (absAngle >= 90.0f && absAngle <= 180.0f)
        {
            localScale.x = -(Mathf.Abs(localScale.x));
            angle -= 180.0f;
        }
        else
        {
            Debug.Log("dafuq");
            localScale.x = Mathf.Abs(localScale.x);
        }
        transform.localScale = localScale;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

	}
}
