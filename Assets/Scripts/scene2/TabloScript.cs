using UnityEngine;

public class TabloScript : MonoBehaviour
{
    public GameObject tabloCellZero;
    public GameObject tabloCellOne;
    private GameObject currentCell;
    
    private bool? value = null; // Текущее значение (null, если не установлено)
    
    public event System.Action<bool> OnValueChanged;

    // Устанавливает значение табло и обновляет визуализацию
    public void SetValue(bool newValue)
    {
        if (value == newValue) return;
        
        value = newValue;
        OnValueChanged?.Invoke(newValue);
        
        value = newValue; // Сохраняем значение
        if (currentCell != null)
        {
            Destroy(currentCell);
        }
        GameObject prefab = newValue ? tabloCellOne : tabloCellZero;
        if (prefab != null)
        {
            currentCell = Instantiate(prefab, transform.position, transform.rotation, transform);
            currentCell.name = newValue ? "Cell_1" : "Cell_0";
            Debug.Log($"{gameObject.name} установлено значение: {(newValue ? 1 : 0)}");
        }
        else
        {
            Debug.LogError($"Tablo cell prefab не назначен для {(newValue ? "1" : "0")}");
        }
    }
    
    // Проверяет, есть ли значение
    public bool HasValue()
    {
        return value.HasValue;
    }

    // Возвращает текущее значение
    public bool GetValue()
    {
        return value.GetValueOrDefault();
    }
    
}
