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

public class NPC : Entity
{
    //private NPCPersonality m_personality; // Immutable!
    //private NPCReaction m_currentReaction; // Dependent on environment

    private NPCState m_state;

    [SerializeField]
    private float m_talkDistance = 1.0f;
    public float TalkDistance { get { return m_talkDistance; } }

    [SerializeField]
    private string m_message = "We're doomed!!";

    public bool CanTalk
    {
        get
        {
            return m_state != NPCState.Dying && m_state != NPCState.None;
        }
    }

    override protected void Awake()
    {
        base.Awake();
        m_state = NPCState.None;
    }

    override public void Initialize(int maxHP, float maxSpeed)
    {
        base.Initialize(maxHP, maxSpeed);
        m_state = NPCState.Wandering;
        ResetWanderTarget();
        GameplayManager.Instance.AddNPC(this);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }
	
	// Update is called once per frame
	override protected void Update () 
    {
        base.Update();

        if (IsDying()) { return; }

        switch (m_state)
        {
            case NPCState.Wandering:
            {
                if (Time.time - m_lastWanderDecisionTime >= m_nextWanderDecisionTime)
                {
                    ResetWanderTarget();
                }
                m_velocityTarget = m_wanderPosition - (Vector2) transform.position;
                m_velocityTarget.Normalize();
                m_velocityTarget *= m_maxSpeed;
                break;
            }
            case NPCState.Hit:
            {
                UpdateHit();
                break;
            }
            default: break;
        }
	
	}

    override public void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        if (m_state == NPCState.Wandering)
        {
            ResetWanderTarget();
        }
    }

    private void ResetWanderTarget()
    {
        Camera mainCam = Camera.main;
        float vSize = mainCam.orthographicSize;
        float hSize = mainCam.orthographicSize * mainCam.aspect;

        m_wanderPosition = new Vector2(Random.Range(-hSize, hSize), Random.Range(-vSize, vSize));
        m_nextWanderDecisionTime = Random.Range(m_minWanderDecisionTime, m_maxWanderDecisionTime);
        m_lastWanderDecisionTime = Time.time;
    }
    

    public void OnPlayerAction()
    {
        Debug.Log(m_message);
    }

    public override bool IsEthereal()
    {
        return m_state == NPCState.Dead || m_state == NPCState.Dying || m_state == NPCState.Hit;
    }

    public override bool CanBumpEntity(Entity e)
    {
        return e!= null && e is Enemy && ((Enemy)e).IsHostile;
    }

    override public bool CanShootEntity(Entity e)
    {
        System.Type entityType = e.GetType();
        if (entityType == typeof(PlayerControl))
        {
            // return reactions[PlayerControl] == HOSTILE
            return false;
        }
        else if (entityType == typeof(Enemy))
        {
            // return reactions[PlayerControl] == HOSTILE
            return true;
        }
        else if (entityType == typeof(NPC))
        {
            // return reactions[PlayerControl] == HOSTILE
            return false;
        }
        return false;
    }

    protected override void OnDied()
    {
        GameplayManager.Instance.RemoveNPC(this);
    }

    public override void OnHitFinished()
    {
        base.OnHitFinished();
        Color c = m_renderer.color;
        c.a = 1.0f;
        m_renderer.color = c;
        m_state = NPCState.Wandering;
    }

    protected override bool HitReaction()
    {
        m_state = NPCState.Hit;
        
        Color c = m_renderer.color;
        c.a = 0.5f;
        m_renderer.color = c;
        return true;
    }

    protected override void SetDying()
    {
        m_state = NPCState.Dying;
    }

    public override bool IsDying()
    {
        return m_state == NPCState.Dying || m_state == NPCState.Dead;
    }
}
