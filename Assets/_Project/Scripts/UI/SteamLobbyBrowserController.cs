using System;
using FishNet.Managing;
using FishNet.Transporting;
using HeathenEngineering;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.API;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class SteamLobbyBrowserController : MonoBehaviour
{
    private const string StudyLobbyKey = "study_id";
    private const string StudyLobbyValue = "fishnet_study_v1";

    [Header("검색 버튼")]
    [SerializeField] private Button _refreshButton;

    [Header("검색 결과 Room")]
    [SerializeField] private GameObject _steamRoomItem;
    [SerializeField] private Text _roomNameText;
    [SerializeField] private Text _playerCountText;
    [SerializeField] private Text _roomStateText;

    [Header("검색 결과 없음")]
    [SerializeField] private Text _emptyRoomListText;

    [Header("Lobby 참가")]
    [SerializeField] private LobbyManager _lobbyManager;
    [SerializeField] private Button _joinRoomButton;

    [Header("FishNet")]
    [SerializeField] private NetworkManager _networkManager;

    [Header("Lobby 대기실")]
    [SerializeField] private GameObject _multiplayerLobbyPanel;
    [SerializeField] private GameObject _roomWaitingPanel;
    [SerializeField] private Text _waitingRoomNameText;
    [SerializeField] private Text _waitingRoomSettingsText;
    [SerializeField] private Text _waitingMemberListText;
    [SerializeField] private Text _roomStatusText;
    [SerializeField] private Button _startGameButton;

    private bool _isJoiningLobby;
    private bool _isResolvingInvite;

    private LobbyData _selectedLobby;
    private bool _isSearching;

    public void RefreshSteamLobbies()
    {
        if (_refreshButton == null ||
            _steamRoomItem == null ||
            _roomNameText == null ||
            _playerCountText == null ||
            _roomStateText == null ||
            _emptyRoomListText == null)
        {
            Debug.LogWarning(
                "Steam Lobby 검색 UI 참조가 연결되지 않았습니다.");

            return;
        }

        if (!App.Initialized ||
            !App.Client.LoggedOn)
        {
            Debug.LogWarning(
                "Steam 초기화 또는 로그인 상태를 먼저 확인해 주세요.");

            return;
        }

        if (_isSearching)
        {
            Debug.LogWarning(
                "Steam Lobby를 검색하고 있습니다.");

            return;
        }

        _isSearching = true;
        _selectedLobby = default;

        _refreshButton.interactable = false;
        _steamRoomItem.SetActive(false);

        _emptyRoomListText.text =
            "Steam Lobby를 검색하는 중입니다.";

        _emptyRoomListText.gameObject.SetActive(true);

        // App ID 480을 사용하는 테스트 환경이므로
        // 다른 지역의 테스트 기기도 검색할 수 있게 설정합니다.
        Matchmaking.Client.AddRequestLobbyListDistanceFilter(
            ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

        // 다른 App ID 480 프로젝트의 Lobby를 제외하고
        // 이번 실습에서 생성한 Lobby만 검색합니다.
        Matchmaking.Client.AddRequestLobbyListStringFilter(
            StudyLobbyKey,
            StudyLobbyValue,
            ELobbyComparison.k_ELobbyComparisonEqual);

        // 현재 UI에는 Room 항목이 하나뿐이므로
        // 검색 결과도 첫 번째 Lobby 하나만 요청합니다.
        Matchmaking.Client.AddRequestLobbyListResultCountFilter(1);

        Matchmaking.Client.RequestLobbyList(
            (lobbies, ioError) =>
            {
                _isSearching = false;
                _refreshButton.interactable = true;

                if (ioError)
                {
                    _emptyRoomListText.text =
                        "Steam Lobby 검색에 실패했습니다.";

                    Debug.LogError(
                        "Steam Lobby 검색 중 통신 오류가 발생했습니다.");

                    return;
                }

                if (lobbies.Length == 0)
                {
                    _emptyRoomListText.text =
                        "검색된 Steam Lobby가 없습니다.";

                    Debug.Log(
                        "Steam Lobby 검색 완료 · 결과 없음");

                    return;
                }

                _selectedLobby = lobbies[0];

                string roomName =
                    string.IsNullOrWhiteSpace(_selectedLobby.Name)
                        ? "이름 없는 Steam Lobby"
                        : _selectedLobby.Name;

                _roomNameText.text =
                    roomName;

                _playerCountText.text =
                    $"{_selectedLobby.MemberCount} / " +
                    $"{_selectedLobby.MaxMembers}";

                _roomStateText.text =
                    "참가 가능";

                _emptyRoomListText.gameObject.SetActive(false);
                _steamRoomItem.SetActive(true);

                Debug.Log(
                    "Steam Lobby 검색 성공\n" +
                    $"이름: {roomName}\n" +
                    $"Lobby ID64: " +
                    $"{_selectedLobby.SteamId.m_SteamID}\n" +
                    $"인원: {_selectedLobby.MemberCount} / " +
                    $"{_selectedLobby.MaxMembers}\n" +
                    $"study_id: " +
                    $"{_selectedLobby[StudyLobbyKey]}");
            });
    }

    public void JoinSteamLobbyFromLaunchArgument()
    {
        ulong invitedLobbyId =
            CommandLine.GetSteamLobbyInvite();

        // 일반 실행에는 +connect_lobby 인수가 없으므로
        // 자동 참가를 시작하지 않습니다.
        if (invitedLobbyId == 0)
        {
            return;
        }

        LobbyData invitedLobby =
            invitedLobbyId;

        if (!invitedLobby.IsValid)
        {
            Debug.LogWarning(
                "실행 인수로 전달된 Steam Lobby ID가 " +
                "유효하지 않습니다.\n" +
                $"Lobby ID64: {invitedLobbyId}");

            return;
        }

        Debug.Log(
            "게임 시작 인수에서 Steam Lobby 초대를 " +
            "확인했습니다.\n" +
            $"실행 인수: +connect_lobby {invitedLobbyId}");

        // 실행 인수에는 초대한 사용자의 정보가 없으므로
        // inviter에는 기본값을 전달합니다.
        JoinInvitedSteamLobby(
            invitedLobby,
            default);
    }

    public async void JoinInvitedSteamLobby(
    LobbyData invitedLobby,
    UserData inviter)
    {
        if (_lobbyManager == null ||
            _roomStateText == null)
        {
            Debug.LogWarning(
                "Steam 초대 참가에 필요한 참조가 " +
                "연결되지 않았습니다.");

            return;
        }

        if (!App.Initialized ||
            !App.Client.LoggedOn)
        {
            Debug.LogWarning(
                "Steam 초기화 또는 로그인 상태를 " +
                "먼저 확인해 주세요.");

            return;
        }

        if (!invitedLobby.IsValid)
        {
            Debug.LogWarning(
                "초대로 전달된 Steam Lobby 정보가 " +
                "유효하지 않습니다.");

            return;
        }

        if (_lobbyManager.HasLobby)
        {
            Debug.LogWarning(
                "이미 다른 Steam Lobby에 참가하고 있어 " +
                "초대 요청을 처리하지 않았습니다.");

            return;
        }

        if (_isJoiningLobby ||
            _isResolvingInvite)
        {
            Debug.LogWarning(
                "다른 Steam Lobby 참가 요청을 " +
                "처리하고 있습니다.");

            return;
        }

        _isResolvingInvite = true;

        ulong invitedLobbyId =
            invitedLobby.SteamId.m_SteamID;

        ulong inviterSteamId =
            inviter.IsValid
                ? inviter.SteamId
                : 0;

        bool metadataRequestFinished =
            false;

        UnityAction<LobbyDataUpdateEventData>
            onLobbyDataUpdated = null;

        onLobbyDataUpdated =
            updateData =>
            {
                // 다른 Lobby 또는 특정 멤버 정보 갱신은
                // 이번 초대 Lobby 확인 결과가 아닙니다.
                if (updateData.lobby != invitedLobby ||
                    updateData.member.HasValue)
                {
                    return;
                }

                Matchmaking.Client
                    .EventLobbyDataUpdate
                    .RemoveListener(
                        onLobbyDataUpdated);

                metadataRequestFinished = true;
                _isResolvingInvite = false;

                string studyId =
                    invitedLobby[StudyLobbyKey];

                // App ID 480을 공유하는 다른 프로젝트의
                // Lobby 초대를 잘못 처리하지 않도록 검증합니다.
                if (studyId != StudyLobbyValue)
                {
                    _roomStateText.text =
                        "이 스터디에서 생성한 Lobby가 아닙니다.";

                    Debug.LogWarning(
                        "Steam Lobby 초대 참가를 거부했습니다.\n" +
                        $"Lobby ID64: {invitedLobbyId}\n" +
                        $"study_id: {studyId}");

                    return;
                }

                _selectedLobby =
                    invitedLobby;

                _roomStateText.text =
                    "초대받은 Steam Lobby 참가 중";

                Debug.Log(
                    "Steam Lobby 초대 수락을 확인했습니다.\n" +
                    $"Lobby ID64: {invitedLobbyId}\n" +
                    $"초대한 사용자 Steam ID64: " +
                    $"{inviterSteamId}\n" +
                    $"study_id: {studyId}");

                // 검색 결과 참가와 동일한 검증·연결 흐름을 사용합니다.
                JoinSelectedSteamLobby();
            };

        Matchmaking.Client
            .EventLobbyDataUpdate
            .AddListener(
                onLobbyDataUpdated);

        bool requestAccepted =
            invitedLobby.RequestData();

        if (!requestAccepted)
        {
            Matchmaking.Client
                .EventLobbyDataUpdate
                .RemoveListener(
                    onLobbyDataUpdated);

            _isResolvingInvite = false;

            _roomStateText.text =
                "초대 Lobby 정보를 가져오지 못했습니다.";

            Debug.LogWarning(
                "Steam Lobby 초대 정보 요청이 " +
                "거부되었습니다.\n" +
                $"Lobby ID64: {invitedLobbyId}");

            return;
        }

        _roomStateText.text =
            "초대 Lobby 정보 확인 중";

        // Steam 응답이 오지 않을 때 이벤트가 계속 남지 않도록
        // 10초 후 요청 상태를 정리합니다.
        await Awaitable.WaitForSecondsAsync(
            10f);

        if (metadataRequestFinished)
        {
            return;
        }

        Matchmaking.Client
            .EventLobbyDataUpdate
            .RemoveListener(
                onLobbyDataUpdated);

        _isResolvingInvite = false;

        if (this == null)
        {
            return;
        }

        _roomStateText.text =
            "초대 Lobby 정보 확인 시간이 초과되었습니다.";

        Debug.LogWarning(
            "Steam Lobby 초대 정보 요청 시간이 " +
            "초과되었습니다.\n" +
            $"Lobby ID64: {invitedLobbyId}");
    }

    public void JoinSelectedSteamLobby()
    {
        if (_lobbyManager == null ||
            _networkManager == null ||
            _joinRoomButton == null ||
            _refreshButton == null ||
            _multiplayerLobbyPanel == null ||
            _roomWaitingPanel == null ||
            _waitingRoomNameText == null ||
            _waitingRoomSettingsText == null ||
            _waitingMemberListText == null ||
            _roomStatusText == null ||
            _roomStateText == null ||
            _startGameButton == null)
        {
            Debug.LogWarning(
                "Steam Lobby 참가에 필요한 참조가 연결되지 않았습니다.");

            return;
        }

        if (!App.Initialized ||
            !App.Client.LoggedOn)
        {
            Debug.LogWarning(
                "Steam 초기화 또는 로그인 상태를 먼저 확인해 주세요.");

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

        Transport transport =
            _networkManager.TransportManager.Transport;

        if (!(transport is FishySteamworks.FishySteamworks))
        {
            Debug.LogWarning(
                "현재 FishNet Transport가 " +
                "FishySteamworks가 아닙니다.");

            return;
        }

        // Guest에서 Server가 실행 중이면 FishySteamworks가
        // 전달받은 Host 주소 대신 자기 자신에게 연결할 수 있습니다.
        if (transport.GetConnectionState(true) !=
                LocalConnectionState.Stopped ||
            transport.GetConnectionState(false) !=
                LocalConnectionState.Stopped)
        {
            Debug.LogWarning(
                "FishNet Server 또는 Client 연결이 " +
                "이미 실행 중입니다.");

            return;
        }

        if (_isSearching)
        {
            Debug.LogWarning(
                "Steam Lobby 검색이 끝난 뒤 참가해 주세요.");

            return;
        }

        if (_isJoiningLobby)
        {
            Debug.LogWarning(
                "Steam Lobby 참가를 처리하고 있습니다.");

            return;
        }

        if (_lobbyManager.HasLobby)
        {
            Debug.LogWarning(
                "이미 참가 중인 Steam Lobby가 있습니다.");

            return;
        }

        if (!_selectedLobby.IsValid)
        {
            Debug.LogWarning(
                "먼저 참가할 Steam Lobby를 검색해 주세요.");

            return;
        }

        _isJoiningLobby = true;

        _joinRoomButton.interactable = false;
        _refreshButton.interactable = false;
        _roomStateText.text = "Steam Lobby 참가 중";

        UnityAction<LobbyData> onSuccess = null;
        UnityAction<EChatRoomEnterResponse> onFailed = null;

        onSuccess =
            joinedLobby =>
            {
                // 이번 Steam Lobby 참가 요청에 사용한 리스너를 제거합니다.
                _lobbyManager.evtEnterSuccess.RemoveListener(
                    onSuccess);

                _lobbyManager.evtEnterFailed.RemoveListener(
                    onFailed);

                // LobbyManager.Join 성공 이벤트가 호출되기 전에
                // LobbyManager.Lobby가 참가한 Lobby로 갱신됩니다.
                LobbyData managedLobby =
                    _lobbyManager.Lobby;

                LobbyMemberData owner =
                    managedLobby.Owner;

                if (!managedLobby.IsValid ||
                    !owner.user.IsValid ||
                    owner.user.IsMe)
                {
                    if (_lobbyManager.HasLobby)
                    {
                        _lobbyManager.Leave();
                    }

                    _isJoiningLobby = false;
                    _joinRoomButton.interactable = true;
                    _refreshButton.interactable = true;
                    _roomStateText.text =
                        "Host 정보를 확인하지 못했습니다.";

                    Debug.LogError(
                        "FishNet Guest 연결 실패\n" +
                        "유효한 Lobby Owner Steam ID64를 " +
                        "확인하지 못했습니다.");

                    return;
                }

                // Steam Lobby 참가를 기다리는 사이에 다른 코드가
                // FishNet을 시작하지 않았는지 다시 검사합니다.
                if (transport.GetConnectionState(true) !=
                        LocalConnectionState.Stopped ||
                    transport.GetConnectionState(false) !=
                        LocalConnectionState.Stopped)
                {
                    _lobbyManager.Leave();

                    _isJoiningLobby = false;
                    _joinRoomButton.interactable = true;
                    _refreshButton.interactable = true;
                    _roomStateText.text =
                        "FishNet 연결 상태를 확인해 주세요.";

                    Debug.LogError(
                        "Steam Lobby 참가 후 FishNet 연결 상태가 " +
                        "변경되어 Guest 연결을 시작하지 않았습니다.");

                    return;
                }

                ulong lobbyId =
                    managedLobby.SteamId.m_SteamID;

                ulong ownerSteamId =
                    owner.user.SteamId;

                bool fishNetAttemptFinished = false;

                Action<ClientConnectionStateArgs>
                    onClientConnectionState = null;

                Action onAuthenticated = null;
                Action<string> rollbackFishNetJoin = null;

                rollbackFishNetJoin =
                    reason =>
                    {
                        if (fishNetAttemptFinished)
                        {
                            return;
                        }

                        fishNetAttemptFinished = true;

                        // StopConnection이 상태 이벤트를 다시 발생시키기 전에
                        // 이번 연결 시도의 이벤트부터 제거합니다.
                        _networkManager.ClientManager
                            .OnClientConnectionState -=
                            onClientConnectionState;

                        _networkManager.ClientManager
                            .OnAuthenticated -=
                            onAuthenticated;

                        if (transport.GetConnectionState(false) !=
                            LocalConnectionState.Stopped)
                        {
                            _networkManager.ClientManager.StopConnection();
                        }

                        // FishNet Client를 정리한 뒤
                        // Steam Lobby에서도 나갑니다.
                        if (_lobbyManager.HasLobby)
                        {
                            _lobbyManager.Leave();
                        }

                        _isJoiningLobby = false;
                        _joinRoomButton.interactable = true;
                        _refreshButton.interactable = true;
                        _roomStateText.text =
                            "FishNet 연결 실패 · 다시 시도해 주세요.";

                        Debug.LogError(
                            "FishNet Guest 연결 실패\n" +
                            $"Lobby ID64: {lobbyId}\n" +
                            $"Host Steam ID64: {ownerSteamId}\n" +
                            $"원인: {reason}\n" +
                            $"현재 Lobby 보유 여부: " +
                            $"{_lobbyManager.HasLobby}");
                    };

                onClientConnectionState =
                    args =>
                    {
                        if (fishNetAttemptFinished)
                        {
                            return;
                        }

                        if (args.ConnectionState ==
                            LocalConnectionState.Starting)
                        {
                            _roomStateText.text =
                                "FishNet P2P 연결 중";

                            Debug.Log(
                                "FishNet Guest 상태 변경\n" +
                                "State: Starting\n" +
                                $"Host Steam ID64: {ownerSteamId}");

                            return;
                        }

                        if (args.ConnectionState ==
                            LocalConnectionState.Started)
                        {
                            // Started는 Steam P2P 연결 완료입니다.
                            // FishNet 인증 완료는 OnAuthenticated에서 확인합니다.
                            _roomStateText.text =
                                "Steam P2P 연결 완료 · FishNet 인증 중";

                            Debug.Log(
                                "FishNet Guest 상태 변경\n" +
                                "State: Started\n" +
                                $"Host Steam ID64: {ownerSteamId}");

                            return;
                        }

                        if (args.ConnectionState ==
                            LocalConnectionState.Stopped)
                        {
                            rollbackFishNetJoin(
                                "FishNet 인증 전에 Client 연결이 " +
                                "Stopped 상태가 되었습니다.");
                        }
                    };

                onAuthenticated =
                    () =>
                    {
                        if (fishNetAttemptFinished)
                        {
                            return;
                        }

                        // FishNet 인증 시점에도 Steam Lobby를
                        // 유지하고 있는지 확인합니다.
                        if (!_lobbyManager.HasLobby)
                        {
                            rollbackFishNetJoin(
                                "FishNet 인증 전에 Steam Lobby에서 " +
                                "나가졌습니다.");

                            return;
                        }

                        fishNetAttemptFinished = true;

                        _networkManager.ClientManager
                            .OnClientConnectionState -=
                            onClientConnectionState;

                        _networkManager.ClientManager
                            .OnAuthenticated -=
                            onAuthenticated;

                        LobbyMemberData[] members =
                            managedLobby.Members;

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

                        _waitingRoomNameText.text =
                            managedLobby.Name;

                        _waitingRoomSettingsText.text =
                            $"Lobby ID64: {lobbyId}\n" +
                            $"Host ID64: {ownerSteamId}\n" +
                            $"인원: {managedLobby.MemberCount} / " +
                            $"{managedLobby.MaxMembers}";

                        _waitingMemberListText.text =
                            memberListText;

                        _roomStatusText.text =
                            "FishNet Guest 연결 및 인증 완료";

                        // Guest는 게임 시작 버튼을 사용할 수 없습니다.
                        _startGameButton.interactable = false;

                        _isJoiningLobby = false;
                        _joinRoomButton.interactable = true;
                        _refreshButton.interactable = true;

                        // FishNet 인증까지 완료된 뒤 대기실로 이동합니다.
                        _multiplayerLobbyPanel.SetActive(false);
                        _roomWaitingPanel.SetActive(true);

                        Debug.Log(
                            "FishNet Guest 연결 및 인증 완료\n" +
                            $"Lobby ID64: {lobbyId}\n" +
                            $"Host Steam ID64: {ownerSteamId}\n" +
                            $"내 Steam ID64: {UserData.Me.SteamId}\n" +
                            $"FishNet Client ID: " +
                            $"{_networkManager.ClientManager.Connection.ClientId}\n" +
                            $"Client State: " +
                            $"{transport.GetConnectionState(false)}\n" +
                            $"LobbyManager HasLobby: " +
                            $"{_lobbyManager.HasLobby}");
                    };

                // Starting 상태는 StartConnection 호출 안에서
                // 바로 발생할 수 있으므로 이벤트를 먼저 등록합니다.
                _networkManager.ClientManager
                    .OnClientConnectionState +=
                    onClientConnectionState;

                _networkManager.ClientManager
                    .OnAuthenticated +=
                    onAuthenticated;

                _roomStateText.text =
                    "Host Steam ID로 FishNet 연결 요청 중";

                // 이 overload가 Transport 주소를 설정한 뒤
                // FishySteamworks Client를 시작합니다.
                bool clientStartAccepted =
                    _networkManager.ClientManager.StartConnection(
                        ownerSteamId.ToString());

                if (!clientStartAccepted)
                {
                    rollbackFishNetJoin(
                        "FishNet Client 시작 요청이 즉시 거부되었습니다.");

                    return;
                }

                if (!fishNetAttemptFinished)
                {
                    Debug.Log(
                        "FishNet Guest 연결 요청 수락\n" +
                        $"Lobby ID64: {lobbyId}\n" +
                        $"Host Steam ID64: {ownerSteamId}");
                }
            };

        onFailed =
            response =>
            {
                _lobbyManager.evtEnterSuccess.RemoveListener(
                    onSuccess);

                _lobbyManager.evtEnterFailed.RemoveListener(
                    onFailed);

                _isJoiningLobby = false;

                _joinRoomButton.interactable = true;
                _refreshButton.interactable = true;
                _roomStateText.text =
                    "참가 실패 · 목록을 새로고침해 주세요.";

                Debug.LogError(
                    "Steam Lobby 참가 실패\n" +
                    $"Response: {response}");
            };

        _lobbyManager.evtEnterSuccess.AddListener(
            onSuccess);

        _lobbyManager.evtEnterFailed.AddListener(
            onFailed);

        _lobbyManager.Join(
            _selectedLobby);
    }
}
