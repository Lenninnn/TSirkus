using UnityEngine;

public class IntroInstruction : MonoBehaviour
{
    [SerializeField] private InstructionUI instructionUI;

    private void Start()
    {
        instructionUI.ShowInstruction(
            "EXPLORA Y AVANZA",
            5f
        );
    }
}