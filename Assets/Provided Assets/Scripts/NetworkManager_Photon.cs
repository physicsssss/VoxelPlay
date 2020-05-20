using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun.Demo.Asteroids;

public class NetworkManager_Photon : MonoBehaviourPunCallbacks
{
    [Header("Menus Objects")]
    public GameObject mainMenu;
    public Transform lobbyContainer;
    public GameObject createRoomMenu;
    public GameObject lobbyMenu;

    [Header("Room Creation Menu")]
    public InputField roomNameField;

    [Header("Prefabs")]
    public GameObject roomListTextPrefab;
    void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("NetworkManager");

        if (objs.Length > 1)
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);
    }
    // Start is called before the first frame update
    void Start()
    {
    }
    #region Photon_Overrides
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        
        base.OnConnectedToMaster();
    }
    public override void OnJoinedLobby()
    {
        mainMenu.SetActive(false);
        lobbyMenu.SetActive(true);
        base.OnJoinedLobby();
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo ri in roomList)
        {
            Instantiate(roomListTextPrefab, lobbyContainer).GetComponent<RoomListEntry>().Initialize(ri.Name,(byte)ri.PlayerCount,ri.MaxPlayers);
        }
        base.OnRoomListUpdate(roomList);
    }
   
    public override void OnCreatedRoom()
    {
        Debug.Log("Room Created");
        base.OnCreatedRoom();
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Create Room Failed");
        base.OnCreateRoomFailed(returnCode, message);
    }
    
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Join Room Failed");
        base.OnJoinRoomFailed(returnCode, message);
    }
    public override void OnJoinedRoom()
    {
        SceneManager.LoadScene(1);
        base.OnJoinedRoom();
    }

    #endregion

    #region Network_Connection_Buttons
    public void CreateNetworkRoom()
    {
        if (roomNameField.text != "") {
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 16;
            roomOptions.IsOpen = true;
            roomOptions.IsVisible = true;

            PhotonNetwork.CreateRoom(roomNameField.text, roomOptions);
        }
        else
        {
            Debug.LogError("Input Field Empty");
        }
    }

    public void JoinNetworkRoom(string _name)
    {
        PhotonNetwork.JoinRoom(_name);
    }

    public void ConnectToMaster()
    {
        PhotonNetwork.ConnectUsingSettings();
    }
    #endregion
    // Update is called once per frame
    void Update()
    {
        
    }
   
}
