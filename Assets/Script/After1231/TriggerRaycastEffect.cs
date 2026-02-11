using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// トリガーを引くと進行方向にRaycastを飛ばし、ヒット地点にエフェクトを生成
/// </summary>
public class TriggerRaycastEffect : MonoBehaviour
{
    [Header("有効/無効")]
    [Tooltip("この機能を有効にするか")]
    public bool isEnabled = true;

    [Header("エフェクト設定")]
    [Tooltip("ヒット時に生成するエフェクトPrefab")]
    public GameObject effectPrefab;
    
    [Header("Zakoヒット時クローン設定")]
    [Tooltip("Zakoヒット時に生成するPrefab（未設定なら生成しない）")]
    public GameObject cloneOnZakoHit;

    [Tooltip("Zakoヒット時クローンのDestroyまでの秒数")]
    public float cloneDestroyDelay = 2f;

    [Header("Zakoヒット時Emission設定")]
    [Tooltip("Emissionを赤く光らせるまでの秒数")]
    public float emissionFadeDuration = 0.5f;

    [Tooltip("Emissionの赤色強度")]
    public float emissionRedIntensity = 2f;

    [Header("Raycast設定")]
    [Tooltip("Raycastの最大距離")]
    public float maxDistance = 100f;

    [Tooltip("ヒット対象のレイヤー")]
    public LayerMask hitLayers = ~0; // デフォルトは全レイヤー

    [Header("コントローラー設定")]
    [Tooltip("右手を使用（falseで左手）")]
    public bool useRightHand = true;

    [Header("Raycast発射元")]
    [Tooltip("Raycast発射元Transform（未設定ならこのオブジェクト）")]
    public Transform rayOrigin;

    [Header("デバッグ")]
    public bool showDebugRay = true;

    // 入力デバイス
    private InputDevice _controller;
    private bool _wasTriggered = false;

    void Start()
    {
        if (rayOrigin == null)
        {
            rayOrigin = transform;
        }
    }

    #region 有効/無効制御

    public void Enable() => isEnabled = true;
    public void Disable() => isEnabled = false;
    public void SetEnabled(bool enabled) => isEnabled = enabled;

    #endregion

    void Update()
    {
        if (!isEnabled) return;
        if (effectPrefab == null) return;

        // コントローラー取得
        UpdateController();

        // トリガー入力チェック
        if (_controller.isValid && _controller.TryGetFeatureValue(CommonUsages.triggerButton, out bool isTriggered))
        {
            // トリガーが押された瞬間のみ実行
            if (isTriggered && !_wasTriggered)
            {
                ShootRaycast();
            }
            _wasTriggered = isTriggered;
        }
    }

    private void UpdateController()
    {
        if (!_controller.isValid)
        {
            var characteristics = useRightHand
                ? InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller
                : InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;

            var devices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);

            if (devices.Count > 0)
            {
                _controller = devices[0];
            }
        }
    }

    private void ShootRaycast()
    {
        Vector3 origin = rayOrigin.position;
        Vector3 direction = rayOrigin.forward;

        // デバッグ表示
        if (showDebugRay)
        {
            Debug.DrawRay(origin, direction * maxDistance, Color.red, 1f);
        }

        // Raycast実行
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, hitLayers))
        {
            // エフェクト生成
            GameObject effect = Instantiate(effectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(effect, 2f);
            Debug.Log($"[TriggerRaycastEffect] Hit: {hit.collider.name} at {hit.point}");

            // zakoタグなら死亡処理
            if (hit.collider.CompareTag("zako"))
            {
                HandleZakoHit(hit.collider.gameObject, hit.point, rayOrigin.forward);
            }

            // Coreという名前ならEmission解除
            if (hit.collider.name.Contains("Core"))
            {
                HandleCoreHit(hit.collider.gameObject);
            }
        }
    }

    private void HandleCoreHit(GameObject core)
    {
        EmissionFadeOnCount emissionFade = core.GetComponent<EmissionFadeOnCount>();
        if (emissionFade != null)
        {
            emissionFade.DeactivateEmission();
            Debug.Log($"[TriggerRaycastEffect] Core hit: {core.name} - DeactivateEmission called");
        }
    }

    private void HandleZakoHit(GameObject zako, Vector3 hitPoint, Vector3 hitForward)
    {
        Animator animator = zako.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        if (emissionFadeDuration > 0f)
        {
            StartCoroutine(FadeEmissionToRed(zako, emissionFadeDuration, emissionRedIntensity));
        }

        if (cloneOnZakoHit != null)
        {
            Vector3 opposite = -hitForward;
            Quaternion rotation = opposite.sqrMagnitude > 0f
                ? Quaternion.LookRotation(opposite.normalized, Vector3.up)
                : Quaternion.identity;
            GameObject clone = Instantiate(cloneOnZakoHit, hitPoint, rotation);
            Destroy(clone, cloneDestroyDelay);
        }
        Destroy(zako, 2.4f);
        Debug.Log($"[TriggerRaycastEffect] Zako hit: {zako.name} - Die triggered, destroying in 1s");
    }

    private System.Collections.IEnumerator FadeEmissionToRed(GameObject target, float duration, float intensity)
    {
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) yield break;

        var materials = new System.Collections.Generic.List<Material>();
        var original = new System.Collections.Generic.List<Color>();

        foreach (var r in renderers)
        {
            var mats = r.materials;
            foreach (var m in mats)
            {
                if (m == null) continue;
                m.EnableKeyword("_EMISSION");
                materials.Add(m);
                original.Add(m.GetColor("_EmissionColor"));
            }
        }

        if (materials.Count == 0) yield break;

        Color targetColor = Color.red * intensity;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            for (int i = 0; i < materials.Count; i++)
            {
                materials[i].SetColor("_EmissionColor", Color.LerpUnclamped(original[i], targetColor, k));
            }
            yield return null;
        }

        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].SetColor("_EmissionColor", targetColor);
        }
    }
}
