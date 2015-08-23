using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Entity : MonoBehaviour
{
    public float m_minWanderDecisionTime = 0.8f;
    public float m_maxWanderDecisionTime = 1.5f;

    protected float m_lastWanderDecisionTime;
    protected float m_nextWanderDecisionTime;

    public float m_maxSpeed = 1.0f;
    public float m_orientationSpeed = 1.0f;
    public float m_sightRadius = 6.0f;

    public float m_hitRecovery = 0.5f;
    protected float m_hitTime;

    public float m_shootCooldown = 0.4f;
    protected float m_lastShoot;
    

    public int m_startHP = 10;

    public Vector2 Orientation
    {
        get
        {
            return m_currentOrientation;
        }
    }

    public float Angle
    {
        get
        {
            float angle = Mathf.Atan2(m_currentOrientation.y, m_currentOrientation.x);
            return Mathf.Rad2Deg * angle;
        }
    }
    public bool WieldsWeapon
    {
        get
        {
            return m_weapon != null;
        }
    }

    public int  WeaponDamage
    {
        get { return m_weapon.m_damage; }
    }

    public float WeaponRecoil
    {
        get { return m_weapon.m_recoilPercent; }
    }

    virtual public int MeleeDamage
    {
        get { return 1; }
    }

    public int SortingOrder
    {
        get { return m_renderer.sortingOrder; }
    }

    public LayerMask m_visibilityMask;

    public AudioClip m_deathSound;
    public AudioClip m_hitSound;
    public AudioClip m_shootSound;

    protected SpriteRenderer m_renderer;
    protected AudioSource m_audioSource;
    protected Rigidbody2D m_body;
    protected Weapon m_weapon;
    protected Light m_light;

    protected Collider2D m_mainCollider;
    protected Collider2D m_bodyCollider;

    protected CharacterLife m_lifeData;

    // Physics vars
    protected Vector2 m_velocityTarget;
    protected Vector2 m_orientationTarget;
    protected Vector2 m_currentOrientation;

    // Targets
    protected Vector2 m_wanderPosition;
    protected Entity m_targetEntity;

    #region UnityImplements

    virtual protected void Awake ()
    {
        m_renderer = GetComponent<SpriteRenderer>();
        m_audioSource = GetComponent<AudioSource>();
        m_body = GetComponent<Rigidbody2D>();
        m_weapon = GetComponentInChildren<Weapon>();

        m_mainCollider = GetComponent<Collider2D>();
        m_bodyCollider = GetComponentInChildren<Collider2D>();

        m_lifeData = new CharacterLife();
        m_hitTime = 0.0f;
        m_lastShoot = -1.0f;
	}

    // Use this for initialization
    virtual protected void Start()
    {
        Initialize(m_startHP, m_maxSpeed); 
    }

    virtual public void Initialize(int maxHP, float maxSpeed)
    {
        m_maxSpeed = maxSpeed;
        m_startHP = maxHP;
        m_lifeData.Initialise(m_startHP);

        m_mainCollider.enabled = true;
        m_bodyCollider.enabled = true;

    }

    // Update is called once per frame
    virtual protected void Update()
    {
        if (GameplayManager.Instance.paused) return;

    }

    virtual protected void FixedUpdate()
    {
        if (GameplayManager.Instance.paused) return;

        if (IsDying() || m_body.isKinematic)
        {
            m_body.velocity = Vector2.zero;
        }
        else m_body.velocity = m_velocityTarget;

        if (m_orientationTarget != m_currentOrientation)
        {
            m_currentOrientation = m_orientationTarget;
        }
        // TODO: Probably update facing as well as some point
    }

    virtual public void OnCollisionEnter2D(Collision2D collision)
    {
        Entity e = collision.collider.GetComponent<Entity>();
        if (e != null)
        {
            OnBumped(e);
        }
        m_body.isKinematic = true;
    }
    virtual public void OnCollisionExit2D(Collision2D collision)
    {
        m_body.isKinematic = false;
    }
    #endregion


    abstract public bool IsDying();
    abstract protected void SetDying();
    abstract protected void HitReaction();
    abstract protected void OnDied();

    public bool CanSee(Entity e)
    {
        Vector2 direction = e.transform.position - transform.position;
        RaycastHit2D[] info = Physics2D.RaycastAll(transform.position, direction.normalized, m_sightRadius, m_visibilityMask);
        for (int i = 0; i < info.Length; ++i)
        {
            if (info[i].collider != null)
            {
                Entity spotted = info[i].collider.GetComponent<Entity>();
                if (spotted == null)
                {
                    spotted = info[i].collider.GetComponentInParent<Entity>();
                }
                if (spotted == e) return true;
            }
        }
        return false;
    }

    abstract public bool CanBumpEntity(Entity e);
    abstract public bool CanShootEntity(Entity e);

    virtual public void UpdateHit()
    {
        if (Time.time - m_hitTime >= m_hitRecovery)
        {
            OnHitFinished();
        }
    }
    virtual public void OnHitFinished() { m_hitTime = 0.0f; }

    public void OnBumped (Entity e)
    {
        if (IsEthereal()) return;
        if (!e.CanBumpEntity(this))
        {
            return;
        }

        bool dead = m_lifeData.UpdateHP(-1);
        Debug.LogFormat("Entity {0} hit! Current Hp: {1}", name, m_lifeData.HP);
        if (dead)
        {
            StartCoroutine(DieInSeconds(1.0f));
        }
        else
        {
            HitReaction();
            m_hitTime = Time.time;
        }
    }

    virtual public bool CanShoot ()
    {
        return !IsDying() && m_weapon != null &&  (m_lastShoot < 0 || Time.time - m_lastShoot >= m_shootCooldown);
    }

    virtual public void Shoot ()
    {
        List<Entity> entities = m_weapon.GetShootTargets();
        for (int i = 0; i < entities.Count; ++i)
        {
            entities[i].OnShot(this);
        }

        if (m_weapon.m_recoilPercent > 0.0f)
        transform.Translate(m_currentOrientation * m_weapon.m_recoilPercent);

        if (m_audioSource != null && m_shootSound != null)
        {
            m_audioSource.PlayOneShot(m_shootSound);
        }
        m_lastShoot = Time.time;
    }

    public void OnShot(Entity e)
    {
        if (IsDying()) return;

        int damage = (e.WieldsWeapon) ? e.WeaponDamage : e.MeleeDamage;
        Vector2 direction = e.Orientation;
    
        bool died = m_lifeData.UpdateHP(-damage);
        Debug.LogFormat("Entity {0} hit! Current Hp: {1}", name, m_lifeData.HP);
        
        transform.Translate(direction * e.WeaponRecoil);
        if (died)
        {
            m_audioSource.PlayOneShot(m_deathSound);
            StartCoroutine(DieInSeconds(1.0f));
        }
        else
        {
            m_audioSource.PlayOneShot(m_hitSound);
            HitReaction();
        }
    }

    abstract public bool IsEthereal();
    
    public IEnumerator DieInSeconds(float length)
    {
        SetDying();
        m_mainCollider.enabled = false;
        m_bodyCollider.enabled = false;

        float t = Time.time;
        float elapsed = Time.time - t;
        Color c;

        while (elapsed < length)
        {
            c = m_renderer.color;
            c.a = Mathf.SmoothStep(1.0f, 0.0f, elapsed / length);
            m_renderer.color = c;
            yield return null;
            elapsed = Time.time - t;
        }


        c = m_renderer.color;
        c.a = 1.0f;
        m_renderer.color = c;

        m_mainCollider.enabled = false;
        m_bodyCollider.enabled = false;
        OnDied(); // Notify Gameplay manager!
        gameObject.SetActive(false);
    }

    public bool OwnsCollider (Collider2D collider)
    {
        return collider == m_bodyCollider || collider == m_mainCollider;
    }
}
