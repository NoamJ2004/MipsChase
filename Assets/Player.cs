using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // External tunables.
    static public float m_fMaxSpeed = 0.10f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.66f;
    public float m_fIncSpeed = 0.0025f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.06f;
    public float m_fFastRotateSpeed = 0.2f;
    public float m_fFastRotateMax = 10.0f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;

    // Internal variables.
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fAngle;
    public float m_fSpeed;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public eState m_nState;
    public float m_fDiveStartTime;

    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(0,     0,   0),
        new Color(255, 255, 255),
        new Color(0,     0, 255),
        new Color(0,   255,   0),
    };

    public bool IsDiving()
    {
        return (m_nState == eState.kDiving);
    }

    void CheckForDive()
    {
        if (Input.GetMouseButton(0) && (m_nState != eState.kDiving && m_nState != eState.kRecovering))
        {
            // Start the dive operation
            m_nState = eState.kDiving;
            m_fSpeed = 0.0f;

            // Store starting parameters.
            m_vDiveStartPos = transform.position;
            m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
            m_fDiveStartTime = Time.time;
        }
    }

    void Start()
    {
        // Initialize variables.
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void UpdateDirectionAndSpeed()
    {
        // Get relative positions between the mouse and player
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(transform.position.x - vScreenPos.x, transform.position.y - vScreenPos.y);

        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        // Calculate how far away from the player the mouse is.
        float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;

        // Based on distance, calculate the speed the player is requesting.
        if (fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (fMouseMagnitude > m_fMagnitudeSlow)
        {
            m_fTargetSpeed = m_fSlowSpeed;
        }
        else
        {
            m_fTargetSpeed = 0.0f;
        }
    }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    void Update()
    {
        UpdateDirectionAndSpeed(); //Always update the direction and speed requrested by user

        switch (m_nState) //State Machine
        {
            case eState.kMoveSlow:
            {
                m_fAngle = m_fTargetAngle; //When slow, player can rotate to face the mouse
                m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fTargetSpeed, m_fIncSpeed); 

                if (m_fSpeed >= m_fSlowSpeed) //If speed is more than slow threshold, transition to moving fast state
                {
                    m_nState = eState.kMoveFast;
                }

                CheckForDive(); //Player can dive while in the slow state
                break;
            }

            case eState.kMoveFast:
            {
                float ang = Mathf.DeltaAngle(m_fAngle, m_fTargetAngle); //Calculates shortest angle difference to the mouse

                if (Mathf.Abs(ang) < m_fFastRotateMax) //Can only turn if within angle threshold when in fast state
                {
                    m_fAngle += ang * m_fFastRotateSpeed; //Gradually rotate towards the target
                }
                else
                {
                    m_fSpeed = Mathf.MoveTowards(m_fSpeed, 0.0f, m_fIncSpeed); //If mouse too far, slow down 
                }

                m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fTargetSpeed, m_fIncSpeed);

                if (m_fSpeed < m_fSlowSpeed) //If speed drops below threshold, go back to the slow state
                {
                    m_nState = eState.kMoveSlow;
                }

                CheckForDive(); //Player can dive while in fast state
                break;
            }

            case eState.kDiving:
            {
                float t = (Time.time - m_fDiveStartTime) / m_fDiveTime; //Dive progress

                if (t >= 1.0f)
                {
                    transform.position = m_vDiveEndPos; //Dive finished
                    m_nState = eState.kRecovering; //Transition to recovery state
                    m_fDiveStartTime = Time.time;
                }
                else
                {
                    transform.position = Vector3.Lerp(m_vDiveStartPos, m_vDiveEndPos, t);
                }
                break;
            }

            case eState.kRecovering:
            {
                if (Time.time - m_fDiveStartTime >= m_fDiveRecoveryTime) //Wait until the recovery time is finished
                {
                    m_nState = eState.kMoveSlow; //Transition back to slow state
                }
                break;
            }
        }

        transform.rotation = Quaternion.Euler(0, 0, m_fAngle);

        if (m_nState == eState.kMoveSlow || m_nState == eState.kMoveFast) //Only apply movement if not diving or recovering
        {
            Vector3 movement = -transform.right * m_fSpeed; //Move forward in the direction of mouse
            transform.position += movement;
        }
    }
}
