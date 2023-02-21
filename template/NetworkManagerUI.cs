using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button HostButton;
    [SerializeField] private Button ClientButton;
    // Start is called before the first frame update
    private void Awake()
    {
        serverButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
        });
        HostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
        });
        ClientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
        });
    }
}
