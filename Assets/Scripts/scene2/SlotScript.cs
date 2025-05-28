using System;
using UnityEngine;

public class SlotScript : MonoBehaviour
{
    public MonoBehaviour[] inputs;
    public MonoBehaviour outputTarget;

    public bool GetOutput()
    {
        GateScript gate  = GetComponentInChildren<GateScript>();
        if (gate == null)
        {
            return false;
        }
        
        bool input1 = GetInputValue(inputs[0]);
        bool input2 = GetInputValue(inputs[1]);
        bool result = gate.ComputeOutput(input1, input2);

        // Инвертируем результат, если слот с тегом NegatedSlot
        if (gameObject.CompareTag("NegatedSlot"))
        {
            result = !result;
        }
        
        if (outputTarget is TabloScript tablo)
        {
            tablo.SetValue(result);
        }
        return result;
    }

    private bool GetInputValue(MonoBehaviour sourse)
    {
        if (sourse is SlotScript slot)
        {
            return slot.GetOutput();
        }
        else if (sourse is InputScript input)
        {
            return input.GetOutput();
        }
        return false;
    }

    public void PlaceGate(GateScript.GateType type)
    {
        GateScript existingGate = GetComponentInChildren<GateScript>();
        if (existingGate != null)
        {
            Destroy(existingGate.gameObject);
        }

        GameObject gatePrefab = RoomManager.instance.GetGatePrefab(type);
        if (gatePrefab != null)
        {
            GameObject gate = Instantiate(gatePrefab, transform.position, transform.rotation, transform);
            gate.GetComponent<GateScript>().gateType = type;
            PipeScript pipe = transform.parent.GetComponentInChildren<PipeScript>();
            if (pipe != null)
            {
                bool output = GetOutput();
                pipe.UpdatePipeAppearance(output);
                //Debug.Log($"PlaceGate: Обновлена труба для {gameObject.name}, output = {output}, gateType = {type}, tag = {gameObject.tag}");
            }
            else
            {
                Debug.LogWarning($"PipeScript не найден в {transform.parent.name}");
            }
        }
    }

    // private void OnMouseDown()
    // {
    //     if (RoomManager.instance.isRoomCompleted)
    //     {
    //         Debug.Log("Комната завершена, взаимодействие с слотами заблокировано");
    //         return;
    //     }
    //     
    //     if (RoomManager.instance.inventoryUI.GetSelectedGateType() != null)
    //     {
    //         PlaceGate(RoomManager.instance.inventoryUI.GetSelectedGateType().Value);
    //     }
    // }
}
