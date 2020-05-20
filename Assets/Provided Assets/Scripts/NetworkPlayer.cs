using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;
using VoxelPlay;

public class NetworkPlayer : MonoBehaviour,IPunObservable
{
    public PhotonView pv;
    public CharacterController cc;
    public VoxelPlayPlayer vpp;
    public VoxelPlayFirstPersonController vpfpc;
    public GameObject CameraObject;
    public SkinnedMeshRenderer playerHead;
    private void Awake()
    {
        if (PhotonNetwork.IsConnected && !pv.IsMine)
        {
            DisablePlayerControls();
        }
    }
    void DisablePlayerControls()
    {
        cc.enabled = false;
        vpp.enabled = false;
        vpfpc.enabled = false;
        CameraObject.SetActive(false);
        playerHead.enabled = true;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            Vector3 pos = transform.localPosition;
            Quaternion rot = transform.rotation;
            stream.Serialize(ref pos);
            stream.Serialize(ref rot);
        }
        else
        {
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            stream.Serialize(ref pos);  // pos gets filled-in. must be used somewhere
            stream.Serialize(ref rot);
            transform.position = pos;
            transform.rotation = rot;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
