using UnityEngine;
using System.Collections;

public enum NPCPersonality
{
    Cautious,
    Aggresive,
    Peaceful
}

public enum NPCReaction
{
    Friendly,
    Neutral,
    Hostile
}

public enum NPCState
{
    Idle,
    Chasing,
    Escaping,
    Attacking,
    Hit,
    Talking,
    Wandering,
    Dying,
    Dead,
    None
}

public class NPC : MonoBehaviour
{
    private NPCPersonality m_personality; // Immutable!
    
    private NPCReaction m_currentReaction; // Dependent on environment

    private NPCState m_state;

    [SerializeField]
    private float m_maxSpeed = 1.0f;
    
    [SerializeField]
    private float m_defaultHP = 3.0f;

    [SerializeField]
    private float m_talkDistance = 1.0f;
    public float TalkDistance { get { return m_talkDistance; } }

    [SerializeField]
    private string m_message = "We're doomed!!";

    public float Orientation
    {
        get
        {
            float angle = Mathf.Atan2(m_currentOrientation.y, m_currentOrientation.x);
            return Mathf.Rad2Deg * angle;
        }
    }

    public bool CanTalk
    {
        get
        {
            return m_state != NPCState.Dying && m_state != NPCState.None;
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

        m_personality = NPCPersonality.Cautious;
        m_currentReaction = NPCReaction.Neutral;
        m_state = NPCState.None;

        m_lifeData = new CharacterLife();
    }

	// Use this for initialization
	void Start () 
    {
        m_state = NPCState.Wandering;
        GameplayManager.Instance.AddNPC(this);
	}

    void FixedUpdate()
    {
        if (m_state == NPCState.Dying || m_body.isKinematic)
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

        if (m_state == NPCState.Dying)
        {
            return;
        }

        UpdateReaction();


        switch (m_state)
        {
            case NPCState.Wandering:
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

    public void Initialize (NPCPersonality personality, int maxHP, float maxSpeed)
    {
        m_personality = personality;
        m_maxSpeed = maxSpeed;
        m_lifeData.Initialise(maxHP);

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; ++i)
        {
            colliders[i].enabled = true;
        }

        m_state = NPCState.Wandering;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D otherBody = collision.collider.GetComponent<Rigidbody2D>();        
        if (otherBody != null)
        {
            m_velocityTarget = Vector2.zero;                 
            m_body.isKinematic = true;
        }

        if (m_state == NPCState.Wandering)
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
    

    public void OnHit (Weapon w)
    {
        if (m_state == NPCState.Dying) { return;  }
        if (m_lifeData.UpdateHP(-w.m_damage))
        {
            StartCoroutine(DieInSeconds(1.0f));
        }
    }

    public IEnumerator DieInSeconds(float length)
    {
        m_state = NPCState.Dying;
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

        
        c = m_renderer.color;
        c.a = 1.0f;
        m_renderer.color = c;
        GameplayManager.Instance.RemoveNPC(this);
        gameObject.SetActive(false);
    }

    public void OnPlayerAction()
    {
        Debug.Log(m_message);
    }
}
