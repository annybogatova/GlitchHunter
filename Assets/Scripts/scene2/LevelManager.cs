using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    public RoomManager currentRoomManager;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
    }

    public void EnterRoom(RoomManager roomManager)
    {
        Debug.Log($"Игрок вошел в комнату {roomManager.gameObject.name}");
        currentRoomManager = roomManager;
        currentRoomManager.InitializeRoom();
    }
}
