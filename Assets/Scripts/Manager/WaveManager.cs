using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public List<SectorManager> sectors;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sectors[0].StartWave();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
