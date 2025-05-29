using System.Collections;
using UnityEngine;

public enum SignType
{
    GreaterThan, // >
    LessThan     // <
}
public class SignScript : MonoBehaviour
{
    public int wallIndex;       // Индекс стены (0-2)
    public int comparisonIndex; // Индекс сравнения (0-2)
    public bool isCorrect = false;
    
    private Renderer signRenderer;
    private MaterialPropertyBlock materialBlock;
    private Room2Manager roomManager;
    private Color defaultColor;
    private Color defaultEmission;

    private void Awake()
    {
        signRenderer = GetComponent<Renderer>();
        materialBlock = new MaterialPropertyBlock();
        signRenderer.GetPropertyBlock(materialBlock);
        
        // Сохраняем исходные цвета
        defaultColor = signRenderer.material.GetColor("_BaseColor");
        defaultEmission = signRenderer.material.GetColor("_EmissionColor");
        
        roomManager = FindObjectOfType<Room2Manager>();
    }

    public void PlaceSign(string signType)
    {
        // Проверяем правильность знака
        bool correct = roomManager.CheckSign(wallIndex, comparisonIndex, signType);
        isCorrect = correct;
        
        // Устанавливаем визуал знака
        if (correct)
        {
            SetSignColor(Color.green);
            Debug.Log($"Знак {signType} установлен правильно!");
        }
        else
        {
            SetSignColor(Color.red);
            Debug.Log($"Знак {signType} установлен неправильно!");
            StartCoroutine(ResetSignAfterDelay());
        }
    }

    private void SetSignColor(Color color)
    {
        signRenderer.GetPropertyBlock(materialBlock);
        materialBlock.SetColor("_BaseColor", color);
        materialBlock.SetColor("_EmissionColor", color * 2.5f);
        signRenderer.SetPropertyBlock(materialBlock);
    }

    private IEnumerator ResetSignAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        
        // Сбрасываем знак
        ResetSignColor();
        isCorrect = false;
        
        // Генерируем новые числа
        roomManager.RegenerateNumbers(wallIndex, comparisonIndex);
    }

    public void ResetSignColor()
    {
        signRenderer.GetPropertyBlock(materialBlock);
        materialBlock.SetColor("_BaseColor", defaultColor);
        materialBlock.SetColor("_EmissionColor", defaultEmission);
        signRenderer.SetPropertyBlock(materialBlock);
    }
}