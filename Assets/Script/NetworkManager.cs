using UnityEngine;
using OscJack;
using System.Net;
using System.Net.Sockets;
using System.Collections;

/// <summary>
/// OSC通信を統合管理するシングルトン（固定IPモード）
/// </summary>
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("設定")]
    [SerializeField] private NetworkConfig config;

    [Header("参照（自動取得可）")]
    [SerializeField] private PlayerTracker playerTracker;

    [Header("送信制御")]
    [Tooltip("送信を有効にするかどうか")]
    public bool sendEnabled = true;

    [Header("デバッグ")]
    [SerializeField] private bool enableDebugLog = false;

    // 内部状態
    private OscClient _client;
    private string _devicePath;
    private float _lastSendTime;

    // プロパティ
    public OscClient Client => _client;
    public string DevicePath => _devicePath;
    public string TargetIP => config.TargetIP;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        InitializeNetwork();
    }

    void Update()
    {
        if (_client == null || playerTracker == null) return;
        if (!sendEnabled) return;
        if (Time.time - _lastSendTime < config.SendInterval) return;

        SendPlayerData();
        _lastSendTime = Time.time;
    }

    void OnDestroy()
    {
        _client?.Dispose();
        if (Instance == this) Instance = null;
    }

    #region 送信制御

    public void EnableSend() => sendEnabled = true;
    public void DisableSend() => sendEnabled = false;
    public void SetSendEnabled(bool enabled) => sendEnabled = enabled;

    #endregion

    #region ネットワーク初期化

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) ReinitializeNetwork();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) ReinitializeNetwork();
    }

    public void ReinitializeNetwork()
    {
        _client?.Dispose();
        _client = null;
        StartCoroutine(DelayedNetworkInit());
    }

    private IEnumerator DelayedNetworkInit()
    {
        yield return new WaitForSeconds(0.5f);
        InitializeNetwork();
        if (enableDebugLog) Debug.Log("[NetworkManager] Network reinitialized");
    }

    public void InitializeNetwork()
    {
        // デバイスIP取得（自身のパス用）
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                string ipString = ip.ToString();
                if (ipString.StartsWith("192.168"))
                {
                    _devicePath = "/" + ipString;
                    break;
                }
            }
        }

        // OSCクライアント初期化
        _client = new OscClient(config.TargetIP, config.SendPort);
        Debug.Log($"[NetworkManager] Initialized - Target: {config.TargetIP}");

        // ブロードキャストで自身を通知
        using var broadcast = new OscClient("255.255.255.255", config.SendPort);
        broadcast.Send("/setAddress/VRnotrame", _devicePath?.TrimStart('/') ?? "unknown");
    }

    #endregion

    #region データ送信

    private void SendPlayerData()
    {
        var data = playerTracker.GetTrackingData();

        string position =
            $"{data.headPos.x:F4}#{data.headPos.y:F4}#{data.headPos.z:F4}@" +
            $"{data.leftHandPos.x:F4}#{data.leftHandPos.y:F4}#{data.leftHandPos.z:F4}@" +
            $"{data.rightHandPos.x:F4}#{data.rightHandPos.y:F4}#{data.rightHandPos.z:F4}";

        string rotation =
            $"{data.headRot.x:F4}#{data.headRot.y:F4}#{data.headRot.z:F4}#{data.headRot.w:F4}@" +
            $"{data.leftHandRot.x:F4}#{data.leftHandRot.y:F4}#{data.leftHandRot.z:F4}#{data.leftHandRot.w:F4}@" +
            $"{data.rightHandRot.x:F4}#{data.rightHandRot.y:F4}#{data.rightHandRot.z:F4}#{data.rightHandRot.w:F4}";

        _client.Send("/VRnotrame/transform", $"{position}%{rotation}");
    }

    public void BroadcastTriggerToVRFrame()
    {
        using var broadcast = new OscClient("255.255.255.255", 20005);
        broadcast.Send("/Direct/VRrame", "Trigger");
        Debug.Log("[NetworkManager] Broadcast: /Direct/VRFrame Trigger");
    }

    public void BroadcastTriggerToWindows()
    {
        using var broadcast = new OscClient("255.255.255.255", 20002);
        broadcast.Send("/Direct/Windows", "Trigger");
        Debug.Log("[NetworkManager] Broadcast: /Direct/Windows Trigger");
    }

    #endregion
}
