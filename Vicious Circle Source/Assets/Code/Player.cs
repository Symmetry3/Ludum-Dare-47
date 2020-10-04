using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class Player : MonoBehaviour
{
    public float m_LoopSpeed;
    public float m_StraightSpeed;
    public float m_SteeringSpeed;
    public Vector3 m_LoopPointLocalOffset;
    public Vector3 m_EatOffset;
    public float m_EatRadius;
    public float m_CaptureRadius;
    public int m_MaxBodySegments;
    public GameObject m_BodySegmentPrefab;
    public Vector3 m_BodySegmentOffset;
    public float m_BodyStiffness;
    public float m_BodyRotationStiffness;
    
    public LayerMask m_PickupLayer;
    public LayerMask m_PowerupLayer;

    public Transform m_Head;

    public Transform m_CameraTarget;
    public InputAction m_TurnAction;

    private bool _inLoop;
    private int _loopDir;
    private float _captureDeg;

    private bool _hasCapture;
    private Collider2D[] _powerups;

    private Vector3 _lastLoopPoint;
    private Vector3 _heading;
    private Transform[] _bodyPieces;
    private int _lastBodyIndex;

    

    public void OnSwitchLoop(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Started)
        {
            if (_inLoop) ExitLoop();
            else EnterLoop();
        }
    }

    //public void OnTurn(InputAction.CallbackContext ctx)
    //{
    //    if (_inLoop) return;

    //    _heading = Vector3.RotateTowards(_heading, m_Head.up*ctx.ReadValue<float>(), Time.deltaTime*m_SteeringSpeed, 0f);
    //    //_heading += m_Head.up * ctx.ReadValue<float>() * Time.deltaTime * m_SteeringSpeed;
    //    _heading.Normalize();
    //}

    private void ExitLoop()
    {
        _heading =/* _loopDir **/ m_Head.right;
        _inLoop = false;
        
    }

    private void EnterLoop()
    {
        _loopDir *= -1;
        _inLoop = true;

        Vector3 loopPos = m_Head.TransformPoint(m_LoopPointLocalOffset*_loopDir);
        m_CameraTarget.position = loopPos;

        Collider2D[] cols = Physics2D.OverlapCircleAll(loopPos, m_CaptureRadius, m_PowerupLayer);
        if (cols != null && cols.Length > 0)
        {
            _powerups = cols;
            _hasCapture = true;
            _captureDeg = 0;
        }
        else _hasCapture = false;
    }

    
    void Start()
    {
        _loopDir = -1;
        EnterLoop();

        _bodyPieces = new Transform[m_MaxBodySegments];
    }

  
    void Update()
    {
        float dt = Time.deltaTime;
        if (_inLoop)
        {
            float deg =  dt * m_LoopSpeed * _loopDir;
            m_Head.RotateAround(m_Head.TransformPoint(m_LoopPointLocalOffset*_loopDir), Vector3.back, deg);

            if (_hasCapture)
            {
                _captureDeg += deg;
                if (Mathf.Abs(_captureDeg) >= 360f)
                {
                    for (int i = 0; i < _powerups.Length; i++)
                    {
                        GameObject obj = Instantiate(m_BodySegmentPrefab);
                        obj.transform.position = _powerups[i].transform.position;
                        _bodyPieces[_lastBodyIndex] = obj.transform;
                        _lastBodyIndex++;

                        if (_lastBodyIndex == m_MaxBodySegments)
                        {
                            //eat tail
                        }
                    }
                    //grow body

                    _hasCapture = false;
                }
            }
        }
        else
        {
            //_heading = Vector3.RotateTowards(_heading, m_Head.up*m_TurnAction.ReadValue<float>(), Time.deltaTime*m_SteeringSpeed, 0f);
            //m_Head.right = _heading;
            m_Head.position += _heading * dt * m_StraightSpeed;
            m_CameraTarget.position = m_Head.position;
        }

        for (int i = 0; i < _lastBodyIndex; i++)
        {
            Vector3 target = (i==0?m_Head:_bodyPieces[i-1]).TransformPoint(m_BodySegmentOffset);
            Vector3 current = _bodyPieces[i].position;            
            Vector3 targetRight = (i==0?m_Head.right:_bodyPieces[i-1].right);
            Vector3 currentRight = _bodyPieces[i].right;
            _bodyPieces[i].position = Vector3.MoveTowards(current, target, dt * m_BodyStiffness);
            _bodyPieces[i].right = Vector3.MoveTowards(currentRight, targetRight, dt * m_BodyRotationStiffness);
        }
    }

    private void FixedUpdate()
    {
        Collider2D col = Physics2D.OverlapCircle(m_Head.TransformPoint(m_EatOffset), m_EatRadius, m_PickupLayer);
        if (col != null)
        {
            //increase score or something
        }

        
    }
}
