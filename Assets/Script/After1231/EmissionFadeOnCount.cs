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
    public Renderer[] targetRenderers;

    [Tooltip("対象のマテリアルインデックス")]
    public int materialIndex = 0;

    [Tooltip("Emissionの色")]
    public Color emissionColor = Color.red;

    [Tooltip("開始時のEmission強度")]
    public float startIntensity = 0f;

    [Tooltip("最大Emission強度")]
    public float maxIntensity = 2f;

    [Header("フェード設定")]
    [Tooltip("フェードにかかる時間（秒）")]
    public float fadeDuration = 1.0f;

    [Tooltip("イージングを使用")]
    public bool useEasing = true;

    [Header("スケール対象")]
    [Tooltip("スケールを変更する対象リスト")]
    public GameObject[] scaleTargets;

    [Tooltip("1回目のスケール（%）")]
    public float scalePercent1 = 80f;

    [Tooltip("2回目のスケール（%）")]
    public float scalePercent2 = 50f;

    [Tooltip("3回目の0スケール後に戻すスケール（%）")]
    public float scalePercent3Reset = 100f;

    [Tooltip("3回目で0スケールにしてから戻すまでの待ち時間（秒）")]
    public float scaleZeroHoldSeconds = 0.05f;

    [Tooltip("3回目で0スケールまで変化させる時間（秒）")]
    public float scaleToZeroDuration = 0.1f;

    [Tooltip("3回目でリセットスケールまで戻す時間（秒）")]
    public float scaleToResetDuration = 0.1f;

    [Header("連動するBlendShape")]
    [Tooltip("DeactivateEmission時にResetTriggerを実行するリスト")]
    public BlendShapeFadeOnContact[] blendShapeFaders;

    [Header("ボスEmissionパルス")]
    [Tooltip("リセット時にEmissionをパルスさせる対象Renderer")]
    public Renderer[] bossRenderers;

    [Tooltip("ボスEmission対象のマテリアルインデックス")]
    public int bossMaterialIndex = 0;

    [Tooltip("ボスEmissionを赤くする時間（秒）")]
    public float bossPulseUpSeconds = 0.3f;

    [Tooltip("ボスEmissionを元に戻す時間（秒）")]
    public float bossPulseDownSeconds = 1.0f;

    [Tooltip("ボスEmissionの赤色強度")]
    public float bossEmissionIntensity = 2f;

    [Header("デバッグ")]
    public bool showDebugLog = false;

    private int _currentCount = 0;
    private int _deactivateCount = 0;
    private bool _isFading = false;
    private bool _isActivated = false;
    private Material[] _materials;
    private Vector3[] _scaleTargetBaseScales;
    private Coroutine _scaleRoutine;
    private Coroutine _bossPulseRoutine;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = new Renderer[] { GetComponent<Renderer>() };
        }

        bool anyRenderer = false;
        foreach (var r in targetRenderers)
        {
            if (r != null)
            {
                anyRenderer = true;
                break;
            }
        }

        if (!anyRenderer)
        {
            Debug.LogError("[EmissionFadeOnCount] Rendererが見つかりません！");
            return;
        }

        // マテリアルを取得（インスタンス化）
        var materials = new System.Collections.Generic.List<Material>();
        foreach (var r in targetRenderers)
        {
            if (r == null) continue;
            if (materialIndex < r.materials.Length)
            {
                materials.Add(r.materials[materialIndex]);
            }
            else
            {
                Debug.LogError($"[EmissionFadeOnCount] マテリアルインデックス {materialIndex} が無効です: {r.name}");
            }
        }

        if (materials.Count == 0)
        {
            Debug.LogError("[EmissionFadeOnCount] 有効なマテリアルが見つかりません！");
            return;
        }

        _materials = materials.ToArray();

        // 初期Emissionを設定
        SetEmissionIntensity(startIntensity);

        // Emissionを有効化
        foreach (var m in _materials)
        {
            if (m != null)
            {
                m.EnableKeyword("_EMISSION");
            }
        }

        CacheBaseScales();
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
    /// カウントをリセット（Emissionはそのまま）
    /// </summary>
    public void ResetCount()
    {
        _currentCount = 0;
        _deactivateCount = 0;
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
        _deactivateCount = 0;
        _isActivated = false;
        SetEmissionIntensity(startIntensity);
        RestoreBaseScales();
        if (showDebugLog)
        {
            Debug.Log("[EmissionFadeOnCount] 全リセット");
        }
    }

    /// <summary>
    /// 受付モード中のヒット処理（1回目/2回目のスケール変更、3回目でリセット）
    /// </summary>
    public void DeactivateEmission()
    {
        if (!_isActivated) return;

        _deactivateCount++;
        if (showDebugLog)
        {
            Debug.Log($"[EmissionFadeOnCount] Deactivateカウント: {_deactivateCount}/3");
        }

        if (_deactivateCount == 1)
        {
            ApplyScalePercent(scalePercent1);
            return;
        }
        if (_deactivateCount == 2)
        {
            ApplyScalePercent(scalePercent2);
            return;
        }

        if (_scaleRoutine != null)
        {
            StopCoroutine(_scaleRoutine);
        }
        _scaleRoutine = StartCoroutine(ScaleAndReset());
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

    private void CacheBaseScales()
    {
        if (scaleTargets == null || scaleTargets.Length == 0)
        {
            _scaleTargetBaseScales = null;
            return;
        }

        _scaleTargetBaseScales = new Vector3[scaleTargets.Length];
        for (int i = 0; i < scaleTargets.Length; i++)
        {
            _scaleTargetBaseScales[i] = scaleTargets[i] != null
                ? scaleTargets[i].transform.localScale
                : Vector3.one;
        }
    }

    private IEnumerator ScaleAndReset()
    {
        if (scaleTargets == null || scaleTargets.Length == 0)
        {
            // BlendShapeFadeOnContactのResetTriggerを全て実行（ベジェカーブリセット想定）
            if (blendShapeFaders != null)
            {
                foreach (var fader in blendShapeFaders)
                {
                    if (fader != null)
                    {
                        fader.ResetAndRestore();
                    }
                }
                if (showDebugLog)
                {
                    Debug.Log($"[EmissionFadeOnCount] {blendShapeFaders.Length}個のBlendShapeFaderをリセット");
                }
            }

            _currentCount = 0;
            _deactivateCount = 0;
            _isActivated = false;
            StartCoroutine(FadeEmission(maxIntensity, startIntensity));
            _scaleRoutine = null;
            yield break;
        }

        if (_scaleTargetBaseScales == null || _scaleTargetBaseScales.Length != scaleTargets.Length)
        {
            CacheBaseScales();
        }

        Vector3[] fromScales = new Vector3[scaleTargets.Length];
        for (int i = 0; i < scaleTargets.Length; i++)
        {
            var obj = scaleTargets[i];
            fromScales[i] = obj != null ? obj.transform.localScale : Vector3.one;
        }

        if (scaleToZeroDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < scaleToZeroDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaleToZeroDuration);
                if (useEasing)
                {
                    t = t * t * (3f - 2f * t);
                }
                for (int i = 0; i < scaleTargets.Length; i++)
                {
                    var obj = scaleTargets[i];
                    if (obj == null) continue;
                    obj.transform.localScale = Vector3.LerpUnclamped(fromScales[i], _scaleTargetBaseScales[i] * 0f, t);
                }
                yield return null;
            }
        }
        ApplyScalePercent(0f);

        // BlendShapeFadeOnContactのResetTriggerを全て実行（ベジェカーブリセット想定）
        if (blendShapeFaders != null)
        {
            foreach (var fader in blendShapeFaders)
            {
                if (fader != null)
                {
                    fader.ResetAndRestore();
                }
            }
            if (showDebugLog)
            {
                Debug.Log($"[EmissionFadeOnCount] {blendShapeFaders.Length}個のBlendShapeFaderをリセット");
            }
        }

        TriggerBossEmissionPulse();

        if (scaleZeroHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(scaleZeroHoldSeconds);
        }

        if (scaleToResetDuration > 0f)
        {
            float elapsed = 0f;
            Vector3[] toScales = new Vector3[scaleTargets.Length];
            float ratio = scalePercent3Reset * 0.01f;
            for (int i = 0; i < scaleTargets.Length; i++)
            {
                toScales[i] = _scaleTargetBaseScales[i] * ratio;
            }

            while (elapsed < scaleToResetDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaleToResetDuration);
                if (useEasing)
                {
                    t = t * t * (3f - 2f * t);
                }
                for (int i = 0; i < scaleTargets.Length; i++)
                {
                    var obj = scaleTargets[i];
                    if (obj == null) continue;
                    obj.transform.localScale = Vector3.LerpUnclamped(_scaleTargetBaseScales[i] * 0f, toScales[i], t);
                }
                yield return null;
            }
        }
        ApplyScalePercent(scalePercent3Reset);

        _currentCount = 0;
        _deactivateCount = 0;
        _isActivated = false;
        StartCoroutine(FadeEmission(maxIntensity, startIntensity));
        _scaleRoutine = null;
    }

    private void ApplyScalePercent(float percent)
    {
        if (_scaleTargetBaseScales == null) return;
        float ratio = percent * 0.01f;

        for (int i = 0; i < scaleTargets.Length; i++)
        {
            var obj = scaleTargets[i];
            if (obj == null) continue;
            obj.transform.localScale = _scaleTargetBaseScales[i] * ratio;
        }
    }

    private void RestoreBaseScales()
    {
        if (_scaleTargetBaseScales == null) return;
        for (int i = 0; i < scaleTargets.Length; i++)
        {
            var obj = scaleTargets[i];
            if (obj == null) continue;
            obj.transform.localScale = _scaleTargetBaseScales[i];
        }
    }

    private void TriggerBossEmissionPulse()
    {
        if (bossRenderers == null || bossRenderers.Length == 0) return;
        if (_bossPulseRoutine != null)
        {
            StopCoroutine(_bossPulseRoutine);
        }
        _bossPulseRoutine = StartCoroutine(PulseBossEmission());
    }

    private IEnumerator PulseBossEmission()
    {
        var materials = new System.Collections.Generic.List<Material>();
        var original = new System.Collections.Generic.List<Color>();

        foreach (var r in bossRenderers)
        {
            if (r == null) continue;
            if (bossMaterialIndex < r.materials.Length)
            {
                var m = r.materials[bossMaterialIndex];
                if (m == null) continue;
                m.EnableKeyword("_EMISSION");
                materials.Add(m);
                original.Add(m.GetColor(EmissionColorID));
            }
        }

        if (materials.Count == 0) yield break;

        Color targetColor = Color.red * bossEmissionIntensity;

        float elapsed = 0f;
        float up = Mathf.Max(0f, bossPulseUpSeconds);
        while (elapsed < up)
        {
            elapsed += Time.deltaTime;
            float t = up > 0f ? Mathf.Clamp01(elapsed / up) : 1f;
            for (int i = 0; i < materials.Count; i++)
            {
                materials[i].SetColor(EmissionColorID, Color.LerpUnclamped(original[i], targetColor, t));
            }
            yield return null;
        }

        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].SetColor(EmissionColorID, targetColor);
        }

        elapsed = 0f;
        float down = Mathf.Max(0f, bossPulseDownSeconds);
        while (elapsed < down)
        {
            elapsed += Time.deltaTime;
            float t = down > 0f ? Mathf.Clamp01(elapsed / down) : 1f;
            for (int i = 0; i < materials.Count; i++)
            {
                materials[i].SetColor(EmissionColorID, Color.LerpUnclamped(targetColor, original[i], t));
            }
            yield return null;
        }

        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].SetColor(EmissionColorID, original[i]);
        }

        _bossPulseRoutine = null;
    }

    private void SetEmissionIntensity(float intensity)
    {
        if (_materials == null || _materials.Length == 0) return;
        Color finalColor = emissionColor * intensity;
        foreach (var m in _materials)
        {
            if (m != null)
            {
                m.SetColor(EmissionColorID, finalColor);
            }
        }
    }
}
