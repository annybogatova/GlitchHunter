using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomManager : MonoBehaviour
{
    public static RoomManager instance;
    
    public GameObject zeroPrefab;
    public GameObject onePrefab;
    public Material pipeMaterial;
    public GameObject roomContainer;


    private void Awake()
    {
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupSlotsAndPipes();
        GenerateInputs();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SetupSlotsAndPipes()
    {
        if (roomContainer == null)
        {
            Debug.LogError("Room_1 container не назначен в GameManager!");
            return;
        }
        
        Transform[] walls = roomContainer.GetComponentsInChildren<Transform>(true);
        walls = walls.Where(t => t.parent == roomContainer.transform).ToArray();

        foreach (Transform wall in walls)
        {
            Transform slotsContainer = wall.Find("Slots");
            if (slotsContainer == null)
            {
                Debug.LogWarning($"Slots не найдены в {wall.name}");
                continue;
            }

            // Находим все Slot_X
            Transform[] slots = slotsContainer.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("Slot_"))
                .ToArray();

            foreach (Transform slot in slots)
            {
                // Добавляем SlotScript к SlotModel
                Transform slotModel = slot.Find("SlotObject");
                if (slotModel != null)
                {
                    if (!slotModel.gameObject.CompareTag("Slot"))
                    {
                        slotModel.gameObject.tag = "Slot";
                    }
                    SlotScript slotScript = slotModel.GetComponent<SlotScript>();
                    if (slotScript == null)
                    {
                        slotScript = slotModel.gameObject.AddComponent<SlotScript>();
                        // Добавляем коллайдер, если отсутствует
                        if (slotModel.GetComponent<Collider>() == null)
                        {
                            slotModel.gameObject.AddComponent<BoxCollider>();
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"SlotModel не найден в {slot.name}");
                }
                
                // Добавляем PipeScript к Pipe
                Transform pipe = slot.Find("Pipe");
                if (pipe != null)
                {
                    if (!pipe.gameObject.CompareTag("Pipe"))
                    {
                        pipe.gameObject.tag = "Pipe";
                    }
                    PipeScript pipeScript = pipe.GetComponent<PipeScript>();
                    if (pipeScript == null)
                    {
                        pipeScript = pipe.gameObject.AddComponent<PipeScript>();
                    }
                    Renderer rend = pipe.GetComponent<Renderer>();
                    if (rend != null && pipeMaterial != null)
                    {
                        rend.material = pipeMaterial;
                    }
                }
                else
                {
                    Debug.LogWarning($"Pipe не найден в {slot.name}");
                }
            }
        }
    }

    private void GenerateInputs()
    {
        if (roomContainer == null)
        {
            Debug.LogError("Room_1 container не назначен в GameManager!");
            return;
        }
        
        // Находим все стены
        Transform[] walls = roomContainer.GetComponentsInChildren<Transform>(true);
        walls = walls.Where(t => t.parent == roomContainer.transform).ToArray();

        foreach (Transform wall in walls)
        {
            // Находим Inputs и InputMarkers
            Transform inputsContainer = wall.Find("Inputs");
            Transform markersContainer = wall.Find("InputMarkers");
            if (inputsContainer == null || markersContainer == null)
            {
                Debug.LogWarning($"Inputs или InputMarkers не найдены в {wall.name}");
                continue;
            }

            // Находим маркеры и Input_N
            GameObject[] markers = markersContainer.GetComponentsInChildren<Transform>(true)
                .Where(t => t.gameObject.CompareTag("InputMarker"))
                .Select(t => t.gameObject)
                .ToArray();
            GameObject[] inputFolders = inputsContainer.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("Input_"))
                .Select(t => t.gameObject)
                .ToArray();
            
            if (markers.Length != inputFolders.Length)
            {
                Debug.LogWarning($"Количество маркеров ({markers.Length}) не совпадает с количеством Input_N ({inputFolders.Length}) в {wall.name}");
            }

            for (int i = 0; i < Mathf.Min(markers.Length, inputFolders.Length); i++)
            {
                GameObject marker = markers[i];
                GameObject inputFolder = inputFolders[i];

                // Проверяем Pipe
                Transform pipe = inputFolder.transform.Find("Pipe");
                if (pipe == null)
                {
                    Debug.LogWarning($"Pipe не найден в {inputFolder.name}");
                    continue;
                }
                
                if (!pipe.gameObject.CompareTag("Pipe"))
                {
                    pipe.gameObject.tag = "Pipe";
                }
                PipeScript pipeScript = pipe.GetComponent<PipeScript>();
                if (pipeScript == null)
                {
                    pipeScript = pipe.gameObject.AddComponent<PipeScript>();
                }
                Renderer rend = pipe.GetComponent<Renderer>();
                if (rend != null && pipeMaterial != null)
                {
                    rend.material = pipeMaterial;
                }
                
                // Добавляем InputScript
                InputScript input = inputFolder.GetComponent<InputScript>();
                if (input == null)
                {
                    input = inputFolder.AddComponent<InputScript>();
                }
                
                input.value = Random.value > 0.5f;

                // Создаем модельку
                GameObject modelPrefab = input.value ? onePrefab : zeroPrefab;
                if (modelPrefab != null)
                {
                    GameObject model = Instantiate(modelPrefab, marker.transform.position, marker.transform.rotation, inputFolder.transform);
                    model.name = input.value ? "Model_1" : "Model_0";
                }
                else
                {
                    Debug.LogError("Model prefab не назначен для " + (input.value ? "1" : "0"));
                }
            }
        }
    }
}
