using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject attackBugPrefab;
    public GameObject sabotageBugPrefab;

    public enum EnemyType
    {
        Attacker,
        Saboteur
    }

    /// <summary>
    /// Основной метод для спавна врагов.
    /// </summary>
    /// <param name="spawnPoints">Список возможных точек (Transform) для спавна.</param>
    /// <param name="type">Тип врага для спавна (Attacker или Saboteur).</param>
    /// <param name="count">Количество врагов, которых нужно заспавнить.</param>
    /// <param name="spawnStrategy">Как выбирать точки из списка (по умолчанию - случайные уникальные).</param>
    /// <returns>Список созданных игровых объектов врагов.</returns>
    ///
    public List<GameObject> SpawnEnemies(List<Transform> spawnPoints, EnemyType type, int count)
    {
        List<GameObject> enemies = new List<GameObject>();
        // --- 1. Валидация входных данных ---
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("EnemySpawner: Список точек спавна пуст или не назначен!");
            return enemies; // Возвращаем пустой список
        }
        if (count <= 0)
        {
            Debug.LogWarning("EnemySpawner: Запрошено создание 0 или менее врагов.");
            return enemies; // Возвращаем пустой список
        }
        
        // --- 2. Выбор нужного префаба ---
        GameObject prefab = null;
        switch (type)
        {
            case EnemyType.Attacker:
                prefab = attackBugPrefab;
                break;
            case EnemyType.Saboteur:
                prefab = sabotageBugPrefab;
                break;
            default:
                Debug.LogError($"EnemySpawner: Неизвестный тип врага: {type}");
                break;
        }
        if (prefab == null)
        {
            Debug.LogError($"EnemySpawner: Префаб для типа врага {type} не назначен!");
            return enemies;
        }
        
        // --- 3. Логика спавна (пример: случайные уникальные точки) ---
        List<Transform> availableSpawnPoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < count; i++)
        {
            if (availableSpawnPoints.Count == 0)
            {
                Debug.LogWarning($"EnemySpawner: Не хватило уникальных точек спавна для {type}. Заспавнено {enemies.Count} из {count}.");
                break;
            }
            int randomIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnPoint = availableSpawnPoints[randomIndex];
            
            GameObject newEnemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            enemies.Add(newEnemy);
            availableSpawnPoints.RemoveAt(randomIndex);
            
        }
        return enemies;
    }
}
