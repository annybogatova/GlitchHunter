using UnityEngine;

public enum SignType
{
    GreaterThan, // >
    LessThan     // <
}
public class SignScript : MonoBehaviour
{
    public SignType? currentSign; // Текущий знак (null, если не размещён)
    [SerializeField] private Renderer signRenderer; // Renderer для знака
    private Material signMaterial; // Материал знака

    // Цвета
    private static readonly Color DefaultColor = Color.white;
    private static readonly Color CorrectColor = Color.green;
    private static readonly Color IncorrectColor = Color.red;

    private void Awake()
    {
        if (signRenderer == null)
        {
            signRenderer = GetComponent<Renderer>();
            if (signRenderer == null)
            {
                Debug.LogError($"Renderer не найден на {gameObject.name}!");
                return;
            }
        }
        signMaterial = signRenderer.material; // Получаем материал
    }

    public void PlaceSign(SignType signType)
    {
        currentSign = signType;
        signMaterial.color = DefaultColor; // Сбрасываем цвет
        Debug.Log($"Знак {signType} размещён на {gameObject.name}");
    }

    public void SetCorrect()
    {
        signMaterial.color = CorrectColor;
    }

    public void SetIncorrect()
    {
        signMaterial.color = IncorrectColor;
    }

    public void ResetSign()
    {
        currentSign = null;
        signMaterial.color = DefaultColor;
    }
}
