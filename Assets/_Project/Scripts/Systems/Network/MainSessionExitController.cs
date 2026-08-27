using FishNet;
using FishNet.Managing;
using HeathenEngineering.SteamworksIntegration;
using UnityEngine;
using UnitySceneManager =
    UnityEngine.SceneManagement.SceneManager;

[DisallowMultipleComponent]
public sealed class MainSessionExitController : MonoBehaviour
{
    private const string TitleSceneName =
        "Title";

    private bool _isLeaving;
    private bool _hasObservedClientConnection;
    private bool _hasLoggedMissingNetworkManager;

    private void Update()
    {
        if (_isLeaving)
        {
            return;
        }

        // InstanceFinder.NetworkManager는 찾지 못할 때마다
        // 로그를 출력하므로 안전하게 목록을 확인합니다.
        NetworkManager networkManager =
            NetworkManager.Instances.Count > 0
                ? NetworkManager.Instances[0]
                : null;

        if (networkManager == null)
        {
            // NetworkManager가 없는 동안에는 한 번만 알립니다.
            if (!_hasLoggedMissingNetworkManager)
            {
                _hasLoggedMissingNetworkManager = true;

                Debug.Log(
                    "현재 씬에서 NetworkManager를 찾지 못했습니다.");
            }

            // 기존 연결이 있던 상태에서 NetworkManager가 사라졌다면
            // 세션 종료로 판단합니다.
            if (_hasObservedClientConnection)
            {
                Debug.LogWarning(
                    "Main에서 FishNet 연결 종료를 감지했습니다.\n" +
                    "Steam Lobby를 정리하고 Title 씬으로 돌아갑니다.");

                OnLeaveSessionButtonClicked();
            }

            return;
        }

        // NetworkManager가 다시 발견되면 다음 소실 시
        // 로그를 다시 한 번 출력할 수 있도록 초기화합니다.
        _hasLoggedMissingNetworkManager = false;

        bool isClientConnected =
            networkManager.Initialized &&
            networkManager.IsClientStarted;

        if (isClientConnected)
        {
            _hasObservedClientConnection = true;
            return;
        }

        if (!_hasObservedClientConnection)
        {
            return;
        }

        Debug.LogWarning(
            "Main에서 FishNet Client 연결 종료를 감지했습니다.\n" +
            "Steam Lobby를 정리하고 Title 씬으로 돌아갑니다.");

        OnLeaveSessionButtonClicked();
    }

    public void OnLeaveSessionButtonClicked()
    {
        // 버튼을 빠르게 여러 번 눌러도
        // 종료 처리는 한 번만 진행합니다.
        if (_isLeaving)
        {
            return;
        }

        _isLeaving = true;

        NetworkManager networkManager =
            InstanceFinder.NetworkManager;

        LobbyData lobby =
            default;

        bool hasLobby =
            SteamSettings.Initialized &&
            LobbyData.SessionLobby(out lobby) &&
            lobby.IsValid;

        bool wasHost =
            networkManager != null &&
            networkManager.Initialized &&
            networkManager.IsHostStarted;

        ulong lobbyId =
            hasLobby
                ? lobby.SteamId.m_SteamID
                : 0;

        // Host가 나가는 동안 새로운 사용자가 들어오지 않도록
        // FishNet 연결을 종료하기 전에 Lobby를 잠급니다.
        if (hasLobby &&
            lobby.IsOwner)
        {
            bool joinableChanged =
                lobby.SetJoinable(false);

            if (!joinableChanged)
            {
                Debug.LogWarning(
                    "Steam Lobby 참가 잠금 요청에 실패했습니다.");
            }
        }

        if (networkManager != null &&
            networkManager.Initialized)
        {
            // Host도 로컬 Client를 함께 실행하므로
            // Host와 Guest 모두 Client부터 종료합니다.
            if (networkManager.IsClientStarted)
            {
                networkManager.ClientManager
                    .StopConnection();
            }

            // Server는 Host에서만 실행되므로
            // Guest에서는 이 조건을 통과하지 않습니다.
            if (networkManager.IsServerStarted)
            {
                // 원격 Guest에게 연결 종료 메시지를 전송한 뒤
                // FishNet Server를 종료합니다.
                networkManager.ServerManager
                    .StopConnection(true);
            }
        }

        // FishNet 연결을 먼저 정리한 뒤
        // 현재 Steam Session Lobby에서 나갑니다.
        if (hasLobby)
        {
            lobby.Leave();
        }

        string roleText =
            wasHost
                ? "Host"
                : "Guest";

        string lobbyIdText =
            lobbyId == 0
                ? "확인되지 않음"
                : lobbyId.ToString();

        Debug.Log(
            "Main 멀티플레이 세션 종료 요청 완료\n" +
            $"종료 역할: {roleText}\n" +
            $"Lobby ID64: {lobbyIdText}\n" +
            $"Title 씬 이동: {TitleSceneName}");

        // Title에 배치된 새 NetworkManager가 생성되면서
        // Destroy Oldest 설정에 따라 기존 NetworkManager를 교체합니다.
        UnitySceneManager.LoadScene(
            TitleSceneName);
    }
}