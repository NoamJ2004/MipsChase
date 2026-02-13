using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
   {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
   };

    // External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 6.5f;
    public float m_fScaredDistance = 3.0f;
    public int m_nMaxMoveAttempts = 50;

    // Internal variables.
    public eState m_nState;
    public float m_fHopStart;
    public Vector3 m_vHopStartPos;
    public Vector3 m_vHopEndPos;

    void Start()
    {
        // Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindObjectOfType(typeof(Player)) as Player;
    }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving())
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            }
        }
    }

    Vector3 ClampToScreen(Vector3 pos) //Added to prevent target from going off screen
    {
        Vector3 min = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));

        pos.x = Mathf.Clamp(pos.x, min.x, max.x);
        pos.y = Mathf.Clamp(pos.y, min.y, max.y);

        return pos;
    }


    void Update()
    {
        if (m_nState == eState.kCaught) //If target is caught, do nothing
            return;

        float distToPlayer = Vector3.Distance(transform.position, m_player.transform.position); //Calculating the distance between the player and the target
        switch (m_nState) //State Machine
        {
            case eState.kIdle:
            {
                if (distToPlayer < m_fScaredDistance) //If the player is too close, start the hop 
                {
                    m_nState = eState.kHopStart;
                }
                break;
            }

            case eState.kHopStart:
            {
                Vector3 d = (transform.position - m_player.transform.position).normalized; //Determine the direction away from the player
                m_vHopStartPos = transform.position; //Store the starting position of the hop
                Vector3 e = transform.position + d * m_fHopSpeed * m_fHopTime; //Calculate the hop end position
                m_vHopEndPos = ClampToScreen(e);
                m_fHopStart = Time.time; //Record when the hop started
                m_nState = eState.kHop; //Transition to hopping state
                break;
            }

            case eState.kHop:
            {
                float t = (Time.time - m_fHopStart) / m_fHopTime; //Hop progress

                if (t >= 1.0f)
                {
                    transform.position = m_vHopEndPos; //Hop is done, go to end position
                    m_nState = eState.kIdle; //Return to being idle
                }
                else
                {
                    transform.position = Vector3.Lerp(m_vHopStartPos, m_vHopEndPos, t);
                }
                break;
            }
        }
    }
}