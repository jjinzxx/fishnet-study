using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.API;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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

    [Header("Lobby 대기실")]
    [SerializeField] private GameObject _multiplayerLobbyPanel;
    [SerializeField] private GameObject _roomWaitingPanel;
    [SerializeField] private Text _waitingRoomNameText;
    [SerializeField] private Text _waitingRoomSettingsText;
    [SerializeField] private Text _waitingMemberListText;
    [SerializeField] private Text _roomStatusText;
    [SerializeField] private Button _startGameButton;

    private bool _isJoiningLobby;

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

    public void JoinSelectedSteamLobby()
    {
        if (_lobbyManager == null ||
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
        _roomStateText.text = "참가 중";

        UnityAction<LobbyData> onSuccess = null;
        UnityAction<EChatRoomEnterResponse> onFailed = null;

        onSuccess =
            joinedLobby =>
            {
                // 다음 참가 요청에 이전 이벤트가 다시 실행되지 않도록
                // 이번 요청에 등록한 리스너를 제거합니다.
                _lobbyManager.evtEnterSuccess.RemoveListener(
                    onSuccess);

                _lobbyManager.evtEnterFailed.RemoveListener(
                    onFailed);

                _isJoiningLobby = false;

                _joinRoomButton.interactable = true;
                _refreshButton.interactable = true;

                // LobbyManager.Join은 성공 이벤트를 발생시키기 전에
                // LobbyManager.Lobby를 참가한 Lobby로 갱신합니다.
                LobbyData managedLobby =
                    _lobbyManager.Lobby;

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

                ulong lobbyId =
                    managedLobby.SteamId.m_SteamID;

                ulong ownerSteamId =
                    managedLobby.Owner.user.SteamId;

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
                    "Steam Lobby 참가 완료 · FishNet 연결 전";

                // 현재 사용자는 Guest이므로
                // 게임 시작 버튼을 사용할 수 없습니다.
                _startGameButton.interactable = false;

                _multiplayerLobbyPanel.SetActive(false);
                _roomWaitingPanel.SetActive(true);

                Debug.Log(
                    "Steam Lobby 참가 성공\n" +
                    $"이름: {managedLobby.Name}\n" +
                    $"Lobby ID64: {lobbyId}\n" +
                    $"Host ID64: {ownerSteamId}\n" +
                    $"내 Steam ID64: {UserData.Me.SteamId}\n" +
                    $"인원: {managedLobby.MemberCount} / " +
                    $"{managedLobby.MaxMembers}\n" +
                    $"LobbyManager HasLobby: " +
                    $"{_lobbyManager.HasLobby}");
            };

        onFailed =
            response =>
            {
                // 실패한 요청의 리스너도 반드시 제거합니다.
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

        // LobbyManager.Join에는 콜백 매개변수가 없으므로
        // 이번 요청의 성공·실패 이벤트를 먼저 등록합니다.
        _lobbyManager.evtEnterSuccess.AddListener(
            onSuccess);

        _lobbyManager.evtEnterFailed.AddListener(
            onFailed);

        _lobbyManager.Join(
            _selectedLobby);
    }
}
