using UnityEngine;

public class PuzzleDoor : MonoBehaviour
{
    [SerializeField] private GameObject doorVisual;

    public void OpenDoor()
    {
        doorVisual.SetActive(false);

        Debug.Log("PUERTA ABIERTA");
    }

    public void CloseDoor()
    {
        doorVisual.SetActive(true);

        Debug.Log("PUERTA CERRADA");
    }
}