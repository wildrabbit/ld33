using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour 
{
    private PlayerControl m_parent;


    private SpriteRenderer m_renderer;
    private LineRenderer m_lineRenderer;

    public Vector2 m_laserOffset = new Vector2(0.68f, 0.03f);
    public int m_damage = 2;
    public bool m_piercing = false;

    private Color m_colour;
    private Color m_disabledColour = Color.red;

    private Vector2[] m_laserPositions;
	// Use this for initialization
	void Start () 
    {
        m_parent = GetComponentInParent<PlayerControl>();
        m_renderer = GetComponent<SpriteRenderer>();

        m_lineRenderer = GetComponentInChildren<LineRenderer>();
        m_lineRenderer.transform.localPosition = m_laserOffset;
        m_lineRenderer.sortingLayerName = m_renderer.sortingLayerName;

        m_laserPositions = new Vector2[2];
        m_laserPositions.Fill(Vector2.zero);

        m_colour = m_lineRenderer.material.color;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (GameplayManager.Instance.paused)
        {
            return;
        }

        m_renderer.sortingOrder = m_parent.SortingOrder + 1;
        m_lineRenderer.sortingOrder = m_parent.SortingOrder;
        float angle = m_parent.Orientation;
        float radAngle = Mathf.Deg2Rad * angle;
        Vector2 localScale = transform.localScale;
        float absAngle = Mathf.Abs(angle);
        if (absAngle >= 90.0f && absAngle <= 180.0f)
        {
            localScale.x = -(Mathf.Abs(localScale.x));
            angle -= 180.0f;
        }
        else
        {
            localScale.x = Mathf.Abs(localScale.x);
        }
        transform.localScale = localScale;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        m_laserPositions[0] = m_lineRenderer.transform.position;
        m_laserPositions[1] = (Vector2)m_lineRenderer.transform.position + m_parent.Range * new Vector2(Mathf.Cos(radAngle), Mathf.Sin(radAngle));

        m_lineRenderer.SetPosition(0, m_laserPositions[0]);
        m_lineRenderer.SetPosition(1, m_laserPositions[1]);
	}

    public void SetLaserState(bool enabled)
    {
        m_lineRenderer.material.color = enabled ? m_colour : m_disabledColour;
    }
}
