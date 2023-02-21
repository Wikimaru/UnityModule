using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Transform spawnObjectPrefab;
    private Transform spawnObjectTranform;

    private NetworkVariable<int> randomNumber = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
    private NetworkVariable<MyCustomData> NiceData = new NetworkVariable<MyCustomData>(new MyCustomData { NiceInt = 1, NiceBool = true, NiceString = "Nice" }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public struct MyCustomData : INetworkSerializable
    {
        public int NiceInt;
        public bool NiceBool;
        public FixedString128Bytes NiceString;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref NiceInt);
            serializer.SerializeValue(ref NiceBool);
            serializer.SerializeValue(ref NiceString);
        }
    }

    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += (int previousValue, int newValue) => {
            Debug.Log(OwnerClientId + "; random Number:" + randomNumber.Value);
            Debug.Log(OwnerClientId + ";  Nice Data:" + NiceData.Value.NiceInt);
        };
    }

    void Update()
    {
        if (!IsOwner) return;
        if (Input.GetKey(KeyCode.T))
        {
            spawnObjectTranform = Instantiate(spawnObjectPrefab);
            spawnObjectTranform.GetComponent<NetworkObject>().Spawn(true);
            TestServerRpc(new ServerRpcParams());
            TestClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { 1 } } });
            /*
            randomNumber.Value = Random.Range(0, 100);
            */
        }
        if (Input.GetKey(KeyCode.Y))
        {
            Destroy(spawnObjectTranform.gameObject);
            /*
            randomNumber.Value = Random.Range(0, 100);
            */
        }

        Vector3 moveDir = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.W)) moveDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;

        float moveSpeed = 3f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }

    [ServerRpc]
    private void TestServerRpc(ServerRpcParams serverRpcParams)
    {
        //Client ==> Server/Host(Code Only on Server)
        Debug.Log("TestServerRpc" + OwnerClientId + ":" + serverRpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void TestClientRpc(ClientRpcParams clientRpcParams)
    {
        //Server/Host ==> Client
        Debug.Log("TestClientRpc");
    }


}
