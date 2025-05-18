using UnityEngine;

public class SlotScript : MonoBehaviour
{
    private bool value = true;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("SlotScript start");
    }

    public bool GetOutput()
    {
        return value;
    }
}
