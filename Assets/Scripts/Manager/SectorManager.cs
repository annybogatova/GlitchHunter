using System.Collections.Generic;
using UnityEngine;

public class SectorManager : MonoBehaviour
{
    [Header("Настройка сектора")]
    public List<SectorPartController> sectorParts;

    [Header("Параметры волны")]
    public int totalBugLimit = 50;

    private int _totalEnemiesAlive = 0;
    private int _totalSpawnedEnemies = 0;
    
    private bool _waveActive = false;

    public void StartWave()
    {
        if (_waveActive)
        {
            Debug.LogWarning("SectorManager: Волна уже активна.");
            return;
        }
        _waveActive = true;
        _totalEnemiesAlive = 0;
        _totalSpawnedEnemies = 0;

        foreach (var part in sectorParts)
        {
            part.OnEnemySpawned += RegisterEnemy;
            part.OnEnemyDied += UnregisterEnemy;

            part.StartWave();
        }
    }
    private void RegisterEnemy(GameObject enemy)
    {
        _totalEnemiesAlive++;
        _totalSpawnedEnemies++;
    }

    private void UnregisterEnemy(GameObject enemy)
    {
        _totalEnemiesAlive--;
        CheckWaveStatus();
    }
    
    private void CheckWaveStatus()
    {
        if (_totalEnemiesAlive <= 0 && _waveActive)
        {
            EndWave();
        }
    }

    public void RequestReinforcements(SectorPartController controller)
    {
        Debug.Log("SectorManager::RequestReinforcements");

        if (_totalSpawnedEnemies >= totalBugLimit)
        {
            Debug.Log("📛 Превышен лимит врагов, подкрепление невозможно");
            return;
        }

        int allowed = totalBugLimit - _totalSpawnedEnemies;
        int toSpawn = Mathf.Min(5, allowed);

        if (toSpawn > 0)
        {
            Debug.Log($"🔁 Отправляем подкрепление: {toSpawn} саботажников");
            controller.SpawnAdditionalSaboteurs(toSpawn);
        }
    }
    
    private void EndWave()
    {
        _waveActive = false;

        foreach (var part in sectorParts)
        {
            part.OnEnemySpawned -= RegisterEnemy;
            part.OnEnemyDied -= UnregisterEnemy;
        }

        Debug.Log("SectorManager: Волна завершена.");
    }
}
