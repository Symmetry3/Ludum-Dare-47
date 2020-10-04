using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float m_Speed;
    public float m_Wander;
    public float m_WanderFrequency;
    public int m_PositivityDamage;
    public float m_DistanceLife = 80f;

    public float m_Radius;
    public LayerMask m_PlayerLayer;
    public float m_HomingAmount;

    private Vector3 _heading;
    private Transform _transform;
    private float _wander;
    private float _homing;
    private float _life;

    public void SteerTowardPlayer(Vector3 playerPos, int positive)
    {
        if ((positive < 0 && m_PositivityDamage > 0)||(positive > 0 &&  m_PositivityDamage < 0)) return;
        //if ((positive && m_PositivityDamage >= 0)||(!positive && m_PositivityDamage < 0))
        {
            Vector3 newDir = (playerPos-_transform.position).normalized;
            _heading = Vector3.RotateTowards(_heading, newDir, m_HomingAmount, 0f);
            _homing = 0.5f;
        }

    }

    public void Shoot(Vector3 dir)
    {
        _transform = transform;            

        _heading = dir;
        _transform.forward = dir;
        _life = m_DistanceLife;

        GameManager.Instance.AddObjectToLevel(_transform);
    }    

    public void DestroyProjectile()
    {
        GetComponent<SphereCollider>().enabled = false;
        _life = 0f;
        StartCoroutine("ScaleDown");
    }

    IEnumerator ScaleDown()
    {
        float t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime * 2.5f;
            if (t < 0f) t = 0f;
            _transform.localScale = Vector3.one*t*t*t;

            yield return null;
        }

        Destroy(gameObject);
    }    

    void Update()
    {
        if (_life <= 0f) return;

        _wander = Mathf.Sin(Time.time*m_WanderFrequency);
        if (_homing <= 0f)
            _heading = Vector3.RotateTowards(_heading, _transform.right*_wander, Time.deltaTime * m_Wander, 0f);
        else _homing -= Time.deltaTime;

        _transform.position += _heading * Time.deltaTime * m_Speed;
        _life -= m_Speed * Time.deltaTime;

        if (_life <= 0f) DestroyProjectile();
    }

    //private void FixedUpdate()
    //{
    //    RaycastHit hit;
        
    //    if (Physics.SphereCast(_transform.position, m_Radius, _heading, out hit, Time.fixedDeltaTime*m_Speed, m_PlayerLayer))
    //    {
    //        PlayerV2.Instance.TakePositivityDamage(m_PositivityDamage);
    //        Destroy(gameObject);
    //        return;
    //    }
    //    else
    //    {
    //        Collider[] cols = Physics.OverlapSphere(_transform.position, m_Radius, m_PlayerLayer);
    //        if (cols != null && cols.Length > 0)
    //        {
    //            PlayerV2.Instance.TakePositivityDamage(m_PositivityDamage);
    //            Destroy(gameObject);
    //            return;
    //        }
    //    }
    //}
}
