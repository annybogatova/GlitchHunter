using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public Button andButton;
    public Button orButton;
    public Image andHighlight; // Рамка подсветки AND
    public Image orHighlight; // Рамка подсветки OR
    
    private GateScript.GateType? selectedGateType = null;
    private PlayerInputController _playerInputController;
    
    private void Awake()
    {
        _playerInputController = FindObjectOfType<PlayerInputController>();

        if (_playerInputController != null)
        {
            _playerInputController.OnInventory1Pressed += () => SelectGate(GateScript.GateType.AND);
            _playerInputController.OnInventory2Pressed += () => SelectGate(GateScript.GateType.OR);
        }
    }
    void Start()
    {
        // Подписываемся на клики
        andButton.onClick.AddListener(() => SelectGate(GateScript.GateType.AND));
        orButton.onClick.AddListener(() => SelectGate(GateScript.GateType.OR));
    }

    // Update is called once per frame
    private void SelectGate(GateScript.GateType type)
    {
        selectedGateType = type;
        Debug.Log(selectedGateType);
        // Подсвечиваем выбранную ячейку
        // andHighlight.enabled = (type == GateScript.GateType.AND);
        // orHighlight.enabled = (type == GateScript.GateType.OR);
    }
    public GateScript.GateType? GetSelectedGateType()
    {
        return selectedGateType;
    }
    
    
    
}
