using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD : MonoBehaviour
{
    public static HUD Instance;

    public GameObject m_Parent;
    public GameObject m_Progress;
    public Animator m_NeutralAnimator;
    public GameObject m_PosIndicatorPrefab;
    public GameObject m_NegIndicatorPrefab;
    public Color m_PositiveIndicatorColor;
    public Color m_NegativeIndicatorColor;
    public GameObject[] m_PosLevelIndicators;
    public GameObject[] m_NegLevelIndicators;
    
    public RectTransform m_IndicatorParent;
    public int m_IndicatorOffset;
    public int m_MaxIndicators;

    private GameObject[] _posIndicators;
    private GameObject[] _negIndicators;
    private Animator[] _posAnimators;
    private Animator[] _negAnimators;

    public void SetHud(int positiveAmount, int negativeAmount)
    {
        m_NeutralAnimator.SetBool("Active", true);
        for (int i = 0; i < m_MaxIndicators; i++)
        {
            _posAnimators[i].SetBool("Active", false);
            _negAnimators[i].SetBool("Active", false);
            if (i >= positiveAmount)
                _posIndicators[i].SetActive(false);
            else _posIndicators[i].SetActive(true);

            if (i >= negativeAmount)
                _negIndicators[i].SetActive(false);
            else _negIndicators[i].SetActive(true);
        }

    }

    public void SetLevel(int level)
    {
        
        for (int i = 0; i < 5; i++)
        {
            m_PosLevelIndicators[i].SetActive(false);
            m_NegLevelIndicators[i].SetActive(false);
            if (level>0)
            {
                if (i < level)
                    m_PosLevelIndicators[i].SetActive(true);
            }
            else if (level<0)
            {
                if (i < Mathf.Abs(level))
                    m_NegLevelIndicators[i].SetActive(true);
            }
            
        }

    }

    public void ShowHUD(bool value)
    {
        if (value) m_Progress.SetActive(true);
        m_Parent.SetActive(value);
    }

    public void SetPositivity(int amount)
    {
        for (int i = 0; i < m_MaxIndicators; i++)
        {
            _posAnimators[i].SetBool("Active", false);
            _negAnimators[i].SetBool("Active", false);
            if (amount > 0)
            {
                if (i < amount) _posAnimators[i].SetBool("Active", true);
            }
            else if (amount < 0)
            {
                 if (i < -amount) _negAnimators[i].SetBool("Active", true);
            }
            else
            {
                //m_NeutralAnimator.SetBool("Active", true);
            }

        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _posIndicators = new GameObject[m_MaxIndicators];
        _negIndicators = new GameObject[m_MaxIndicators];
        _posAnimators = new Animator[m_MaxIndicators];
        _negAnimators = new Animator[m_MaxIndicators];
        for (int i = 0; i < m_MaxIndicators; i++)
        {
            GameObject obj = Instantiate(m_PosIndicatorPrefab);
            RectTransform rt = obj.GetComponent<RectTransform>();
            Animator anim = obj.GetComponent<Animator>();
            rt.SetParent(m_IndicatorParent);
            rt.anchoredPosition = new Vector2((i+1)*m_IndicatorOffset+20,0f);

            obj.SetActive(false);
            _posIndicators[i] = obj;
            _posAnimators[i] = anim;

            obj = Instantiate(m_NegIndicatorPrefab);
            rt = obj.GetComponent<RectTransform>();
            anim = obj.GetComponent<Animator>();
            rt.SetParent(m_IndicatorParent);
            rt.anchoredPosition = new Vector2(-(i+1)*m_IndicatorOffset-20,0f);

            obj.SetActive(false);
            _negIndicators[i] = obj;
            _negAnimators[i] = anim;
        }

        ShowHUD(false);
    }


}
