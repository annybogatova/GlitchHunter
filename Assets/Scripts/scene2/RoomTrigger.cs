using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    public RoomManager roomManager; // Ссылка на RoomManager этой комнаты

    private void Awake()
    {
        // Убедимся, что коллайдер является триггером
        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (roomManager == null)
            {
                Debug.LogError("RoomManager не назначен для триггера!");
                return;
            }
            LevelManager.instance.EnterRoom(roomManager);
        }
    }
}
