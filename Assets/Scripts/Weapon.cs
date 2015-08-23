using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Weapon : MonoBehaviour 
{
    private Entity m_parent;

    private SpriteRenderer m_renderer;
    
    public int m_damage = 2;
    public bool m_piercing = false;
    public float m_maxRange = 6.0f;
    public float m_recoilPercent = 0.2f;
    public float m_errorChance = 0.1f;

    public float m_defaultWidth = 0.01f;
    public float m_shootWidth = 0.05f;
    public float m_shootVisibleTime = 0.3f;
    private float m_shootVisible;

    private float m_effectiveRange;
    public float Range
    {
        get { return m_effectiveRange; }
    }

    // Laser stuff
    private LineRenderer m_laserAim;

    public Vector2 m_laserOffset = new Vector2(0.68f, 0.03f);
    public LayerMask m_shootVisibility;
    public LayerMask m_laserVisibility;
    private int m_characterVisibilityMask;
    
    private Color m_colour;
    private Color m_disabledColour = Color.red;

    private Vector2[] m_laserPositions;
	
    
    // Use this for initialization
	void Awake () 
    {
        m_parent = GetComponentInParent<Entity>();
        m_renderer = GetComponent<SpriteRenderer>();

        m_laserAim = GetComponentInChildren<LineRenderer>();
        m_laserAim.transform.localPosition = m_laserOffset;
        m_laserAim.sortingLayerName = m_renderer.sortingLayerName;

        m_laserPositions = new Vector2[2];
        m_laserPositions.Fill(Vector2.zero);

        m_colour = m_laserAim.material.color;

        m_characterVisibilityMask = (1 << LayerMask.NameToLayer("Characters"));
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (GameplayManager.Instance.paused)
        {
            return;
        }

        if (m_shootVisible >= 0 && Time.time - m_shootVisible > m_shootVisibleTime)
        {
            m_laserAim.SetWidth(m_defaultWidth, m_defaultWidth);
            m_shootVisible = -1.0f;
        }

        UpdateOrientation();
        UpdateVisibility();
        
        SetLaserState(m_parent.CanShoot());
	}

    public void Shoot ()
    {
        m_laserAim.SetWidth(m_shootWidth, m_shootWidth);
        m_shootVisible = Time.time;
    }

    public List<Entity> GetShootTargets ()
    {
        List<Entity> result = new List<Entity>();
        if (!m_piercing)
        {
            RaycastHit2D info = Physics2D.Raycast(m_laserAim.transform.position, m_parent.Orientation, m_effectiveRange, m_shootVisibility);
            if (info.collider != null && !m_parent.OwnsCollider(info.collider))
            {
                Entity collidedEntity = info.collider.GetComponentInParent<Entity>();
                if (collidedEntity != null)
                {
                    result.Add(collidedEntity);
                }
            }
        }
        else
        {
            RaycastHit2D[] infos = Physics2D.RaycastAll(m_laserAim.transform.position, m_parent.Orientation, m_effectiveRange, m_shootVisibility);
            if (infos != null && infos.Length > 0)
            {
                for (int i = 0; i < infos.Length; ++i)
                {
                    if (infos[i].collider != null)
                    {
                        Entity collidedEntity = infos[i].collider.GetComponent<Entity>();
                        if (collidedEntity != null)
                        {
                            result.Add(collidedEntity);
                        }
                    }
                }
            }
        }
        return result;
    }

    public void SetLaserState(bool enabled)
    {
        m_laserAim.material.color = enabled ? m_colour : m_disabledColour;
    }

    void UpdateOrientation ()
    {
        m_renderer.sortingOrder = m_parent.SortingOrder + 1;
        m_laserAim.sortingOrder = m_parent.SortingOrder;
        float angle = m_parent.Angle;
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
    }

    void UpdateVisibility ()
    {
        LayerMask visibility = m_laserVisibility;
        if (!m_piercing)
        {
            visibility.value = visibility.value | m_characterVisibilityMask;
        }

        RaycastHit2D[] info = Physics2D.RaycastAll(m_laserAim.transform.position, m_parent.Orientation, m_maxRange, visibility);
        m_effectiveRange = m_maxRange;
        if (info != null && info.Length > 0)
        {
            for (int i = 0 ; i < info.Length; ++i)
            {
                Collider2D rayCollider = info[i].collider;
                if (rayCollider != null && !m_parent.OwnsCollider(rayCollider))
                {
                    if (info[i].distance < m_effectiveRange)
                    {
                        m_effectiveRange = info[i].distance;                        
                    }
                }
            }            
        }

        float radAngle = Mathf.Deg2Rad * m_parent.Angle;
        
        m_laserPositions[0] = m_laserAim.transform.position;
        m_laserPositions[1] = (Vector2)m_laserAim.transform.position + m_effectiveRange * new Vector2(Mathf.Cos(radAngle), Mathf.Sin(radAngle));

        m_laserAim.SetPosition(0, m_laserPositions[0]);
        m_laserAim.SetPosition(1, m_laserPositions[1]);
    }
}
