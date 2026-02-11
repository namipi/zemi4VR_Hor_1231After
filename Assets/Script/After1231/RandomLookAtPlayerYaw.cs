using UnityEngine;
using System.Collections;

/// <summary>
/// When enabled, rotates to face the player (Y axis only) at random intervals with a smooth fade.
/// </summary>
public class RandomLookAtPlayerYaw : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Player Transform (optional). If null, tries Camera.main.")]
    public Transform player;

    [Header("Timing")]
    [Tooltip("Minimum interval between rotations (seconds)")]
    public float minInterval = 0.7f;

    [Tooltip("Maximum interval between rotations (seconds)")]
    public float maxInterval = 1.5f;

    [Header("Rotation")]
    [Tooltip("Seconds to rotate toward the player")]
    public float rotateDuration = 0.25f;

    [Tooltip("Use easing for rotation")]
    public bool useEasing = true;

    [Header("Clone On Rotate Start")]
    [Tooltip("Rotate開始時に生成するPrefab（未設定なら生成しない）")]
    public GameObject clonePrefab;

    [Tooltip("クローンのローカル座標（親はこのオブジェクト）")]
    public Vector3 cloneLocalPosition = Vector3.zero;

    [Header("Debug")]
    public bool showDebugLog = false;

    public bool _isEnabled = false;
    private Coroutine _loopRoutine;

    void Start()
    {
        if (player == null)
        {
            var go = GameObject.Find("CenterEyeAnchor");
            if (go != null)
            {
                player = go.transform;
                if (showDebugLog)
                {
                    Debug.Log("[RandomLookAtPlayerYaw] CenterEyeAnchor found on Start.");
                }
            }
            else if (showDebugLog)
            {
                Debug.Log("[RandomLookAtPlayerYaw] CenterEyeAnchor not found on Start.");
            }
        }
    }

    void Update()
    {
        if (_isEnabled)
        {
            StartLoop();
        }
        else
        {
            StopLoop();
        }
    }

    void OnDisable()
    {
        StopLoop();
    }

    /// <summary>
    /// Enable/disable behavior from public function.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (showDebugLog)
        {
            Debug.Log(_isEnabled
                ? "[RandomLookAtPlayerYaw] Enabled."
                : "[RandomLookAtPlayerYaw] Disabled.");
        }
    }

    private void StartLoop()
    {
        if (_loopRoutine != null) return;
        _loopRoutine = StartCoroutine(Loop());
    }

    private void StopLoop()
    {
        if (_loopRoutine != null)
        {
            StopCoroutine(_loopRoutine);
            _loopRoutine = null;
        }
    }

    private IEnumerator Loop()
    {
        while (_isEnabled)
        {
            float wait = Random.Range(minInterval, maxInterval);
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }

            if (!_isEnabled) break;

            Transform target = player;
            if (target == null)
            {
                var go = GameObject.Find("CenterEyeAnchor");
                if (go != null)
                {
                    player = go.transform;
                    target = player;
                    if (showDebugLog)
                    {
                        Debug.Log("[RandomLookAtPlayerYaw] CenterEyeAnchor found during loop.");
                    }
                }
            }
            if (target == null && Camera.main != null)
            {
                target = Camera.main.transform;
                if (showDebugLog)
                {
                    Debug.Log("[RandomLookAtPlayerYaw] Using Camera.main as target.");
                }
            }

            if (target == null)
            {
                if (showDebugLog)
                {
                    Debug.Log("[RandomLookAtPlayerYaw] Target not found. Skipping.");
                }
                continue;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) continue;

            Quaternion from = transform.rotation;
            Quaternion to = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            if (showDebugLog)
            {
                Debug.Log("[RandomLookAtPlayerYaw] Rotating toward target.");
            }
            SpawnCloneOnRotateStart();
            yield return StartCoroutine(RotateOverTime(from, to, rotateDuration));
        }

        _loopRoutine = null;
    }

    private IEnumerator RotateOverTime(Quaternion from, Quaternion to, float duration)
    {
        if (duration <= 0f)
        {
            transform.rotation = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            if (useEasing)
            {
                k = k * k * (3f - 2f * k);
            }
            transform.rotation = Quaternion.SlerpUnclamped(from, to, k);
            yield return null;
        }

        transform.rotation = to;
    }

    private void SpawnCloneOnRotateStart()
    {
        if (clonePrefab == null) return;
        GameObject clone = Instantiate(clonePrefab, transform);
        clone.transform.localPosition = cloneLocalPosition;
        clone.transform.localRotation = Quaternion.identity;
    }
}
