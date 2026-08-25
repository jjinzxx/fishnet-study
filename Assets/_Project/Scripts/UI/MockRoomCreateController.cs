using UnityEngine;
using UnityEngine.UI;

public sealed class MockRoomCreateController : MonoBehaviour
{
    [Header("Room 생성 입력")]
    [SerializeField] private InputField _roomNameInputField;
    [SerializeField] private Dropdown _maxPlayersDropdown;

    [Header("화면")]
    [SerializeField] private GameObject _createRoomPanel;
    [SerializeField] private GameObject _roomWaitingPanel;

    [Header("Room 대기실 UI")]
    [SerializeField] private Text _waitingRoomNameText;
    [SerializeField] private Text _waitingRoomSettingsText;
    [SerializeField] private Text _waitingMemberListText;
    [SerializeField] private Text _roomStatusText;
    [SerializeField] private Button _startGameButton;

    public void CreateMockRoom()
    {
        if (_roomNameInputField == null ||
            _maxPlayersDropdown == null ||
            _createRoomPanel == null ||
            _roomWaitingPanel == null ||
            _waitingRoomNameText == null ||
            _waitingRoomSettingsText == null ||
            _waitingMemberListText == null ||
            _roomStatusText == null ||
            _startGameButton == null)
        {
            Debug.LogWarning(
                "Mock Room 생성 UI 참조가 연결되지 않았습니다.");

            return;
        }

        string roomName =
            _roomNameInputField.text.Trim();

        if (roomName.Length == 0)
        {
            Debug.LogWarning(
                "Room 이름을 입력해 주세요.");

            return;
        }

        // Dropdown의 Value는 0부터 시작
        int maxPlayers =
            _maxPlayersDropdown.value == 0
                ? 2
                : 4;

        _waitingRoomNameText.text = roomName;
        _waitingRoomSettingsText.text =
            $"난이도: 1\n최대 인원: {maxPlayers}";
        _waitingMemberListText.text =
            "Player (방장, 나)";
        _roomStatusText.text =
            "다른 플레이어를 기다리는 중입니다.";

        // Room을 막 생성한 시점에는 Host 혼자이므로
        // 최소 시작 인원 2명을 만족하지 못합니다.
        _startGameButton.interactable = false;

        _createRoomPanel.SetActive(false);
        _roomWaitingPanel.SetActive(true);
    }
}