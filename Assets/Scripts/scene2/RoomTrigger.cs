using UnityEngine;
public interface IRoomManager
{
    void InitializeRoom();
    string roomId { get; } // Для идентификации комнаты
}
public class RoomTrigger : MonoBehaviour
{
    [SerializeField] private MonoBehaviour roomManagerComponent; // Поле для перетаскивания в инспекторе
    private IRoomManager roomManager; // Интерфейс для работы с менеджером

    private void Awake()
    {
        // Проверяем коллайдер
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        else
        {
            Debug.LogError($"Collider не найден на {gameObject.name}!");
        }

        // Проверяем, реализует ли компонент IRoomManager
        if (roomManagerComponent != null)
        {
            roomManager = roomManagerComponent as IRoomManager;
            if (roomManager == null)
            {
                Debug.LogError($"Компонент {roomManagerComponent.name} не реализует IRoomManager!");
            }
        }
        else
        {
            Debug.LogError($"RoomManagerComponent не назначен для триггера {gameObject.name}!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (roomManager == null)
            {
                Debug.LogError($"RoomManager не назначен или не реализует IRoomManager для триггера {gameObject.name}!");
                return;
            }
            LevelManager.instance.EnterRoom(roomManager);
        }
    }
}
