using HeathenEngineering.SteamworksIntegration;
using UnityEngine;
using UnityEngine.UI;

public sealed class SteamLobbyMemberController : MonoBehaviour
{
    [Header("Steam Lobby")]
    [SerializeField] private LobbyManager _lobbyManager;

    [Header("Lobby 대기실 UI")]
    [SerializeField] private Text _waitingRoomSettingsText;
    [SerializeField] private Text _waitingMemberListText;
    [SerializeField] private Text _roomStatusText;
    [SerializeField] private Button _startGameButton;

    public void RefreshWaitingRoomMembers()
    {
        if (_lobbyManager == null ||
            _waitingRoomSettingsText == null ||
            _waitingMemberListText == null ||
            _roomStatusText == null ||
            _startGameButton == null)
        {
            Debug.LogWarning(
                "Steam Lobby 멤버 UI 참조가 연결되지 않았습니다.");

            return;
        }

        if (!SteamSettings.Initialized)
        {
            Debug.LogWarning(
                "Steam API가 초기화되지 않았습니다.");

            return;
        }

        // 로컬 사용자가 Lobby를 나가는 과정에서도
        // 멤버 퇴장 이벤트가 전달될 수 있으므로 조용히 종료합니다.
        if (!_lobbyManager.HasLobby)
        {
            return;
        }

        LobbyData lobby =
            _lobbyManager.Lobby;

        LobbyMemberData[] members =
            lobby.Members;

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
            lobby.SteamId.m_SteamID;

        ulong ownerSteamId =
            lobby.Owner.user.SteamId;

        _waitingRoomSettingsText.text =
            $"Lobby ID64: {lobbyId}\n" +
            $"Host ID64: {ownerSteamId}\n" +
            $"인원: {lobby.MemberCount} / " +
            $"{lobby.MaxMembers}";

        _waitingMemberListText.text =
            memberListText;

        if (lobby.IsOwner)
        {
            _roomStatusText.text =
                lobby.MemberCount >= 2
                    ? "Steam Lobby 멤버 확인 완료 · FishNet 연결 전"
                    : "다른 Steam 사용자를 기다리는 중입니다.";
        }
        else
        {
            _roomStatusText.text =
                "방장이 네트워크 연결을 시작할 때까지 기다려 주세요.";
        }

        // Steam Lobby 멤버가 2명이 되어도 아직 FishNet Host와
        // Client를 시작하지 않았으므로 버튼은 활성화하지 않습니다.
        _startGameButton.interactable = false;

        Debug.Log(
            "Steam Lobby 멤버 UI 갱신\n" +
            $"Lobby ID64: {lobbyId}\n" +
            $"Host ID64: {ownerSteamId}\n" +
            $"인원: {lobby.MemberCount} / " +
            $"{lobby.MaxMembers}\n" +
            $"내가 방장인가: {lobby.IsOwner}\n" +
            $"멤버 목록:\n{memberListText}");
    }
}