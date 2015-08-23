using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum EnemyPersonality
{
    Cautious,
    Aggresive,
    Peaceful
}

public enum EnemyReaction
{
    Friendly,
    Neutral,
    Hostile
}

public enum EnemyState
{
    Idle,
    Chasing,
    Escaping,
    Attacking,
    Hit,
    Wandering,
    Dying,
    Dead,
    None
}

public class Enemy : MonoBehaviour 
{
    private float m_reactionTime;
    private float m_reactionDuration;
    private const float HOSTILE_ON_SIGHT_DURATION = 15.0f;
    private const float HOSTILE_ON_HIT_DURATION = 25.0f;

    private EnemyPersonality m_personality; // Immutable!
    
    private EnemyReaction m_currentReaction; // Dependent on environment

    private EnemyState m_state;

    public float m_recoil = 0.2f;

    public float m_sightRadius = 6.0f;

    public LayerMask m_visibilityMask;

    public AudioClip m_deathSound;
    public AudioClip m_hitSound;

    [SerializeField]
    private float m_maxSpeed = 1.0f;

    public float Orientation
    {
        get
        {
            float angle = Mathf.Atan2(m_currentOrientation.y, m_currentOrientation.x);
            return Mathf.Rad2Deg * angle;
        }
    }


    private Rigidbody2D m_body;
    private SpriteRenderer m_renderer;
    private AudioSource m_audioSource;

    // Physics vars
    private Vector2 m_velocityTarget;
    private Vector2 m_orientationTarget;
    private Vector2 m_currentOrientation;

    private PlayerControl m_chasePC;
    private NPC m_chaseNPC;

    private Vector2 m_wanderTarget;

    private CharacterLife m_lifeData;

    private const float MIN_DECISION_TIME = 0.8f;
    private const float MAX_DECISION_TIME = 1.5f;

    private float m_lastDecisionTime;
    private float m_nextDecisionTime;

    void Awake ()
    {
        m_renderer = GetComponent<SpriteRenderer>();
        m_body = GetComponent<Rigidbody2D>();
        m_audioSource = GetComponent<AudioSource>();

        m_personality = EnemyPersonality.Cautious;
        m_currentReaction = EnemyReaction.Neutral;
        m_state = EnemyState.None;

        m_lifeData = new CharacterLife();
    }

	// Use this for initialization
	void Start () 
    {
        m_currentReaction = EnemyReaction.Neutral;
        m_state = EnemyState.Wandering;
	}

    void FixedUpdate()
    {
        if (m_state == EnemyState.Dying || m_body.isKinematic)
        {
            m_velocityTarget = Vector2.zero;
        }
        m_body.velocity = m_velocityTarget;
    }
	
	// Update is called once per frame
	void Update () 
    {
        if (GameplayManager.Instance.paused)
        {
            return;
        }

        if (m_state == EnemyState.Dying)
        {
            return;
        }

        UpdateReaction();


        switch (m_state)
        {
            case EnemyState.Wandering:
                {
                    if (m_currentReaction == EnemyReaction.Hostile)
                    {
                        List<GameObject> targets = GameplayManager.Instance.GetTargetsOnSight(this);
                        if (targets.Count > 0)
                        {
                            m_state = EnemyState.Chasing;
                            m_reactionTime = -1.0f;
                            m_chasePC = targets[0].GetComponent<PlayerControl>();
                            m_chaseNPC = targets[0].GetComponent<NPC>();
                        }
                        else
                        {
                            if (Time.time - m_lastDecisionTime >= m_nextDecisionTime)
                            {
                                ResetWanderTarget();
                            }
                            m_velocityTarget = m_wanderTarget - (Vector2)transform.position;
                            m_velocityTarget.Normalize();
                            m_velocityTarget *= m_maxSpeed;
                        }
                    }
                    else
                    {
                        if (Time.time - m_lastDecisionTime >= m_nextDecisionTime)
                        {
                            ResetWanderTarget();
                        }
                        m_velocityTarget = m_wanderTarget - (Vector2)transform.position;
                        m_velocityTarget.Normalize();
                        m_velocityTarget *= m_maxSpeed;
                    }
                    break;
                }
            case EnemyState.Chasing:
                {
                    Vector2 chaseTarget = transform.position;
                    if (m_chaseNPC == null || !CanSee(m_chaseNPC))
                    {
                        if (m_chasePC != null && CanSee(m_chasePC))
                        {
                            chaseTarget = m_chasePC.transform.position;
                        }
                        else
                        {
                            List<GameObject> targets = GameplayManager.Instance.GetTargetsOnSight(this);
                            if (targets.Count > 0)
                            {
                                m_chasePC = targets[0].GetComponent<PlayerControl>();
                                m_chaseNPC = targets[0].GetComponent<NPC>();
                            }
                            else
                            {
                                m_state = EnemyState.Wandering;
                                ResetWanderTarget();
                                // Start reaction timers
                                m_reactionTime = Time.time;
                            }
                        }
                    }
                    else
                    {
                        chaseTarget = m_chaseNPC.transform.position;
                    }

                    m_velocityTarget = chaseTarget - (Vector2)transform.position;
                }
                break;
            default: break;
        }
	
	}
    private void UpdateReaction()
    {
        if (m_reactionTime >= 0 && Time.time - m_reactionTime > m_reactionDuration)
        {
            switch(m_currentReaction)
            {
                case EnemyReaction.Neutral:
                    {
                        break;
                    }
                case EnemyReaction.Hostile:
                    {
                        m_reactionTime = -1.0f;
                        Debug.LogFormat("{0} goes back to neutral", name);
                        m_currentReaction = EnemyReaction.Neutral;
                        m_state = EnemyState.Wandering;
                        m_chaseNPC = null;
                        m_chasePC = null;
                        ResetWanderTarget();
                        break;
                    }
                case EnemyReaction.Friendly:
                    {
                        break;
                    }
            }
        }
    }

    public void Initialize (EnemyPersonality personality, int maxHP, float maxSpeed)
    {
        m_personality = personality;
        m_maxSpeed = maxSpeed;
        m_lifeData.Initialise(maxHP);

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; ++i)
        {
            colliders[i].enabled = true;
        }

        m_currentReaction = EnemyReaction.Neutral;
        m_state = EnemyState.Wandering;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D otherBody = collision.collider.GetComponent<Rigidbody2D>();        
        if (otherBody != null)
        {
            m_velocityTarget = Vector2.zero;                 
            m_body.isKinematic = true;
        }

        if (m_state == EnemyState.Wandering)
        {
            ResetWanderTarget();
        }
    }

    public void OnCollisionExit2D(Collision2D collision)
    {
        m_body.isKinematic = false;
    }

    

    private void ResetWanderTarget()
    {
        Camera mainCam = Camera.main;
        float vSize = mainCam.orthographicSize;
        float hSize = mainCam.orthographicSize * mainCam.aspect;

        m_wanderTarget = new Vector2(Random.Range(-hSize, hSize), Random.Range(-vSize, vSize));
        m_nextDecisionTime = Random.Range(MIN_DECISION_TIME, MAX_DECISION_TIME);
        m_lastDecisionTime = Time.time;
    }
    

    public void OnHit (Weapon w, Vector3 direction)
    {
        if (m_state == EnemyState.Dying) { return;  }
        bool died = m_lifeData.UpdateHP(-w.m_damage);
        transform.Translate(direction * m_recoil);
        if (died)
        {
            m_audioSource.PlayOneShot(m_deathSound);
            StartCoroutine(DieInSeconds(1.0f));
        }
        else
        {
            m_audioSource.PlayOneShot(m_hitSound);
            m_currentReaction = EnemyReaction.Hostile;

            List<GameObject> objects = GameplayManager.Instance.GetTargetsOnSight(this);
            if (objects.Count > 0)
            {
                m_chasePC = objects[0].GetComponent<PlayerControl>();
                m_chaseNPC = objects[0].GetComponent<NPC>();
                m_reactionTime = -1.0f;
            }
            else
            {
                m_state = EnemyState.Wandering;
                ResetWanderTarget();
                m_reactionTime = Time.time;
            }

            m_reactionDuration = HOSTILE_ON_HIT_DURATION;
            Debug.LogFormat("{0} goes hostile upon being hit", name);
            GameplayManager.Instance.OnEnemyHit(this);
        }
    }

    public IEnumerator DieInSeconds(float length)
    {
        m_state = EnemyState.Dying;
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; ++i)
        {
            colliders[i].enabled = false;
        }

        float t = Time.time;        
        float elapsed = Time.time - t;
        Color c;

        while (elapsed < length)
        {
            c = m_renderer.color;
            c.a = Mathf.Lerp(1.0f, 0.0f, elapsed / length);
            m_renderer.color = c;
            yield return null;
            elapsed = Time.time - t;
        }

        GameplayManager.Instance.RemoveEnemy(this);

        c = m_renderer.color;
        c.a = 1.0f;
        m_renderer.color = c;

        gameObject.SetActive(false);
    }

    public bool CanHitCharacter(PlayerControl go)
    {
        return m_currentReaction == EnemyReaction.Hostile;
    }

    public bool CanHitCharacter(Enemy go)
    {
        return false;
    }

    public bool CanHitCharacter(NPC go)
    {
        return m_currentReaction == EnemyReaction.Hostile;
    }

    public bool CanSee(Enemy e)
    {
        return CanSeeEntity<Enemy>(e);
    }

    private bool CanSeeEntity<T>(T entity) where T:MonoBehaviour
    {
        Vector2 direction = entity.transform.position - transform.position;
        RaycastHit2D[] info = Physics2D.RaycastAll(transform.position, direction.normalized, m_sightRadius, m_visibilityMask);
        for (int i = 0; i < info.Length; ++i)
        {
            if (info[i].collider != null)
            {
                T spotted = info[i].collider.GetComponent<T>();
                if (spotted == null)
                {
                    spotted = info[i].collider.GetComponentInParent<T>();
                }
                if (spotted == entity) return true;
            }
        }
        return false;
    }

    public bool CanSee(PlayerControl pc)
    {
        return CanSeeEntity<PlayerControl>(pc);
    }
    
    public bool CanSee(NPC npc)
    {
        return CanSeeEntity<NPC>(npc);
    }

    public void OnSawEnemyHit(Enemy e)
    {
        if (GameplayManager.Instance.paused) { return; }
        if (m_state == EnemyState.Dead) { return; }

        m_currentReaction = EnemyReaction.Hostile;
        List<GameObject> objects = GameplayManager.Instance.GetTargetsOnSight(this);
        if (objects.Count > 0)
        {
            m_chasePC = objects[0].GetComponent<PlayerControl>();
            m_chaseNPC = objects[0].GetComponent<NPC>();
            m_reactionTime = -1.0f;
        }
        else
        {
            m_state = EnemyState.Wandering;
            ResetWanderTarget();
            m_reactionTime = Time.time;
        }       
        m_reactionDuration = HOSTILE_ON_SIGHT_DURATION;
        Debug.LogFormat("{0} turned hostile after seeing another enemy hit", name, e.name);
    }

    public void OnSawHostile(PlayerControl pc)
    {

    }

    public void OnSawHostile(NPC npc)
    {

    }
}
