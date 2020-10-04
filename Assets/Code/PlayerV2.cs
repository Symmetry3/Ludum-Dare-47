using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerV2 : MonoBehaviour
{
    public static PlayerV2 Instance;

    public float m_Speed;
    public float m_Acceleration;
    public float m_StopSpeed;
    public float m_TurnSmoothingMin;
    public float m_TurnSmoothingMax;
    public float m_PlayerRadius;
    public float m_StuckDistance;
    public float m_MinStuckForce;
    public float m_MaxStuckForce;
    public float m_DashForce;
    public float m_MaxBreath;
    public float m_DashFalloff;
    public CharacterController m_Controller;
    public float m_ThoughtWidthInner;
    public float m_ThoughtWidthOuter;
    public LayerMask m_ThoughtLayer;

    public LayerMask m_LoopLayer;

    public Image m_BreathMeter;

    private Vector2 _moveInput;
    private Vector2 _aimInput;

    private Vector3 _move;
    private Vector3 _pull;
    private int _positivity;
    private Transform _transform;
    private Loop _stuckInLoop;
    private bool _stuckOnPullTarget;
    private float _breath;
    private bool _breathBtn;
    private float _dashSpeed;
    private bool _breathingOut;
    private bool _changingLevel;
    private Requirement _currentReq;

    public Vector3 Position { get { return _transform.position; } private set{ } }

    public void DebugOne(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed)
            TakePositivityDamage(1);
    }

     public void DebugTwo(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed)
            TakePositivityDamage(2);
    }

    public void DebugThree(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed)
            TakePositivityDamage(-3);
    }

    public void StartLevel(Requirement req)
    {
        _currentReq = req;
        _positivity = 0;
        _changingLevel = false;

    }

    public void TakePositivityDamage(int amount)
    {
        if (_changingLevel) return;

        _positivity += amount;
        if ((_positivity>0 && _positivity >= _currentReq.positivity) || ( _positivity < 0 && _positivity <= _currentReq.negativity))
        {
            int dir = _positivity > 0 ? (_positivity - _currentReq.positivity + 1) : (_positivity-_currentReq.negativity-1);
            Debug.Log("changing level: " + dir);
            GameManager.Instance.ChangeLevel(dir, _transform);
            _changingLevel = true;
            enabled = false;
            _stuckOnPullTarget = false;
        }
    }

    public void Breathe(InputAction.CallbackContext ctx)
    {
        _breathBtn = ctx.phase == InputActionPhase.Performed;
    }

    public void Move(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    public void Aim(InputAction.CallbackContext ctx)
    {
        _aimInput = ctx.ReadValue<Vector2>();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _transform = transform;


    }

    private void Update()
    {
        float dt = Time.deltaTime;

        bool moveInput = true;
        Vector3 targetDir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
        if (targetDir.x < 0.01f && targetDir.x > -0.01f && targetDir.z < 0.01f && targetDir.z > -0.01f)
            moveInput = false;

        if (_dashSpeed > 0f)
        {
            if (!moveInput)
                targetDir = _transform.forward;

            //_dashSpeed -= dt * m_DashFalloff;
        }

        if (!_changingLevel)
        {
            if (_breathBtn)
            {
                _breathingOut = false;
                _breath += Time.deltaTime;
                if (_breath > m_MaxBreath) _breath = m_MaxBreath;
            }
            else
            {
                if (!_breathingOut)
                {
                    if (!moveInput) targetDir = _transform.forward;

                    _move = targetDir * (m_Speed+_dashSpeed);
                    _transform.forward = targetDir;
                }

                _breathingOut = true;
                _dashSpeed = m_DashForce * _breath;
                _breath -= Time.deltaTime;
                if (_breath < 0f) _breath = 0;
            }
        }

        m_BreathMeter.fillAmount = _breath/m_MaxBreath;

        if (moveInput && !_changingLevel)
            _move = Vector3.MoveTowards(_move, targetDir * (m_Speed+_dashSpeed), dt * m_Acceleration);
        else _move = Vector3.MoveTowards(_move, Vector3.zero, dt * m_StopSpeed);

        Vector3 pos = _transform.position;
        if (_stuckOnPullTarget)
        {
            //_positivity += _stuckInLoop.m_PositiveNegative * dt;
            float force;
            if (_positivity >= 0f)
            {            
                force = Mathf.Lerp(m_MinStuckForce, m_MaxStuckForce, (_stuckInLoop.m_PositiveNegative+1f)/2f);
            }
            else force = Mathf.Lerp(m_MaxStuckForce, m_MinStuckForce, (_stuckInLoop.m_PositiveNegative+1f)/2f);

            _transform.position = Vector3.MoveTowards(pos, _stuckInLoop.m_PullTarget.position, dt * force);
        }

        m_Controller.Move((_move+_pull) * dt);
        //_transform.position += (_move+_pull) * dt;
        float turnAmount =  Mathf.Lerp(m_TurnSmoothingMin,m_TurnSmoothingMax, Vector3.Angle(_transform.forward, targetDir)/180f);

        if (moveInput)
            _transform.forward = Vector3.RotateTowards(_transform.forward, targetDir, dt * turnAmount, 0f);

        if (_stuckOnPullTarget && Vector3.Distance(_transform.position, _stuckInLoop.m_PullTarget.position) > m_StuckDistance + 0.25f)
        {
            _stuckOnPullTarget = false;
            _stuckInLoop = null;
        }
    }

    private void FixedUpdate()
    {
        Vector3 curPos = _transform.position;
        Collider[] cols = Physics.OverlapSphere(curPos, m_PlayerRadius, m_LoopLayer);
        if (cols != null && cols.Length > 0 && !_stuckOnPullTarget)
        {
            Vector3 finalPull = Vector3.zero;
            for (int i = 0; i < cols.Length; i++)
            {                
                Loop loop = cols[i].GetComponent<Loop>();
                Vector3 p = loop.m_PullTarget.position;
                float d = Vector3.Distance(p,curPos);// - loop.m_InnerRadius;

                if (d < m_StuckDistance)
                {
                    _stuckOnPullTarget = true;
                    _stuckInLoop = loop;
                    break;
                }

                d = Mathf.Clamp(d, 0, loop.m_OuterRadius)/loop.m_OuterRadius;
                //Debug.Log("In loop: " + d);
                Vector3 pull = (p-curPos).normalized * Mathf.Lerp(loop.m_MinStrength, loop.m_MaxPullStrength,1f-d);
                finalPull += pull;
            }

            _pull = finalPull / cols.Length;
        }
        else
        {
            _pull = Vector3.zero;
        }

        cols = Physics.OverlapSphere(curPos, m_PlayerRadius+m_ThoughtWidthOuter, m_ThoughtLayer);
        //float homing = (_positivity >= 0 ? _positivity/_currentReq.positivity : _positivity/_currentReq.negativity);
        if (cols != null && cols.Length > 0)
        {
            for (int i = 0; i < cols.Length; i++)
            {
                Projectile proj = cols[i].GetComponent<Projectile>();
                proj.SteerTowardPlayer(curPos, _positivity);

                if (Vector3.Distance(cols[i].transform.position, curPos) < m_PlayerRadius+m_ThoughtWidthInner)
                {
                    TakePositivityDamage(proj.m_PositivityDamage);
                    proj.DestroyProjectile();
                }
            }
        }
    }


}
