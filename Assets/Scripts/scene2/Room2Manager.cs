using UnityEngine;

public class Room2Manager : MonoBehaviour, IRoomManager
{
    public static Room2Manager instance;
    public GameObject zeroPrefab;
    public GameObject onePrefab;
    public GameObject roomContainer;
    public InventoryUI inventoryUI;
    
    // UI поля
    public GameObject inventoryPanel;
    public TMPro.TextMeshProUGUI goalText;
    public GameObject rulesPanel;
    public TMPro.TextMeshProUGUI rulesText;
    public GameObject successPanel;
    public TMPro.TextMeshProUGUI successText;
    public GameObject goalPanel;

    // Элементы инвентаря
    [SerializeField] private InventoryItem[] inventoryItems; // "GreaterThan", "LessThan"

    private PlayerInputController _playerInputController;
    private bool[,,] tabloNumbers; // 3 стены x 6 табло x 8 бит
    private SignScript[,] signs; // 3 стены x 3 знака
    private bool[] signCorrect; // Статус каждого знака (9 знаков)
    public string roomId { get => "Room_2"; }
    public bool isRoomCompleted = false;
    
    private void Awake()
    {
        instance = this;
        _playerInputController = FindFirstObjectByType<PlayerInputController>();
        if (_playerInputController != null)
        {
            //_playerInputController.OnInteractPressed += () => Interact();
        }
        else
        {
            Debug.LogWarning("No PlayerInputController found!");
        }

        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (rulesPanel != null) rulesPanel.SetActive(false);
        if (successPanel != null) successPanel.SetActive(false);
        if (goalPanel != null) goalPanel.SetActive(false);
        else if (goalText != null) goalText.gameObject.SetActive(false);
    }

    public void InitializeRoom()
    {
        Debug.Log("InitializeRoom Room_2");
        if (LevelManager.instance.IsRoomCompleted(roomId))
        {
            isRoomCompleted = true;
            Debug.Log($"Комната {roomId} уже завершена, пропускаем инициализацию");
            return;
        }

        InitializeTablosAndSigns();
        GenerateNumbers();
        InitializeUI();
        
        if (inventoryUI != null && inventoryItems != null)
        {
            inventoryUI.InitializeInventory(inventoryItems, roomId);
            Debug.Log($"InventoryUI инициализирован для комнаты {roomId}");
        }
    }
    
    public InventoryItem[] GetInventoryItems()
    {
        return inventoryItems;
    }

    private void InitializeTablosAndSigns()
    {
        tabloNumbers = new bool[3, 6, 8]; // 3 стены, 6 табло, 8 бит
        signs = new SignScript[3, 3]; // 3 стены, 3 знака
        signCorrect = new bool[9]; // 9 знаков

        for (int wall = 0; wall < 3; wall++)
        {
            string wallName = $"Wall_{wall + 1}";
            Transform wallTransform = roomContainer.transform.Find(wallName);
            if (wallTransform == null)
            {
                Debug.LogError($"Wall {wallName} not found in roomContainer!");
                continue;
            }

            // Проверяем табло
            for (int tablo = 0; tablo < 6; tablo++)
            {
                string tabloName = $"Tablo_{wall + 1}_{(tablo + 1)}";
                string comparisonPath = $"Comparison_{(tablo / 2 + 1)}/{tabloName}";
                Transform tabloTransform = wallTransform.Find(comparisonPath);
                if (tabloTransform == null)
                {
                    Debug.LogError($"Tablo {tabloName} not found at path {comparisonPath}!");
                    continue;
                }
            }

            // Настраиваем знаки
            for (int sign = 0; sign < 3; sign++)
            {
                string signName = $"Sign_{wall + 1}_{(sign + 1)}";
                string comparisonPath = $"Comparison_{(sign + 1)}/{signName}";
                Transform signTransform = wallTransform.Find(comparisonPath);
                if (signTransform == null)
                {
                    Debug.LogError($"Sign {signName} not found at path {comparisonPath}!");
                    continue;
                }

                signs[wall, sign] = signTransform.GetComponent<SignScript>();
                if (signs[wall, sign] == null)
                {
                    signs[wall, sign] = signTransform.gameObject.AddComponent<SignScript>();
                    Debug.Log($"Added SignScript to {signName}");
                }
            }
        }
    }

    private void GenerateNumbers()
    {
        tabloNumbers = new bool[3, 6, 8]; // Важно: именно такой порядок размерностей

        for (int wall = 0; wall < 3; wall++)
        {
            for (int tablo = 0; tablo < 6; tablo++)
            {
                // Генерируем 8-битное число
                for (int bit = 0; bit < 8; bit++)
                {
                    tabloNumbers[wall, tablo, bit] = Random.value > 0.5f;
                }

                // Находим табло
                string tabloName = $"Tablo_{wall + 1}_{(tablo + 1)}";
                string comparisonPath = $"Wall_{wall + 1}/Comparison_{(tablo / 2 + 1)}/{tabloName}";
                Transform tabloTransform = roomContainer.transform.Find(comparisonPath);
                if (tabloTransform == null)
                {
                    Debug.LogError($"Tablo {tabloName} not found at path {comparisonPath}!");
                    continue;
                }

                // Заменяем ячейки
                for (int bit = 0; bit < 8; bit++)
                {
                    string cellName = $"cell_{wall + 1}_{(tablo + 1)}_{bit + 1}";
                    Transform cellTransform = tabloTransform.Find(cellName);
                    if (cellTransform == null)
                    {
                        Debug.LogWarning($"cell {cellName} not found in {tabloName}!");
                        continue;
                    }

                    GameObject prefab = tabloNumbers[wall, tablo, bit] ? onePrefab : zeroPrefab;
                    if (prefab != null)
                    {
                        GameObject instance = Instantiate(prefab, cellTransform.position, cellTransform.rotation, tabloTransform);
                        instance.name = tabloNumbers[wall, tablo, bit] ? "One" : "Zero";
                        Destroy(cellTransform.gameObject); // Удаляем ячейку
                    }
                    else
                    {
                        Debug.LogWarning($"Prefab for bit {bit} (value: {tabloNumbers[wall, tablo, bit]}) is null!");
                    }
                }
            }
        }
    }
    
    public bool CheckSign(int wallIndex, int comparisonIndex, string signType)
    {
        // Получаем индексы табло для сравнения
        int leftTabloIndex = comparisonIndex * 2;
        int rightTabloIndex = comparisonIndex * 2 + 1;

        // Конвертируем в числа
        byte leftNumber = ConvertToByte(wallIndex, leftTabloIndex);
        byte rightNumber = ConvertToByte(wallIndex, rightTabloIndex);

        // Проверяем правильность знака
        if (signType == "GreaterThan")
        {
            return leftNumber > rightNumber;
        }
        else if (signType == "LessThan")
        {
            return leftNumber < rightNumber;
        }
    
        Debug.LogError($"Неизвестный тип знака: {signType}");
        return false;
    }
    
    private byte ConvertToByte(int wallIndex, int tabloIndex)
    {
        if (wallIndex < 0 || wallIndex >= 3 || 
            tabloIndex < 0 || tabloIndex >= 6)
        {
            Debug.LogError($"Неверные индексы: wall={wallIndex}, tablo={tabloIndex}");
            return 0;
        }
    
        byte result = 0;
        for (int bit = 0; bit < 8; bit++)
        {
            if (tabloNumbers[wallIndex, tabloIndex, bit])
            {
                result |= (byte)(1 << (7 - bit));
            }
        }
        return result;
    }
    public void RegenerateNumbers(int wallIndex, int comparisonIndex)
    {
        int leftTabloIndex = comparisonIndex * 2;
        int rightTabloIndex = comparisonIndex * 2 + 1;
        
        // Генерируем новые числа
        for (int bit = 0; bit < 8; bit++)
        {
            tabloNumbers[wallIndex, leftTabloIndex, bit] = Random.value > 0.5f;
            tabloNumbers[wallIndex, rightTabloIndex, bit] = Random.value > 0.5f;
        }
        
        // Обновляем визуал
        UpdateTabloVisual(wallIndex, leftTabloIndex);
        UpdateTabloVisual(wallIndex, rightTabloIndex);
        SignScript sign = GetSign(wallIndex, comparisonIndex);
        if (sign != null) sign.ResetSignColor();
    }

    private void UpdateTabloVisual(int wallIndex, int tabloIndex)
    {
        string tabloName = $"Tablo_{wallIndex + 1}_{tabloIndex + 1}";
        string comparisonPath = $"Wall_{wallIndex + 1}/Comparison_{(tabloIndex / 2 + 1)}/{tabloName}";
        Transform tabloTransform = roomContainer.transform.Find(comparisonPath);
        
        if (tabloTransform == null) return;
        
        // Удаляем старые модели
        foreach (Transform child in tabloTransform)
        {
            if (child.name == "One" || child.name == "Zero")
            {
                Destroy(child.gameObject);
            }
        }
        
        // Создаем новые модели
        for (int bit = 0; bit < 8; bit++)
        {
            string cellName = $"cell_{wallIndex + 1}_{tabloIndex + 1}_{bit + 1}";
            Transform cellTransform = tabloTransform.Find(cellName);
            
            if (cellTransform != null)
            {
                GameObject prefab = tabloNumbers[wallIndex, tabloIndex, bit] ? 
                    onePrefab : zeroPrefab;
                
                if (prefab != null)
                {
                    Instantiate(prefab, cellTransform.position, 
                        cellTransform.rotation, tabloTransform);
                }
            }
        }
    }
    
    private SignScript GetSign(int wallIndex, int comparisonIndex)
    {
        if (signs == null || wallIndex < 0 || wallIndex >= signs.GetLength(0) || 
            comparisonIndex < 0 || comparisonIndex >= signs.GetLength(1))
        {
            return null;
        }
        return signs[wallIndex, comparisonIndex];
    }

    private void InitializeUI()
    {
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(true);
            if (rulesText != null)
            {
                rulesText.text = "Правила:\n" +
                                 "1. Сравните числа на табло.\n" +
                                 "2. Разместите знаки '>' или '<' между парами чисел.\n" +
                                 "3. Правильный знак станет зелёным, неправильный — красным.\n" +
                                 "4. При ошибке числа обновятся через 2 секунды.";
            }
            Debug.Log("RulesPanel активирован");
        }

        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (goalPanel != null) goalPanel.SetActive(false);
        else if (goalText != null) goalText.gameObject.SetActive(false);
        if (successPanel != null) successPanel.SetActive(false);
    }

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
        if (goalPanel != null)
        {
            goalPanel.SetActive(true);
            Debug.Log("GoalPanel активирован");
        }
        else if (goalText != null)
        {
            goalText.gameObject.SetActive(true);
            Debug.Log("GoalText активирован");
        }
    }
    
    public void Interact()
    {
        if (isRoomCompleted) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            SignScript sign = hit.collider.GetComponent<SignScript>();
            if (sign != null && !sign.isCorrect)
            {
                string selectedItemId = inventoryUI.GetSelectedItemId();
                
                if (selectedItemId == "GreaterThan" || selectedItemId == "LessThan")
                {
                    sign.PlaceSign(selectedItemId);
                }
            }
        }
    }
}
