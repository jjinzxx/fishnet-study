using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Managing.Scened;
using HeathenEngineering.SteamworksIntegration;
using Steamworks;

public sealed class SteamLobbyReadyController : NetworkBehaviour
{
    private const string MainSceneName = "Main";

    [Header("Steam Lobby")]
    [SerializeField] private LobbyManager _lobbyManager;

    [Header("Lobby 대기실 UI")]
    [SerializeField] private Button _readyButton;
    [SerializeField] private Text _readyButtonText;
    [SerializeField] private Button _startGameButton;

    private readonly SyncDictionary<int, bool> _guestReadyStates = new();
    private bool _isGameStarting;

    public override void OnStartClient()
    {
        if (_readyButton == null ||
            _startGameButton == null)
        {
            Debug.LogWarning(
                "준비 상태 버튼 UI 참조가 연결되지 않았습니다.");

            return;
        }

        bool isHost =
            IsHostInitialized;

        bool isGuest =
            IsClientOnlyInitialized;

        _startGameButton.gameObject.SetActive(
            isHost);

        _readyButton.gameObject.SetActive(
            isGuest);

        string role =
            isHost
                ? "Host"
                : isGuest
                    ? "Guest"
                    : "Unknown";

        Debug.Log(
            "준비 상태 버튼 표시를 갱신했습니다.\n" +
            $"Role: {role}\n" +
            $"StartGameButton: {isHost}\n" +
            $"ReadyButton: {isGuest}");

        _guestReadyStates.OnChange -=
            OnGuestReadyStatesChanged;

        _guestReadyStates.OnChange +=
            OnGuestReadyStatesChanged;

        RefreshReadyButtonText();
    }

    public override void OnStopClient()
    {
        _guestReadyStates.OnChange -=
            OnGuestReadyStatesChanged;
    }

    public override void OnSpawnServer(
        NetworkConnection connection)
    {
        if (connection == null ||
            !connection.IsActive ||
            !connection.IsAuthenticated)
        {
            return;
        }

        // Host는 준비하지 않고 직접 게임 시작을 요청하므로
        // Guest 준비 상태 목록에 등록하지 않습니다.
        if (connection.IsLocalClient)
        {
            Debug.Log(
                "Host 로컬 연결은 Guest 준비 상태에 등록하지 않습니다.\n" +
                $"FishNet Client ID: {connection.ClientId}");

            return;
        }

        int clientId =
            connection.ClientId;

        // Observer가 다시 생성되더라도 기존 준비 상태를
        // false로 덮어쓰지 않습니다.
        if (_guestReadyStates.ContainsKey(clientId))
        {
            return;
        }

        _guestReadyStates.Add(
            clientId,
            false);

        Debug.Log(
            "서버가 Guest 준비 상태를 등록했습니다.\n" +
            $"FishNet Client ID: {clientId}\n" +
            "Ready: False");
    }

    public override void OnDespawnServer(
    NetworkConnection connection)
    {
        // Host 전체 종료 중에는 서버가 이미 정지했으므로
        // SyncDictionary를 수정하지 않습니다.
        if (connection == null ||
            !IsServerStarted ||
            connection.IsLocalClient)
        {
            return;
        }

        int clientId =
            connection.ClientId;

        if (!_guestReadyStates.Remove(clientId))
        {
            return;
        }

        Debug.Log(
            "서버가 이탈한 Guest 준비 상태를 제거했습니다.\n" +
            $"FishNet Client ID: {clientId}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyServerRpc(
        NetworkConnection sender = null)
    {
        if (sender == null ||
            !sender.IsActive ||
            !sender.IsAuthenticated)
        {
            Debug.LogWarning(
                "준비 상태 변경 요청자를 확인할 수 없습니다.");

            return;
        }

        // Host는 준비하지 않고 직접 게임을 시작하므로
        // 준비 상태 변경 요청을 허용하지 않습니다.
        if (sender.IsLocalClient)
        {
            Debug.LogWarning(
                "Host는 준비 상태를 변경하지 않습니다.");

            return;
        }

        int clientId =
            sender.ClientId;

        // OnSpawnServer에서 등록되지 않은 연결은
        // 준비 상태를 새로 만들거나 변경할 수 없습니다.
        if (!_guestReadyStates.TryGetValue(
                clientId,
                out bool currentReady))
        {
            Debug.LogWarning(
                "준비 상태 변경 요청을 거부했습니다.\n" +
                $"등록되지 않은 FishNet Client ID: {clientId}");

            return;
        }

        bool nextReady =
            !currentReady;

        _guestReadyStates[clientId] =
            nextReady;

        Debug.Log(
            "서버가 Guest 준비 상태를 변경했습니다.\n" +
            $"FishNet Client ID: {clientId}\n" +
            $"Ready: {nextReady}");
    }

    private void OnGuestReadyStatesChanged(
    SyncDictionaryOperation operation,
    int clientId,
    bool ready,
    bool asServer)
    {
        if (asServer)
        {
            if (!IsHostInitialized ||
                _startGameButton == null)
            {
                return;
            }

            bool canStartGame =
                _guestReadyStates.Count > 0;

            if (canStartGame)
            {
                foreach (bool guestReady in
                         _guestReadyStates.Values)
                {
                    if (!guestReady)
                    {
                        canStartGame = false;
                        break;
                    }
                }
            }

            _startGameButton.interactable =
                canStartGame;

            return;
        }

        if (!IsClientOnlyInitialized)
        {
            return;
        }

        NetworkConnection localConnection =
            LocalConnection;

        if (localConnection == null ||
            !localConnection.IsActive ||
            !localConnection.IsAuthenticated)
        {
            return;
        }

        bool localEntryChanged =
            (operation == SyncDictionaryOperation.Add ||
             operation == SyncDictionaryOperation.Set ||
             operation == SyncDictionaryOperation.Remove) &&
            clientId == localConnection.ClientId;

        bool collectionStateChanged =
            operation == SyncDictionaryOperation.Clear ||
            operation == SyncDictionaryOperation.Complete;

        if (localEntryChanged ||
            collectionStateChanged)
        {
            RefreshReadyButtonText();
        }
    }

    private void RefreshReadyButtonText()
    {
        if (_readyButtonText == null)
        {
            Debug.LogWarning(
                "ReadyButtonText UI 참조가 연결되지 않았습니다.");

            return;
        }

        // Host에게는 ReadyButton이 표시되지 않지만
        // 재접속을 고려하여 기본 문구로 초기화합니다.
        if (!IsClientOnlyInitialized)
        {
            _readyButtonText.text =
                "준비";

            return;
        }

        NetworkConnection localConnection =
            ClientManager.Connection;

        if (localConnection == null ||
            !localConnection.IsActive ||
            !localConnection.IsAuthenticated)
        {
            _readyButtonText.text =
                "준비";

            return;
        }

        bool isReady =
            _guestReadyStates.TryGetValue(
                localConnection.ClientId,
                out bool ready) &&
            ready;

        _readyButtonText.text =
            isReady
                ? "준비 취소"
                : "준비";
    }

    public void OnReadyButtonClicked()
    {
        if (!IsClientInitialized)
        {
            Debug.LogWarning(
                "준비 요청을 보낼 수 없습니다.\n" +
                "FishNet 클라이언트가 아직 초기화되지 않았습니다.");

            return;
        }

        ToggleReadyServerRpc();
    }

    [Server]
    public void OnStartGameButtonClicked()
    {
        // 현재 프로젝트에서는 Steam Lobby 방장이
        // FishNet Listen Server도 함께 실행합니다.
        if (!base.IsServerStarted ||
            !base.IsHostInitialized)
        {
            Debug.LogWarning(
                "게임 시작 요청을 처리할 수 없습니다.\n" +
                "현재 사용자가 FishNet Host가 아닙니다.");

            return;
        }

        // 버튼을 빠르게 여러 번 눌러도
        // 씬 이동은 한 번만 요청합니다.
        if (_isGameStarting)
        {
            Debug.LogWarning(
                "이미 게임 시작을 처리하고 있습니다.");

            return;
        }

        if (_lobbyManager == null)
        {
            Debug.LogWarning(
                "LobbyManager 참조가 연결되지 않았습니다.");

            return;
        }

        if (!SteamSettings.Initialized)
        {
            Debug.LogWarning(
                "Steam API가 초기화되지 않았습니다.");

            return;
        }

        if (!_lobbyManager.HasLobby)
        {
            Debug.LogWarning(
                "현재 참가 중인 Steam Lobby가 없습니다.");

            return;
        }

        LobbyData lobby =
            _lobbyManager.Lobby;

        if (!lobby.IsValid)
        {
            Debug.LogWarning(
                "현재 Steam Lobby 정보가 유효하지 않습니다.");

            return;
        }

        if (!lobby.IsOwner)
        {
            Debug.LogWarning(
                "게임 시작 요청을 거부했습니다.\n" +
                "현재 사용자가 Steam Lobby 방장이 아닙니다.");

            return;
        }

        int steamMemberCount =
            lobby.MemberCount;

        if (steamMemberCount < 2)
        {
            Debug.LogWarning(
                "게임 시작 요청을 거부했습니다.\n" +
                $"현재 Steam Lobby 인원: {steamMemberCount}\n" +
                "최소 인원: 2");

            return;
        }

        int authenticatedCount = 0;
        int authenticatedGuestCount = 0;

        foreach (NetworkConnection connection in
                 base.ServerManager.Clients.Values)
        {
            if (connection == null ||
                !connection.IsActive ||
                !connection.IsAuthenticated)
            {
                continue;
            }

            // FishySteamworks의 연결 주소는
            // 접속자의 Steam ID64로 사용됩니다.
            if (!ulong.TryParse(
                    connection.GetAddress(),
                    out ulong steamId64) ||
                steamId64 == 0)
            {
                Debug.LogWarning(
                    "FishNet 연결 주소를 Steam ID64로 " +
                    "변환하지 못했습니다.\n" +
                    $"FishNet Client ID: {connection.ClientId}");

                return;
            }

            CSteamID steamId =
                new CSteamID(steamId64);

            // 인증 이후 Lobby에서 나간 연결까지
            // 게임 시작 직전에 다시 확인합니다.
            if (!lobby.IsAMember(steamId))
            {
                Debug.LogWarning(
                    "Steam Lobby 멤버가 아닌 FishNet 연결이 있습니다.\n" +
                    $"FishNet Client ID: {connection.ClientId}\n" +
                    $"Steam ID64: {steamId64}");

                return;
            }

            authenticatedCount++;

            // Host는 준비 상태 목록에 들어가지 않습니다.
            if (connection.IsHost)
            {
                continue;
            }

            authenticatedGuestCount++;

            if (!_guestReadyStates.TryGetValue(
                    connection.ClientId,
                    out bool guestReady) ||
                !guestReady)
            {
                Debug.LogWarning(
                    "준비하지 않은 Guest가 있어 " +
                    "게임을 시작할 수 없습니다.\n" +
                    $"FishNet Client ID: {connection.ClientId}");

                return;
            }
        }

        // Steam Lobby에만 들어오고 FishNet 연결이 끝나지 않았거나,
        // FishNet 연결만 남아 있는 상태를 거부합니다.
        if (authenticatedCount != steamMemberCount)
        {
            Debug.LogWarning(
                "Steam Lobby 인원과 FishNet 인증 인원이 " +
                "일치하지 않습니다.\n" +
                $"Steam Lobby: {steamMemberCount}\n" +
                $"FishNet 인증: {authenticatedCount}");

            return;
        }

        // 접속 종료 처리에서 제거되지 않은 준비 상태처럼
        // 현재 Guest 연결과 맞지 않는 데이터도 거부합니다.
        if (authenticatedGuestCount == 0 ||
            authenticatedGuestCount != _guestReadyStates.Count)
        {
            Debug.LogWarning(
                "Guest 연결 수와 준비 상태 등록 수가 " +
                "일치하지 않습니다.\n" +
                $"인증 Guest: {authenticatedGuestCount}\n" +
                $"준비 상태: {_guestReadyStates.Count}");

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                MainSceneName))
        {
            Debug.LogError(
                $"{MainSceneName} 씬을 불러올 수 없습니다.\n" +
                "Build Profiles의 Scene List를 확인해 주세요.");

            return;
        }

        // 게임이 시작된 Lobby는 검색이나 초대로
        // 새 사용자가 들어오지 못하도록 닫습니다.
        if (!_lobbyManager.SetJoinable(false))
        {
            Debug.LogWarning(
                "Steam Lobby 참가 잠금에 실패하여 " +
                "게임 시작을 취소했습니다.");

            return;
        }

        _isGameStarting = true;

        if (_startGameButton != null)
        {
            _startGameButton.interactable = false;
            _startGameButton.gameObject.SetActive(false);
        }

        Debug.Log(
            "서버가 게임 시작 조건을 승인했습니다.\n" +
            $"Steam Lobby 인원: {steamMemberCount}\n" +
            $"FishNet 인증 인원: {authenticatedCount}\n" +
            $"준비 완료 Guest: {authenticatedGuestCount}\n" +
            $"이동 씬: {MainSceneName}");

        SceneLoadData sceneLoadData =
            new SceneLoadData(MainSceneName)
            {
                ReplaceScenes = ReplaceOption.All
            };

        base.SceneManager.LoadGlobalScenes(
            sceneLoadData);
    }


}
