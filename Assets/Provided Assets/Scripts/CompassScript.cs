using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CompassScript : MonoBehaviour
{
    public Vector3 NorthDirection;
    public Transform player;

    public RectTransform NorthLayer;
    public Rect MissionLayer;
    void Start()
    {
        
       
    }

    void Update()
    {
        ChangeNorthDirection();
        ChangeMissionDirection();

       
    }
    void ChangeNorthDirection()
    {
        NorthDirection.z = player.eulerAngles.y;
        NorthLayer.localEulerAngles = NorthDirection;
    }
    void ChangeMissionDirection()
    {

    }
}