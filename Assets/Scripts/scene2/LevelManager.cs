using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    public GameObject[] rooms; // Массив объектов комнат (Room_1, Room_2, и т.д.)
    private List<string> completedRooms = new List<string>(); // Список завершённых комнат
    private readonly string completedRoomsKey = "CompletedRooms"; // Ключ для PlayerPrefs
    private readonly string[] roomIds = { "Room_1", "Room_2" }; // ID комнат
    private string currentRoomId; // ID текущей активной комнаты (null при старте)

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            PlayerPrefs.DeleteKey(completedRoomsKey); // ВРЕМЕННЫЙ СБРОС ДЛЯ ТЕСТА
            LoadCompletedRooms();
            InitializeFirstRoom();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeFirstRoom()
    {
        if (rooms.Length == 0)
        {
            Debug.LogError("Массив rooms не настроен в LevelManager!");
            return;
        }

        if (rooms.Length != roomIds.Length)
        {
            Debug.LogError($"Количество комнат ({rooms.Length}) не совпадает с количеством roomIds ({roomIds.Length})!");
            return;
        }

        // Проверяем, что все комнаты имеют компонент, реализующий IRoomManager
        for (int i = 0; i < rooms.Length; i++)
        {
            IRoomManager manager = rooms[i].GetComponent<IRoomManager>();
            if (manager == null)
            {
                Debug.LogError($"Компонент, реализующий IRoomManager, не найден на объекте {rooms[i].name}!");
                continue;
            }
            if (manager.roomId != roomIds[i])
            {
                Debug.LogWarning($"IRoomManager на {rooms[i].name} имеет roomId '{manager.roomId}', ожидается '{roomIds[i]}'");
            }
        }

        Debug.Log("LevelManager инициализирован, ожидается вход игрока в триггер");
    }

    private void LoadCompletedRooms()
    {
        string rooms = PlayerPrefs.GetString(completedRoomsKey, "");
        if (!string.IsNullOrEmpty(rooms))
        {
            completedRooms = new List<string>(rooms.Split(','));
        }
        Debug.Log($"Загружено завершённых комнат: {completedRooms.Count} ({string.Join(", ", completedRooms)})");
    }

    private void SaveCompletedRooms()
    {
        PlayerPrefs.SetString(completedRoomsKey, string.Join(",", completedRooms));
        PlayerPrefs.Save();
        Debug.Log($"Сохранено завершённых комнат: {completedRooms.Count} ({string.Join(", ", completedRooms)})");
    }

    public void EnterRoom(IRoomManager roomManager)
    {
        if (roomManager == null)
        {
            Debug.LogError("RoomManager is null!");
            return;
        }

        // Проверяем, можно ли войти в эту комнату (последовательность)
        int roomIndex = System.Array.IndexOf(roomIds, roomManager.roomId);
        if (roomIndex == -1)
        {
            Debug.LogError($"Комната с ID {roomManager.roomId} не найдена в roomIds!");
            return;
        }

        // Если это не первая комната, проверяем завершение предыдущих
        if (roomIndex > 0)
        {
            for (int i = 0; i < roomIndex; i++)
            {
                if (!completedRooms.Contains(roomIds[i]))
                {
                    Debug.LogWarning($"Нельзя войти в {roomManager.roomId}! Сначала завершите {roomIds[i]}");
                    return;
                }
            }
        }

        // Активируем комнату
        currentRoomId = roomManager.roomId;
        roomManager.InitializeRoom();
        Debug.Log($"Игрок вошёл в комнату {roomManager.roomId} (индекс {roomIndex})");
    }

    public void CompleteRoom(string roomId)
    {
        if (!completedRooms.Contains(roomId))
        {
            completedRooms.Add(roomId);
            SaveCompletedRooms();
            Debug.Log($"Комната {roomId} отмечена как завершённая");
        }
    }

    public bool IsRoomCompleted(string roomId)
    {
        bool completed = completedRooms.Contains(roomId);
        Debug.Log($"Проверка комнаты {roomId}: {(completed ? "Завершена" : "Не завершена")}");
        return completed;
    }
}