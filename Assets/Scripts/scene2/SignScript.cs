using System.Collections;
using UnityEngine;

public enum SignType
{
    GreaterThan,
    LessThan
}

public class SignScript : MonoBehaviour
{
    public int wallIndex;       // 0-2
    public int comparisonIndex; // 0-2
    public bool isCorrect { get; private set; }

    private Room2Manager roomManager;
    private GameObject currentSign; // Текущий префаб знака
    private MaterialPropertyBlock materialBlock;

    private void Awake()
    {
        roomManager = FindObjectOfType<Room2Manager>();
        materialBlock = new MaterialPropertyBlock();
    }

    public void PlaceSign(string signType)
    {
        // Отключаем Sign_X_Y
        gameObject.SetActive(false);

        // Создаём префаб
        GameObject prefab = signType == "GreaterThan" ? roomManager.greaterThanPrefab : roomManager.lessThanPrefab;
        if (prefab == null)
        {
            Debug.LogError($"Префаб для {signType} не назначен!");
            gameObject.SetActive(true);
            return;
        }

        currentSign = Instantiate(prefab, transform.position, Quaternion.identity, transform.parent);
        // Проверяем правильность
        bool correct = roomManager.CheckSign(wallIndex, comparisonIndex, signType);
        isCorrect = correct;

        // Находим материал text
        Renderer signRenderer = currentSign.GetComponent<Renderer>();
        if (signRenderer == null)
        {
            Debug.LogError($"Renderer не найден на префабе {signType}!");
            gameObject.SetActive(true);
            Destroy(currentSign);
            return;
        }

        // Ищем материал с именем "text"
        Material[] materials = signRenderer.materials;
        int textMaterialIndex = -1;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].name.ToLower().Contains("text"))
            {
                textMaterialIndex = i;
                break;
            }
        }

        if (textMaterialIndex == -1)
        {
            Debug.LogError($"Материал 'text' не найден в префабе {signType}!");
            gameObject.SetActive(true);
            Destroy(currentSign);
            return;
        }

        // Применяем цвет к материалу text
        signRenderer.GetPropertyBlock(materialBlock, textMaterialIndex);
        materialBlock.SetColor("_BaseColor", correct ? Color.green : Color.red);
        signRenderer.SetPropertyBlock(materialBlock, textMaterialIndex);

        Debug.Log($"Знак {signType} установлен {(correct ? "правильно" : "неправильно")}");

        if (!correct)
        {
            roomManager.ResetSignAfterDelay(wallIndex, comparisonIndex);
        }
    }

    public void ResetSign()
    {
        // Удаляем префаб
        if (currentSign != null)
        {
            Destroy(currentSign);
            currentSign = null;
        }

        // Включаем Sign_X_Y
        gameObject.SetActive(true);
        isCorrect = false;
    }
}