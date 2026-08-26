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
}