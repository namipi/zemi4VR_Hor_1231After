using UnityEngine;
using System.Collections;

/// <summary>
/// Records local position on Start and returns to it over a duration when invoked.
/// </summary>
public class ReturnToStartLocalPosition : MonoBehaviour
{
    [Header("Return Settings")]
    public float defaultDuration = 1.0f;
    public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool stopPrevious = true;

    private Vector3 _startLocalPosition;
    private Quaternion _startLocalRotation;
    private Coroutine _returnRoutine;

    void Start()
    {
        _startLocalPosition = transform.localPosition;
        _startLocalRotation = transform.localRotation;
    }

    public void RecordStartLocalPosition()
    {
        _startLocalPosition = transform.localPosition;
        _startLocalRotation = transform.localRotation;
    }

    public void ReturnToStart()
    {
        ReturnToStartWithDuration(defaultDuration);
    }

    public void ReturnToStartWithDuration(float duration)
    {
        if (duration <= 0f)
        {
            transform.localPosition = _startLocalPosition;
            transform.localRotation = _startLocalRotation;
            return;
        }

        if (stopPrevious && _returnRoutine != null)
        {
            StopCoroutine(_returnRoutine);
        }

        _returnRoutine = StartCoroutine(ReturnRoutine(duration));
    }

    private IEnumerator ReturnRoutine(float duration)
    {
        Vector3 fromPos = transform.localPosition;
        Vector3 toPos = _startLocalPosition;
        Quaternion fromRot = transform.localRotation;
        Quaternion toRot = _startLocalRotation;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            float eased = easing != null ? easing.Evaluate(normalized) : normalized;
            transform.localPosition = Vector3.LerpUnclamped(fromPos, toPos, eased);
            transform.localRotation = Quaternion.SlerpUnclamped(fromRot, toRot, eased);
            yield return null;
        }

        transform.localPosition = toPos;
        transform.localRotation = toRot;
        _returnRoutine = null;
    }
}
