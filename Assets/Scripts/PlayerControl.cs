using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PlayerState
{
    None = -1,
    Idle = 0,
    Walking,
    Hit,
    Death
}

public class PlayerControl : Entity 
{
    private PlayerInput m_input;

    // FSM vars
    private PlayerState m_oldState;
    private PlayerState m_state;

    override protected void Awake()
    {
        base.Awake();
        m_input = new PlayerInput();
    }

    override public void Initialize(int maxHP, float maxSpeed)
    {
        base.Initialize(maxHP, maxSpeed);
        m_input.Reset();
        GameplayManager.Instance.AddPlayer(this);
        m_state = PlayerState.Idle;
    }

    // Update is called once per frame
    override protected void Update()
    {
        base.Update();
        FetchInput();
        UpdateMove();
   }

    override public bool IsDying()
    {
        return m_state == PlayerState.Death;
    }

    override protected void SetDying()
    {
        m_state = PlayerState.Death;
    }

    public override void OnHitFinished()
    {
        base.OnHitFinished();

        Color c = m_renderer.color;
        c.a = 1.0f;
        m_renderer.color = c;

        if (m_oldState != PlayerState.None)
        {
            m_state = m_oldState;
        }
        else
        {
            m_state = PlayerState.Idle;
        }
        m_oldState = PlayerState.None;
    }

    public override bool CanShoot()
    {
        return base.CanShoot();
    }

    private void UpdateMove()
    {
        if (m_state == PlayerState.Death)
        {
            return;
        }

        if (m_state == PlayerState.Hit)
        {
            UpdateHit();
        }

        m_velocityTarget = Vector2.zero;
        if (m_input.m_movementInput  != Vector2.zero)
        {
            m_velocityTarget = m_input.m_movementInput * m_maxSpeed;
        }

        m_orientationTarget = (m_input.m_aimingInput - (Vector2)transform.position).normalized;

        if (m_lastShoot < 0 || Time.time - m_lastShoot >= m_shootCooldown)
        {
            m_lastShoot = -1.0f;
        }
        
        if (m_input.m_shootPressed && CanShoot())
        {
            Shoot();
        }            


        if (m_weapon != null)
        {
        }

        if (m_input.m_actionWasPressed && !m_input.m_actionPressed)
        {
            GameplayManager.Instance.OnPlayerAction();
        }
    }

    override public void Shoot ()
    {
        base.Shoot();
        CameraShake cs = Camera.main.GetComponent<CameraShake>();
        if (cs != null)
        {
            cs.StartShakeWithDuration(0.1f);
        }
    }
    //---------------
    void FetchInput()
    {
        m_input.Read();
    }

    public override bool IsEthereal()
    {
        return m_state == PlayerState.Hit || m_state == PlayerState.Death;
    }

    override protected bool HitReaction(Entity attacker)
    {
        if (m_state == PlayerState.Hit)
        {
            Debug.LogFormat("Already hit!!");
            return false;
        }
        else
        {
            m_state = PlayerState.Hit;
            attacker.HitLanded();
            m_oldState = m_state;

            Color c = m_renderer.color;
            c.a = 0.5f;
            m_renderer.color = c;
            return true;
        }
    }

    override public bool CanShootEntity(Entity e)
    {
        return e != this;
    }

    override public bool CanBumpEntity(Entity e)
    {
        return false;
    }

    protected override void OnDied()
    {
        GameplayManager.Instance.RemovePlayer(this);
        GameplayManager.Instance.OnPlayerDied();
    }
}
