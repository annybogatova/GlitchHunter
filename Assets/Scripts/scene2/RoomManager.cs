using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class RoomManager : MonoBehaviour
{
    public static RoomManager instance;
    public GateScript.GateType? selectedGateType = null;
    public GameObject zeroPrefab;
    public GameObject onePrefab;
    public GameObject andGatePrefab;
    public GameObject orGatePrefab;
    public Material pipeMaterial;
    public GameObject roomContainer;
    public InventoryUI inventoryUI;
    public TextAsset connectionsJson;
    
    private PlayerInputController _playerInputController;
    private Dictionary<string, Dictionary<string, string[]>> wallConnections;

    private void Awake()
    {
        instance = this;
        _playerInputController = FindObjectOfType<PlayerInputController>();

        if (_playerInputController != null)
        {
            _playerInputController.OnInteractPressed += () => Interact();
        }
        
        // Парсинг JSON-файла
        if (connectionsJson != null)
        {
            try
            {
                WallConnections config = JsonUtility.FromJson<WallConnections>(connectionsJson.text);
                wallConnections = new Dictionary<string, Dictionary<string, string[]>>();
                foreach (var wall in config.walls)
                {
                    var slotDict = new Dictionary<string, string[]>();
                    foreach (var slot in wall.slots)
                    {
                        slotDict[slot.slotName] = slot.inputs;
                    }
                    wallConnections[wall.wallName] = slotDict;
                }
                Debug.Log($"JSON успешно распарсен. Найдено стен: {wallConnections.Keys.Count} ({string.Join(", ", wallConnections.Keys)})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка при парсинге JSON: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("JSON-файл не привязан в RoomManager!");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupSlotsAndPipes();
        GenerateInputs();
        AssignSlotConnections();
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
                    if (!slotModel.gameObject.CompareTag("Slot") && !slotModel.gameObject.CompareTag("NegatedSlot"))
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

private void AssignSlotConnections()
{
    if (roomContainer == null)
    {
        Debug.LogError("Room_1 container не назначен в RoomManager!");
        return;
    }

    if (wallConnections == null)
    {
        Debug.LogError("Конфигурация стен из JSON не загружена!");
        return;
    }

    // Получаем все трансформы
    Transform[] walls = roomContainer.GetComponentsInChildren<Transform>(true);
    Debug.Log($"Все трансформы: {string.Join(", ", walls.Select(t => t.name))}");

    // Фильтруем только прямые дочерние объекты roomContainer с корректными именами
    walls = walls.Where(t => t.parent == roomContainer.transform && t.name.StartsWith("wall_")).ToArray();
    Debug.Log($"Отфильтрованные стены: {string.Join(", ", walls.Select(t => t.name))}");

    // Сортируем стены по числовому значению в имени
    walls = walls.OrderBy(t =>
    {
        string numberPart = t.name.Replace("wall_", "");
        if (int.TryParse(numberPart, out int number))
        {
            return number;
        }
        Debug.LogWarning($"Невалидное имя стены: {t.name}. Ожидается формат 'wall_X', где X - число.");
        return int.MaxValue; // Помещаем невалидные имена в конец
    }).ToArray();

    Debug.Log($"Найдено стен: {walls.Length} ({string.Join(", ", walls.Select(w => w.name))})");

    for (int i = 0; i < walls.Length; i++)
    {
        Transform wall = walls[i];
        string wallName = wall.name;
        Debug.Log($"Обрабатываем стену: {wallName}");

        if (!wallConnections.ContainsKey(wallName))
        {
            Debug.LogWarning($"Конфигурация для {wallName} не найдена в JSON");
            continue;
        }

        Transform slotsContainer = wall.Find("Slots");
        Transform inputsContainer = wall.Find("Inputs");
        if (slotsContainer == null || inputsContainer == null)
        {
            Debug.LogWarning($"Slots или Inputs не найдены в {wallName}");
            continue;
        }

        // Находим слоты с тегами Slot или NegatedSlot
        SlotScript[] slots = slotsContainer.GetComponentsInChildren<SlotScript>(true)
            .Where(s => s.gameObject.tag == "Slot" || s.gameObject.tag == "NegatedSlot")
            .OrderBy(s => 
            {
                string slotNumber = s.transform.parent.name.Replace("Slot_", "");
                return int.TryParse(slotNumber, out int num) ? num : int.MaxValue;
            })
            .ToArray();
        InputScript[] inputs = inputsContainer.GetComponentsInChildren<InputScript>(true)
            .OrderBy(i => 
            {
                string inputNumber = i.name.Replace("Input_", "");
                return int.TryParse(inputNumber, out int num) ? num : int.MaxValue;
            })
            .ToArray();

        Debug.Log($"Найдено слотов в {wallName}: {slots.Length} ({string.Join(", ", slots.Select(s => $"{s.transform.parent.name} ({s.gameObject.tag})"))})");
        Debug.Log($"Найдено входов в {wallName}: {inputs.Length} ({string.Join(", ", inputs.Select(i => i.name))})");

        // Создаем словари для быстрого доступа по имени
        var slotDict = slots.ToDictionary(s => s.transform.parent.name, s => s);
        var inputDict = inputs.ToDictionary(i => i.name, i => i);

        var slotConnections = wallConnections[wallName];
        foreach (var slotConnection in slotConnections)
        {
            string slotName = slotConnection.Key;
            string[] sourceNames = slotConnection.Value;

            if (slotDict.ContainsKey(slotName))
            {
                SlotScript slot = slotDict[slotName];
                MonoBehaviour[] sources = new MonoBehaviour[2];

                for (int k = 0; k < 2; k++)
                {
                    string sourceName = sourceNames[k];
                    if (slotDict.ContainsKey(sourceName))
                    {
                        sources[k] = slotDict[sourceName];
                        Debug.Log($"Связь для {slotName}: Источник {k + 1} = {sourceName} (Slot)");
                    }
                    else if (inputDict.ContainsKey(sourceName))
                    {
                        sources[k] = inputDict[sourceName];
                        Debug.Log($"Связь для {slotName}: Источник {k + 1} = {sourceName} (Input)");
                    }
                    else
                    {
                        Debug.LogError($"Источник {sourceName} не найден для {slotName} на {wallName}");
                    }
                }
                slot.inputs = sources;
            }
            else
            {
                Debug.LogWarning($"Слот {slotName} не найден на {wallName}");
            }
        }

        // Связываем Slot_7 с табло
        if (slotDict.ContainsKey("Slot_7"))
        {
            SlotScript slot7 = slotDict["Slot_7"];
            slot7.outputTarget = GameObject.Find($"Tablo/tablo{i + 1}")?.GetComponent<TabloScript>();
            if (slot7.outputTarget == null)
            {
                Debug.LogWarning($"Tablo{i + 1} не найдено для {wallName}");
            }
            else
            {
                Debug.Log($"Slot_7 связан с табло: {slot7.outputTarget.name}");
            }
        }
    }
}

    public GameObject GetGatePrefab(GateScript.GateType type)
    {
        switch (type)
        {
            case GateScript.GateType.AND:
                return andGatePrefab;
            case GateScript.GateType.OR:
                return orGatePrefab;
            default:
                return null;
        }
    }

    public void Interact()
    {
        if (inventoryUI.GetSelectedGateType() != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                SlotScript slot = hit.collider.GetComponent<SlotScript>();
                if (slot != null)
                {
                    slot.PlaceGate(inventoryUI.GetSelectedGateType().Value);
                }
            }
        }
    }
}
