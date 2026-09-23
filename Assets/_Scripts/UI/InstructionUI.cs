using System.Collections;
using TMPro;
using UnityEngine;

public class InstructionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text instructionText;

    private Coroutine currentRoutine;

    private void Awake()
    {
        instructionText.gameObject.SetActive(false);
    }

    public void ShowInstruction(string message, float duration = 4f)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine =
            StartCoroutine(ShowRoutine(message, duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        instructionText.text = message;
        instructionText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        instructionText.gameObject.SetActive(false);
        currentRoutine = null;
    }
}