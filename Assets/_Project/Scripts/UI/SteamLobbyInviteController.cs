using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.API;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SteamLobbyInviteController : MonoBehaviour
{
    [Header("Steam Lobby")]
    [SerializeField] private LobbyManager _lobbyManager;

    public void OnInviteFriendButtonClicked()
    {
        if (_lobbyManager == null)
        {
            Debug.LogWarning(
                "친구 초대에 필요한 LobbyManager 참조가 " +
                "연결되지 않았습니다.");

            return;
        }

        if (!SteamSettings.Initialized ||
            !App.Client.LoggedOn)
        {
            Debug.LogWarning(
                "Steam API가 초기화되지 않았거나 " +
                "Steam에 로그인되지 않았습니다.");

            return;
        }

        if (!_lobbyManager.HasLobby)
        {
            Debug.LogWarning(
                "친구를 초대할 Steam Lobby가 없습니다.");

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

        if (lobby.MemberCount >=
            lobby.MaxMembers)
        {
            Debug.LogWarning(
                "Steam Lobby가 가득 차서 친구를 " +
                "초대할 수 없습니다.\n" +
                $"현재 인원: {lobby.MemberCount} / " +
                $"{lobby.MaxMembers}");

            return;
        }

        if (!Overlay.Client.IsEnabled)
        {
            Debug.LogWarning(
                "Steam Overlay가 활성화되지 않았습니다.\n" +
                "Steam 클라이언트에서 게임 내 Steam Overlay " +
                "설정을 확인해 주세요.");

            return;
        }

        Overlay.Client.ActivateInviteDialog(
            lobby);

        Debug.Log(
            "Steam 친구 초대 창을 열었습니다.\n" +
            $"Lobby ID64: {lobby.SteamId.m_SteamID}\n" +
            $"현재 인원: {lobby.MemberCount} / " +
            $"{lobby.MaxMembers}");
    }
}