using UnityEngine;

public class IntroDialogueTrigger : MonoBehaviour
{
    [Header("Secuencia de introducción")]
    [SerializeField]
    private IntroDialogueSequence dialogueSequence;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        PlayerIdentity player =
            other.GetComponentInParent<PlayerIdentity>();

        if (player == null)
            return;

        triggered = true;

        Debug.Log(
            $"🎪 INTRO TRIGGER: Player {player.PlayerId} entró a la sala principal."
        );

        if (dialogueSequence != null)
        {
            dialogueSequence.BeginSequence();
        }
        else
        {
            Debug.LogWarning(
                "⚠️ IntroDialogueTrigger no tiene IntroDialogueSequence asignado."
            );
        }

        // Ya cumplió su función.
        // Evitamos que vuelva a dispararse.
        Collider triggerCollider =
            GetComponent<Collider>();

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }
}