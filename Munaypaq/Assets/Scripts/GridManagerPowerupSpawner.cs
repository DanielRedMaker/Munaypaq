using UnityEngine;

public class GridManagerPowerupSpawner : MonoBehaviour
{
    public static GridManagerPowerupSpawner Instance { get; private set; }

    [Header("Powerup ground prefabs")]
    public GameObject powerupGroundTrashBinPrefab;
    public GameObject powerupGroundAnnouncementPrefab;
    public GameObject powerupGroundSpeedBoostPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnPowerupAt(Vector3 worldPos, PowerupType type)
    {
        // Asegúrate de snap a tile center para que quede ordenado
        Vector3 spawnPos = (GridManager.Instance != null) ? GridManager.Instance.GetNearestWalkableTile(worldPos) : worldPos;

        GameObject prefab = null;
        switch (type)
        {
            case PowerupType.TrashBin: prefab = powerupGroundTrashBinPrefab; break;
            case PowerupType.Announcement: prefab = powerupGroundAnnouncementPrefab; break;
            case PowerupType.SpeedBoost: prefab = powerupGroundSpeedBoostPrefab; break;
        }

        if (prefab != null)
            Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}
