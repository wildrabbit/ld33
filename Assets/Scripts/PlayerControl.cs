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

    private const int COLLISION_TYPE_HORIZONTAL = 0;
    private const int COLLISION_TYPE_VERTICAL = 1;

    public float m_maxSpeed = 1.0f;
    public float m_aimSpeed = 1.0f;
    public float m_range = 2.0f;
    public float m_errorChance = 0.1f;

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


#region unity_methods
    void Awake()
    {
        m_body = GetComponent<Rigidbody2D>();
        m_renderer = GetComponent<SpriteRenderer>();

        m_moveState = MovementState.None;
        m_nextMove = MovementState.None;
        m_actionState = ActionState.None;
        m_nextAction = ActionState.None;
    }

	// Use this for initialization
	void Start () 
    {
        m_moveState = MovementState.Idle;
        EnterState();
        m_actionState = ActionState.Idle;
        EnterActionState();

        m_movementInput = m_velocityTarget = m_aimingInput = m_aimingTarget = m_currentAiming = Vector2.zero;
	}

    // Update is called once per frame
    void Update()
    {
        if (Game.paused) return;

        FetchInput();
        UpdateMove();
        if (m_nextMove != MovementState.None && m_nextMove != m_moveState)
        {
            if (m_moveState != MovementState.None)
            {
                ExitState();
            }
            m_moveState = m_nextMove;
            EnterState();
            m_nextMove = MovementState.None;
        }

        UpdateAction();
        if (m_nextAction != ActionState.None && m_nextAction != m_actionState)
        {
            if (m_actionState != ActionState.None)
            {
                ExitActionState();
            }
            m_actionState = m_nextAction;
            EnterActionState();
            m_nextAction = ActionState.None;
        }

   }

    void FixedUpdate()
    {
        m_body.velocity = m_velocityTarget;

        if (m_aimingTarget != m_currentAiming)
        {
            m_currentAiming = m_aimingTarget;
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

        // Begin with gamepad controls
        m_movementInput.Set(Input.GetAxis("HorizontalPad"), Input.GetAxis("VerticalPad"));
        // Discard dead zones
        if (m_movementInput.magnitude < DEADZONE_RADIUS)
        {
            m_movementInput.Set(0.0f, 0.0f);
        }
        else
        {
            m_movementInput.Normalize();
        }

        m_aimingInput.Set(Input.GetAxis("AimHorizontal"), Input.GetAxis("AimVertical"));
        if (m_aimingInput.magnitude < DEADZONE_RADIUS)
        {
            m_aimingInput.Set(0.0f, 0.0f);
        }

        m_wasUsingGamepad = m_movementInput != Vector2.zero || m_aimingInput != Vector2.zero ||  shootPressed || actionPressed;

        if (!m_wasUsingGamepad)
        {
            m_movementInput.Set(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));            

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            m_aimingInput = mousePos - (Vector2)transform.position;
            m_aimingInput.Normalize();

            shootPressed = Input.GetButton("Shoot");
            actionPressed = Input.GetButton("Action");
        }
        

        m_shootWasPressed = m_shootPressed;
        m_shootPressed = shootPressed;
        m_actionWasPressed= m_actionPressed;
        m_actionPressed= actionPressed;
    }
}
