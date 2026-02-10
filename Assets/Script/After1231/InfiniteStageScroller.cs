using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ステージを無限スクロールさせるスクリプト
/// Prefabを指定した方向に動かしながら、一定距離進んだらクローンを生成して無限に続くように見せる
/// </summary>
public class InfiniteStageScroller : MonoBehaviour
{
    [Header("ステージ設定")]
    [Tooltip("スクロールするステージのPrefab")]
    public GameObject stagePrefab;

    [Tooltip("ステージの長さ（次のクローンを生成する距離）")]
    public float stageLength = 50f;

    [Tooltip("最大同時生成数（古いものは削除される）")]
    public int maxStageCount = 3;

    [Header("移動設定")]
    [Tooltip("移動方向（正規化される）")]
    public Vector3 moveDirection = Vector3.back;

    [Tooltip("移動速度")]
    public float moveSpeed = 10f;

    [Header("生成位置")]
    [Tooltip("最初のステージ生成位置")]
    public Vector3 spawnOffset = Vector3.zero;

    [Header("制御")]
    [Tooltip("スクロールを有効にする")]
    public bool isScrolling = true;

    [Tooltip("デバッグログを表示")]
    public bool showDebugLog = false;

    // 生成されたステージのリスト
    private List<GameObject> activeStages = new List<GameObject>();

    // 累積移動距離
    private float totalDistance = 0f;

    // 次のステージ生成までの距離
    private float nextSpawnDistance;

    // 正規化された移動方向
    private Vector3 normalizedDirection;

    void Start()
    {
        if (stagePrefab == null)
        {
            Debug.LogError("[InfiniteStageScroller] stagePrefabが設定されていません！");
            return;
        }

        normalizedDirection = moveDirection.normalized;
        nextSpawnDistance = stageLength;

        // 初期ステージをローカル座標で生成 (スクローラー自身の位置 = Vector3.zero)
        SpawnStage(spawnOffset);

        // 最初から2つ生成しておく（継ぎ目が見えないように）
        SpawnStage(spawnOffset - normalizedDirection * stageLength);
    }

    void Update()
    {
        if (!isScrolling || stagePrefab == null) return;

        float moveAmount = moveSpeed * Time.deltaTime;

        // 全てのステージを移動
        MoveAllStages(moveAmount);

        // 累積距離を更新
        totalDistance += moveAmount;

        // 一定距離進んだら新しいステージを生成
        if (totalDistance >= nextSpawnDistance)
        {
            SpawnNextStage();
            nextSpawnDistance += stageLength;
        }

        // 古いステージを削除
        CleanupOldStages();
    }

    /// <summary>
    /// 全てのアクティブなステージを移動
    /// </summary>
    private void MoveAllStages(float moveAmount)
    {
        Vector3 movement = normalizedDirection * moveAmount;

        foreach (var stage in activeStages)
        {
            if (stage != null)
            {
                stage.transform.localPosition += movement;
            }
        }
    }

    /// <summary>
    /// 次のステージを生成
    /// </summary>
    private void SpawnNextStage()
    {
        // 最後尾のステージの後ろに生成
        Vector3 spawnLocalPosition;

        if (activeStages.Count > 0 && activeStages[activeStages.Count - 1] != null)
        {
            // 最後のステージから逆方向にstageLength分離れた位置
            spawnLocalPosition = activeStages[activeStages.Count - 1].transform.localPosition - normalizedDirection * stageLength;
        }
        else
        {
            spawnLocalPosition = spawnOffset - normalizedDirection * totalDistance;
        }

        SpawnStage(spawnLocalPosition);
    }

    /// <summary>
    /// 指定位置にステージを生成（ローカル座標）
    /// </summary>
    private void SpawnStage(Vector3 localPosition)
    {
        GameObject newStage = Instantiate(stagePrefab, transform);
        newStage.transform.localPosition = localPosition;
        newStage.transform.localRotation = Quaternion.identity;
        newStage.name = $"{stagePrefab.name}_Clone_{activeStages.Count}";
        activeStages.Add(newStage);

        if (showDebugLog)
        {
            Debug.Log($"[InfiniteStageScroller] ステージ生成: {newStage.name} at local {localPosition}");
        }
    }

    /// <summary>
    /// 画面外に出た古いステージを削除
    /// </summary>
    private void CleanupOldStages()
    {
        while (activeStages.Count > maxStageCount)
        {
            if (activeStages[0] != null)
            {
                if (showDebugLog)
                {
                    Debug.Log($"[InfiniteStageScroller] ステージ削除: {activeStages[0].name}");
                }
                Destroy(activeStages[0]);
            }
            activeStages.RemoveAt(0);
        }
    }

    #region 公開メソッド

    /// <summary>
    /// スクロールを開始
    /// </summary>
    public void StartScrolling()
    {
        isScrolling = true;
        if (showDebugLog) Debug.Log("[InfiniteStageScroller] スクロール開始");
    }

    /// <summary>
    /// スクロールを停止
    /// </summary>
    public void StopScrolling()
    {
        isScrolling = false;
        if (showDebugLog) Debug.Log("[InfiniteStageScroller] スクロール停止");
    }

    /// <summary>
    /// 速度を設定
    /// </summary>
    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
        if (showDebugLog) Debug.Log($"[InfiniteStageScroller] 速度変更: {speed}");
    }

    /// <summary>
    /// 速度を加算
    /// </summary>
    public void AddSpeed(float amount)
    {
        moveSpeed += amount;
        if (showDebugLog) Debug.Log($"[InfiniteStageScroller] 速度加算: {moveSpeed}");
    }

    /// <summary>
    /// 移動方向を設定
    /// </summary>
    public void SetDirection(Vector3 direction)
    {
        moveDirection = direction;
        normalizedDirection = direction.normalized;
    }

    /// <summary>
    /// 全ステージをリセット
    /// </summary>
    public void ResetStages()
    {
        // 全て削除
        foreach (var stage in activeStages)
        {
            if (stage != null) Destroy(stage);
        }
        activeStages.Clear();

        // 初期状態に戻す
        totalDistance = 0f;
        nextSpawnDistance = stageLength;

        // 初期ステージを再生成
        SpawnStage(spawnOffset);
        SpawnStage(spawnOffset - normalizedDirection * stageLength);

        if (showDebugLog) Debug.Log("[InfiniteStageScroller] リセット完了");
    }

    #endregion

    void OnDestroy()
    {
        // クリーンアップ
        foreach (var stage in activeStages)
        {
            if (stage != null) Destroy(stage);
        }
        activeStages.Clear();
    }
}
