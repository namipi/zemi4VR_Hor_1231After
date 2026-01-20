using UnityEngine;
using OscJack;
using System.Net;
using System.Net.Sockets;
using System.Collections;

/// <summary>
/// OSC通信を統合管理するシングルトン
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
    [SerializeField] private bool enableTestSend = true;

    // 内部状態
    private OscClient _client;
    private Coroutine _testCoroutine;

    private string _currentTargetIP;
    private string _devicePath;
    private float _lastSendTime;

    public OscClient Client => _client;
    public string DevicePath => _devicePath;

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

    void Start()
    {

    }

    public void EnableSend()
    {
        sendEnabled = true;
    }

    /// <summary>
    /// 送信を無効にする
    /// </summary>
    public void DisableSend()
    {
        sendEnabled = false;
    }

    /// <summary>
    /// 送信の有効/無効を設定
    /// </summary>
    public void SetSendEnabled(bool enabled)
    {
        sendEnabled = enabled;
    }


    void OnDestroy()
    {
        _client?.Dispose();
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// アプリがフォーカスを取得/失った時に呼ばれる
    /// スリープ復帰時の再接続に使用
    /// </summary>
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Debug.Log("[NetworkManager] Application gained focus - reinitializing network...");
            ReinitializeNetwork();
        }
    }

    /// <summary>
    /// アプリが一時停止/再開した時に呼ばれる
    /// スリープ復帰時の再接続に使用
    /// </summary>
    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            Debug.Log("[NetworkManager] Application resumed from pause - reinitializing network...");
            ReinitializeNetwork();
        }
    }

    /// <summary>
    /// ネットワークを再初期化する
    /// </summary>
    public void ReinitializeNetwork()
    {
        // 既存のクライアントを破棄
        _client?.Dispose();
        _client = null;

        // 少し遅延してから再初期化（ネットワークインターフェースの復帰を待つ）
        StartCoroutine(DelayedNetworkInit());
    }

    private IEnumerator DelayedNetworkInit()
    {
        // ネットワークインターフェースの復帰を待つ
        yield return new WaitForSeconds(0.5f);

        InitializeNetwork();

        // 以前のターゲットIPがあれば再設定
        if (!string.IsNullOrEmpty(_currentTargetIP))
        {
            string savedIP = _currentTargetIP;
            _currentTargetIP = null; // SetTargetが動作するようにリセット
            SetTarget(savedIP);
        }

        Debug.Log("[NetworkManager] Network reinitialized successfully");
    }

    // 固定送信先IP
    private const string FIXED_TARGET_IP = "192.168.181.204";

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

        // OSCクライアント初期化（固定IPに送信）
        _client = new OscClient(FIXED_TARGET_IP, config.SendPort);
        _currentTargetIP = FIXED_TARGET_IP;
        Debug.Log($"[NetworkManager] Initialized with fixed target IP: {FIXED_TARGET_IP}");

        // ブロードキャストで自身を通知
        using var broadcast = new OscClient("255.255.255.255", config.SendPort);
        broadcast.Send("/setAddress/VRnotrame", _devicePath?.TrimStart('/') ?? "unknown");
    }

    private void SendPlayerData()
    {
        var data = playerTracker.GetTrackingData();

        // 位置データ（小数点第4位まで）
        string position =
            $"{data.headPos.x:F4}#{data.headPos.y:F4}#{data.headPos.z:F4}@" +
            $"{data.leftHandPos.x:F4}#{data.leftHandPos.y:F4}#{data.leftHandPos.z:F4}@" +
            $"{data.rightHandPos.x:F4}#{data.rightHandPos.y:F4}#{data.rightHandPos.z:F4}";

        // 回転データ（小数点第4位まで）
        string rotation =
            $"{data.headRot.x:F4}#{data.headRot.y:F4}#{data.headRot.z:F4}#{data.headRot.w:F4}@" +
            $"{data.leftHandRot.x:F4}#{data.leftHandRot.y:F4}#{data.leftHandRot.z:F4}#{data.leftHandRot.w:F4}@" +
            $"{data.rightHandRot.x:F4}#{data.rightHandRot.y:F4}#{data.rightHandRot.z:F4}#{data.rightHandRot.w:F4}";

        // 位置と回転を%で結合して送信
        Debug.Log(_currentTargetIP);
        _client.Send("/VRnotrame/transform", $"{position}%{rotation}");
    }

    /// <summary>
    /// 実行時に接続先を変更
    /// </summary>
    public void SetTarget(string ip)
    {
        // IPが変わっていない場合は何もしない
        if (_currentTargetIP == ip)
        {
            return;
        }

        Debug.Log($"[NetworkManager] SetTarget: {ip}");

        _client?.Dispose();
        _client = new OscClient(ip, config.SendPort);
        _currentTargetIP = ip;
    }

    /// <summary>
    /// /Direct/VRFrame に "Trigger" をブロードキャストで送信
    /// </summary>
    public void BroadcastTriggerToVRFrame()
    {
        using var broadcast = new OscClient("255.255.255.255", 20005);
        broadcast.Send("/Direct/VRrame", "Trigger");
        Debug.Log("[NetworkManager] Broadcast: /Direct/VRFrame Trigger");
    }

    /// <summary>
    /// /Direct/Windows に "Trigger" をブロードキャストで送信
    /// </summary>
    public void BroadcastTriggerToWindows()
    {
        using var broadcast = new OscClient("255.255.255.255", 20002);
        broadcast.Send("/Direct/Windows", "Trigger");
        Debug.Log("[NetworkManager] Broadcast: /Direct/Windows Trigger");
    }
}
