using System;
using FishNet.Managing;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SteamNetworkShutdownController : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;

    private Func<bool> _quitHandler;

    private void Awake()
    {
        _quitHandler = () =>
        {
            // 다음 Play Mode 실행에 종료 이벤트가 남지 않도록
            // 현재 종료 콜백을 먼저 해제합니다.
            Application.wantsToQuit -= _quitHandler;

            if (_networkManager == null ||
                !_networkManager.Initialized ||
                _networkManager.TransportManager == null ||
                _networkManager.TransportManager.Transport == null)
            {
                return true;
            }

            // Heathen이 Steam API를 종료하기 전에
            // FishySteamworks의 Client와 Server 소켓을 먼저 닫습니다.
            _networkManager.TransportManager.Transport.Shutdown();

            // 애플리케이션 종료를 계속 진행합니다.
            return true;
        };

        Application.wantsToQuit += _quitHandler;
    }
}