using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISpawner : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnEnable()
    {
        if (spawnObject)
        {
            spawnObject.SetActive(true);
        }
    }
    private void Start()
    {
        if (Random.Range(0.0f, 1.0f)<=probability)
        {
            spawnObject = Instantiate(enemyPrefab, new Vector3(transform.position.x, transform.position.y - 1, transform.position.z), Quaternion.identity);
        }else if( Random.Range(0.0f, 1.0f)> probability)
        {
            spawnObject = Instantiate(npcPrefab, new Vector3(transform.position.x, transform.position.y-1, transform.position.z), Quaternion.identity);
        }
    }
    [Range(0.0f,1.0f)]
    public float probability=0.4f;
    public GameObject enemyPrefab;
    public GameObject npcPrefab;
    private GameObject spawnObject;

    private void OnDisable()
    {
        if (spawnObject)
        {
            spawnObject.SetActive(false);
        }
    }
}
