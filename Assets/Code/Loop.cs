using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loop : MonoBehaviour
{
    public float m_MinStrength;
    public float m_MaxPullStrength;
    public float m_LoopSpeed;
    public float m_InnerRadius;
    public float m_OuterRadius;
    public float m_PositiveNegative;
    public Transform m_PullTarget;
    public float m_SpawnAnimSpeed;
    public AnimationCurve m_SpawnAnimCurve;
    public float m_DespawnAnimSpeed;
    public AnimationCurve m_DespawnAnimCurve;
    public GameObject m_Projectile;
    public int m_NumProjectiles;
    public float m_ProjectileIntervalMin;
    public float m_ProjectileIntervalMax;
    public float m_ShootPlayerRange;

    private int _clockwise = 1;
    private Vector3 _lastPos;
    private bool _doneSpawning;
    private float _projTimer;
    private int _projLeft;

    public void Spawn()
    {
        _projLeft = m_NumProjectiles;
        _doneSpawning = false;
        StartCoroutine("SpawnAnimation");
    }

    IEnumerator SpawnAnimation()
    {
        SphereCollider col = gameObject.GetComponent<SphereCollider>();
        col.enabled = false;

        Transform trans = transform;
        trans.localScale = Vector3.zero;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * m_SpawnAnimSpeed;
            if (t > 1f) t = 1f;
            trans.localScale = Vector3.one*m_SpawnAnimCurve.Evaluate(t);
            yield return null;
        }

        trans.localScale = Vector3.one;

        col.enabled = true;

         _doneSpawning = true;
        _projTimer = Random.Range(m_ProjectileIntervalMin,m_ProjectileIntervalMax);
    }

    IEnumerator DespawnAnimation()
    {
        SphereCollider col = gameObject.GetComponent<SphereCollider>();
        col.enabled = false;

        Transform trans = transform;
        trans.localScale = Vector3.one;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * m_DespawnAnimSpeed;
            if (t > 1f) t = 1f;
            trans.localScale = Vector3.one*m_DespawnAnimCurve.Evaluate(t);
            yield return null;
        }

        trans.localScale = Vector3.zero;
        GameManager.Instance.SpawnCircle(m_PositiveNegative >= 0);
        Destroy(gameObject);

    }

    private void Start()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.radius = m_OuterRadius;

        m_PullTarget.localPosition = new Vector3(m_InnerRadius,0,0);
        _clockwise = Random.Range(0f,1f) > 0.5f ? 1 : -1;

    }

    public Vector3 GetHeading()
    {
        return (m_PullTarget.position-_lastPos);
    }

    private void Update()
    {
        if (!_doneSpawning) return;

        Vector3 pos = transform.position;
        _lastPos = m_PullTarget.position;
        m_PullTarget.RotateAround(pos, Vector3.up, Time.deltaTime * m_LoopSpeed * _clockwise);

        //to do: only shoot if !changing level
        if (_projTimer <= 0f)
        {
            Vector3 dir = new Vector3(Random.Range(-1f,1f), 0f, Random.Range(-1f,1f)).normalized;
            Vector3 playerPos = PlayerV2.Instance.Position;
            if (Vector3.Distance(pos, playerPos) < m_ShootPlayerRange)
                dir = (playerPos-pos).normalized;
            
            GameObject obj = Instantiate(m_Projectile);
            obj.transform.position = pos;

            Projectile proj = obj.GetComponent<Projectile>();
            proj.Shoot(dir);

            _projTimer = Random.Range(m_ProjectileIntervalMin,m_ProjectileIntervalMax);
            _projLeft--;
            if (_projLeft <= 0)
            {
                _doneSpawning = false;
                StartCoroutine("DespawnAnimation");
            }
        }else _projTimer -= Time.deltaTime;
    }

}
