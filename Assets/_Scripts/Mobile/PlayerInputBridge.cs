using UnityEngine;

public class PlayerInputBridge : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    public bool InteractPressed { get; private set; }

    public void SetMove(Vector2 value)
    {
        MoveInput = value;
    }

    public void SetLook(Vector2 value)
    {
        LookInput = value;
    }

    public void PressInteract()
    {
        InteractPressed = true;
    }

    public bool ConsumeInteract()
    {
        if (!InteractPressed)
            return false;

        InteractPressed = false;
        return true;
    }

    public void ResetInput()
    {
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
        InteractPressed = false;
    }
}