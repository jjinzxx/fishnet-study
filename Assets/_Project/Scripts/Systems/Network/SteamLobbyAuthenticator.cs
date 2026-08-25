using System;
using FishNet.Authenticating;
using FishNet.Connection;
using HeathenEngineering.SteamworksIntegration;
using Steamworks;
using UnityEngine;

public sealed class SteamLobbyAuthenticator : Authenticator
{
    [Header("Steam Lobby")]
    [SerializeField] private LobbyManager _lobbyManager;

    public override event Action<NetworkConnection, bool>
        OnAuthenticationResult;

    public override void OnRemoteConnection(
        NetworkConnection connection)
    {
        bool isLobbyMember = false;
        ulong steamId64 = 0;

        string transportAddress =
            string.Empty;

        string failureReason =
            string.Empty;

        if (_lobbyManager == null)
        {
            failureReason =
                "LobbyManager 참조가 연결되지 않았습니다.";
        }
        else if (!SteamSettings.Initialized)
        {
            failureReason =
                "Steam API가 초기화되지 않았습니다.";
        }
        else if (!_lobbyManager.HasLobby)
        {
            failureReason =
                "Host가 관리 중인 Steam Lobby가 없습니다.";
        }
        else if (!_lobbyManager.Lobby.IsOwner)
        {
            failureReason =
                "FishNet Server를 실행한 사용자가 " +
                "Steam Lobby 방장이 아닙니다.";
        }
        else
        {
            transportAddress =
                connection.GetAddress();

            if (!ulong.TryParse(
                    transportAddress,
                    out steamId64) ||
                steamId64 == 0)
            {
                failureReason =
                    "FishySteamworks 연결 주소를 " +
                    "Steam ID64로 변환하지 못했습니다.";
            }
            else
            {
                CSteamID steamId =
                    new CSteamID(steamId64);

                isLobbyMember =
                    _lobbyManager.Lobby.IsAMember(
                        steamId);

                if (!isLobbyMember)
                {
                    failureReason =
                        "현재 Steam Lobby 멤버가 아닙니다.";
                }
            }
        }

        if (isLobbyMember)
        {
            Debug.Log(
                "Steam Lobby 멤버 FishNet 인증 성공\n" +
                $"FishNet Client ID: {connection.ClientId}\n" +
                $"Steam ID64: {steamId64}");
        }
        else
        {
            string steamIdText =
                steamId64 == 0
                    ? "확인 실패"
                    : steamId64.ToString();

            Debug.LogWarning(
                "Steam Lobby 멤버 FishNet 인증 실패\n" +
                $"FishNet Client ID: {connection.ClientId}\n" +
                $"Steam ID64: {steamIdText}\n" +
                $"이유: {failureReason}");
        }

        // FishNet이 이 결과를 받아 인증 완료 또는 연결 종료를 처리합니다.
        OnAuthenticationResult?.Invoke(
            connection,
            isLobbyMember);
    }
}