using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestLobby : MonoBehaviour
{
    [SerializeField] private Button CreateButton;
    [SerializeField] private Button JoinButton;
    [SerializeField] private Button FindButton;
    [SerializeField] private Button JoinWithCodeButton;
    [SerializeField] private TMP_InputField CodeRoom;
    [SerializeField] private Button PrintButton;
    [SerializeField] private Button LeaveButton;

    private string playerName;

    private Lobby hostLobby;
    private Lobby JoinedLobby;
    private float heartbeatTimer;
    private float lobbyUpdateTimer;

    private void Awake()
    {
        CreateButton.onClick.AddListener(() => {
            CreateLobby();
        });
        JoinButton.onClick.AddListener(() => {
            JoinLobby();
        });
        FindButton.onClick.AddListener(() => {
            ListLobbies();
        });
        JoinWithCodeButton.onClick.AddListener(() => {
            JoinLobbyByCode(CodeRoom.text);
        });
        PrintButton.onClick.AddListener(() => {
            Printplayers();
        });
        LeaveButton.onClick.AddListener(() => {
            LeaveLobby();
        });
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
         {
             Debug.Log("Singed in " + AuthenticationService.Instance.PlayerId);
         };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        playerName = "NiceGuy" + UnityEngine.Random.Range(10, 99);
        Debug.Log(playerName);
    }

    private void Update()
    {
        HandleLobbyHeartBeat();
        HandleLoobyPollForUpdates();
    }

    private async void HandleLobbyHeartBeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if(heartbeatTimer < 0f) 
            {
                float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }


    private async void CreateLobby() 
    {
        try {
            string lobbyName = "MyLobby";
            int maxPlayer = 4;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = true,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject> 
                {
                    { "GameMode",new DataObject(DataObject.VisibilityOptions.Public,"CaptureTheFlag")},
                    { "Map",new DataObject(DataObject.VisibilityOptions.Public,"NiceMap")}
                }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayer,createLobbyOptions);

            hostLobby = lobby;
            JoinedLobby = hostLobby;

            Debug.Log("Created Lobby!" + lobby.Name + " " + lobby.MaxPlayers + " "+ lobby.Id + " " + lobby.LobbyCode);

            Printplayers(hostLobby);
            //For Debug Only
            CodeRoom.text = lobby.LobbyCode;
            //==============
        }
        catch (LobbyServiceException e) 
        {
            Debug.Log(e);
        }

    }

    private async void ListLobbies()
    {
        try 
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter> 
                { 
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT) ,
                    new QueryFilter(QueryFilter.FieldOptions.S1,"CaptureTheFlag",QueryFilter.OpOptions.EQ)
                },
                Order = new List<QueryOrder> { new QueryOrder(false, QueryOrder.FieldOptions.Created) }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value);
            } 
        }catch (LobbyServiceException e) 
        {
            Debug.Log(e);
        }
    }

    private async void JoinLobby() 
    {
        try
        {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            Debug.Log(queryResponse.Results[0].Id);
            await Lobbies.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode,joinLobbyByCodeOptions);
            JoinedLobby = lobby;
            Printplayers(JoinedLobby);

            Debug.Log("Joined Lobby with code " + lobbyCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void QuickJoinLobby() 
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private Player GetPlayer() 
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,playerName) }
                    }
        };
    }

    private void Printplayers() 
    {
        Printplayers(JoinedLobby);
    }

    private void Printplayers(Lobby lobby) 
    {
        Debug.Log("Player in Lobby " + lobby.Name + " " + lobby.Data["GameMode"].Value + " " + lobby.Data["Map"].Value);
        foreach(Player player in lobby.Players) 
        {
            Debug.Log(player.Id+" "+player.Data["PlayerName"].Value);
        }
    }
    private async void HandleLoobyPollForUpdates() 
    {
        if(JoinedLobby != null) 
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if(lobbyUpdateTimer < 0f) 
            {
                float lobbyUpdateTimerMax = 1.5f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(JoinedLobby.Id);
                JoinedLobby = lobby;
            }
        }
    }

    private async void UpdateLobbyGameMode(string gameMode) 
    {
        try 
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                {"GameMode",new DataObject(DataObject.VisibilityOptions.Public,gameMode) }
            }
            });
            JoinedLobby = hostLobby;
        } catch (LobbyServiceException e) 
        {
            Debug.Log(e);
        }
    }

    private async void UpdatePlayerName(string newPlayerName)
    {
        try 
        {
            playerName = newPlayerName;
            await LobbyService.Instance.UpdatePlayerAsync(JoinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
            {
                {"PlayerName",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,playerName) }
            }
            });
        }
        catch ( LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void LeaveLobby() 
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(JoinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e) 
        {
            Debug.Log(e);
        }
    }

    private async void MigrateLobbyHost() 
    {
        try 
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions 
            {
                HostId = JoinedLobby.Players[1].Id
            });
            JoinedLobby = hostLobby;

            Printplayers(hostLobby);
        }catch (LobbyServiceException e) 
        {
            Debug.Log(e);
        }
    }

    private async void DeleteLobby() 
    {
        try 
        {
            await LobbyService.Instance.DeleteLobbyAsync(JoinedLobby.Id);
        }catch(LobbyServiceException e) 
        {
            Debug.Log(e);
        }
    }

}
