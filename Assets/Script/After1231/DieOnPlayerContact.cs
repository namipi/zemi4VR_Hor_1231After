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
            HandleTargetDeath(other.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isEnabled) return;

        if (IsTarget(collision.gameObject))
        {
            HandleTargetDeath(collision.gameObject);
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

    private void HandleTargetDeath(GameObject target)
    {
        Animator animator = target.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger(dieTriggerName);
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
}
