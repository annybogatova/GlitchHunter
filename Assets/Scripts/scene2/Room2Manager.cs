using System.Collections;
using System.Linq;
using UnityEngine;

public class Room2Manager : MonoBehaviour, IRoomManager
{
    public static Room2Manager instance;
    public GameObject zeroPrefab;
    public GameObject onePrefab;
    public GameObject greaterThanPrefab; // Префаб для >
    public GameObject lessThanPrefab;   // Префаб для <
    public GameObject roomContainer;
    public InventoryUI inventoryUI;
    
    // UI
    public GameObject inventoryPanel;
    public TMPro.TextMeshProUGUI goalText;
    public GameObject rulesPanel;
    public TMPro.TextMeshProUGUI rulesText;
    public GameObject successPanel;
    public TMPro.TextMeshProUGUI successText;
    public GameObject goalPanel;

    [SerializeField] private InventoryItem[] inventoryItems; // GreaterThan, LessThan

    private PlayerInputController _playerInputController;
    private bool[,,] tabloNumbers; // 3×6×8
    private SignScript[,] signs; // 3×3
    private bool[] signCorrect; // 9
    public string roomId { get => "Room_2"; }
    public bool isRoomCompleted = false;

    private void Awake()
    {
        instance = this;
        _playerInputController = FindObjectOfType<PlayerInputController>();
        if (_playerInputController != null)
        {
            _playerInputController.OnInteractPressed += () => Interact();
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
            Debug.Log($"Room {roomId} уже завершена, пропускаем инициализацию");
            return;
        }

        InitializeTablosAndSigns();
        GenerateNumbers();
        InitializeUI();
        
        if (inventoryUI != null && inventoryItems != null)
        {
            inventoryUI.InitializeInventory(inventoryItems, roomId);
            Debug.Log($"InventoryUI initialized for room {roomId}");
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

    private void InitializeTablosAndSigns()
    {
        tabloNumbers = new bool[3, 6, 8];
        signs = new SignScript[3, 3];
        signCorrect = new bool[9];

        for (int wall = 0; wall < 3; wall++)
        {
            string wallName = $"Wall_{wall + 1}";
            Transform wallTransform = roomContainer.transform.Find(wallName);
            if (wallTransform == null)
            {
                Debug.LogError($"Wall {wallName} not found!");
                continue;
            }

            for (int comparison = 0; comparison < 3; comparison++)
            {
                string comparisonPath = $"Comparison_{comparison + 1}";
                Transform comparisonTransform = wallTransform.Find(comparisonPath);
                if (comparisonTransform == null)
                {
                    Debug.LogError($"Comparison {comparisonPath} not found in {wallName}!");
                    continue;
                }

                string signName = $"Sign_{wall + 1}_{comparison + 1}";
                Transform signTransform = comparisonTransform.Find(signName);
                if (signTransform == null)
                {
                    Debug.LogError($"Sign {signName} not found!");
                    continue;
                }
                signs[wall, comparison] = signTransform.GetComponent<SignScript>();
                if (signs[wall, comparison] == null)
                {
                    signs[wall, comparison] = signTransform.gameObject.AddComponent<SignScript>();
                    signs[wall, comparison].wallIndex = wall;
                    signs[wall, comparison].comparisonIndex = comparison;
                }
            }
        }
    }

    private void GenerateNumbers()
    {
        for (int wall = 0; wall < 3; wall++)
        {
            for (int tablo = 0; tablo < 6; tablo++)
            {
                for (int bit = 0; bit < 8; bit++)
                {
                    tabloNumbers[wall, tablo, bit] = Random.value > 0.5f;
                }
                UpdateTabloVisual(wall, tablo);
            }
        }
    }

    private void UpdateTabloVisual(int wallIndex, int tabloIndex)
    {
        string tabloName = $"Tablo_{wallIndex + 1}_{tabloIndex + 1}";
        string comparisonPath = $"Wall_{wallIndex + 1}/Comparison_{(tabloIndex / 2 + 1)}/{tabloName}";
        Transform tabloTransform = roomContainer.transform.Find(comparisonPath);
        if (tabloTransform == null)
        {
            Debug.LogError($"Tablo {tabloName} not found at {comparisonPath}!");
            return;
        }

        for (int bit = 0; bit < 8; bit++)
        {
            string cellName = $"cell_{wallIndex + 1}_{tabloIndex + 1}_{bit + 1}";
            Transform cellTransform = tabloTransform.Find(cellName);
            if (cellTransform == null)
            {
                Debug.LogWarning($"Cell {cellName} not found in {tabloName}!");
                continue;
            }

            // Удаляем старые префабы
            foreach (Transform child in cellTransform)
            {
                if (child.name == "One" || child.name == "Zero")
                {
                    Destroy(child.gameObject);
                }
            }

            GameObject prefab = tabloNumbers[wallIndex, tabloIndex, bit] ? onePrefab : zeroPrefab;
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, cellTransform.position, cellTransform.rotation, cellTransform);
                instance.name = tabloNumbers[wallIndex, tabloIndex, bit] ? "One" : "Zero";
            }
        }
    }

    public bool CheckSign(int wallIndex, int comparisonIndex, string signType)
    {
        int leftTabloIndex = comparisonIndex * 2;
        int rightTabloIndex = comparisonIndex * 2 + 1;

        byte leftNumber = ConvertToByte(wallIndex, leftTabloIndex);
        byte rightNumber = ConvertToByte(wallIndex, rightTabloIndex);

        bool correct = signType == "GreaterThan" ? leftNumber > rightNumber : leftNumber < rightNumber;
        signCorrect[wallIndex * 3 + comparisonIndex] = correct;

        if (correct)
        {
            CheckRoomCompletion();
        }

        return correct;
    }

    private byte ConvertToByte(int wallIndex, int tabloIndex)
    {
        if (wallIndex < 0 || wallIndex >= 3 || tabloIndex < 0 || tabloIndex >= 6)
        {
            Debug.LogError($"Invalid indices: wall={wallIndex}, tablo={tabloIndex}");
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
    public void ResetSignAfterDelay(int wallIndex, int comparisonIndex)
    {
        StartCoroutine(ResetSignCoroutine(wallIndex, comparisonIndex));
    }

    private IEnumerator ResetSignCoroutine(int wallIndex, int comparisonIndex)
    {
        yield return new WaitForSeconds(2f);

        // Сбрасываем знак
        if (signs[wallIndex, comparisonIndex] != null)
        {
            signs[wallIndex, comparisonIndex].ResetSign();
            signCorrect[wallIndex * 3 + comparisonIndex] = false;
        }

        // Перегенерируем числа
        RegenerateNumbers(wallIndex, comparisonIndex);
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
    }

    private void CheckRoomCompletion()
    {
        if (signCorrect.All(correct => correct))
        {
            isRoomCompleted = true;
            LevelManager.instance.CompleteRoom(roomId);
            successPanel?.SetActive(true);
            Debug.Log($"Комната {roomId} завершена!");
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

    public void CloseRulesPanel()
    {
        rulesPanel?.SetActive(false);
        inventoryPanel?.SetActive(true);
        goalPanel?.SetActive(true);
        Debug.Log("RulesPanel закрыт, InventoryPanel и GoalPanel активны");
    }

    public void ShowRulesPanel()
    {
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(true);
            rulesText.text = "Правила:\n" +
                             "1. Сравните числа на табло.\n" +
                             "2. Разместите знаки '>' или '<'.\n" +
                             "3. Правильный знак — зелёный, неправильный — красный.\n" +
                             "4. При ошибке числа обновятся через 2 секунды.";
            Debug.Log("RulesPanel открыт");
        }
    }
}
