using UnityEngine;
using UnityEngine.UI;

public sealed class MockRoomJoinController : MonoBehaviour
{
    [Header("화면")]
    [SerializeField] private GameObject _multiplayerLobbyPanel;
    [SerializeField] private GameObject _roomWaitingPanel;

    [Header("Mock Room 정보")]
    [SerializeField] private Text _waitingRoomNameText;
    [SerializeField] private Text _waitingRoomSettingsText;
    [SerializeField] private Text _waitingMemberListText;
    [SerializeField] private Button _startGameButton;

    public void JoinMockRoom()
    {
        if (_multiplayerLobbyPanel == null ||
            _roomWaitingPanel == null ||
            _waitingRoomNameText == null ||
            _waitingRoomSettingsText == null ||
            _waitingMemberListText == null ||
            _startGameButton == null)
        {
            Debug.LogWarning(
                "Mock Room 참가 UI 참조가 연결되지 않았습니다.");

            return;
        }

        // 실제 서버 응답을 대신하여
        // 참가한 Mock Room 정보를 화면에 표시합니다.
        _waitingRoomNameText.text = "Like's World";
        _waitingRoomSettingsText.text =
            "난이도: 1\n최대 인원: 4";
        _waitingMemberListText.text =
            "Like (방장)\nGuest (나)";

        // 이번 실습의 로컬 사용자는 Guest이므로
        // 게임 시작 버튼을 사용할 수 없도록 설정합니다.
        _startGameButton.interactable = false;

        _multiplayerLobbyPanel.SetActive(false);
        _roomWaitingPanel.SetActive(true);
    }
}