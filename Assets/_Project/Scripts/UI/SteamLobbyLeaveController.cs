using FishNet.Managing;
using FishNet.Transporting;
using HeathenEngineering.SteamworksIntegration;
using UnityEngine;
using UnityEngine.UI;


public sealed class SteamLobbyLeaveController : MonoBehaviour
{
    [Header("Steam Lobby")]
    [SerializeField] private LobbyManager _lobbyManager;

    [Header("FishNet")]
    [SerializeField] private NetworkManager _networkManager;

    [Header("화면")]
    [SerializeField] private GameObject _multiplayerLobbyPanel;
    [SerializeField] private GameObject _roomWaitingPanel;

    [Header("다시 생성하기")]
    [SerializeField] private Button _confirmCreateRoomButton;

    public void LeaveSteamLobby()
    {
        if (_lobbyManager == null ||
            _networkManager == null ||
            _multiplayerLobbyPanel == null ||
            _roomWaitingPanel == null ||
            _confirmCreateRoomButton == null)
        {
            Debug.LogWarning(
                "Steam Lobby 퇴장에 필요한 참조가 연결되지 않았습니다.");

            return;
        }

        if (!SteamSettings.Initialized)
        {
            Debug.LogWarning(
                "Steam API가 초기화되지 않았습니다.");

            return;
        }

        if (!_networkManager.Initialized ||
            _networkManager.TransportManager == null ||
            _networkManager.TransportManager.Transport == null)
        {
            Debug.LogWarning(
                "FishNet NetworkManager 또는 Transport가 " +
                "초기화되지 않았습니다.");

            return;
        }

        if (!_lobbyManager.HasLobby)
        {
            Debug.LogWarning(
                "현재 참가 중인 Steam Lobby가 없습니다.");

            return;
        }

        // LobbyManager.Leave를 호출하면 현재 Lobby 정보가 비워지므로
        // 로그에 사용할 정보는 퇴장 전에 보관합니다.
        LobbyData leavingLobby =
            _lobbyManager.Lobby;

        ulong lobbyId =
            leavingLobby.SteamId.m_SteamID;

        bool wasOwner =
            leavingLobby.IsOwner;

        int previousMemberCount =
            leavingLobby.MemberCount;

        Transport transport =
            _networkManager.TransportManager.Transport;

        LocalConnectionState clientStateBefore =
            transport.GetConnectionState(false);

        LocalConnectionState serverStateBefore =
            transport.GetConnectionState(true);

        // Host와 Guest 모두 Local Client부터 종료합니다.
        if (clientStateBefore != LocalConnectionState.Stopped)
        {
            _networkManager.ClientManager.StopConnection();
        }

        // Server가 실행 중인 Host만 Server 종료가 처리됩니다.
        if (serverStateBefore != LocalConnectionState.Stopped)
        {
            // true를 전달하여 접속 중인 원격 Client에게
            // 연결 종료 메시지를 먼저 보냅니다.
            _networkManager.ServerManager.StopConnection(true);
        }

        LocalConnectionState clientStateAfter =
            transport.GetConnectionState(false);

        LocalConnectionState serverStateAfter =
            transport.GetConnectionState(true);

        // FishNet 연결을 먼저 정리한 뒤 Steam Lobby에서 나갑니다.
        _lobbyManager.Leave();

        _confirmCreateRoomButton.interactable = true;

        _roomWaitingPanel.SetActive(false);
        _multiplayerLobbyPanel.SetActive(true);

        Debug.Log(
            "FishNet 연결 및 Steam Lobby 퇴장 완료\n" +
            $"Lobby ID64: {lobbyId}\n" +
            $"퇴장 전 인원: {previousMemberCount}\n" +
            $"퇴장 전 방장이었는가: {wasOwner}\n" +
            $"FishNet Client: " +
            $"{clientStateBefore} → {clientStateAfter}\n" +
            $"FishNet Server: " +
            $"{serverStateBefore} → {serverStateAfter}\n" +
            $"현재 Lobby 보유 여부: " +
            $"{_lobbyManager.HasLobby}");
    }
}