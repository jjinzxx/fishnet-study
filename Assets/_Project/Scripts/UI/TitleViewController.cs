using UnityEngine;

public sealed class TitleViewController : MonoBehaviour
{
    [Header("Title UI")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _modeSelectionPanel;
    [SerializeField] private GameObject _multiplayerLobbyPanel;
    [SerializeField] private GameObject _createRoomPanel;

    public void OpenModeSelection()
    {
        if (_mainMenuPanel == null ||
            _modeSelectionPanel == null)
        {
            Debug.LogWarning(
                "Title UI 참조가 연결되지 않았습니다.");

            return;
        }

        // 네트워크 요청이 필요 없는 로컬 메뉴 전환이므로
        // 두 패널의 표시 상태만 변경합니다.
        _mainMenuPanel.SetActive(false);
        _modeSelectionPanel.SetActive(true);
    }

    public void OpenCoopLobby()
    {
        if (_modeSelectionPanel == null ||
            _multiplayerLobbyPanel == null)
        {
            Debug.LogWarning(
                "협동전 UI 참조가 연결되지 않았습니다.");

            return;
        }

        // 아직 Room 검색을 시작하는 단계가 아니므로
        // 모드 선택 화면과 대기방 목록의 표시만 변경합니다.
        _modeSelectionPanel.SetActive(false);
        _multiplayerLobbyPanel.SetActive(true);
    }

    public void OpenCreateRoomSettings()
    {
        if (_multiplayerLobbyPanel == null ||
            _createRoomPanel == null)
        {
            Debug.LogWarning(
                "Room 생성 UI 참조가 연결되지 않았습니다.");

            return;
        }

        // 아직 실제 Room을 생성하지 않으므로
        // 대기방 목록과 생성 설정 화면의 표시만 변경합니다.
        _multiplayerLobbyPanel.SetActive(false);
        _createRoomPanel.SetActive(true);
    }
}