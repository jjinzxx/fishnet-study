using System;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using HeathenEngineering.SteamworksIntegration;
using UnityEngine;
using UnityEngine.UI;

public sealed class SteamLobbyMemberController : MonoBehaviour
{
    [Header("Steam Lobby")]
    [SerializeField] private LobbyManager _lobbyManager;

    [Header("FishNet")]
    [SerializeField] private NetworkManager _networkManager;

    [Header("Lobby 대기실 UI")]
    [SerializeField] private Text _waitingRoomSettingsText;
    [SerializeField] private Text _waitingMemberListText;
    [SerializeField] private Text _roomStatusText;

    private bool _fishNetEventsSubscribed;

    public void RefreshWaitingRoomMembers()
    {
        if (_lobbyManager == null ||
            _networkManager == null ||
            _waitingRoomSettingsText == null ||
            _waitingMemberListText == null ||
            _roomStatusText == null)
        {
            Debug.LogWarning(
                "Steam Lobby 멤버 UI 참조가 연결되지 않았습니다.");

            return;
        }

        if (!SteamSettings.Initialized)
        {
            Debug.LogWarning(
                "Steam API가 초기화되지 않았습니다.");

            return;
        }

        // 로컬 사용자가 Lobby를 나가는 과정에서도
        // 멤버 퇴장 이벤트가 전달될 수 있으므로 조용히 종료합니다.
        if (!_lobbyManager.HasLobby)
        {
            return;
        }

        if (!_networkManager.Initialized ||
            _networkManager.ServerManager == null ||
            _networkManager.ClientManager == null)
        {
            Debug.LogWarning(
                "FishNet NetworkManager가 초기화되지 않았습니다.");

            return;
        }

        LobbyData lobby = _lobbyManager.Lobby;

        _roomStatusText.gameObject.SetActive(true);


        // Heathen의 evtCreated는 CreateSteamLobby 완료 콜백보다
        // 먼저 호출되므로 Host 시작 전 이벤트를 등록할 수 있습니다.
        if (lobby.IsOwner &&
            !_fishNetEventsSubscribed)
        {
            var serverManager =
                _networkManager.ServerManager;

            Action<NetworkConnection, bool>
                onAuthenticationResult =
                    (connection, authenticated) =>
                    {
                        if (!authenticated)
                        {
                            return;
                        }

                        string transportAddress = connection.GetAddress();

                        if (ulong.TryParse(
                            transportAddress,
                            out ulong steamId64) &&
                            steamId64 != 0)
                        {
                            Debug.Log(
                                "Host가 FishNet Client 인증과 Steam ID 매핑을 확인했습니다.\n" +
                                $"FishNet Client ID: {connection.ClientId}\n" +
                                $"Steam ID64: {steamId64}");
                        }
                        else
                        {
                            Debug.LogWarning(
                                "FishNet Client ID에 대응하는 Steam ID64를 확인하지 못했습니다.\n" +
                                $"FishNet Client ID: {connection.ClientId}\n" +
                                $"Transport 주소: {transportAddress}");
                        }

                        RefreshWaitingRoomMembers();
                    };

            Action<NetworkConnection, RemoteConnectionStateArgs>
                onRemoteConnectionState =
                    (connection, state) =>
                    {
                        if (state.ConnectionState !=
                                RemoteConnectionState.Started &&
                            state.ConnectionState !=
                                RemoteConnectionState.Stopped)
                        {
                            return;
                        }

                        Debug.Log(
                            "Host의 FishNet 원격 연결 상태 변경\n" +
                            $"FishNet Client ID: {connection.ClientId}\n" +
                            $"State: {state.ConnectionState}");

                        RefreshWaitingRoomMembers();
                    };

            _fishNetEventsSubscribed = true;

            serverManager.OnAuthenticationResult +=
                onAuthenticationResult;

            serverManager.OnRemoteConnectionState +=
                onRemoteConnectionState;

            // NetworkManager는 DontDestroyOnLoad이므로
            // 이 UI가 파괴될 때 등록한 이벤트를 반드시 제거합니다.
            destroyCancellationToken.Register(
                () =>
                {
                    if (serverManager == null)
                    {
                        return;
                    }

                    serverManager.OnAuthenticationResult -=
                        onAuthenticationResult;

                    serverManager.OnRemoteConnectionState -=
                        onRemoteConnectionState;
                });
        }

        LobbyMemberData[] members =
            lobby.Members;

        string memberListText =
            string.Empty;

        foreach (LobbyMemberData member in members)
        {
            string memberName =
                string.IsNullOrWhiteSpace(member.user.Name)
                    ? member.user.SteamId.ToString()
                    : member.user.Name;

            string roleText =
                string.Empty;

            if (member.IsOwner &&
                member.user.IsMe)
            {
                roleText = " (방장, 나)";
            }
            else if (member.IsOwner)
            {
                roleText = " (방장)";
            }
            else if (member.user.IsMe)
            {
                roleText = " (나)";
            }

            if (memberListText.Length > 0)
            {
                memberListText += "\n";
            }

            memberListText +=
                $"{memberName}{roleText}";
        }

        ulong lobbyId =
            lobby.SteamId.m_SteamID;

        ulong ownerSteamId =
            lobby.Owner.user.SteamId;

        _waitingRoomSettingsText.text =
            $"Lobby ID64: {lobbyId}\n" +
            $"Host ID64: {ownerSteamId}\n" +
            $"인원: {lobby.MemberCount} / " +
            $"{lobby.MaxMembers}";

        _waitingMemberListText.text =
            memberListText;

        int authenticatedCount = 0;
        bool guestAuthenticated = false;

        if (lobby.IsOwner)
        {
            foreach (NetworkConnection connection in
                     _networkManager.ServerManager.Clients.Values)
            {
                if (connection != null &&
                    connection.IsActive &&
                    connection.IsAuthenticated)
                {
                    authenticatedCount++;
                }
            }

            if (!_networkManager.ServerManager.Started)
            {
                _roomStatusText.text =
                    "FishNet Host 연결을 시작하는 중입니다.";
            }
            else if (authenticatedCount < lobby.MemberCount)
            {
                _roomStatusText.text =
                    $"FishNet 인증 인원: {authenticatedCount} / " +
                    $"{lobby.MemberCount} · 연결을 기다리는 중입니다.";
            }
            else if (authenticatedCount > lobby.MemberCount)
            {
                _roomStatusText.text =
                    $"Steam 멤버 정보 동기화 중 · Steam " +
                    $"{lobby.MemberCount}명 / FishNet 인증 " +
                    $"{authenticatedCount}명";
            }
            else if (lobby.MemberCount < 2)
            {
                _roomStatusText.text =
                    $"FishNet 인증 인원: {authenticatedCount} / " +
                    $"{lobby.MemberCount} · 다른 사용자를 기다리는 중입니다.";
            }
            else
            {
                _roomStatusText.text =
                    $"FishNet 인증 인원: {authenticatedCount} / " +
                    $"{lobby.MemberCount} · 현재 멤버 인증 완료";
            }
        }
        else
        {
            NetworkConnection localConnection =
                _networkManager.ClientManager.Connection;

            guestAuthenticated =
                localConnection != null &&
                localConnection.IsActive &&
                localConnection.IsAuthenticated;

            // Steam 멤버 이벤트가 다시 발생해도
            // (19)의 인증 완료 문구가 이전 문구로 덮이지 않게 합니다.
            _roomStatusText.text =
                guestAuthenticated
                    ? "FishNet Guest 연결 및 인증 완료"
                    : "FishNet Guest 연결 및 인증을 기다리는 중입니다.";
        }

        string fishNetAuthenticatedText =
            lobby.IsOwner
                ? authenticatedCount.ToString()
                : guestAuthenticated
                    ? "1"
                    : "0";

        Debug.Log(
            "Steam Lobby / FishNet 멤버 UI 갱신\n" +
            $"Lobby ID64: {lobbyId}\n" +
            $"Host ID64: {ownerSteamId}\n" +
            $"Steam Lobby 인원: {lobby.MemberCount} / " +
            $"{lobby.MaxMembers}\n" +
            $"FishNet 인증 인원: {fishNetAuthenticatedText}\n" +
            $"내가 방장인가: {lobby.IsOwner}\n" +
            $"멤버 목록:\n{memberListText}");
    }
}