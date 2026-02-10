using UnityEngine;

/// <summary>
/// Lightのintensityをランダムにちらつかせるスクリプト
/// Lightコンポーネントを持つオブジェクトにアタッチ
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Intensity設定")]
    [Tooltip("最小Intensity")]
    public float minIntensity = 0.5f;

    [Tooltip("最大Intensity")]
    public float maxIntensity = 2.0f;

    [Header("ちらつき設定")]
    [Tooltip("ちらつきの最小間隔（秒）")]
    public float minFlickerInterval = 0.05f;

    [Tooltip("ちらつきの最大間隔（秒）")]
    public float maxFlickerInterval = 0.2f;

    [Tooltip("スムーズに変化させる（急激な変化を抑える）")]
    public bool smoothTransition = false;

    [Tooltip("スムーズ変化の速度")]
    public float smoothSpeed = 10f;

    [Header("制御")]
    [Tooltip("ちらつきを有効にする")]
    public bool isFlickering = true;

    [Tooltip("無効時にライトをオフにする")]
    public bool turnOffWhenDisabled = false;

    private Light _light;
    private float _nextFlickerTime;
    private float _targetIntensity;
    private float _originalIntensity;

    void Start()
    {
        _light = GetComponent<Light>();
        _originalIntensity = _light.intensity;
        _targetIntensity = _light.intensity;
        SetNextFlickerTime();
    }

    void Update()
    {
        if (!isFlickering)
        {
            if (turnOffWhenDisabled)
            {
                _light.intensity = 0f;
            }
            return;
        }

        if (Time.time >= _nextFlickerTime)
        {
            _targetIntensity = Random.Range(minIntensity, maxIntensity);
            SetNextFlickerTime();

            if (!smoothTransition)
            {
                _light.intensity = _targetIntensity;
            }
        }

        if (smoothTransition)
        {
            _light.intensity = Mathf.Lerp(_light.intensity, _targetIntensity, smoothSpeed * Time.deltaTime);
        }
    }

    void SetNextFlickerTime()
    {
        _nextFlickerTime = Time.time + Random.Range(minFlickerInterval, maxFlickerInterval);
    }

    #region 公開メソッド

    /// <summary>
    /// ちらつきを開始
    /// </summary>
    public void StartFlicker()
    {
        isFlickering = true;
    }

    /// <summary>
    /// ちらつきを停止
    /// </summary>
    public void StopFlicker()
    {
        isFlickering = false;
    }

    /// <summary>
    /// ちらつきを停止して元のIntensityに戻す
    /// </summary>
    public void StopAndRestore()
    {
        isFlickering = false;
        _light.intensity = _originalIntensity;
    }

    /// <summary>
    /// Intensity範囲を設定
    /// </summary>
    public void SetIntensityRange(float min, float max)
    {
        minIntensity = min;
        maxIntensity = max;
    }

    /// <summary>
    /// ちらつき間隔を設定
    /// </summary>
    public void SetFlickerInterval(float min, float max)
    {
        minFlickerInterval = min;
        maxFlickerInterval = max;
    }

    #endregion
}
