using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[System.Serializable]
public struct Requirement
{
    public int positivity;
    public int negativity;
}

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    public GameObject m_PauseScreen;
    public GameObject m_VirtuousCircle;
    public GameObject m_ViscousCircle;

    public float m_MapRadius;
    public float m_HeightBetweenFloors;
    public float m_LevelChangeWait;
    public float m_LevelChangeSpeed;
    public AnimationCurve m_LevelChangeCurve;
    public float m_HoleSpeed;
    public AnimationCurve m_HoleCurve;
    public int m_CircleCount;
    public GameObject m_Level;
    public GameObject m_Background;
    public float m_BackgroundOffset;
    public int m_FinalHeight;
    public float m_IntroFadeSpeed;
    public Image m_Fade;
    public TextMeshProUGUI m_TitleAText;
    public TextMeshProUGUI m_TitleBText;
    public TextMeshProUGUI m_ControlsText;


    public Level m_NeutralLevel;
    public Level[] m_PositiveLevels;
    public Level[] m_NegativeLevels;

    public GameObject m_PeacePrefab;
    public GameObject m_ChaosPrefab;

    private bool _inPeace;
    private bool _inChaos;

    public AudioSource m_CleanMusic;
    public AudioSource m_DirtyMusic;
    public float m_MaxCleanVolume;
    public float m_MaxDirtyVolume;

    private int _currentHeight;
    
    private GameObject _currentLevel;
    private GameObject _currentBackground;
    private Level _currentLevelData;
    private Material _currentBGMat;

    private bool _hasClearedMind;

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

    private bool _paused;
    public bool Paused { get { return _paused;}  }

    public void Quit(InputAction.CallbackContext ctx)
    {
        if (_paused) Application.Quit();
    }

    public void Pause(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed)
        {
            if (_paused)
            {
                _paused = false;
                m_PauseScreen.SetActive(false);
                Time.timeScale = 1f;
            }
            else
            {
                _paused = true;
                m_PauseScreen.SetActive(true);
                Time.timeScale = 0f;
            }
        }
    }


    private void Awake()
    {
        Instance = this;
    }

    public void RestartLevel()
    {
        StartCoroutine("RestartRoutine");
    }

    IEnumerator RestartRoutine()
    {
        float cleanVol = m_CleanMusic.volume;
        float dirtyVol = m_DirtyMusic.volume;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 0.2f;
            if (t > 1f) t = 1f;
            m_Fade.color = new Color(0f, 0f, 0f, t);
            m_CleanMusic.volume = Mathf.Lerp(cleanVol, 0f, t);
            m_DirtyMusic.volume = Mathf.Lerp(dirtyVol, 0f, t);
            yield return null;
        }

        SceneManager.LoadScene(0);
    }

    public void SpawnCircle(bool virtuous)
    {
        _hasClearedMind = true;

        if (_changingLevel && !_inPeace && !_inChaos)
            return;

        if (_inPeace) virtuous = true;
        if (_inChaos) virtuous = false;

        GameObject obj = Instantiate(virtuous?_currentLevelData.m_VirtuousCircle:_currentLevelData.m_ViscousCircle);
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
                {
                    GameObject obj = Instantiate(m_PeacePrefab, new Vector3(0f, _currentHeight*m_HeightBetweenFloors, 0f), Quaternion.identity);
                    _inPeace = true;
                    _inChaos = false;
                }   
                else
                {
                    GameObject obj = Instantiate(m_ChaosPrefab, new Vector3(0f, _currentHeight*m_HeightBetweenFloors, 0f), Quaternion.identity);
                    _inPeace = false;
                    _inChaos = true;
                }

                HUD.Instance.m_Progress.SetActive(false);
                PlayerV2.Instance.enabled = true;
                PlayerV2.Instance.AtEnd();
                HUD.Instance.ShowHUD(false);
                return;
            }
            else _currentHeight = m_FinalHeight*posNeg;
        }


        HUD.Instance.SetLevel(_currentHeight);
        HUD.Instance.ShowHUD(false);
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
        Vector3 pos = player.transform.position;

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

            if (direction > 0) 
                mat.SetVector("_Hole", new Vector4(pos.x, 0f, pos.z, 1f));
            else mat.SetVector("_Hole", new Vector4(pos.x, 0f, pos.z, (i < count-1)?1f:-1f));

            pieces[i] = obj;
            bgs[i] = bg;
        }

        float startCleanVol = m_CleanMusic.volume;
        float startDirtyVol = m_DirtyMusic.volume;
        float finalCleanVol = Mathf.Lerp(0f,m_MaxCleanVolume, (_currentHeight+m_FinalHeight)/(m_FinalHeight*2f));
        float finalDirtyVol = Mathf.Lerp(m_MaxDirtyVolume,0f, (_currentHeight+m_FinalHeight)/(m_FinalHeight*2f));
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * m_HoleSpeed;
            float value = m_HoleCurve.Evaluate(t);

            if (direction > 0)
            {
                _currentBGMat.SetVector("_Hole", new Vector4(pos.x, 0f, pos.z, -value));
            }
            else
            {
                _currentBGMat.SetVector("_Hole", new Vector4(pos.x, 0f, pos.z, value));
            }

            m_CleanMusic.volume = Mathf.Lerp(startCleanVol,finalCleanVol,t);
            m_DirtyMusic.volume = Mathf.Lerp(startDirtyVol,finalDirtyVol,t);


            yield return null;
        }

        m_CleanMusic.volume = finalCleanVol; 
        m_DirtyMusic.volume = finalDirtyVol; 

        t = 0f;
        
        Vector3 targetPos = pos + new Vector3(0, count*m_HeightBetweenFloors*posNeg, 0);
        while (t < 1f)
        {
            t += Time.deltaTime * m_LevelChangeSpeed;
            if (t > 1f) t = 1f;
            player.position = Vector3.Lerp(pos, targetPos, m_LevelChangeCurve.Evaluate(t));
            yield return null;
        }

        player.position = targetPos;

        Material lastMat = bgs[count-1].GetComponent<MeshRenderer>().material;
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime * m_HoleSpeed;
            float value = m_HoleCurve.Evaluate(t);

            if (direction > 0)
            {
                lastMat.SetVector("_Hole", new Vector4(pos.x, 0f, pos.z, value));
            }
            else
            {
                lastMat.SetVector("_Hole", new Vector4(pos.x, 0f, pos.z, -value));
            }

            yield return null;
        }

        

        for (int i = 0; i < count-1; i++)
        {
            Destroy(pieces[i]);
            Destroy(bgs[i]);
        }

        Destroy(_currentLevel);
        Destroy(_currentBackground);

        _currentLevel = pieces[count-1];
        _currentBackground = bgs[count-1]; 
        _currentBGMat = lastMat;


        _changingLevel = false;
        PlayerV2.Instance.enabled = true;
        _currentLevelData = m_NeutralLevel;
        if (_currentHeight > 0) _currentLevelData = m_PositiveLevels[_currentHeight-1];
        else if (_currentHeight < 0) _currentLevelData = m_NegativeLevels[Mathf.Abs(_currentHeight)-1];

        HUD.Instance.SetHud(_currentLevelData.requirement.positivity, Mathf.Abs(_currentLevelData.requirement.negativity));
        HUD.Instance.ShowHUD(true);
        PlayerV2.Instance.StartLevel(_currentLevelData.requirement);

        

        for (int i = 0; i < _currentLevelData.circleAmount-2; i++)
            SpawnCircle(Random.value<_currentLevelData.positivity);

        SpawnCircle(false);
        SpawnCircle(true);
    }


    private void ChangeLevelTest()
    {
        ChangeLevel(-3, PlayerV2.Instance.transform);
    }

    IEnumerator StartGame()
    {
        m_CleanMusic.Play();
        m_DirtyMusic.Play();
        float targetCleanVol = Mathf.Lerp(0f,m_MaxCleanVolume, 0.5f);
        float targetDirtyVol = Mathf.Lerp(0f,m_MaxDirtyVolume, 0.5f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * m_IntroFadeSpeed;
            if (t > 1f) t = 1f;
            m_Fade.color = new Color(0f, 0f, 0f, 1f-t);
            m_CleanMusic.volume = Mathf.Lerp(0f, targetCleanVol, t);
            m_DirtyMusic.volume = Mathf.Lerp(0f, targetDirtyVol, t);
            yield return null;
        }

        Color cA = m_TitleAText.color;
        Color cB = m_TitleBText.color;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * m_IntroFadeSpeed;
            if (t > 1f) t = 1f;
            cA.a = t;
            cB.a = t; 
            m_TitleAText.color = cA;
            m_TitleBText.color = cB;
            yield return null;
        }

        PlayerV2.Instance.enabled = true;

        cA = m_ControlsText.color;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * m_IntroFadeSpeed;
            if (t > 1f) t = 1f;
            cA.a = 1f;
            m_ControlsText.color = cA;
            yield return null;
        }


        while (!_hasClearedMind)
        {
            yield return null;
        }

        m_TitleAText.enabled = false;
        m_TitleBText.enabled = false;
        m_ControlsText.enabled = false;

        Begin();
    }

    private void Begin()
    {
        HUD.Instance.ShowHUD(true);
        HUD.Instance.SetLevel(_currentHeight);
        HUD.Instance.SetHud(_currentLevelData.requirement.positivity, Mathf.Abs(_currentLevelData.requirement.negativity));
        PlayerV2.Instance.StartLevel(m_NeutralLevel.requirement);

        for (int i = 0; i < m_CircleCount; i++)
            SpawnCircle(Random.value<m_NeutralLevel.positivity);
    }

    private void Start()
    {
        _hasClearedMind = false;

        _currentLevel = Instantiate(m_Level);
        _currentBackground = Instantiate(m_Background);
        _currentBGMat = _currentBackground.GetComponent<MeshRenderer>().material;
        _currentBGMat.SetColor("_Color", m_NeutralLevel.backgroundColor);
        _currentBGMat.SetFloat("_Intensity", m_NeutralLevel.backgroundIntensity);
        _currentLevelData = m_NeutralLevel;

        PlayerV2.Instance.enabled = false;

        StartCoroutine("StartGame");
        //Invoke("ChangeLevelTest", 5f);

        GameObject startCircle = Instantiate(m_ViscousCircle);
        startCircle.transform.position = Vector3.zero;

        Loop loop = startCircle.GetComponent<Loop>();
        loop.Spawn();

        startCircle.transform.SetParent(_currentLevel.transform, true);
    }
}
