using UnityEngine;
using System.Collections;

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
    private EnemyPersonality m_personality; // Immutable!
    
    private EnemyReaction m_currentReaction; // Dependent on environment

    private EnemyState m_state;

    public float m_recoil = 0.2f;

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

    // Physics vars
    private Vector2 m_velocityTarget;
    private Vector2 m_orientationTarget;
    private Vector2 m_currentOrientation;
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

        m_personality = EnemyPersonality.Cautious;
        m_currentReaction = EnemyReaction.Neutral;
        m_state = EnemyState.None;

        m_lifeData = new CharacterLife();
    }

	// Use this for initialization
	void Start () 
    {
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
                    if (Time.time - m_lastDecisionTime >= m_nextDecisionTime)
                    {
                        ResetWanderTarget();
                    }
                    m_velocityTarget = m_wanderTarget - (Vector2) transform.position;
                    m_velocityTarget.Normalize();
                    m_velocityTarget *= m_maxSpeed;
                    break;
                }
        }
	
	}
    private void UpdateReaction()
    {

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
            StartCoroutine(DieInSeconds(1.0f));
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
        return true;
    }

    public bool CanHitCharacter(Enemy go)
    {
        return false;
    }

    public bool CanHitCharacter(NPC go)
    {
        return false;
    }
}
