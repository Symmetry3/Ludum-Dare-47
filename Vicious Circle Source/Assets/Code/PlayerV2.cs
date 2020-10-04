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
    public float m_MaxRoll;
    public Transform m_Model;
    public MeshRenderer m_Eyes;
    public GameObject m_ClearMindObj;
    public float m_ClearMindRadius;
    
    public Color m_NeutralEyeColor;
    public Color m_PositiveEyeColor;
    public Color m_NegativeEyeColor;

    public LayerMask m_LoopLayer;

    public Image m_BreathMeter;

    public AudioSource m_HitAudio;
    public AudioClip[] m_PositiveHits;
    public AudioClip[] m_NegativeHits;
    public float m_PosHitVol;
    public float m_NegHitVol;

    public AudioSource m_ChargeClear;
    public AudioSource m_Clear;

    private Material _eyeMat;
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

    private bool _inEnd;

    public Vector3 Position { get { return _transform.position; } private set{ } }

    //public void DebugOne(InputAction.CallbackContext ctx)
    //{
    //    if (ctx.phase == InputActionPhase.Performed)
    //        TakePositivityDamage(1);
    //}

    //public void DebugTwo(InputAction.CallbackContext ctx)
    //{
    //    if (ctx.phase == InputActionPhase.Performed)
    //        TakePositivityDamage(2);
    //}

    //public void DebugThree(InputAction.CallbackContext ctx)
    //{
    //    if (ctx.phase == InputActionPhase.Performed)
    //        TakePositivityDamage(-3);
    //}

    public void StartLevel(Requirement req)
    {
        _currentReq = req;
        _positivity = 0;
        _changingLevel = false;
        _move = Vector3.zero;
        _dashSpeed = 0f;
    }

    public void AtEnd()
    {
        DoClearMind(100f);

        _inEnd = true;
        _changingLevel = false;
    }

    public void TakePositivityDamage(int amount)
    {
        if (_changingLevel || _inEnd) return;

        _positivity += amount;
        if (_positivity > 0) _eyeMat.SetColor("_BaseColor", m_PositiveEyeColor);
        else if (_positivity < 0) _eyeMat.SetColor("_BaseColor", m_NegativeEyeColor);
        else _eyeMat.SetColor("_BaseColor", m_NeutralEyeColor);

        HUD.Instance.SetPositivity(_positivity);

        if ((_positivity>0 && _positivity >= _currentReq.positivity) || ( _positivity < 0 && _positivity <= _currentReq.negativity))
        {
            int dir = amount;//_positivity > 0 ? (_positivity - _currentReq.positivity + 1) : (_positivity-_currentReq.negativity-1);
            Debug.Log("changing level: " + dir);
            _changingLevel = true;
            enabled = false;
            _stuckOnPullTarget = false;
            GameManager.Instance.ChangeLevel(dir, _transform);
        }
    }

    public void Breathe(InputAction.CallbackContext ctx)
    {
        if (!_breathBtn && !_changingLevel && enabled == true) m_ChargeClear.Play();

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

        _eyeMat = m_Eyes.material;
        _eyeMat.SetColor("_BaseColor", m_NeutralEyeColor);
        m_ClearMindObj.SetActive(false);

        _breathingOut = true;
       
    }



    IEnumerator ClearMind(float radius)
    {
        m_ClearMindObj.transform.localScale = Vector3.zero;
        m_ClearMindObj.SetActive(true);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            if (t > 1f) t = 1f;
            float f = 1f-t;
            m_ClearMindObj.transform.localScale = Vector3.one*radius*(1f-(f*f));

            yield return null;
        }

        m_ClearMindObj.SetActive(false);

        
    }

    private void DoClearMind(float radius)
    {
        if (_inEnd) GameManager.Instance.RestartLevel();

        m_ChargeClear.Stop();
        m_Clear.pitch = Random.Range(0.9f,1.1f);
        m_Clear.Play();
        StartCoroutine("ClearMind",radius);
        Collider[] cols = Physics.OverlapSphere(_transform.position, radius, m_LoopLayer|m_ThoughtLayer);
        if (cols != null && cols.Length > 0)
        {
            for (int i = 0; i < cols.Length; i++)
            {
                Projectile proj = cols[i].GetComponent<Projectile>();
                if (proj) proj.DestroyProjectile();
                else
                {
                    Loop loop = cols[i].GetComponent<Loop>();
                    if (loop) loop.Despawn();
                }
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance.Paused) return;

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
                if (m_ChargeClear.isPlaying) m_ChargeClear.Play();
                if (!_breathingOut && _breath > 0.25f)
                {
                    if (!moveInput) targetDir = _transform.forward;

                    _move = targetDir * (m_Speed+_dashSpeed);
                    _transform.forward = targetDir;

                    float rad = _breath/m_MaxBreath*m_ClearMindRadius*2f;
                    DoClearMind(rad);
                }

                _breathingOut = true;
                _dashSpeed = m_DashForce * _breath;
                _breath -= Time.deltaTime * m_DashFalloff;
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

            if (_stuckInLoop)
                _transform.position = Vector3.MoveTowards(pos, _stuckInLoop.m_PullTarget.position, dt * force);
            else
            {
                _stuckOnPullTarget = false;
            }
        }
        else
        {
            m_Controller.Move((_move+_pull) * dt);
        }

        //_transform.position += (_move+_pull) * dt;
        float angle = Vector3.Angle(_transform.forward, targetDir)/180f;
        float angleDir = (Vector3.Angle(_transform.right, targetDir)>90f)?1:-1f;
        float turnAmount = Mathf.Lerp(m_TurnSmoothingMin,m_TurnSmoothingMax, angle);
        m_Model.localRotation = Quaternion.Slerp(m_Model.localRotation, Quaternion.Euler(0f, 0f, m_MaxRoll*angle*angleDir), Time.deltaTime * turnAmount);

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

                if (Vector3.Distance(cols[i].transform.position, curPos)-proj.m_Radius < m_PlayerRadius+m_ThoughtWidthInner)
                {
                    TakePositivityDamage(proj.m_PositivityDamage);
                    proj.DestroyProjectile();
                    if (proj.m_PositivityDamage > 0)
                        m_HitAudio.PlayOneShot(m_PositiveHits[Random.Range(0,m_PositiveHits.Length)], m_PosHitVol);
                    else m_HitAudio.PlayOneShot(m_NegativeHits[Random.Range(0,m_NegativeHits.Length)], m_NegHitVol);
                }
            }
        }
    }


}
