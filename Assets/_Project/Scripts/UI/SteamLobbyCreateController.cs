using FishNet.Managing;
using FishNet.Transporting;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.API;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public sealed class SteamLobbyCreateController : MonoBehaviour
{
    private const string StudyLobbyKey = "study_id";
    private const string StudyLobbyValue = "fishnet_study_v1";

    [Header("Steam Lobby")]
    [SerializeField] private LobbyManager _lobbyManager;

    [Header("FishNet")]
    [SerializeField] private NetworkManager _networkManager;

    [Header("Lobby 생성 입력")]
    [SerializeField] private InputField _roomNameInputField;
    [SerializeField] private Dropdown _maxPlayersDropdown;
    [SerializeField] private Button _confirmCreateRoomButton;

    [Header("화면")]
    [SerializeField] private GameObject _createRoomPanel;
    [SerializeField] private GameObject _roomWaitingPanel;

    [Header("Lobby 대기실 UI")]
    [SerializeField] private Text _waitingRoomNameText;
    [SerializeField] private Text _waitingRoomSettingsText;
    [SerializeField] private Text _waitingMemberListText;
    [SerializeField] private Text _roomStatusText;
    [SerializeField] private Button _startGameButton;

    private bool _isCreatingLobby;

    private bool TryStartFishNetHost()
    {
        if (_networkManager == null)
        {
            Debug.LogError(
                "FishNet NetworkManager 참조가 연결되지 않았습니다.");

            return false;
        }

        if (!App.Initialized ||
            !App.Client.LoggedOn)
        {
            Debug.LogError(
                "Steam 초기화 또는 로그인 상태가 올바르지 않아 " +
                "FishNet Host를 시작할 수 없습니다.");

            return false;
        }

        if (!_networkManager.Initialized ||
            _networkManager.TransportManager == null ||
            _networkManager.TransportManager.Transport == null)
        {
            Debug.LogError(
                "FishNet NetworkManager 또는 Transport가 " +
                "초기화되지 않았습니다.");

            return false;
        }

        Transport transport =
            _networkManager.TransportManager.Transport;

        if (!(transport is FishySteamworks.FishySteamworks))
        {
            Debug.LogError(
                "현재 FishNet Transport가 " +
                "FishySteamworks가 아닙니다.");

            return false;
        }

        if (transport.GetConnectionState(true) !=
                LocalConnectionState.Stopped ||
            transport.GetConnectionState(false) !=
                LocalConnectionState.Stopped)
        {
            Debug.LogWarning(
                "FishNet Server 또는 Client 연결이 " +
                "이미 실행 중입니다.");

            return false;
        }

        bool serverStartAccepted =
            _networkManager.ServerManager.StartConnection();

        if (!serverStartAccepted)
        {
            Debug.LogError(
                "FishNet Server 시작 요청이 거부되었습니다.");

            return false;
        }

        bool clientStartAccepted =
            _networkManager.ClientManager.StartConnection();

        if (!clientStartAccepted)
        {
            // Local Client를 시작하지 못했다면
            // Server만 남지 않도록 즉시 원래 상태로 되돌립니다.
            _networkManager.ServerManager.StopConnection(false);

            Debug.LogError(
                "FishNet Local Client 시작 요청이 거부되어 " +
                "Server를 다시 중지했습니다.");

            return false;
        }

        Debug.Log(
            "FishNet Host 시작 요청 수락\n" +
            "Server 시작 요청: 성공\n" +
            "Local Client 시작 요청: 성공");

        return true;
    }

    public void CreateSteamLobby()
    {
        if (_lobbyManager == null ||
            _networkManager == null ||
            _roomNameInputField == null ||
            _maxPlayersDropdown == null ||
            _confirmCreateRoomButton == null ||
            _createRoomPanel == null ||
            _roomWaitingPanel == null ||
            _waitingRoomNameText == null ||
            _waitingRoomSettingsText == null ||
            _waitingMemberListText == null ||
            _roomStatusText == null ||
            _startGameButton == null)
        {
            Debug.LogWarning(
                "Steam Lobby 생성에 필요한 참조가 연결되지 않았습니다.");

            return;
        }

        if (!App.Initialized ||
            !App.Client.LoggedOn)
        {
            Debug.LogWarning(
                "Steam 초기화 또는 로그인 상태를 먼저 확인해 주세요.");

            return;
        }

        if (_isCreatingLobby)
        {
            Debug.LogWarning(
                "Steam Lobby를 생성하고 있습니다.");

            return;
        }

        if (_lobbyManager.HasLobby)
        {
            Debug.LogWarning(
                "이미 참가 중인 Steam Lobby가 있습니다.");

            return;
        }

        string roomName =
            _roomNameInputField.text.Trim();

        if (roomName.Length == 0)
        {
            Debug.LogWarning(
                "Room 이름을 입력해 주세요.");

            return;
        }

        // Dropdown에는 2인과 4인 항목만 존재합니다.
        int maxPlayers =
            _maxPlayersDropdown.value == 0
                ? 2
                : 4;

        _lobbyManager.createArguments.usageHint =
            CreateArguments.UseHintOptions.Session;

        _lobbyManager.createArguments.name =
            roomName;

        _lobbyManager.createArguments.type =
            ELobbyType.k_ELobbyTypePublic;

        _lobbyManager.createArguments.slots =
            maxPlayers;

        // App ID 480을 사용하는 다른 프로젝트의 Lobby와
        // 구분하기 위한 검색용 메타데이터입니다.
        _lobbyManager.createArguments.metadata.Clear();

        _lobbyManager.createArguments.metadata.Add(
            new MetadataTempalate
            {
                key = StudyLobbyKey,
                value = StudyLobbyValue
            });

        _isCreatingLobby = true;
        _confirmCreateRoomButton.interactable = false;

        _lobbyManager.Create(
            (result, lobby, ioError) =>
            {
                _isCreatingLobby = false;

                if (ioError ||
                    result != EResult.k_EResultOK ||
                    !lobby.IsValid)
                {
                    _confirmCreateRoomButton.interactable = true;

                    Debug.LogError(
                        "Steam Lobby 생성 실패\n" +
                        $"Result: {result}\n" +
                        $"IO Error: {ioError}\n" +
                        $"Lobby Valid: {lobby.IsValid}");

                    return;
                }

                if (!TryStartFishNetHost())
                {
                    // Host를 시작할 수 없는 공개 Lobby가
                    // 검색 결과에 남지 않도록 즉시 닫습니다.
                    _lobbyManager.Leave();

                    _confirmCreateRoomButton.interactable = true;

                    Debug.LogError(
                        "FishNet Host를 시작하지 못해 " +
                        "생성한 Steam Lobby를 닫았습니다.");

                    return;
                }

                ulong lobbyId =
                    lobby.SteamId.m_SteamID;

                ulong ownerSteamId =
                    lobby.Owner.user.SteamId;

                _waitingRoomNameText.text =
                    lobby.Name;

                _waitingRoomSettingsText.text =
                    $"Lobby ID64: {lobbyId}\n" +
                    $"Host ID64: {ownerSteamId}\n" +
                    $"인원: {lobby.MemberCount} / {lobby.MaxMembers}";

                _waitingMemberListText.text =
                    $"{lobby.Owner.user.Name} (방장, 나)";

                _roomStatusText.text =
                    "Steam Lobby 생성 완료 · FishNet Host 연결 중";

                // 아직 FishNet Host를 시작하지 않았으므로
                // 게임 시작 버튼은 비활성화합니다.
                _startGameButton.interactable = false;

                _createRoomPanel.SetActive(false);
                _roomWaitingPanel.SetActive(true);

                Debug.Log(
                    "Steam Lobby 생성 성공\n" +
                    $"이름: {lobby.Name}\n" +
                    $"Lobby ID64: {lobbyId}\n" +
                    $"Owner ID64: {ownerSteamId}\n" +
                    $"인원: {lobby.MemberCount} / {lobby.MaxMembers}\n" +
                    $"내가 방장인가: {lobby.IsOwner}");
            });
    }
}