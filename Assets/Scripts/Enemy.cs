﻿using UnityEngine;
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
    public float m_attackDistance = 1.6f;

    public float m_chaseSpeedMultiplier = 1.1f;

    public float m_attackPreparation = 0.25f;
    public float m_attackPreparationMultiplier = 0.5f;
    public float m_attackMaxDuration = 0.25f;
    public float m_attackSpeedIncrease = 2.5f;
    public float m_attackRecoverDuration = 0.5f;
    
    const int ATTACK_PREPARE = 0;
    const int ATTACK_EXEC = 1;
    const int ATTACK_RECOVER = 2;
    private float m_attackStepTime;
    private float m_attackStepDuration;
    private int m_attackStep;

    public Sprite m_neutralSprite;
    public Sprite m_hostileSprite;

    public Color m_neutralColour;
    public Color m_hostileColour;

    public bool IsHostile
    {
        get { return m_currentReaction == EnemyReaction.Hostile; }
    }

    private bool m_flyingAway;
    private float m_flyingAwayStart;
    private float m_flyingAwayTime;

    private EnemyPersonality m_personality; // Immutable!
    
    private EnemyReaction m_currentReaction; // Dependent on environment

    private EnemyState m_state;

    private static System.Type[] s_excludedTargetTypes = new System.Type[] { typeof(Enemy) };
    public Color m_escapeColour;
    public Sprite m_escapeSprite;
    
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

        m_flyingAway = false;
        m_flyingAwayStart= 0.0f;
        
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
        if (m_flyingAway)
        {
            if (Time.time - m_flyingAwayStart >= m_flyingAwayTime)
            {
                Destroy(gameObject);
            }
            else
            {
                transform.Translate(m_velocityTarget * Time.deltaTime);

                float angle = Mathf.Atan2(m_velocityTarget.y, m_velocityTarget.x) * Mathf.Rad2Deg;
                angle += Random.Range(-10.0f, 10.0f);

                angle *= Mathf.Deg2Rad;
                m_velocityTarget.Set(Mathf.Cos(angle), Mathf.Sin(angle));
                m_velocityTarget *= m_maxSpeed;
            }
            return;
        }

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
                float stateSpeed = m_maxSpeed * m_chaseSpeedMultiplier;
                if (m_targetEntity != null && CanSee(m_targetEntity))
                {
                    m_velocityTarget = (Vector2)(m_targetEntity.transform.position - transform.position);
                    float distanceToTarget = Vector2.Distance(transform.position, m_targetEntity.transform.position);
                    Debug.LogFormat("Distance: {0}", distanceToTarget);
                    if (distanceToTarget <= m_attackDistance)
                    {
                        m_state = EnemyState.Attacking;
                        m_attackStep = ATTACK_PREPARE;
                        m_velocityTarget = -m_velocityTarget;
                        m_attackStepDuration = m_attackPreparation;
                        m_attackStepTime = Time.time;
                    }
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
                m_velocityTarget *= stateSpeed;
                break;
            }
            case EnemyState.Attacking:
            {
                UpdateAttack();
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

    private void UpdateAttack()
    {
        float attackSpeed = m_maxSpeed;
        Vector2 targetVector = (m_targetEntity.transform.position - transform.position);
        if (Time.time - m_attackStepTime >= m_attackStepDuration)
        {
            switch(m_attackStep)
            {
                case ATTACK_PREPARE:
                    {
                        m_attackStep = ATTACK_EXEC;
                        m_attackStepTime = Time.time;
                        m_attackStepDuration = m_attackRecoverDuration;

                        m_velocityTarget = targetVector;
                        attackSpeed *= m_attackSpeedIncrease;
                        break;
                    }
                case ATTACK_EXEC:
                    {
                        m_attackStep = ATTACK_RECOVER;
                        m_attackStepTime = Time.time;
                        m_attackStepDuration = m_attackRecoverDuration;

                        m_velocityTarget = Vector3.zero;
                        attackSpeed = 0.0f;
                        break;
                    }
                case ATTACK_RECOVER:
                    {
                        m_state = EnemyState.Chasing;

                        m_velocityTarget = targetVector;
                        attackSpeed *= m_chaseSpeedMultiplier;
                        break;
                    }
                default: break;
            }
        }
        else
        {
            switch (m_attackStep)
            {
                case ATTACK_PREPARE:
                case ATTACK_RECOVER:
                    {
                        m_velocityTarget = -targetVector;
                        attackSpeed *= m_attackPreparationMultiplier;
                        break;
                    }
                case ATTACK_EXEC:
                    {
                        m_velocityTarget = targetVector;
                        attackSpeed *= m_attackSpeedIncrease;
                        break;
                    }
                default:break;
            }
        }
        m_velocityTarget.Normalize();
        m_velocityTarget *= attackSpeed;
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
        else if (m_state == EnemyState.Attacking)
        {
            m_attackStep = ATTACK_RECOVER;
            m_attackStepTime = Time.time;
            m_attackStepDuration = m_attackRecoverDuration;
        }
        else if (m_state == EnemyState.Chasing)
        {
            m_attackStep = ATTACK_RECOVER;
            m_attackStepTime = Time.time;
            m_attackStepDuration = m_attackRecoverDuration;
            m_state = EnemyState.Attacking;
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

    protected override bool HitReaction(Entity attacker)
    {
        m_state = EnemyState.Hit;

        Color c = m_renderer.color;
        c.a = 0.5f;
        m_renderer.color = c;
        return true;
    }

    override public void HitLanded() 
    {
        if (m_state == EnemyState.Attacking)
        {
            ChangeReaction(EnemyReaction.Neutral);
        }
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



    public void StartFlyAway(float time)
    {
        m_flyingAway = true;
        m_flyingAwayStart = Time.time;
        m_flyingAwayTime = time;
        m_body.isKinematic = true;
        m_mainCollider.enabled = false;
        m_bodyCollider.enabled = false;

        m_renderer.color = m_escapeColour;
        m_renderer.sprite = m_escapeSprite;

        float angle = Random.Range(0.0f, 2 * Mathf.PI);
        m_velocityTarget = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * m_maxSpeed * 1.5f;
    }

    public override string GetDebugLabel()
    {
        return string.Format("{0}_{1}", name, m_state);
    }

}
