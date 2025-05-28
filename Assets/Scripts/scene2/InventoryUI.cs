using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Button[] buttons; // Массив кнопок (например, 2 кнопки)
    [SerializeField] private Image[] highlights; // Массив рамок подсветки
    private string selectedItemId; // ID выбранного элемента
    private PlayerInputController _playerInputController;

    private void Awake()
    {
        _playerInputController = FindFirstObjectByType<PlayerInputController>();
        if (_playerInputController != null)
        {
            _playerInputController.OnInventory1Pressed += () => SelectItem(0);
            _playerInputController.OnInventory2Pressed += () => SelectItem(1);
        }
        else
        {
            Debug.LogWarning("PlayerInputController not found!");
        }
    }

    private void Start()
    {
        // Проверяем корректность настроек
        if (buttons.Length != highlights.Length)
        {
            Debug.LogError($"Количество кнопок ({buttons.Length}) не совпадает с количеством подсветок ({highlights.Length})!");
            return;
        }

        // Изначально ничего не выбрано
        for (int i = 0; i < highlights.Length; i++)
        {
            highlights[i].enabled = false;
        }
    }

    // Инициализация инвентаря для комнаты
    public void InitializeInventory(InventoryItem[] items, string roomId)
    {
        if (items.Length > buttons.Length)
        {
            Debug.LogError($"Слишком много элементов ({items.Length}) для кнопок ({buttons.Length}) в комнате {roomId}!");
            return;
        }

        // Очищаем старые обработчики
        foreach (Button button in buttons)
        {
            button.onClick.RemoveAllListeners();
        }

        // Настраиваем кнопки
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < items.Length)
            {
                InventoryItem item = items[i];
                // Устанавливаем спрайт
                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage != null && item.buttonSprite != null)
                {
                    buttonImage.sprite = item.buttonSprite;
                }
                else
                {
                    Debug.LogWarning($"Спрайт не установлен для кнопки {i} в комнате {roomId}");
                }

                // Активируем кнопку
                buttons[i].gameObject.SetActive(true);

                // Привязываем обработчик клика
                int index = i; // Для замыкания
                buttons[i].onClick.AddListener(() => SelectItem(index));
            }
            else
            {
                // Скрываем лишние кнопки
                buttons[i].gameObject.SetActive(false);
                highlights[i].enabled = false;
            }
        }

        // Сбрасываем выбор
        selectedItemId = null;
        for (int i = 0; i < highlights.Length; i++)
        {
            highlights[i].enabled = false;
        }

        Debug.Log($"Инвентарь инициализирован для комнаты {roomId} с {items.Length} элементами");
    }

    private void SelectItem(int index)
    {
        if (index < 0 || index >= buttons.Length || !buttons[index].gameObject.activeSelf)
        {
            Debug.LogWarning($"Недопустимый индекс {index} или кнопка неактивна!");
            return;
        }

        // Получаем itemId из RoomManager
        InventoryItem[] items = RoomManager.instance.GetInventoryItems();
        if (index < items.Length)
        {
            selectedItemId = items[index].itemId;
            Debug.Log($"Выбран элемент: {selectedItemId}");

            // Обновляем подсветку
            for (int i = 0; i < highlights.Length; i++)
            {
                highlights[i].enabled = (i == index);
            }
        }
    }

    public string GetSelectedItemId()
    {
        return selectedItemId;
    }
}