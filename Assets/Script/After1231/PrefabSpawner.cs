using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 指定したPrefabを指定個数、指定時間おきに指定範囲内でスポーンするスクリプト
/// </summary>
public class PrefabSpawner : MonoBehaviour
{
    [Header("スポーン設定")]
    [Tooltip("スポーンするPrefabのリスト")]
    public List<GameObject> prefabs = new List<GameObject>();

    [Tooltip("最大スポーン数")]
    public int maxSpawnCount = 10;

    [Tooltip("スポーン間隔（秒）")]
    public float spawnInterval = 2f;

    [Header("スポーン範囲")]
    [Tooltip("スポーン範囲の中心（このオブジェクトからの相対位置）")]
    public Vector3 spawnAreaCenter = Vector3.zero;

    [Tooltip("スポーン範囲のサイズ（X, Y, Z）")]
    public Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f);

    [Header("回転設定")]
    [Tooltip("ランダムな回転を適用するか")]
    public bool randomRotation = true;

    [Tooltip("Y軸のみランダム回転")]
    public bool yAxisRotationOnly = true;

    [Header("自動スポーン")]
    [Tooltip("Start時に自動でスポーンを開始するか")]
    public bool autoStart = true;

    // 現在のスポーン数
    private int currentSpawnCount = 0;

    // スポーンされたオブジェクトのリスト
    private List<GameObject> spawnedObjects = new List<GameObject>();

    // スポーン中かどうか
    private bool isSpawning = false;

    // スポーンコルーチン
    private Coroutine spawnCoroutine;

    // クローンが破棄されたときのイベント
    public event Action<GameObject> OnCloneDestroyed;
    public event Action<int> OnSpawnCountChanged;

    void Start()
    {
        if (autoStart)
        {
            StartSpawning();
        }
    }

    /// <summary>
    /// スポーンを開始する
    /// </summary>
    public void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    /// <summary>
    /// スポーンを停止する
    /// </summary>
    public void StopSpawning()
    {
        if (isSpawning && spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            isSpawning = false;
        }
    }

    /// <summary>
    /// スポーンルーチン
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            // 最大数に達していなければスポーン
            if (currentSpawnCount < maxSpawnCount && prefabs.Count > 0)
            {
                SpawnPrefab();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// Prefabをスポーンする
    /// </summary>
    private void SpawnPrefab()
    {
        if (prefabs.Count == 0) return;

        // ランダムにPrefabを選択
        GameObject prefabToSpawn = prefabs[UnityEngine.Random.Range(0, prefabs.Count)];

        // スポーン位置を計算
        Vector3 spawnPosition = GetRandomSpawnPosition();

        // 回転を計算
        Quaternion spawnRotation = GetSpawnRotation();

        // Prefabをインスタンス化
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

        // SpawnedObjectTrackerを追加して破棄を監視
        SpawnedObjectTracker tracker = spawnedObject.AddComponent<SpawnedObjectTracker>();
        tracker.Initialize(this);

        // リストに追加
        spawnedObjects.Add(spawnedObject);
        currentSpawnCount++;

        // イベント発火
        OnSpawnCountChanged?.Invoke(currentSpawnCount);

        Debug.Log($"[PrefabSpawner] Spawned: {prefabToSpawn.name} at {spawnPosition}. Count: {currentSpawnCount}/{maxSpawnCount}");
    }

    /// <summary>
    /// ランダムなスポーン位置を取得
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 worldCenter = transform.position + spawnAreaCenter;

        float randomX = UnityEngine.Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float randomY = UnityEngine.Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        float randomZ = UnityEngine.Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f);

        return worldCenter + new Vector3(randomX, randomY, randomZ);
    }

    /// <summary>
    /// スポーン時の回転を取得
    /// </summary>
    private Quaternion GetSpawnRotation()
    {
        if (!randomRotation)
        {
            return Quaternion.identity;
        }

        if (yAxisRotationOnly)
        {
            return Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        }
        else
        {
            return UnityEngine.Random.rotation;
        }
    }

    /// <summary>
    /// クローンが破棄されたときに呼ばれる（SpawnedObjectTrackerから呼ばれる）
    /// </summary>
    public void NotifyCloneDestroyed(GameObject destroyedObject)
    {
        if (spawnedObjects.Contains(destroyedObject))
        {
            spawnedObjects.Remove(destroyedObject);
            currentSpawnCount--;

            // イベント発火
            OnCloneDestroyed?.Invoke(destroyedObject);
            OnSpawnCountChanged?.Invoke(currentSpawnCount);

            Debug.Log($"[PrefabSpawner] Clone destroyed. Count: {currentSpawnCount}/{maxSpawnCount}");
        }
    }

    /// <summary>
    /// 現在のスポーン数を取得
    /// </summary>
    public int GetCurrentSpawnCount()
    {
        return currentSpawnCount;
    }

    /// <summary>
    /// スポーンされたオブジェクトのリストを取得
    /// </summary>
    public List<GameObject> GetSpawnedObjects()
    {
        return new List<GameObject>(spawnedObjects);
    }

    /// <summary>
    /// すべてのスポーンされたオブジェクトを破棄
    /// </summary>
    public void DestroyAllSpawned()
    {
        foreach (GameObject obj in spawnedObjects.ToArray())
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        currentSpawnCount = 0;
        OnSpawnCountChanged?.Invoke(currentSpawnCount);
    }

    /// <summary>
    /// 即座に1つスポーンする（手動スポーン用）
    /// </summary>
    public void SpawnOne()
    {
        if (currentSpawnCount < maxSpawnCount && prefabs.Count > 0)
        {
            SpawnPrefab();
        }
    }

    /// <summary>
    /// スポーン範囲をGizmosで可視化
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Vector3 worldCenter = transform.position + spawnAreaCenter;
        Gizmos.DrawCube(worldCenter, spawnAreaSize);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(worldCenter, spawnAreaSize);
    }
}

/// <summary>
/// スポーンされたオブジェクトの破棄を監視するコンポーネント
/// </summary>
public class SpawnedObjectTracker : MonoBehaviour
{
    private PrefabSpawner spawner;

    public void Initialize(PrefabSpawner parentSpawner)
    {
        spawner = parentSpawner;
    }

    private void OnDestroy()
    {
        // シーン終了時やアプリ終了時は通知しない
        if (spawner != null && !IsApplicationQuitting())
        {
            spawner.NotifyCloneDestroyed(gameObject);
        }
    }

    private bool IsApplicationQuitting()
    {
        #if UNITY_EDITOR
        return !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
        #else
        return false;
        #endif
    }
}
