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

public class Enemy : Entity 
{
    private float m_reactionTime;
    private float m_reactionDuration;
    public float m_hostileOnSightDuration = 4.0f;
    public float m_hostileOnHitDuration = 8.0f;

    public Sprite m_neutralSprite;
    public Sprite m_hostileSprite;

    public Color m_neutralColour;
    public Color m_hostileColour;

    public bool IsHostile
    {
        get { return m_currentReaction == EnemyReaction.Hostile; }
    }

    private EnemyPersonality m_personality; // Immutable!
    
    private EnemyReaction m_currentReaction; // Dependent on environment

    private EnemyState m_state;

    private static System.Type[] s_excludedTargetTypes = new System.Type[] { typeof(Enemy) };
    
    override protected void Awake ()
    {
        base.Awake();
        m_personality = EnemyPersonality.Cautious;
        m_currentReaction = EnemyReaction.Neutral;
        m_state = EnemyState.None;
    }
    public void ChangeReaction(EnemyReaction reaction)
    {
        m_currentReaction = reaction;
        Color newC = m_currentReaction == EnemyReaction.Neutral ? m_neutralColour : m_hostileColour;
        newC.a = m_renderer.color.a;
        m_renderer.color = newC;

        m_renderer.sprite = m_currentReaction == EnemyReaction.Neutral ? m_neutralSprite : m_hostileSprite;
    }

    override protected void Start()
    {
        base.Start();
        SetPersonality(EnemyPersonality.Cautious);
    }

    override public void Initialize(int maxHP, float maxSpeed)
    {
        base.Initialize(maxHP, maxSpeed);
        GameplayManager.Instance.AddEnemy(this);
        ChangeReaction(EnemyReaction.Neutral);
        
        m_state = EnemyState.Wandering;
        ResetWanderTarget();

        m_reactionDuration = m_reactionTime = 0.0f;
    }

    public void SetPersonality (EnemyPersonality personality)
    {
        m_personality = personality;
        // ChangePersonality stuff
    }

	// Update is called once per frame
    override protected void Update()
    {
        base.Update();
        
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
                        List<Entity> targets = GameplayManager.Instance.GetTargetsOnSight(this, s_excludedTargetTypes);
                        if (targets.Count > 0)
                        {
                            m_state = EnemyState.Chasing;
                            m_reactionTime = -1.0f;
                            m_targetEntity = targets[0];
                        }
                        else
                        {
                            if (Time.time - m_lastWanderDecisionTime >= m_nextWanderDecisionTime)
                            {
                                ResetWanderTarget();
                            }
                            m_velocityTarget = m_wanderPosition - (Vector2)transform.position;
                            m_velocityTarget.Normalize();
                            m_velocityTarget *= m_maxSpeed;
                        }
                    }
                    else
                    {
                        if (Time.time - m_lastWanderDecisionTime >= m_nextWanderDecisionTime)
                        {
                            ResetWanderTarget();
                        }
                        m_velocityTarget = m_wanderPosition - (Vector2)transform.position;
                        m_velocityTarget.Normalize();
                        m_velocityTarget *= m_maxSpeed;
                    }
                    break;
                }
            case EnemyState.Chasing:
            {
                if (m_targetEntity != null && CanSee(m_targetEntity))
                {
                    m_velocityTarget = (Vector2)(m_targetEntity.transform.position - transform.position);
                }
                else
                {
                    List<Entity> targets = GameplayManager.Instance.GetTargetsOnSight(this, s_excludedTargetTypes);
                    if (targets.Count > 0)
                    {
                        m_targetEntity = targets[0];
                        m_velocityTarget = (Vector2)(m_targetEntity.transform.position - transform.position);
                    }
                    else
                    {
                        m_state = EnemyState.Wandering;
                        ResetWanderTarget();
                        m_targetEntity = null;
                        // Start reaction timers
                        m_reactionTime = Time.time;                        
                    }
                }
                m_velocityTarget.Normalize();
                m_velocityTarget *= m_maxSpeed * 1.1f;
                break;
            }
            case EnemyState.Hit:
            {
                UpdateHit();
                break;
            }
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
                        ChangeReaction(EnemyReaction.Neutral);
                        m_state = EnemyState.Wandering;
                        m_targetEntity = null;
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

    override public void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        if (m_state == EnemyState.Wandering)
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

    public override void OnHitFinished()
    {
        base.OnHitFinished();
        
        Color c = m_renderer.color;
        c.a = 1.0f;
        m_renderer.color = c;
        ChangeReaction(EnemyReaction.Hostile);
        m_currentReaction = EnemyReaction.Hostile;

        List<Entity> objects = GameplayManager.Instance.GetTargetsOnSight(this, s_excludedTargetTypes);
        if (objects.Count > 0)
        {
            m_state = EnemyState.Chasing;
            m_targetEntity = objects[0];
            m_reactionTime = -1.0f;
        }
        else
        {
            m_state = EnemyState.Wandering;
            ResetWanderTarget();
            m_reactionTime = Time.time;
        }

        m_reactionDuration = m_hostileOnHitDuration;
        Debug.LogFormat("{0} goes hostile upon being hit", name);
        GameplayManager.Instance.OnEnemyWasHit(this);
        
    }

    protected override bool HitReaction()
    {
        m_state = EnemyState.Hit;

        Color c = m_renderer.color;
        c.a = 0.5f;
        m_renderer.color = c;
        return true;
    }

    public void OnSawEnemyHit(Enemy e)
    {
        if (GameplayManager.Instance.paused) { return; }
        if (m_state == EnemyState.Dead) { return; }

        ChangeReaction(EnemyReaction.Hostile);
        List<Entity> objects = GameplayManager.Instance.GetTargetsOnSight(this, s_excludedTargetTypes);
        if (objects.Count > 0)
        {
            m_targetEntity = objects[0];
            m_reactionTime = -1.0f;
        }
        else
        {
            m_state = EnemyState.Wandering;
            ResetWanderTarget();
            m_reactionTime = Time.time;
        }       
        m_reactionDuration = m_hostileOnSightDuration;
        Debug.LogFormat("{0} turned hostile after seeing another enemy hit", name, e.name);
    }

    public void OnSawEnemyDie(Enemy e)
    {
        if (GameplayManager.Instance.paused) { return; }
        if (m_state == EnemyState.Dead) { return; }

        ChangeReaction(EnemyReaction.Hostile);
        List<Entity> objects = GameplayManager.Instance.GetTargetsOnSight(this, s_excludedTargetTypes);
        if (objects.Count > 0)
        {
            m_targetEntity = objects[0];
            m_reactionTime = -1.0f;
        }
        else
        {
            m_state = EnemyState.Wandering;
            ResetWanderTarget();
            m_reactionTime = Time.time;
        }
        m_reactionDuration = m_hostileOnSightDuration;
        Debug.LogFormat("{0} turned hostile after seeing another enemy die", name, e.name);
    }

    public override bool IsEthereal()
    {
        return m_state == EnemyState.Dying || m_state == EnemyState.Dead || m_state == EnemyState.Hit;
    }

    public override bool CanBumpEntity(Entity e)
    {
        return m_currentReaction == EnemyReaction.Hostile && (e is PlayerControl || e is NPC);
    }

    public override bool CanShootEntity(Entity e)
    {
        return false;
    }

    protected override void OnDied()
    {
        GameplayManager.Instance.RemoveEnemy(this);
        GameplayManager.Instance.OnEnemyDied(this);
    }

    protected override void SetDying()
    {
        m_state = EnemyState.Dying;
    }

    public override bool IsDying()
    {
        return m_state == EnemyState.Dying;
    }
}
