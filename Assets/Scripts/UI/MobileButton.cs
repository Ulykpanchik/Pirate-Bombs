
using UnityEngine;
using UnityEngine.EventSystems;

public class MobileButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType
    {
        Left,
        Right,
        Jump,
        Bomb
    }

    public ButtonType buttonType;

    public static bool LeftHeld;
    public static bool RightHeld;
    public static bool BombHeld;

    private static bool jumpRequested;

    public static bool ConsumeJump()
    {
        bool result = jumpRequested;
        jumpRequested = false;
        return result;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        switch (buttonType)
        {
            case ButtonType.Left:
                LeftHeld = true;
                break;

            case ButtonType.Right:
                RightHeld = true;
                break;

            case ButtonType.Jump:
                jumpRequested = true;
                break;

            case ButtonType.Bomb:
                BombHeld = true;
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        switch (buttonType)
        {
            case ButtonType.Left:
                LeftHeld = false;
                break;

            case ButtonType.Right:
                RightHeld = false;
                break;

            case ButtonType.Bomb:
                BombHeld = false;
                break;
        }
    }

    private void OnDisable()
    {
        switch (buttonType)
        {
            case ButtonType.Left:
                LeftHeld = false;
                break;
            case ButtonType.Right:
                RightHeld = false;
                break;
            case ButtonType.Bomb:
                BombHeld = false;
                break;
        }
    }
}
