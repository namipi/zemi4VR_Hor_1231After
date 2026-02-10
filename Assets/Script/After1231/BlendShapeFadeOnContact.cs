using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 触れたらBlendShapeパラメータを指定した秒数でフェードするスクリプト
/// SkinnedMeshRendererを持つオブジェクトにアタッチ
/// </summary>
public class BlendShapeFadeOnContact : MonoBehaviour
{
    [Header("BlendShape設定")]
    [Tooltip("対象のSkinnedMeshRenderer（未設定の場合は自動取得）")]
    public SkinnedMeshRenderer targetRenderer;

    [Tooltip("フェードするBlendShapeのインデックス")]
    public int blendShapeIndex = 0;

    [Tooltip("BlendShape名で指定する場合（インデックスより優先）")]
    public string blendShapeName;

    [Header("フェード設定")]
    [Tooltip("フェードにかかる時間（秒）")]
    public float fadeDuration = 1.0f;

    [Tooltip("開始値 (0-100)")]
    public float startValue = 0f;

    [Tooltip("終了値 (0-100)")]
    public float endValue = 100f;

    [Tooltip("フェード完了後に逆再生するか")]
    public bool pingPong = false;

    [Tooltip("逆再生までの待機時間")]
    public float pingPongDelay = 0f;

    [Header("トリガー設定")]
    [Tooltip("トリガーとなるタグ（空の場合は全て）")]
    public string triggerTag = "Player";

    [Header("デバッグ")]
    public bool showDebugLog = false;

    [Header("イベント")]
    [Tooltip("フェード開始時に実行")]
    public UnityEvent onFadeStarted;

    [Tooltip("フェード完了時に実行")]
    public UnityEvent onFadeCompleted;

    private bool _hasTriggered = false;
    private bool _isFading = false;
    private int _resolvedIndex = -1;

    void Start()
    {
        // SkinnedMeshRenderer取得
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SkinnedMeshRenderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogError("[BlendShapeFadeOnContact] SkinnedMeshRendererが見つかりません！");
            return;
        }

        // BlendShapeインデックスを解決
        ResolveBlendShapeIndex();

        // 初期値を設定
        if (_resolvedIndex >= 0)
        {
            targetRenderer.SetBlendShapeWeight(_resolvedIndex, startValue);
        }
    }

    void ResolveBlendShapeIndex()
    {
        if (!string.IsNullOrEmpty(blendShapeName))
        {
            _resolvedIndex = targetRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
            if (_resolvedIndex < 0)
            {
                Debug.LogWarning($"[BlendShapeFadeOnContact] BlendShape '{blendShapeName}' が見つかりません");
            }
        }
        else
        {
            _resolvedIndex = blendShapeIndex;
        }

        if (_resolvedIndex < 0 || _resolvedIndex >= targetRenderer.sharedMesh.blendShapeCount)
        {
            Debug.LogError($"[BlendShapeFadeOnContact] 無効なBlendShapeインデックス: {_resolvedIndex}");
            _resolvedIndex = -1;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!CanTrigger(other.gameObject)) return;
        StartFade();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!CanTrigger(collision.gameObject)) return;
        StartFade();
    }

    bool CanTrigger(GameObject obj)
    {
        if (_isFading) return false;
        if (_hasTriggered) return false;  // 一度発動したらリセットするまで反応しない
        if (!string.IsNullOrEmpty(triggerTag) && !obj.CompareTag(triggerTag)) return false;
        if (_resolvedIndex < 0) return false;
        return true;
    }

    /// <summary>
    /// フェードを開始（外部から呼び出し可能）
    /// </summary>
    public void StartFade()
    {
        if (_isFading) return;
        if (_resolvedIndex < 0) return;

        _hasTriggered = true;
        StartCoroutine(FadeCoroutine(startValue, endValue));
    }

    /// <summary>
    /// 逆方向にフェード（外部から呼び出し可能）
    /// </summary>
    public void StartFadeReverse()
    {
        if (_isFading) return;
        if (_resolvedIndex < 0) return;

        StartCoroutine(FadeCoroutine(endValue, startValue));
    }

    /// <summary>
    /// 即座に終了値に設定
    /// </summary>
    public void SetToEnd()
    {
        if (_resolvedIndex >= 0)
        {
            targetRenderer.SetBlendShapeWeight(_resolvedIndex, endValue);
        }
    }

    /// <summary>
    /// 即座に開始値に設定
    /// </summary>
    public void SetToStart()
    {
        if (_resolvedIndex >= 0)
        {
            targetRenderer.SetBlendShapeWeight(_resolvedIndex, startValue);
        }
    }

    /// <summary>
    /// トリガー状態をリセット（再度トリガー可能にする）
    /// </summary>
    public void ResetTrigger()
    {
        _hasTriggered = false;
        if (showDebugLog)
        {
            Debug.Log("[BlendShapeFadeOnContact] トリガーリセット - 再度反応可能");
        }
    }

    /// <summary>
    /// リセットして開始値に戻す（再度トリガー可能）
    /// </summary>
    public void ResetAndRestore()
    {
        _hasTriggered = false;
        if (_resolvedIndex >= 0)
        {
            targetRenderer.SetBlendShapeWeight(_resolvedIndex, startValue);
        }
        if (showDebugLog)
        {
            Debug.Log("[BlendShapeFadeOnContact] リセット＆復元完了");
        }
    }

    private IEnumerator FadeCoroutine(float from, float to)
    {
        _isFading = true;

        // フェード開始イベント
        onFadeStarted?.Invoke();

        if (showDebugLog)
        {
            Debug.Log($"[BlendShapeFadeOnContact] フェード開始: {from} → {to} ({fadeDuration}秒)");
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float value = Mathf.Lerp(from, to, t);
            targetRenderer.SetBlendShapeWeight(_resolvedIndex, value);
            yield return null;
        }

        targetRenderer.SetBlendShapeWeight(_resolvedIndex, to);

        // フェード完了イベント
        onFadeCompleted?.Invoke();

        if (showDebugLog)
        {
            Debug.Log($"[BlendShapeFadeOnContact] フェード完了");
        }

        // PingPong処理
        if (pingPong && Mathf.Approximately(to, endValue))
        {
            if (pingPongDelay > 0)
            {
                yield return new WaitForSeconds(pingPongDelay);
            }
            _isFading = false;
            StartCoroutine(FadeCoroutine(endValue, startValue));
            yield break;
        }

        _isFading = false;
    }
}
