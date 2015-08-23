using UnityEngine;
using System.Collections;

public enum MovementState
{
    None = -1,
    Idle = 0,
    Walking,
    Hit,
    Death
}

public enum ActionState
{
    None = -1,
    Idle = 0,
    Shooting
}

public class PlayerControl : MonoBehaviour 
{
    private const float DEADZONE_RADIUS = 0.1f;
    private const float DEADZONE_TRIGGER = -0.3f;

    public float m_maxSpeed = 1.0f;
    public float m_aimSpeed = 1.0f;
    public float m_range = 2.0f;
    public float m_errorChance = 0.1f;
    public float m_recoil = 0.2f;
    public float m_hitTime = 0.5f;

    public float m_shootCooldown = 0.4f;
    private float m_lastShoot;
    
    public int m_startHP = 10;

    public LayerMask m_characterLayer;
    public LayerMask m_laserVisibility;
    public Transform m_laser;

    private CharacterLife m_lifeData;

    public float Range
    {
        get { return m_effectiveRange; }
    }

    public int SortingOrder
    {
        get { return m_renderer.sortingOrder; }
    }

    public float Orientation
    {
        get 
        {
            float angle = Mathf.Atan2(m_currentAiming.y, m_currentAiming.x);
            return Mathf.Rad2Deg * angle;
        }
    }

    private Rigidbody2D m_body;
    private SpriteRenderer m_renderer;
    private Weapon m_weapon;
    private AudioSource m_audioSource;

    // Input variables
    private bool m_wasUsingGamepad;
    private Vector2 m_movementInput;
    private Vector2 m_aimingInput;
    private bool m_shootWasPressed;
    private bool m_shootPressed;
    private bool m_actionWasPressed;
    private bool m_actionPressed;

    // FSM vars
    private float m_lastHit;
    private MovementState m_oldState;
    private MovementState m_moveState;

    // Physics vars
    private Vector2 m_velocityTarget;
    private Vector2 m_currentAiming;
    private Vector2 m_aimingTarget;

    private float m_effectiveRange;


#region unity_methods
    void Awake()
    {
        m_body = GetComponent<Rigidbody2D>();
        m_renderer = GetComponent<SpriteRenderer>();
        m_weapon = GetComponentInChildren<Weapon>();
        m_audioSource = GetComponent<AudioSource>();

        m_moveState = MovementState.None;
        m_oldState = MovementState.None;

        m_lifeData = new CharacterLife();
        m_lastHit = 0.0f;
    }

	// Use this for initialization
	void Start () 
    {
        m_moveState = MovementState.Idle;
        EnterState();

        m_lifeData.Initialise(m_startHP);
        m_movementInput = m_velocityTarget = m_aimingInput = m_aimingTarget = m_currentAiming = Vector2.zero;

        m_lastShoot = -1.0f;

        m_shootPressed = m_actionPressed = m_shootWasPressed = m_actionWasPressed = false;
	}

    // Update is called once per frame
    void Update()
    {
        if (GameplayManager.Instance.paused) return;

        FetchInput();
        UpdateMove();
        //if (m_nextMove != MovementState.None && m_nextMove != m_moveState)
        //{
        //    if (m_moveState != MovementState.None)
        //    {
        //        ExitState();
        //    }
        //    m_moveState = m_nextMove;
        //    EnterState();
        //    m_nextMove = MovementState.None;
        //}

        //UpdateAction();
        //if (m_nextAction != ActionState.None && m_nextAction != m_actionState)
        //{
        //    if (m_actionState != ActionState.None)
        //    {
        //        ExitActionState();
        //    }
        //    m_actionState = m_nextAction;
        //    EnterActionState();
        //    m_nextAction = ActionState.None;
        //}
   }

    void FixedUpdate()
    {
        if (m_moveState == MovementState.Death)
        {
            return;
        }

        if (m_body.isKinematic)
        {
            m_body.velocity = Vector2.zero;                 
        }
        else m_body.velocity = m_velocityTarget;

        if (m_aimingTarget != m_currentAiming)
        {
            m_currentAiming = m_aimingTarget;
        }

        LayerMask visibility = m_laserVisibility;
        if (m_weapon != null && !m_weapon.m_piercing)
        {
            visibility.value = visibility.value | (1 << LayerMask.NameToLayer("Characters"));
        }

        RaycastHit2D info = Physics2D.Raycast(m_laser.position, m_currentAiming, m_range, visibility);
        if (info.collider != null)
        {
            m_effectiveRange = info.distance;
        }
        else
        {
            m_effectiveRange = m_range;
        }
    }
#endregion
    //---------------
#region FSM
    private void EnterActionState()
    {
    }

    private void EnterState()
    {

    }
	


    private void ExitActionState()
    {
    }

    private void UpdateAction()
    {
    }

    private void ExitState()
    {
    }

    private void UpdateMove()
    {
        if (m_moveState == MovementState.Death)
        {
            return;
        }

        if (m_moveState == MovementState.Hit)
        {
            if (Time.time - m_lastHit >= m_hitTime)
            {
                Color c = m_renderer.color;
                c.a = 1.0f;
                m_renderer.color = c;
                
                if (m_oldState!= MovementState.None)
                {
                    m_moveState = m_oldState;
                }
                else
                {
                    m_moveState = MovementState.Idle;
                }
                m_oldState = MovementState.None;
            }
        }

        m_velocityTarget = Vector2.zero;
        if (m_movementInput != Vector2.zero)
        {
            m_velocityTarget = m_movementInput * m_maxSpeed;
        }

        m_aimingTarget = m_aimingInput;

        if (m_lastShoot < 0 || Time.time - m_lastShoot >= m_shootCooldown)
        {
            m_lastShoot = -1.0f;
            if (m_shootPressed)
            {
                Shoot();
            }            
        }

        m_weapon.SetLaserState(m_lastShoot < 0 || Time.time - m_lastShoot >= m_shootCooldown);

        if (m_actionWasPressed && !m_actionPressed)
        {
            GameplayManager.Instance.OnPlayerAction();
        }
    }

    void Shoot ()
    {
        RaycastHit2D info = Physics2D.Raycast(m_laser.position, m_currentAiming, m_effectiveRange, m_characterLayer);
        CameraShake cs = Camera.main.GetComponent<CameraShake>();
        if (info.collider != null)
        {
            Enemy e = info.collider.GetComponentInParent<Enemy>();
            if (e != null)
            {
                e.OnHit(m_weapon, m_currentAiming);
            }

            NPC n = info.collider.GetComponentInParent<NPC>();
            if (n != null)
            {
                n.OnHit(m_weapon, m_currentAiming);
            }
        }
        transform.Translate(m_currentAiming * -m_recoil);
        if (cs != null)
        {
            cs.StartShakeWithDuration(0.1f);
        }
        m_audioSource.Play();
        m_lastShoot = Time.time;

    }
#endregion
    //---------------
    void FetchInput()
    {
        bool shootPressed = false;
        bool actionPressed = false;

        //// Begin with gamepad controls
        //m_movementInput.Set(Input.GetAxis("HorizontalPad"), Input.GetAxis("VerticalPad"));
        //// Discard dead zones
        //if (m_movementInput.magnitude < DEADZONE_RADIUS)
        //{
        //    m_movementInput.Set(0.0f, 0.0f);
        //}
        //else
        //{
        //    m_movementInput.Normalize();
        //}

        //Vector2 aim = new Vector2(Input.GetAxis("AimHorizontal"), Input.GetAxis("AimVertical"));
        //if (aim.magnitude >= DEADZONE_RADIUS)
        //{
        //    m_aimingInput.Set(aim.x, aim.y);
        //}
        
        //Debug.LogFormat("Trigger:  {0}", Input.GetAxis("ShootPad"));
        //shootPressed = Input.GetAxis("ShootPad") > 0;
        //actionPressed = Input.GetButton("ActionPad");

        //if (m_movementInput != Vector2.zero || m_aimingInput != Vector2.zero ||  shootPressed || actionPressed)
        //{
        //    m_wasUsingGamepad = true;
        //}
        //else
        {
            m_movementInput.Set(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            shootPressed = Input.GetButton("Shoot");
            actionPressed = Input.GetButton("Action");

            if (m_movementInput != Vector2.zero || shootPressed || actionPressed)
            {
                m_wasUsingGamepad = false;
            }
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (!m_wasUsingGamepad)
            {
                m_aimingInput = mousePos - (Vector2)transform.position;
                m_aimingInput.Normalize();
            }            
        }
        
        

        m_shootWasPressed = m_shootPressed;
        m_shootPressed = shootPressed;
        m_actionWasPressed= m_actionPressed;
        m_actionPressed= actionPressed;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (m_moveState == MovementState.Hit || m_moveState == MovementState.Death)
        {
            return;
        }

        bool willCauseDamage = false;
        Enemy e = collision.collider.GetComponent<Enemy>();
        if (e != null)
        {
            willCauseDamage = e.CanHitCharacter(this);
        }

        NPC n = collision.collider.GetComponent<NPC>();
        if (n != null)
        {
            willCauseDamage = n.CanHitCharacter(this);
        }


        if (!willCauseDamage) { return; }
        bool dead = m_lifeData.UpdateHP(-1);
        if (dead)
        {
            StartCoroutine(DieInSeconds(1.0f));
        }
        else
        {
            m_moveState = MovementState.Hit;
            m_oldState = m_moveState;
            m_lastHit = Time.time;
            Color c = m_renderer.color;
            c.a = 0.5f;
            m_renderer.color = c;
        }
        m_body.isKinematic = true;
    }

    public IEnumerator DieInSeconds(float length)
    {
        m_moveState = MovementState.Death;
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

        m_body.isKinematic = true;
        m_body.velocity = Vector2.zero;
        GameplayManager.Instance.OnPlayerDied();

        c = m_renderer.color;
        c.a = 1.0f;
        m_renderer.color = c;

        gameObject.SetActive(false);
    }

    public void OnCollisionExit2D(Collision2D collision)
    {
        m_body.isKinematic = false;
    }


}
