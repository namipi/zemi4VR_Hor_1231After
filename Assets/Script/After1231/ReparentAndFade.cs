using UnityEngine;
using System.Collections;

/// <summary>
/// 起動時にオブジェクトを新しい親に移し、見た目を維持したまま指定座標へフェードする
/// </summary>
public class ReparentAndFade : MonoBehaviour
{
    [Header("対象設定")]
    [Tooltip("移動させるオブジェクト（未設定の場合は自分自身）")]
    public Transform targetObject;

    [Tooltip("新しい親トランスフォーム")]
    public Transform newParent;

    [Header("フェード設定")]
    [Tooltip("最終的なローカル座標")]
    public Vector3 targetLocalPosition = Vector3.zero;

    [Tooltip("最終的なローカル回転（Euler角）")]
    public Vector3 targetLocalRotation = Vector3.zero;

    [Tooltip("回転もフェードするか")]
    public bool fadeRotation = false;

    [Tooltip("フェードにかかる時間（秒）")]
    public float fadeDuration = 1.0f;

    [Tooltip("イージングを使用するか")]
    public bool useEasing = true;

    [Header("タイミング")]
    [Tooltip("起動時に自動実行")]
    public bool executeOnStart = true;

    [Tooltip("実行前の遅延時間（秒）")]
    public float startDelay = 0f;

    [Header("デバッグ")]
    public bool showDebugLog = false;

    private bool _isExecuting = false;

    void Start()
    {
        if (targetObject == null)
        {
            targetObject = transform;
        }

        if (executeOnStart)
        {
            Execute();
        }
    }

    /// <summary>
    /// 親の変更とフェードを実行
    /// </summary>
    public void Execute()
    {
        if (_isExecuting) return;
        StartCoroutine(ExecuteCoroutine());
    }

    private IEnumerator ExecuteCoroutine()
    {
        _isExecuting = true;

        // 遅延
        if (startDelay > 0)
        {
            yield return new WaitForSeconds(startDelay);
        }

        if (newParent == null)
        {
            Debug.LogError("[ReparentAndFade] newParentが設定されていません！");
            _isExecuting = false;
            yield break;
        }

        // 現在のワールド座標を保存
        Vector3 worldPosition = targetObject.position;
        Quaternion worldRotation = targetObject.rotation;

        // 親を変更
        targetObject.SetParent(newParent);

        // ワールド座標を維持（見た目を変えない）
        targetObject.position = worldPosition;
        targetObject.rotation = worldRotation;

        if (showDebugLog)
        {
            Debug.Log($"[ReparentAndFade] 親を変更: {newParent.name}");
        }

        // 開始時のローカル座標を取得
        Vector3 startLocalPos = targetObject.localPosition;
        Quaternion startLocalRot = targetObject.localRotation;
        Quaternion targetRot = Quaternion.Euler(targetLocalRotation);

        // フェード
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // イージング（SmoothStep）
            if (useEasing)
            {
                t = t * t * (3f - 2f * t);
            }

            targetObject.localPosition = Vector3.Lerp(startLocalPos, targetLocalPosition, t);

            if (fadeRotation)
            {
                targetObject.localRotation = Quaternion.Slerp(startLocalRot, targetRot, t);
            }

            yield return null;
        }

        // 最終値を確定
        targetObject.localPosition = targetLocalPosition;
        if (fadeRotation)
        {
            targetObject.localRotation = targetRot;
        }

        if (showDebugLog)
        {
            Debug.Log($"[ReparentAndFade] フェード完了: localPosition = {targetLocalPosition}");
        }

        _isExecuting = false;
    }

    /// <summary>
    /// 即座に最終状態に設定
    /// </summary>
    public void SetImmediate()
    {
        if (newParent == null || targetObject == null) return;

        targetObject.SetParent(newParent);
        targetObject.localPosition = targetLocalPosition;
        if (fadeRotation)
        {
            targetObject.localRotation = Quaternion.Euler(targetLocalRotation);
        }
    }
}
