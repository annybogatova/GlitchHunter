using UnityEngine;

public class TabloScript : MonoBehaviour
{
    public GameObject tabloCellZero;
    public GameObject tabloCellOne;
    private GameObject currentCell;

    public void UpdateDisplay(bool value)
    {
        if (currentCell != null)
        {
            Destroy(currentCell);
        }
        GameObject prefab = value ? tabloCellOne : tabloCellZero;
        if (prefab != null)
        {
            currentCell = Instantiate(prefab, transform.position, transform.rotation, transform);
            currentCell.name = value ? "Cell_1" : "Cell_0";
        }
        else
        {
            Debug.LogError("Tablo cell prefab не назначен для " + (value ? "1" : "0"));
        }
    }
    
}
