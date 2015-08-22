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
    private MovementState m_moveState;
    private MovementState m_nextMove;
    private ActionState m_actionState;
    private ActionState m_nextAction;

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
        m_nextMove = MovementState.None;
        m_actionState = ActionState.None;
        m_nextAction = ActionState.None;

        m_lifeData = new CharacterLife();
    }

	// Use this for initialization
	void Start () 
    {
        m_moveState = MovementState.Idle;
        EnterState();
        m_actionState = ActionState.Idle;
        EnterActionState();

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
                RaycastHit2D info = Physics2D.Raycast(m_laser.position, m_currentAiming, m_effectiveRange, m_characterLayer);
                if (info.collider != null)
                {
                    Enemy e = info.collider.GetComponentInParent<Enemy>();
                    if (e != null)
                    {
                        e.OnHit(m_weapon);
                    }

                    NPC n = info.collider.GetComponentInParent<NPC>();
                    if (n != null)
                    {
                        n.OnHit(m_weapon);
                    }
                }
                m_audioSource.Play();
                m_lastShoot = Time.time;
            }            
        }

        m_weapon.SetLaserState(m_lastShoot < 0 || Time.time - m_lastShoot >= m_shootCooldown);

        if (m_actionWasPressed && !m_actionPressed)
        {
            GameplayManager.Instance.OnPlayerAction();
        }
    }

    void ChangeMovementState (MovementState nextState)
    {
        m_nextMove = nextState;
    }

    void ChangeActionState (ActionState actionState)
    {
        m_nextAction = actionState;
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
        Enemy e = collision.collider.GetComponent<Enemy>();
        if (e != null)
        {
            bool dead = m_lifeData.UpdateHP(-1);
            if (dead)
            {
                
            }
            m_body.isKinematic = true;
        }
    }

    public void OnCollisionExit2D(Collision2D collision)
    {
        m_body.isKinematic = false;
    }


}
