using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomManager : MonoBehaviour, IRoomManager
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
    
    // UI поля
    public GameObject inventoryPanel; // Панель инвентаря
    public TMPro.TextMeshProUGUI goalText; // Текст цели
    public GameObject rulesPanel; // Панель с правилами
    public TMPro.TextMeshProUGUI rulesText; // Текст правил
    public GameObject successPanel; // Панель успеха
    public TMPro.TextMeshProUGUI successText; // Текст успеха
    
    // Элементы инвентаря для этой комнаты
    [SerializeField] private InventoryItem[] inventoryItems; // Настраивается в инспекторе
    
    private PlayerInputController _playerInputController;
    private Dictionary<string, Dictionary<string, string[]>> wallConnections;

    private bool[] targetNumber = new bool[3]; // Целевое 3-битное число (например, [true, true, false] = 110)
    private TabloScript[] tablos; // Табло (tablo1, tablo2, tablo3)
    
    public string roomId { get => "Room_1"; } // Уникальный идентификатор комнаты
    public bool isRoomCompleted = false;
    
    private void Awake()
    {
        instance = this;
        _playerInputController = FindFirstObjectByType<PlayerInputController>();

        if (_playerInputController != null)
        {
            _playerInputController.OnInteractPressed += () => Interact();
        }
        else
        {
            Debug.LogWarning("No player input controller found");
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
                //Debug.Log($"JSON успешно распарсен. Найдено стен: {wallConnections.Keys.Count} ({string.Join(", ", wallConnections.Keys)})");
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
        
        // Инициализация UI
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(false);
        }
        if (successPanel != null)
        {
            successPanel.SetActive(false);
        }
        
        // Инициализируем инвентарь
        if (inventoryUI != null && inventoryItems != null)
        {
            inventoryUI.InitializeInventory(inventoryItems, roomId);
        }
        else
        {
            Debug.LogWarning("InventoryUI или inventoryItems не назначены!");
        }
    }
    
    public InventoryItem[] GetInventoryItems()
    {
        return inventoryItems;
    }

    public void InitializeRoom()
    { 
        Debug.Log("InitializeRoom");
        // Проверяем, завершена ли комната
        if (LevelManager.instance.IsRoomCompleted(roomId))
        {
            isRoomCompleted = true;
            Debug.Log($"Комната {roomId} уже завершена, пропускаем инициализацию");
            return;
        }

        if (isRoomCompleted)
        {
            LevelManager.instance.CompleteRoom(roomId);
            return;
        }
        SetupSlotsAndPipes();
        GenerateInputs();
        AssignSlotConnections();
        InitializeGoal();
        InitializeUI();
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
        
        // Инициализируем массив tablos
        tablos = new TabloScript[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject tabloObj = GameObject.Find($"Tablo/tablo{i + 1}");
            if (tabloObj != null)
            {
                tablos[i] = tabloObj.GetComponent<TabloScript>();
                if (tablos[i] == null)
                {
                    Debug.LogError($"TabloScript не найден на объекте Tablo/tablo{i + 1}");
                }
            }
            else
            {
                Debug.LogError($"Объект Tablo/tablo{i + 1} не найден в иерархии!");
            }
        }

        // Получаем все трансформы
        Transform[] walls = roomContainer.GetComponentsInChildren<Transform>(true);
        //Debug.Log($"Все трансформы: {string.Join(", ", walls.Select(t => t.name))}");

        // Фильтруем только прямые дочерние объекты roomContainer с корректными именами
        walls = walls.Where(t => t.parent == roomContainer.transform && t.name.StartsWith("wall_")).ToArray();
        //Debug.Log($"Отфильтрованные стены: {string.Join(", ", walls.Select(t => t.name))}");

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

        //Debug.Log($"Найдено стен: {walls.Length} ({string.Join(", ", walls.Select(w => w.name))})");

        for (int i = 0; i < walls.Length; i++)
        {
            Transform wall = walls[i];
            string wallName = wall.name;
            //Debug.Log($"Обрабатываем стену: {wallName}");

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

            //Debug.Log($"Найдено слотов в {wallName}: {slots.Length} ({string.Join(", ", slots.Select(s => $"{s.transform.parent.name} ({s.gameObject.tag})"))})");
            //Debug.Log($"Найдено входов в {wallName}: {inputs.Length} ({string.Join(", ", inputs.Select(i => i.name))})");

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
                            //Debug.Log($"Связь для {slotName}: Источник {k + 1} = {sourceName} (Slot)");
                        }
                        else if (inputDict.ContainsKey(sourceName))
                        {
                            sources[k] = inputDict[sourceName];
                            //Debug.Log($"Связь для {slotName}: Источник {k + 1} = {sourceName} (Input)");
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
                if (i < tablos.Length && tablos[i] != null)
                {
                    slot7.outputTarget = tablos[i];
                    //Debug.Log($"Slot_7 на {wallName} связан с табло: tablo{i + 1}");
                }
                else
                {
                    Debug.LogError($"Tablo{i + 1} не инициализировано для Slot_7 на {wallName}");
                }
            }
        }
    }

    private void InitializeGoal()
    {
        Debug.Log("InitializeGoal");
        // Генерируем случайное 3-битное число
        for (int i = 0; i < 3; i++)
        {
            targetNumber[i] = Random.value > 0.5f;
        }
    
        // Отображаем цель в UI (tablo3, tablo2, tablo1)
        if (goalText != null)
        {
            goalText.text = $"Цель: {(targetNumber[2] ? "1" : "0")}{(targetNumber[1] ? "1" : "0")}{(targetNumber[0] ? "1" : "0")}";
        }
        else
        {
            Debug.LogWarning("goalText не назначен в RoomManager!");
        }
    
        // Скрываем панель успеха
        if (successPanel != null)
        {
            successPanel.SetActive(false);
        }
    }
    
    // метод для проверки результата
    public void CheckGoal()
    {
        if (tablos == null || tablos.Length != 3)
        {
            Debug.LogError("Массив tablos не инициализирован или имеет неверную длину!");
            return;
        }

        bool allTablosReady = true;
        bool[] currentNumber = new bool[3];
        for (int i = 0; i < 3; i++)
        {
            if (tablos[i] == null)
            {
                Debug.LogError($"tablos[{i}] (tablo{i + 1}) is null!");
                allTablosReady = false;
                break;
            }
            if (!tablos[i].HasValue())
            {
                Debug.Log($"tablos[{i}] (tablo{i + 1}) has no value yet.");
                allTablosReady = false;
                break;
            }
            currentNumber[i] = tablos[i].GetValue();
            Debug.Log($"tablos[{i}] (tablo{i + 1}) value: {(currentNumber[i] ? 1 : 0)}");
        }

        if (allTablosReady)
        {
            bool success = true;
            for (int i = 0; i < 3; i++)
            {
                if (currentNumber[i] != targetNumber[i])
                {
                    success = false;
                    break;
                }
            }
            Debug.Log($"Current: {(currentNumber[2] ? 1 : 0)}{(currentNumber[1] ? 1 : 0)}{(currentNumber[0] ? 1 : 0)}, Target: {(targetNumber[2] ? 1 : 0)}{(targetNumber[1] ? 1 : 0)}{(targetNumber[0] ? 1 : 0)}");
            if (success)
            {
                isRoomCompleted = true;
                LevelManager.instance.CompleteRoom(roomId);
                if (successPanel != null)
                {
                    successPanel.SetActive(true);
                    if (successText != null)
                    {
                        successText.text = "Задание выполнено!\nПройдите к следующей комнате.";
                    }
                }
                Debug.Log($"Цель достигнута в комнате {roomId}!");
            }
            else
            {
                Debug.Log("Цель не достигнута.");
            }
        }
    }

    private void InitializeUI()
    {
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(true);
            if (rulesText != null)
            {
                rulesText.text = "Правила:\n" +
                                 "1. Разместите вентили (AND или OR) на всех слотах.\n" +
                                 "2. Каждое табло над дверью должно показать 0 или 1.\n" +
                                 "3. Ваша цель — собрать число, указанное вверху экрана.";
            }
            //Debug.Log("RulesPanel активирован");
        }
        else
        {
            Debug.LogWarning("rulesPanel не назначен в RoomManager!");
        }

        if (successPanel != null)
        {
            successPanel.SetActive(false);
        }
    }
    
    // Показ RulesPanel (для кнопки со знаком вопроса)
    public void ShowRulesPanel()
    {
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(true);
            if (rulesText != null)
            {
                rulesText.text = "Правила:\n" +
                                 "1. Разместите вентили (AND или OR) всех на слотах.\n" +
                                 "2. Каждое табло над дверью должно показать 0 или 1.\n" +
                                 "3. Ваша цель — собрать число, указанное вверху экрана.\n";
            }
            //Debug.Log("RulesPanel открыт через кнопку");
        }
        else
        {
            Debug.LogWarning("rulesPanel не назначен в RoomManager!");
        }
    }
    
    // Закрытие RulesPanel
    public void CloseRulesPanel()
    {
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(false);
            Debug.Log("RulesPanel закрыт");
        }
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            Debug.Log("InventoryPanel активирован");
        }
        else
        {
            Debug.LogWarning("inventoryPanel не назначен в RoomManager!");
        }
    }

    // Закрытие SuccessPanel (и, возможно, перехода)
    public void CloseSuccessPanel()
    {
        if (successPanel != null)
        {
            successPanel.SetActive(false);
            Debug.Log("SuccessPanel закрыт");
            
            inventoryPanel.SetActive(false);
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
        if (isRoomCompleted)
        {
            Debug.Log("Комната завершена, взаимодействие с слотами заблокировано");
            return;
        }

        if (inventoryUI == null || string.IsNullOrEmpty(inventoryUI.GetSelectedItemId()))
        {
            Debug.LogWarning("InventoryUI is null or no item selected");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            SlotScript slot = hit.collider.GetComponent<SlotScript>();
            if (slot != null)
            {
                string selectedId = inventoryUI.GetSelectedItemId();
                // Преобразуем itemId в GateType (для обратной совместимости с Room_1)
                GateScript.GateType? gateType = null;
                if (selectedId == "AND") gateType = GateScript.GateType.AND;
                else if (selectedId == "OR") gateType = GateScript.GateType.OR;

                if (gateType.HasValue)
                {
                    slot.PlaceGate(gateType.Value);
                    Debug.Log($"Вентиль {selectedId} размещён в {slot.transform.parent.name}");
                    CheckGoal();
                }
                else
                {
                    Debug.LogError($"Неизвестный тип элемента: {selectedId}");
                }
            }
            else
            {
                Debug.Log("Raycast hit object without SlotScript");
            }
        }
        else
        {
            Debug.Log("Raycast did not hit any object");
        }
    }
}
