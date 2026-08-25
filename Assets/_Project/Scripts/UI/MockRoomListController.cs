using UnityEngine;
using UnityEngine.UI;

public sealed class MockRoomListController : MonoBehaviour
{
    [Header("Mock Room UI")]
    [SerializeField] private GameObject _mockRoomItem;
    [SerializeField] private Text _roomNameText;
    [SerializeField] private Text _playerCountText;
    [SerializeField] private Text _roomStateText;
    [SerializeField] private GameObject _emptyRoomListText;

    public void RefreshMockRooms()
    {
        if (_mockRoomItem == null ||
            _roomNameText == null ||
            _playerCountText == null ||
            _roomStateText == null ||
            _emptyRoomListText == null)
        {
            Debug.LogWarning(
                "Mock Room UI 참조가 연결되지 않았습니다.");

            return;
        }

        // 실제 Room 검색 서비스가 준비되기 전까지
        // 고정된 테스트 데이터를 화면에 표시합니다.
        _roomNameText.text = "jjinzxx's World";
        _playerCountText.text = "1 / 4";
        _roomStateText.text = "대기 중";

        _mockRoomItem.SetActive(true);
        _emptyRoomListText.SetActive(false);
    }
}