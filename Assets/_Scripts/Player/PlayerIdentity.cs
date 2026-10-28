using UnityEngine;

public class PlayerIdentity : MonoBehaviour
{
    [SerializeField] private int playerId = 1;

    public int PlayerId => playerId;

    public void SetPlayerId(int id)
    {
        playerId = id;
        gameObject.name = $"Player_{id}";
    }
}