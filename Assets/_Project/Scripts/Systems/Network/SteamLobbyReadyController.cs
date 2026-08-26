using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.UI;

public sealed class SteamLobbyReadyController : NetworkBehaviour
{
    [Header("Lobby 대기실 UI")]
    [SerializeField] private Button _readyButton;
    [SerializeField] private Text _readyButtonText;
    [SerializeField] private Button _startGameButton;

    private readonly SyncDictionary<int, bool>
        _guestReadyStates = new();

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
}
