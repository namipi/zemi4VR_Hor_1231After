using UnityEngine;
using System.Collections;

/// <summary>
/// 関数が指定回数実行されたらEmissionをフェードで明るくするスクリプト
/// Rendererを持つオブジェクトにアタッチ
/// </summary>
public class EmissionFadeOnCount : MonoBehaviour
{
    [Header("カウント設定")]
    [Tooltip("発動までの実行回数")]
    public int triggerCount = 6;

    [Header("Emission設定")]
    [Tooltip("対象のRenderer（未設定の場合は自動取得）")]
    public Renderer targetRenderer;

    [Tooltip("対象のマテリアルインデックス")]
    public int materialIndex = 0;

    [Tooltip("Emissionの色")]
    public Color emissionColor = Color.white;

    [Tooltip("開始時のEmission強度")]
    public float startIntensity = 0f;

    [Tooltip("最大Emission強度")]
    public float maxIntensity = 2f;

    [Header("フェード設定")]
    [Tooltip("フェードにかかる時間（秒）")]
    public float fadeDuration = 1.0f;

    [Tooltip("イージングを使用")]
    public bool useEasing = true;

    [Header("連動するBlendShape")]
    [Tooltip("DeactivateEmission時にResetTriggerを実行するリスト")]
    public BlendShapeFadeOnContact[] blendShapeFaders;

    [Header("デバッグ")]
    public bool showDebugLog = false;

    private int _currentCount = 0;
    private bool _isFading = false;
    private bool _isActivated = false;
    private Material _material;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogError("[EmissionFadeOnCount] Rendererが見つかりません！");
            return;
        }

        // マテリアルを取得（インスタンス化）
        if (materialIndex < targetRenderer.materials.Length)
        {
            _material = targetRenderer.materials[materialIndex];
        }
        else
        {
            Debug.LogError($"[EmissionFadeOnCount] マテリアルインデックス {materialIndex} が無効です");
            return;
        }

        // 初期Emissionを設定
        SetEmissionIntensity(startIntensity);

        // Emissionを有効化
        _material.EnableKeyword("_EMISSION");
    }

    /// <summary>
    /// カウントを増やす関数（UnityEventから呼び出し用）
    /// 指定回数に達するとEmissionが明るくなる
    /// </summary>
    public void IncrementCount()
    {
        if (_isActivated) return;

        _currentCount++;

        if (showDebugLog)
        {
            Debug.Log($"[EmissionFadeOnCount] カウント: {_currentCount}/{triggerCount}");
        }

        if (_currentCount >= triggerCount)
        {
            ActivateEmission();
        }
    }

    /// <summary>
    /// Emissionを明るくする
    /// </summary>
    public void ActivateEmission()
    {
        if (_isFading || _isActivated) return;
        _isActivated = true;
        StartCoroutine(FadeEmission(startIntensity, maxIntensity));
    }

    /// <summary>
    /// Emissionを元に戻す
    /// </summary>
    public void DeactivateEmission()
    {
        if (_isFading || !_isActivated) return;
        _isActivated = false;
        _currentCount = 0;

        // BlendShapeFadeOnContactのResetTriggerを全て実行
        if (blendShapeFaders != null)
        {
            foreach (var fader in blendShapeFaders)
            {
                if (fader != null)
                {
                    fader.ResetTrigger();
                }
            }
            if (showDebugLog)
            {
                Debug.Log($"[EmissionFadeOnCount] {blendShapeFaders.Length}個のBlendShapeFaderをリセット");
            }
        }

        StartCoroutine(FadeEmission(maxIntensity, startIntensity));
    }

    /// <summary>
    /// カウントをリセット（Emissionはそのまま）
    /// </summary>
    public void ResetCount()
    {
        _currentCount = 0;
        if (showDebugLog)
        {
            Debug.Log("[EmissionFadeOnCount] カウントリセット");
        }
    }

    /// <summary>
    /// 全てをリセット（カウントとEmission両方）
    /// </summary>
    public void ResetAll()
    {
        _currentCount = 0;
        _isActivated = false;
        SetEmissionIntensity(startIntensity);
        if (showDebugLog)
        {
            Debug.Log("[EmissionFadeOnCount] 全リセット");
        }
    }

    private IEnumerator FadeEmission(float from, float to)
    {
        _isFading = true;

        if (showDebugLog)
        {
            Debug.Log($"[EmissionFadeOnCount] Emissionフェード: {from} → {to}");
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (useEasing)
            {
                t = t * t * (3f - 2f * t);
            }

            float intensity = Mathf.Lerp(from, to, t);
            SetEmissionIntensity(intensity);
            yield return null;
        }

        SetEmissionIntensity(to);
        _isFading = false;

        if (showDebugLog)
        {
            Debug.Log("[EmissionFadeOnCount] フェード完了");
        }
    }

    private void SetEmissionIntensity(float intensity)
    {
        if (_material == null) return;
        Color finalColor = emissionColor * intensity;
        _material.SetColor(EmissionColorID, finalColor);
    }
}
