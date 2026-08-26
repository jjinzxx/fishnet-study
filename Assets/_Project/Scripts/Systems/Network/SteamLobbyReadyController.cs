using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public sealed class SteamLobbyReadyController : NetworkBehaviour
{
    private readonly SyncDictionary<int, bool>
        _guestReadyStates = new();

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
}
