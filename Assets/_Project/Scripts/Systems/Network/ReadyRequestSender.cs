using System.Collections;
using System.Collections.Generic;
using System.Text;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.UI;

public class ReadyRequestSender : NetworkBehaviour
{
    private const int CountdownStartSeconds = 3;

    [Header("로비 설정")]
    [Min(1)]
    [SerializeField] private int _minimumPlayerCount = 2;

    [Header("로비 UI")]
    [SerializeField] private Button _readyButton;
    [SerializeField] private Text _readyButtonLabel;
    [SerializeField] private Text _playerListText;
    [SerializeField] private Text _countdownText;

    // 현재 방장을 목록에 표시하기 위한 ClientId입니다.
    private readonly SyncVar<int> _hostClientId = new(-1);

    // -1은 카운트다운이 진행 중이 아니라는 의미입니다.
    private readonly SyncVar<int> _countdownSeconds = new(-1);

    // 방장을 포함한 모든 플레이어의 준비 상태를 저장합니다.
    private readonly SyncDictionary<int, bool> _readyStates = new();

    // Coroutine과 씬 이동의 중복 실행을 막는 서버 전용 상태입니다.
    private Coroutine _countdownCoroutine;
    private bool _isGameStarting;

    private void Awake()
    {
        // SyncType의 값이 변경될 때 클라이언트 UI를 갱신합니다.
        _readyStates.OnChange += OnReadyStatesChanged;
        _hostClientId.OnChange += OnHostClientIdChanged;
        _countdownSeconds.OnChange += OnCountdownSecondsChanged;
    }

    private void OnDestroy()
    {
        // 씬이 교체될 때 남아 있는 이벤트 참조를 정리합니다.
        _readyStates.OnChange -= OnReadyStatesChanged;
        _hostClientId.OnChange -= OnHostClientIdChanged;
        _countdownSeconds.OnChange -= OnCountdownSecondsChanged;
    }

    public override void OnStartServer()
    {
        _isGameStarting = false;
        _countdownCoroutine = null;

        _hostClientId.Value = -1;
        _countdownSeconds.Value = -1;

        if (_readyStates.Count > 0)
            _readyStates.Clear();

        base.ServerManager.OnRemoteConnectionState
            += OnRemoteConnectionState;
    }

    public override void OnStopServer()
    {
        base.ServerManager.OnRemoteConnectionState
            -= OnRemoteConnectionState;

        if (_countdownCoroutine != null)
            StopCoroutine(_countdownCoroutine);

        _countdownCoroutine = null;
    }

    public override void OnStartClient()
    {
        // 이번 구조에서는 방장을 포함한 모든 플레이어가 준비 버튼을 봅니다.
        if (_readyButton != null)
            _readyButton.gameObject.SetActive(true);

        // 초기 동기화가 끝난 상태를 기준으로 UI를 한 번 직접 그립니다.
        RefreshLobbyUI();
    }

    public override void OnSpawnServer(NetworkConnection connection)
    {
        int clientId = connection.ClientId;

        // 현재 학습 프로젝트의 방장은 서버와 클라이언트를 함께 실행한 Host입니다.
        if (connection.IsLocalClient)
            _hostClientId.Value = clientId;

        // Observer에서 잠시 벗어났다가 돌아온 경우 상태를 초기화하지 않습니다.
        if (_readyStates.ContainsKey(clientId))
            return;

        // 새로운 플레이어가 들어오면 기존 카운트다운은 무효입니다.
        CancelStartCountdown();

        _readyStates.Add(clientId, false);

        Debug.Log(
            $"서버 플레이어 등록 — ClientId: {clientId}, Ready: False");

        EvaluateStartCondition();
    }

    private void OnRemoteConnectionState(
        NetworkConnection connection,
        RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState != RemoteConnectionState.Stopped)
            return;

        int clientId = connection.ClientId;
        bool wasHost = clientId == _hostClientId.Value;

        if (wasHost)
            _hostClientId.Value = -1;

        if (!_readyStates.Remove(clientId))
            return;

        // 인원 구성이 달라졌으므로 진행 중인 카운트다운을 취소합니다.
        CancelStartCountdown();

        Debug.Log(
            $"서버 플레이어 제거 — ClientId: {clientId}");

        // Listen Server의 방장이 나가면 서버도 종료되므로
        // 남은 사람끼리 자동 시작시키지 않습니다.
        if (wasHost)
            return;

        // 퇴장 후에도 최소 인원이 충족되고 전원이 준비 상태라면
        // 새로운 3초 카운트다운을 시작할 수 있습니다.
        EvaluateStartCondition();
    }

    public void OnReadyButtonClicked()
    {
        RequestReady();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestReady(NetworkConnection sender = null)
    {
        if (sender == null)
        {
            Debug.LogWarning(
                "준비 요청자를 확인할 수 없습니다.");

            return;
        }

        if (_isGameStarting)
        {
            Debug.LogWarning(
                "준비 요청 거부 — 이미 게임을 시작하고 있습니다.");

            return;
        }

        int clientId = sender.ClientId;

        if (!_readyStates.TryGetValue(
                clientId,
                out bool currentReady))
        {
            Debug.LogWarning(
                $"준비 요청 거부 — 등록되지 않은 플레이어입니다. " +
                $"ClientId: {clientId}");

            return;
        }

        bool nextReady = !currentReady;
        _readyStates[clientId] = nextReady;

        Debug.Log(
            $"서버 준비 상태 변경 — " +
            $"ClientId: {clientId}, Ready: {nextReady}");

        EvaluateStartCondition();
    }

    private void EvaluateStartCondition()
    {
        if (_isGameStarting)
            return;

        if (!AreAllPlayersReady())
        {
            CancelStartCountdown();
            return;
        }

        // 이미 카운트다운이 진행 중이라면 중복 생성하지 않습니다.
        if (_countdownCoroutine != null)
            return;

        _countdownCoroutine =
            StartCoroutine(StartGameCountdown());
    }

    private bool AreAllPlayersReady()
    {
        // 현재 구조는 Listen Server이므로 방장이 등록되어 있어야 합니다.
        if (_hostClientId.Value < 0)
            return false;

        if (!_readyStates.ContainsKey(_hostClientId.Value))
            return false;

        int requiredPlayerCount =
            Mathf.Max(1, _minimumPlayerCount);

        if (_readyStates.Count < requiredPlayerCount)
            return false;

        foreach (bool isReady in _readyStates.Values)
        {
            if (!isReady)
                return false;
        }

        return true;
    }

    private IEnumerator StartGameCountdown()
    {
        Debug.Log(
            "전원 준비 완료 — 3초 카운트다운을 시작합니다.");

        for (int remaining = CountdownStartSeconds;
             remaining > 0;
             remaining--)
        {
            _countdownSeconds.Value = remaining;

            Debug.Log(
                $"게임 시작까지 {remaining}초");

            // 로비에서 Time.timeScale이 바뀌어도 멈추지 않도록
            // 실제 시간을 기준으로 기다립니다.
            yield return new WaitForSecondsRealtime(1f);

            // 매초 서버가 준비 조건을 다시 검사합니다.
            if (!AreAllPlayersReady())
            {
                _countdownCoroutine = null;
                _countdownSeconds.Value = -1;

                Debug.Log(
                    "게임 시작 카운트다운 취소 — " +
                    "준비 상태 또는 접속 인원이 변경되었습니다.");

                yield break;
            }
        }

        _countdownCoroutine = null;
        _countdownSeconds.Value = 0;

        // 마지막 순간의 퇴장이나 준비 취소까지 다시 검사합니다.
        if (_isGameStarting || !AreAllPlayersReady())
        {
            _countdownSeconds.Value = -1;
            yield break;
        }

        _isGameStarting = true;

        Debug.Log(
            $"게임 시작 조건 최종 승인 — " +
            $"전체 인원: {_readyStates.Count}");

        LoadGameScene();
    }

    private void CancelStartCountdown()
    {
        bool wasRunning =
            _countdownCoroutine != null ||
            _countdownSeconds.Value >= 0;

        if (_countdownCoroutine != null)
            StopCoroutine(_countdownCoroutine);

        _countdownCoroutine = null;

        if (_countdownSeconds.Value != -1)
            _countdownSeconds.Value = -1;

        if (wasRunning)
        {
            Debug.Log(
                "게임 시작 카운트다운이 취소되었습니다.");
        }
    }

    private void LoadGameScene()
    {
        if (!base.IsServerInitialized)
        {
            _isGameStarting = false;

            Debug.LogWarning(
                "게임 씬 이동은 서버에서만 실행할 수 있습니다.");

            return;
        }

        SceneLoadData sceneLoadData =
            new("Day08_GameScene")
            {
                ReplaceScenes = ReplaceOption.All
            };

        base.SceneManager.LoadGlobalScenes(sceneLoadData);
    }

    private void OnReadyStatesChanged(
        SyncDictionaryOperation operation,
        int clientId,
        bool isReady,
        bool asServer)
    {
        // Host는 서버 콜백과 클라이언트 콜백을 모두 받을 수 있으므로
        // 실제 UI는 클라이언트 측 콜백에서만 갱신합니다.
        if (asServer)
            return;

        RefreshLobbyUI();
    }

    private void OnHostClientIdChanged(
        int previousClientId,
        int nextClientId,
        bool asServer)
    {
        if (asServer)
            return;

        RefreshLobbyUI();
    }

    private void OnCountdownSecondsChanged(
        int previousSeconds,
        int nextSeconds,
        bool asServer)
    {
        if (asServer)
            return;

        RefreshLobbyUI();
    }

    private void RefreshLobbyUI()
    {
        if (!base.IsClientInitialized)
            return;

        int localClientId =
            base.LocalConnection.ClientId;

        if (_playerListText != null)
        {
            StringBuilder builder = new();

            builder.AppendLine("플레이어 목록");

            List<int> clientIds = new();

            foreach (int clientId in _readyStates.Keys)
                clientIds.Add(clientId);

            clientIds.Sort();

            if (clientIds.Count == 0)
            {
                builder.AppendLine(
                    "플레이어 접속 대기");
            }

            foreach (int clientId in clientIds)
            {
                bool isReady =
                    _readyStates[clientId];

                string hostLabel =
                    clientId == _hostClientId.Value
                        ? " (방장)"
                        : string.Empty;

                string localLabel =
                    clientId == localClientId
                        ? " (나)"
                        : string.Empty;

                string readyLabel =
                    isReady
                        ? "준비"
                        : "준비 안 됨";

                builder.AppendLine(
                    $"ClientId {clientId}" +
                    $"{hostLabel}{localLabel} — " +
                    $"{readyLabel}");
            }

            _playerListText.text =
                builder.ToString();
        }

        if (_readyButtonLabel != null)
        {
            bool isLocalReady =
                _readyStates.TryGetValue(
                    localClientId,
                    out bool ready) &&
                ready;

            _readyButtonLabel.text =
                isLocalReady
                    ? "준비 취소"
                    : "준비";
        }

        if (_readyButton != null)
        {
            // 카운트다운 중에는 준비 취소가 가능해야 합니다.
            // 0이 된 이후의 실제 씬 이동 순간에만 버튼을 잠급니다.
            _readyButton.interactable =
                _countdownSeconds.Value != 0;
        }

        if (_countdownText != null)
        {
            int remainingSeconds =
                _countdownSeconds.Value;

            int requiredPlayerCount =
                Mathf.Max(1, _minimumPlayerCount);

            if (remainingSeconds > 0)
            {
                _countdownText.text =
                    "모든 플레이어 준비 완료\n" +
                    $"{remainingSeconds}초 후 게임을 시작합니다.";
            }
            else if (remainingSeconds == 0)
            {
                _countdownText.text =
                    "게임을 시작합니다...";
            }
            else if (_readyStates.Count <
                     requiredPlayerCount)
            {
                _countdownText.text =
                    "플레이어 대기 중\n" +
                    $"({_readyStates.Count}/" +
                    $"{requiredPlayerCount})";
            }
            else
            {
                _countdownText.text =
                    "모든 플레이어가 준비하면\n" +
                    "3초 뒤 게임을 시작합니다.";
            }
        }
    }
}