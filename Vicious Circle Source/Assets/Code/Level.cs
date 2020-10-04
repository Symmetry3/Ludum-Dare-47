using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Level")]
public class Level : ScriptableObject
{
    public Requirement requirement;
    public int circleAmount;
    [Range(0f,1f)]
    public float positivity;

    public Color backgroundColor;
    [Range(0f,1f)]
    public float backgroundIntensity;

    public GameObject m_VirtuousCircle;
    public GameObject m_ViscousCircle;
}
