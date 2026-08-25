using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField, Min(0f)]
    private float _moveSpeed = 5f;

    private void Update()
    {
        // 모든 게임에 Player가 존재하므로 소유한 Player만 입력을 처리한다.
        if (!IsOwner || Keyboard.current == null)
            return;

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed)
            horizontal -= 1f;

        if (Keyboard.current.dKey.isPressed)
            horizontal += 1f;

        if (Keyboard.current.sKey.isPressed)
            vertical -= 1f;

        if (Keyboard.current.wKey.isPressed)
            vertical += 1f;

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        transform.position += moveDirection * _moveSpeed * Time.deltaTime;
    }
}