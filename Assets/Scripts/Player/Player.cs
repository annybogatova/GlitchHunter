using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Жизни")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Стрельба")]
    [SerializeField] private Camera playerCamera; // Камера для определения цели
    [SerializeField] private Transform shootPoint; // Точка, откуда вылетает "пуля"
    public float range = 100f;
    public int damage = 20;

    void Start()
    {
        currentHealth = maxHealth;
        // Если не назначили камеру в инспекторе, можно попробовать найти главную:
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("Камера для стрельбы не найдена! Назначьте playerCamera в инспекторе или убедитесь, что есть камера с тегом MainCamera.");
            }
        }
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1") && playerCamera != null && shootPoint != null)
        {
            ShootFromGunToMouse();
        }
    }

    void ShootFromGunToMouse()
    {
        // 1. Определяем цель с помощью луча из камеры
        Ray cameraRay = playerCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint;

        // Проверяем, попал ли луч из камеры во что-то
        if (Physics.Raycast(cameraRay, out RaycastHit cameraHit, range))
        {
            targetPoint = cameraHit.point; // Цель - точка попадания луча из камеры
        }
        else
        {
            // Если луч из камеры ни во что не попал, берем точку далеко по направлению луча
            targetPoint = cameraRay.GetPoint(range);
        }

         // Опционально: рисуем луч из камеры для отладки
        Debug.DrawRay(cameraRay.origin, cameraRay.direction * range, Color.magenta, 1f);

        // 2. Вычисляем направление от ствола (shootPoint) к цели (targetPoint)
        Vector3 shootDirection = (targetPoint - shootPoint.position).normalized;

        // 3. Выпускаем луч из ствола в вычисленном направлении
         Debug.DrawRay(shootPoint.position, shootDirection * range, Color.cyan, 1f); // Рисуем луч из ствола

        if (Physics.Raycast(shootPoint.position, shootDirection, out RaycastHit hit, range))
        {
             Debug.DrawRay(shootPoint.position, shootDirection * hit.distance, Color.red, 1f); // До точки попадания

            Debug.Log("Попадание (из ствола) в: " + hit.collider.name + " с тегом: " + hit.collider.tag);

            // Логика урона та же
             if (hit.collider.CompareTag("Enemy"))
            {
                var enemyHealth = hit.collider.GetComponentInParent<Enemy>();
                if (enemyHealth != null)
                {
                     Debug.Log("Нанесен урон врагу: " + hit.collider.name);
                    enemyHealth.TakeDamage(damage);
                }
                 else
                {
                     Debug.LogError("Не найден компонент Enemy на/над объектом: " + hit.collider.name);
                }
            }
        }
         else
        {
            Debug.Log("Луч (из ствола) не попал ни во что.");
        }
    }
}