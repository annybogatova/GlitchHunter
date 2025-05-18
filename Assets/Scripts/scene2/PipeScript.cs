using UnityEngine;

public class PipeScript : MonoBehaviour
{
    private MonoBehaviour _source;
    private Renderer _renderer;
    private MaterialPropertyBlock _material;
    private Color _color;
    private bool _lastOutput;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("PipeScript start");
        
        _source = transform.parent.GetComponentInChildren<SlotScript>();
        if (_source == null)
        {
            _source = transform.parent.GetComponentInChildren<InputScript>();
        }
        Debug.Log("slot founded");
        if (_source == null)
        {
            Debug.LogError("Ни SlotScript, ни InputScript не найдены в родительском объекте " + transform.parent.name);
            return;
        }
        
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError("Renderer не найден на объекте " + gameObject.name);
            return;
        }
        _material = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_material);
        _color = _renderer.material.GetColor("_BaseColor");

        _lastOutput = GetOutput();
        UpdatePipeAppearance(_lastOutput);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (_renderer == null || _source == null) return;

        bool output = GetOutput();
        if (output != _lastOutput)
        {
            UpdatePipeAppearance(output);
            _lastOutput = output;
        }
    }

    private bool GetOutput()
    {
        if (_source is SlotScript slot)
        {
            return slot.GetOutput();
        }
        else if (_source is InputScript input)
        {
            return input.GetOutput();
        }
        return false;
        
    }

    private void UpdatePipeAppearance(bool output)
    {
        _renderer.GetPropertyBlock(_material);
        if (output)
        {
            _material.SetColor("_BaseColor", Color.green);
            _material.SetColor("_EmissionColor", Color.green * 2.5f); // Свечение
        }
        else
        {
            _material.SetColor("_BaseColor", Color.red);
            _material.SetColor("_EmissionColor", Color.red * 2.5f); // Свечение
        }

        // Для слотов возвращаем дефолтный цвет, если нет вентиля
        // if (_source is SlotScript slot && slot.GetComponentInChildren<GateScript>() == null)
        // {
        //     _material.SetColor("_BaseColor", _color);
        //     _material.SetColor("_EmissionColor", Color.black); // Без свечения
        // }

        _renderer.SetPropertyBlock(_material);
    }
    
}
