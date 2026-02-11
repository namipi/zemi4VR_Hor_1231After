using UnityEngine;

/// <summary>
/// 当たった相手にDieアニメーションを発動して破棄するスクリプト
/// プレイヤーなどにアタッチして使用
/// </summary>
public class DieOnPlayerContact : MonoBehaviour
{
    [Header("ターゲット判定")]
    [Tooltip("対象のタグリスト")]
    public string[] targetTags = { "zako" };

    [Tooltip("対象の名前リスト（含むかどうか）")]
    public string[] targetNames = { "Chuboss" };

    [Header("死亡設定")]
    [Tooltip("Destroyまでの遅延時間")]
    public float destroyDelay = 2.4f;

    [Tooltip("Animatorのトリガー名")]
    public string dieTriggerName = "Die";

    [Header("ヒット時クローン設定")]
    [Tooltip("ヒット時に生成するPrefab（未設定なら生成しない）")]
    public GameObject cloneOnHit;

    [Tooltip("ヒット時クローンのDestroyまでの秒数")]
    public float cloneDestroyDelay = 2f;

    [Header("ヒット時Emission設定")]
    [Tooltip("Emissionを赤く光らせるまでの秒数")]
    public float emissionFadeDuration = 0.5f;

    [Tooltip("Emissionの赤色強度")]
    public float emissionRedIntensity = 2f;

    [Header("OSC設定")]
    [Tooltip("OSC送信用")]
    public SendOSC sendOSC;

    [Tooltip("Chuboss撃破時に送信するOSCアドレス")]
    public string chubossOscAddress = "/cue/call/KillChuboss";

    [Header("有効/無効")]
    public bool isEnabled = true;

    #region 有効/無効制御

    public void Enable() => isEnabled = true;
    public void Disable() => isEnabled = false;
    public void SetEnabled(bool enabled) => isEnabled = enabled;

    #endregion

    void OnTriggerEnter(Collider other)
    {
        if (!isEnabled) return;

        if (IsTarget(other.gameObject))
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            HandleTargetDeath(other.gameObject, hitPoint, transform.forward);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isEnabled) return;

        if (IsTarget(collision.gameObject))
        {
            Vector3 hitPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : collision.transform.position;
            HandleTargetDeath(collision.gameObject, hitPoint, transform.forward);
        }
    }

    private bool IsTarget(GameObject obj)
    {
        // タグチェック
        foreach (string t in targetTags)
        {
            if (obj.CompareTag(t)) return true;
        }
        // 名前チェック（含むかどうか）
        foreach (string n in targetNames)
        {
            if (obj.name.Contains(n)) return true;
        }
        return false;
    }

    private void HandleTargetDeath(GameObject target, Vector3 hitPoint, Vector3 hitForward)
    {
        Animator animator = target.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger(dieTriggerName);
        }

        if (emissionFadeDuration > 0f)
        {
            StartCoroutine(FadeEmissionToRed(target, emissionFadeDuration, emissionRedIntensity));
        }

        if (cloneOnHit != null)
        {
            Vector3 opposite = -hitForward;
            Quaternion rotation = opposite.sqrMagnitude > 0f
                ? Quaternion.LookRotation(opposite.normalized, Vector3.up)
                : Quaternion.identity;
            GameObject clone = Instantiate(cloneOnHit, hitPoint, rotation);
            Destroy(clone, cloneDestroyDelay);
        }

        // Chubossの場合はOSCを送信
        if (target.name.Contains("Chuboss") && sendOSC != null)
        {
            sendOSC.SendOsc(chubossOscAddress);
            Debug.Log($"[DieOnPlayerContact] OSC送信: {chubossOscAddress}");
        }

        Destroy(target, destroyDelay);
        Debug.Log($"[DieOnPlayerContact] {target.name} - Die triggered, destroying in {destroyDelay}s");
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
