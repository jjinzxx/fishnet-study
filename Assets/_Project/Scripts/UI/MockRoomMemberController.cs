using UnityEngine;
using UnityEngine.UI;

public sealed class MockRoomMemberController : MonoBehaviour
{
    [Header("Room 대기실 UI")]
    [SerializeField] private Text _waitingMemberListText;
    [SerializeField] private Text _roomStatusText;
    [SerializeField] private Button _startGameButton;
    [SerializeField] private Button _addMockGuestButton;

    public void AddMockGuest()
    {
        if (_waitingMemberListText == null ||
            _roomStatusText == null ||
            _startGameButton == null ||
            _addMockGuestButton == null)
        {
            Debug.LogWarning(
                "Mock Guest UI 참조가 연결되지 않았습니다.");

            return;
        }

        // 이 버튼은 Mock Room을 생성한 Host만 사용할 수 있습니다.
        if (!_waitingMemberListText.text.Contains(
                "(방장, 나)"))
        {
            Debug.LogWarning(
                "Host 상태에서만 Mock Guest를 추가할 수 있습니다.");

            return;
        }

        _waitingMemberListText.text =
            "Player (방장, 나)\nGuest (테스트)";
        _roomStatusText.text =
            "최소 시작 인원 2명이 충족되었습니다.";

        _startGameButton.interactable = true;
        _addMockGuestButton.interactable = false;
    }
}