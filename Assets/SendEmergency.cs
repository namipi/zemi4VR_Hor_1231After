using UnityEngine;
using OscJack;

/// <summary>
/// 自分の座標と回転を基準として、各トラッキングデータの差分を送信するスクリプト
/// Screenの時と同様のロジックで相対座標・相対回転を計算する
/// </summary>
public class SendEmergency : MonoBehaviour
{
    [Header("追跡対象（未設定時は自動取得）")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("自動取得用の名前")]
    [SerializeField] private string headName = "CenterEyeAnchor";
    [SerializeField] private string leftHandName = "LeftHandAnchor";
    [SerializeField] private string rightHandName = "RightHandAnchor";

    [Header("送信設定")]
    [Tooltip("OSCの送信先アドレス")]
    [SerializeField] private string oscAddress = "/emergency/transform";

    [Tooltip("座標のスケール係数（PlayerTrackerと同じく2倍）")]
    [SerializeField] private float positionScale = 2f;

    [Header("自動送信設定（オプション）")]
    [Tooltip("有効にすると指定間隔で自動送信")]
    [SerializeField] private bool autoSend = false;

    [Tooltip("自動送信の間隔（秒）")]
    [SerializeField] private float sendInterval = 0.1f;

    private float _lastSendTime;

    void Start()
    {
        // 未設定のTransformを名前で自動取得
        if (head == null && !string.IsNullOrEmpty(headName))
        {
            var go = GameObject.Find(headName);
            if (go != null) head = go.transform;
        }
        if (leftHand == null && !string.IsNullOrEmpty(leftHandName))
        {
            var go = GameObject.Find(leftHandName);
            if (go != null) leftHand = go.transform;
        }
        if (rightHand == null && !string.IsNullOrEmpty(rightHandName))
        {
            var go = GameObject.Find(rightHandName);
            if (go != null) rightHand = go.transform;
        }

        Debug.Log($"[SendEmergency] head={head?.name}, left={leftHand?.name}, right={rightHand?.name}");
    }

    void Update()
    {
        if (!autoSend) return;
        if (Time.time - _lastSendTime < sendInterval) return;

        Send();
        _lastSendTime = Time.time;
    }

    /// <summary>
    /// 自動送信を有効にする
    /// </summary>
    public void EnableAutoSend()
    {
        autoSend = true;
    }

    /// <summary>
    /// 自動送信を無効にする
    /// </summary>
    public void DisableAutoSend()
    {
        autoSend = false;
    }

    /// <summary>
    /// 自動送信の有効/無効を設定
    /// </summary>
    public void SetAutoSendEnabled(bool enabled)
    {
        autoSend = enabled;
    }

    /// <summary>
    /// 現在のトラッキングデータを自分基準の相対座標・回転で送信
    /// 外部からUnityEventなどで呼び出し可能
    /// </summary>
    public void Send()
    {
        if (NetworkManager.Instance?.Client == null) return;
        if (head == null || leftHand == null || rightHand == null) return;

        // 自分の位置・回転を基準とした相対座標・回転を取得
        Vector3 headPos = ToLocal(head.position);
        Vector3 leftPos = ToLocal(leftHand.position);
        Vector3 rightPos = ToLocal(rightHand.position);

        Quaternion headRot = ToLocalRotation(head.rotation);
        Quaternion leftRot = ToLocalRotation(leftHand.rotation);
        Quaternion rightRot = ToLocalRotation(rightHand.rotation);

        // 位置データ（小数点第4位まで）
        string position =
            $"{headPos.x:F4}#{headPos.y:F4}#{headPos.z:F4}@" +
            $"{leftPos.x:F4}#{leftPos.y:F4}#{leftPos.z:F4}@" +
            $"{rightPos.x:F4}#{rightPos.y:F4}#{rightPos.z:F4}";

        // 回転データ（小数点第4位まで）
        string rotation =
            $"{headRot.x:F4}#{headRot.y:F4}#{headRot.z:F4}#{headRot.w:F4}@" +
            $"{leftRot.x:F4}#{leftRot.y:F4}#{leftRot.z:F4}#{leftRot.w:F4}@" +
            $"{rightRot.x:F4}#{rightRot.y:F4}#{rightRot.z:F4}#{rightRot.w:F4}";

        // 位置と回転を%で結合して送信
        NetworkManager.Instance.Client.Send(oscAddress, $"{position}%{rotation}");
    }

    /// <summary>
    /// ワールド座標を自分基準のローカル座標に変換
    /// </summary>
    private Vector3 ToLocal(Vector3 worldPos)
        => transform.InverseTransformPoint(worldPos) * positionScale;

    /// <summary>
    /// ワールド回転を自分基準のローカル回転に変換
    /// </summary>
    private Quaternion ToLocalRotation(Quaternion worldRot)
        => Quaternion.Inverse(transform.rotation) * worldRot;
}
