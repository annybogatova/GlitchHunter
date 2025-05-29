using UnityEngine;

public interface IRoomManager
{
    void InitializeRoom();
    string roomId { get; } // Для идентификации комнаты
}