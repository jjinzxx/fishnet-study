using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.API;
using UnityEngine;
using UnityEngine.UI;
using FishyTransport = FishySteamworks.FishySteamworks;

[DisallowMultipleComponent]
public sealed class MainArrivalController : MonoBehaviour
{
    [Header("Main UI")]
    [SerializeField] private Text _connectionStatusText;

    private void Start()
    {
        if (_connectionStatusText == null)
        {
            Debug.LogWarning(
                "Main 연결 상태 Text 참조가 연결되지 않았습니다.");

            return;
        }

        // Steam API가 종료된 상태에서 Steam 정보를 읽으면
        // InvalidOperationException이 발생할 수 있으므로 먼저 확인합니다.
        if (!SteamSettings.Initialized ||
            !App.Client.LoggedOn)
        {
            _connectionStatusText.text =
                "세션 확인 실패\n" +
                "Steam API 초기화와 로그인을 확인해 주세요.";

            Debug.LogWarning(
                "Main에서 Steam 세션 정보를 확인할 수 없습니다.");

            return;
        }

        // NetworkManager는 Title에서 DontDestroyOnLoad로
        // 유지되므로 Main Inspector에 직접 연결하지 않습니다.
        NetworkManager networkManager =
            InstanceFinder.NetworkManager;

        if (networkManager == null ||
            !networkManager.Initialized ||
            networkManager.ClientManager == null ||
            networkManager.TransportManager == null)
        {
            _connectionStatusText.text =
                "세션 확인 실패\n" +
                "Title 씬에서 멀티플레이 흐름을 시작해 주세요.";

            Debug.LogWarning(
                "Main에서 유지된 FishNet NetworkManager를 " +
                "찾지 못했습니다.");

            return;
        }

        NetworkConnection localConnection =
            networkManager.ClientManager.Connection;

        if (!networkManager.IsClientStarted ||
            localConnection == null ||
            !localConnection.IsActive ||
            !localConnection.IsAuthenticated)
        {
            _connectionStatusText.text =
                "세션 확인 실패\n" +
                "FishNet 연결 또는 인증이 유지되지 않았습니다.";

            Debug.LogWarning(
                "Main에서 인증된 FishNet 연결을 " +
                "확인하지 못했습니다.");

            return;
        }

        FishyTransport fishyTransport =
            networkManager.TransportManager.Transport
                as FishyTransport;

        if (fishyTransport == null)
        {
            _connectionStatusText.text =
                "세션 확인 실패\n" +
                "현재 Transport가 FishySteamworks가 아닙니다.";

            Debug.LogWarning(
                "Main에서 FishySteamworks Transport를 " +
                "확인하지 못했습니다.");

            return;
        }

        UserData localUser =
            UserData.Me;

        if (!localUser.IsValid)
        {
            _connectionStatusText.text =
                "세션 확인 실패\n" +
                "로컬 Steam 사용자 정보를 확인하지 못했습니다.";

            Debug.LogWarning(
                "Main에서 로컬 Steam 사용자 정보가 " +
                "유효하지 않습니다.");

            return;
        }

        // Title의 LobbyManager는 제거되었지만
        // 실제 Session Lobby 멤버십은 유지됩니다.
        if (!LobbyData.SessionLobby(
                out LobbyData lobby) ||
            !lobby.IsValid)
        {
            _connectionStatusText.text =
                "세션 확인 실패\n" +
                "현재 Steam Session Lobby를 찾지 못했습니다.";

            Debug.LogWarning(
                "Main에서 Steam Session Lobby를 " +
                "다시 가져오지 못했습니다.");

            return;
        }

        if (!lobby.IsAMember(localUser.id))
        {
            _connectionStatusText.text =
                "세션 확인 실패\n" +
                "현재 Steam 사용자가 Lobby 멤버가 아닙니다.";

            Debug.LogWarning(
                "현재 Steam 사용자가 Session Lobby에 " +
                "포함되어 있지 않습니다.");

            return;
        }

        bool isHost =
            networkManager.IsHostStarted;

        bool isGuest =
            networkManager.IsClientOnlyStarted;

        if (!isHost &&
            !isGuest)
        {
            _connectionStatusText.text =
                "세션 확인 실패\n" +
                "FishNet Host 또는 Guest 역할을 구분하지 못했습니다.";

            Debug.LogWarning(
                "Main에서 FishNet 실행 역할을 " +
                "확인하지 못했습니다.");

            return;
        }

        bool isLobbyOwner =
            lobby.IsOwner;

        bool rolesMatch =
            (isLobbyOwner && isHost) ||
            (!isLobbyOwner && isGuest);

        ulong heathenSteamId =
            localUser.SteamId;

        ulong fishySteamId =
            fishyTransport.LocalUserSteamID;

        bool steamIdsMatch =
            heathenSteamId != 0 &&
            heathenSteamId == fishySteamId;

        int authenticatedConnectionCount = 0;

        string authenticatedConnectionText =
            "Host에서 확인";

        bool authenticatedCountMatches =
            true;

        // 전체 연결 목록은 서버 권한 정보이므로
        // Host에서만 확인합니다.
        if (isHost &&
            networkManager.ServerManager != null)
        {
            foreach (NetworkConnection connection in
                     networkManager.ServerManager.Clients.Values)
            {
                if (connection != null &&
                    connection.IsActive &&
                    connection.IsAuthenticated)
                {
                    authenticatedConnectionCount++;
                }
            }

            authenticatedConnectionText =
                $"{authenticatedConnectionCount} / " +
                $"{lobby.MemberCount}";

            authenticatedCountMatches =
                authenticatedConnectionCount ==
                lobby.MemberCount;
        }

        bool validationPassed =
            rolesMatch &&
            steamIdsMatch &&
            authenticatedCountMatches;

        string steamName =
            string.IsNullOrWhiteSpace(localUser.Name)
                ? "(이름 확인 실패)"
                : localUser.Name;

        string lobbyName =
            string.IsNullOrWhiteSpace(lobby.Name)
                ? "(이름 없음)"
                : lobby.Name;

        string fishNetRoleText =
            isHost
                ? "Host"
                : "Guest";

        string steamRoleText =
            isLobbyOwner
                ? "방장"
                : "참가자";

        string serverStateText =
            networkManager.IsServerStarted
                ? "실행 중"
                : "실행하지 않음";

        string steamIdMatchText =
            steamIdsMatch
                ? "일치"
                : "불일치";

        string roleMatchText =
            rolesMatch
                ? "정상"
                : "불일치";

        string validationText =
            validationPassed
                ? "정상"
                : "확인 필요";

        string statusText =
            "Main 씬 도착 및 세션 확인\n" +
            $"검증 결과: {validationText}\n\n" +
            $"Steam 사용자: {steamName}\n" +
            $"Steam App ID: {App.Client.Id.m_AppId}\n" +
            $"Heathen Steam ID64: {heathenSteamId}\n" +
            $"Fishy Steam ID64: {fishySteamId}\n" +
            $"Steam ID 대응: {steamIdMatchText}\n\n" +
            $"Lobby 이름: {lobbyName}\n" +
            $"Lobby ID64: {lobby.SteamId.m_SteamID}\n" +
            $"Lobby Owner ID64: {lobby.Owner.user.SteamId}\n" +
            $"Lobby 인원: {lobby.MemberCount} / " +
            $"{lobby.MaxMembers}\n" +
            $"Steam Lobby 역할: {steamRoleText}\n\n" +
            $"FishNet 역할: {fishNetRoleText}\n" +
            $"FishNet Client ID: {localConnection.ClientId}\n" +
            "FishNet 인증: 완료\n" +
            $"FishNet Server: {serverStateText}\n" +
            $"Host 인증 인원: {authenticatedConnectionText}\n" +
            $"세션 역할 대응: {roleMatchText}\n" +
            $"현재 씬: {gameObject.scene.name}";

        _connectionStatusText.text =
            statusText;

        if (validationPassed)
        {
            Debug.Log(
                "Main 세션 검증 완료\n" +
                statusText);
        }
        else
        {
            Debug.LogWarning(
                "Main 세션 검증 결과를 확인해 주세요.\n" +
                statusText);
        }
    }
}