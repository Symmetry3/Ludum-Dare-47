using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Requirement
{
    public int positivity;
    public int negativity;
}

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    public GameObject m_VirtuousCircle;
    public GameObject m_ViscousCircle;

    public float m_MapRadius;
    public float m_HeightBetweenFloors;
    public float m_LevelChangeWait;
    public float m_LevelChangeSpeed;
    public AnimationCurve m_LevelChangeCurve;
    public int m_CircleCount;
    public GameObject m_Level;
    public GameObject m_Background;
    public float m_BackgroundOffset;
    public int m_FinalHeight;

    public Level m_NeutralLevel;
    public Level[] m_PositiveLevels;
    public Level[] m_NegativeLevels;

    private int _currentHeight;
    
    private GameObject _currentLevel;
    private GameObject _currentBackground;
    private Level _currentLevelData;

    //private List<GameObject> _currentLevelObjects = new List<GameObject>();
    private bool _changingLevel;

    private struct LevelChangeData
    {
        public Transform[] levels;
        public Transform[] backgrounds;
        public Material[] backgroundMaterials;
        public int lastHeight;
        public int direction;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnCircle(bool virtuous)
    {
        if (_changingLevel)
            return;

        GameObject obj = Instantiate(virtuous?m_VirtuousCircle:m_ViscousCircle);
        Vector2 p = Random.insideUnitCircle*(m_MapRadius-10);
        obj.transform.position = new Vector3(p.x, _currentHeight*m_HeightBetweenFloors, p.y);

        Loop loop = obj.GetComponent<Loop>();
        loop.Spawn();

        obj.transform.SetParent(_currentLevel.transform, true);
        //_currentLevelObjects.Add(obj);
    }

    public void ChangeLevel(int direction, Transform player)
    {
        _changingLevel = true;

        int lastHeight = _currentHeight;
        _currentHeight += direction;
        int posNeg = (int)Mathf.Sign(direction);
        if (Mathf.Abs(_currentHeight) > m_FinalHeight)
        {
            _currentHeight = m_FinalHeight*posNeg;
            direction = (m_FinalHeight-Mathf.Abs(lastHeight))*posNeg;
            if (Mathf.Abs(lastHeight) == m_FinalHeight)
            {
                //Game Over or Win
                if (_currentHeight > 0)
                    Debug.Log("You have found peace");
                else Debug.Log("You have found chaos");

                return;
            }
            else _currentHeight = m_FinalHeight*posNeg;
        }

        StartCoroutine(ChangeLevelRoutine(player, direction, lastHeight));
    }

    public void AddObjectToLevel(Transform transform)
    {
        transform.SetParent(_currentLevel.transform, true);
    }

    IEnumerator ChangeLevelRoutine(Transform player, int direction, int lastHeight)
    {
        int count = Mathf.Abs(direction);
        GameObject[] pieces = new GameObject[count];
        GameObject[] bgs = new GameObject[count];
        float posNeg = Mathf.Sign(direction);

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(m_Level);
            Vector3 p = player.position;
            float height = (lastHeight*m_HeightBetweenFloors) + ((i+1)*Mathf.Sign(direction)*m_HeightBetweenFloors);
            p.y = height;
            obj.transform.position = p;

            GameObject bg = Instantiate(m_Background);
            bg.transform.position = p + (Vector3.up*m_BackgroundOffset);
            Material mat = bg.GetComponent<MeshRenderer>().material;
            Level level = m_NeutralLevel;
            int index = lastHeight+((i+1)*(int)posNeg);
            if (index > 0) level = m_PositiveLevels[Mathf.Abs(index)-1];
            else if (index < 0) level = m_NegativeLevels[Mathf.Abs(index)-1];
            mat.SetColor("_Color", level.backgroundColor);
            mat.SetFloat("_Intensity", level.backgroundIntensity);
            pieces[i] = obj;
            bgs[i] = bg;
        }

        yield return new WaitForSeconds(m_LevelChangeWait);

        float t = 0f;
        Vector3 pos = player.transform.position;
        Vector3 targetPos = pos + new Vector3(0, count*m_HeightBetweenFloors*posNeg, 0);
        while (t < 1f)
        {
            t += Time.deltaTime * m_LevelChangeSpeed;
            if (t > 1f) t = 1f;
            player.position = Vector3.Lerp(pos, targetPos, m_LevelChangeCurve.Evaluate(t));
            yield return null;
        }

        player.position = targetPos;

        for (int i = 0; i < count-1; i++)
        {
            Destroy(pieces[i]);
            Destroy(bgs[i]);
        }

        Destroy(_currentLevel);
        Destroy(_currentBackground);

        _currentLevel = pieces[count-1];
        _currentBackground = bgs[count-1];        

        _changingLevel = false;
        PlayerV2.Instance.enabled = true;
        _currentLevelData = m_NeutralLevel;
        if (_currentHeight > 0) _currentLevelData = m_PositiveLevels[_currentHeight-1];
        else if (_currentHeight < 0) _currentLevelData = m_NegativeLevels[Mathf.Abs(_currentHeight)-1];

        PlayerV2.Instance.StartLevel(_currentLevelData.requirement);

        for (int i = 0; i < _currentLevelData.circleAmount-1; i++)
            SpawnCircle(Random.value<_currentLevelData.positivity);

        SpawnCircle(true);
    }


    private void ChangeLevelTest()
    {
        ChangeLevel(-3, PlayerV2.Instance.transform);
    }

    private void Start()
    {
        _currentLevel = Instantiate(m_Level);
        _currentBackground = Instantiate(m_Background);
        Material mat = _currentBackground.GetComponent<MeshRenderer>().material;
        mat.SetColor("_Color", m_NeutralLevel.backgroundColor);
        mat.SetFloat("_Intensity", m_NeutralLevel.backgroundIntensity);
        _currentLevelData = m_NeutralLevel;
        PlayerV2.Instance.StartLevel(m_NeutralLevel.requirement);

        for (int i = 0; i < m_CircleCount; i++)
            SpawnCircle(Random.value<m_NeutralLevel.positivity);
        //Invoke("ChangeLevelTest", 5f);
    }
}
