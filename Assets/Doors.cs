using UnityEngine;
using System.Collections;

public class Doors : MonoBehaviour
{
    public GameObject Door;
    public GameObject Wall;
    public GameObject ResetSpawnPrefab;
    [Header("扉の配置数")]
    public int count = 5;

    [Header("追加壁の配置")]
    [Tooltip("ランダム配置に加えて、追加で壁を生成するローカル座標")]
    public Vector3[] additionalWallPositions;

    [Header("移動設定")]
    public float moveDistance = 1.5f;
    public float moveDuration = 1f;

    private Vector3 _originalLocalPosition;
    private bool _isMoving = false;

    void Start()
    {
        _originalLocalPosition = transform.localPosition;
        Generate();
    }

    /// <summary>
    /// ドアが開いたら移動してリセットする
    /// </summary>
    public void OnDoorOpened()
    {
        if (_isMoving) return;
        StartCoroutine(MoveAndReset());
    }

    private IEnumerator MoveAndReset()
    {
        _isMoving = true;

        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + new Vector3(-moveDistance, 0f, 0f);
        float elapsed = 0f;

        // 1秒かけてX方向に移動
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.localPosition = endPos;
        Debug.Log("[Doors] 移動完了。リセット開始。");

        // 元の位置に戻す
        transform.localPosition = _originalLocalPosition;

        // 既存のDoor/Wallを削除
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // Reset時に指定プレハブを子として生成
        if (ResetSpawnPrefab != null)
        {
            GameObject resetInstance = Instantiate(ResetSpawnPrefab, transform);
            resetInstance.transform.localPosition = Vector3.zero;
            resetInstance.transform.localRotation = Quaternion.identity;
        }

        // 再配置
        Generate();

        _isMoving = false;
    }

    void Generate()
    {
        int layers = 2;
        float layerHeight = 0.85f;
        int totalSlots = count * layers;

        // ランダムにDoorを配置するスロットを決定（各レイヤーにcount個）
        int doorSlot = Random.Range(0, totalSlots);

        for (int layer = 0; layer < layers; layer++)
        {
            float baseY = 0.533f + (layer * layerHeight);

            for (int i = 0; i < count; i++)
            {
                int currentSlot = layer * count + i;

                // Z方向: index 0 = 0.356, index 1 = 1.0, ... (0.644刻み)
                float z = -0.356f + (i * -0.644f);
                Vector3 localPos = new Vector3(0f, baseY, z);

                // ランダムスロットはDoor、それ以外はWall
                GameObject prefab = (currentSlot == doorSlot) ? Door : Wall;
                GameObject instance = Instantiate(prefab, transform);
                instance.transform.localPosition = localPos;
                instance.transform.localRotation = Quaternion.identity;
                instance.name = (currentSlot == doorSlot) ? $"Door_L{layer}_{i}" : $"Wall_L{layer}_{i}";
            }
        }

        // ランダムとは別に追加壁を生成
        GenerateAdditionalWalls();

        int doorLayer = doorSlot / count;
        int doorIndex = doorSlot % count;
        Debug.Log($"[Doors] ランダム Door 位置 = Layer {doorLayer}, Index {doorIndex}");
    }

    /// <summary>
    /// ランダムとは別に追加壁を生成
    /// </summary>
    void GenerateAdditionalWalls()
    {
        if (additionalWallPositions == null || additionalWallPositions.Length == 0) return;

        for (int i = 0; i < additionalWallPositions.Length; i++)
        {
            GameObject instance = Instantiate(Wall, transform);
            instance.transform.localPosition = additionalWallPositions[i];
            instance.transform.localRotation = Quaternion.identity;
            instance.name = $"Wall_Fixed_{i}";
        }

        Debug.Log($"[Doors] 追加壁を {additionalWallPositions.Length} 個生成");
    }
}



