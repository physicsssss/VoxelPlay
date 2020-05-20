using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

public sealed class NetworkHandler : MonoBehaviour
{
    private static NetworkHandler _instance;
    GameObject instancedNetworkPlayer;
    public static NetworkHandler instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NetworkHandler>();
            }
            return _instance;
        }
    }
    void OnInitialized()
    {
        if (Application.isPlaying)
        {
            if (!instance.InstantiatePlayer())
            {
                Debug.LogError("Player not instantiated");
            }
            env.distanceAnchor = instancedNetworkPlayer.transform;
            Invoke("EnvironmentRedraw", 2f);
        }
    }
    void EnvironmentRedraw()
    {
        env.Redraw();
    }
    private NetworkHandler()
    {

    }
    NetworkManager_Photon nmp;
    VoxelPlayEnvironment env;
    public Transform spawnPoint;
    public GameObject networkPlayerPrefab;
    public GameObject tempCamera;
    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject LocalPlayerInstance;
    // Start is called before the first frame update
    void Start()
    {
        if(GameObject.FindGameObjectWithTag("NetworkManager"))
        nmp = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<NetworkManager_Photon>();
        env = VoxelPlayEnvironment.instance;
        env.OnInitialized += OnInitialized;

        if (!nmp)
        {
            Debug.LogError("Network Manager Not Found");
        }
        if (!env)
        {
            Debug.LogError("Voxel Play Environment not found");
        }

    }
    /// <summary>
    /// True when player is instantiated susccessfully
    /// </summary>
    /// <returns></returns>
    public bool InstantiatePlayer()
    {
        if (!networkPlayerPrefab)
        {
            return false;
        }
        Destroy(tempCamera);
        instancedNetworkPlayer = PhotonNetwork.Instantiate(networkPlayerPrefab.name, new Vector3(spawnPoint.position.x + Random.Range(0, 100), spawnPoint.position.y, spawnPoint.position.z + Random.Range(0, 100)), Quaternion.identity);
        return true;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
