using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SectorPartController : MonoBehaviour
{
    public event Action<GameObject> OnEnemySpawned;
    public event Action<GameObject> OnEnemyDied;
    
    [Header("Spawn Settings")]
    public int initialAttackerCount = 5;
    public int initialSaboteurCount = 5;
    
    [Header("Links")]
    public EnemySpawner spawner; // Назначается извне (например, через SectorManager)
    public SectorManager sectorManager; // Назначается родителем
    
    private List<Transform> _attackerSpawnPoints;
    private List<Transform> _saboteurSpawnPoints;
    
    private List<GameObject> _activeSaboteurs = new();
    private List<GameObject> _activeAttackers = new();
    private int _saboteursKilled = 0;

    private void Awake() //получаем точки спавна из сцены
    {
        var attackerParent = transform.Find("AttackSpawnPoints");
        if (attackerParent != null)
        {
            _attackerSpawnPoints = attackerParent.GetComponentsInChildren<Transform>()
                .Where(t=>t != attackerParent)
                .ToList();
        }
        var saboteurParent = transform.Find("SabotageSpawnPoints");
        if (saboteurParent != null)
        {
            _saboteurSpawnPoints = saboteurParent.GetComponentsInChildren<Transform>()
                .Where(t => t != saboteurParent)
                .ToList();
        }
    }
    public void StartWave()
    {
        if (_attackerSpawnPoints == null || _attackerSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No attacker spawn points found");
            return;
        }
        
        // Спавн атакующих багов
        var attackers = spawner.SpawnEnemies(_attackerSpawnPoints, EnemySpawner.EnemyType.Attacker, initialAttackerCount);
        _activeAttackers.AddRange(attackers);
        
        foreach (var attacker in attackers)
        {
            OnEnemySpawned?.Invoke(attacker);

            var enemy = attacker.GetComponent<Enemy>();
            if (enemy != null)
                enemy.OnDeath += HandleAttackerDeath;
        }
        if (_saboteurSpawnPoints == null || _attackerSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No saboteur spawn points found");
        }
        // Спавн саботажников
        var saboteurs = spawner.SpawnEnemies(_saboteurSpawnPoints, EnemySpawner.EnemyType.Saboteur, initialSaboteurCount);
        _activeSaboteurs.AddRange(saboteurs);

        // Подпишемся на события их смерти
        foreach (var sab in saboteurs)
        {
            OnEnemySpawned?.Invoke(sab);

            var enemy = sab.GetComponent<Enemy>();
            if (enemy != null)
                enemy.OnDeath += OnSaboteurKilled;
        }
    }
    private void HandleAttackerDeath(GameObject attacker)
    {
        _activeAttackers.Remove(attacker);
        attacker.GetComponent<Enemy>().OnDeath -= HandleAttackerDeath;
        OnEnemyDied?.Invoke(attacker);
    }
    private void OnSaboteurKilled(GameObject saboteur)
    {
        _saboteursKilled++;
        _activeSaboteurs.Remove(saboteur);
        saboteur.GetComponent<Enemy>().OnDeath -= OnSaboteurKilled;
        OnEnemyDied?.Invoke(saboteur);

        // if (_saboteursKilled >= saboteursKilledThreshold)
        // {
        //     sectorManager.RequestReinforcements(this);
        // }
    }
    
    public void SpawnAdditionalSaboteurs(int count)
    {
        var newSaboteurs = spawner.SpawnEnemies(_saboteurSpawnPoints, EnemySpawner.EnemyType.Saboteur, count);
        _activeSaboteurs.AddRange(newSaboteurs);
        foreach (var sab in newSaboteurs)
        {
            OnEnemySpawned?.Invoke(sab);
            Enemy enemy = sab.GetComponent<Enemy>();
            if (enemy != null)
                enemy.OnDeath += OnSaboteurKilled;
        }
    }
    
    public int GetTotalActiveEnemies()
    {
        // В будущем можно сюда добавить и атакующих
        return _activeSaboteurs.Count;
    }
}
