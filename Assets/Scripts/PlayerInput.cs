using UnityEngine;
using System.Collections;

public class PlayerInput
{
    private const float DEADZONE_RADIUS = 0.1f;
    private const float DEADZONE_TRIGGER = -0.3f;

    // Input variables
    public bool m_wasUsingGamepad;
    public Vector2 m_movementInput;
    public Vector2 m_aimingInput;
    public bool m_shootWasPressed;
    public bool m_shootPressed;
    public bool m_actionWasPressed;
    public bool m_actionPressed;

    public void Reset()
    {

    }

    public void Read()
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
            if (!m_wasUsingGamepad)
            {
                m_aimingInput = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
        }

        m_shootWasPressed = m_shootPressed;
        m_shootPressed = shootPressed;
        m_actionWasPressed = m_actionPressed;
        m_actionPressed = actionPressed;
    }
}