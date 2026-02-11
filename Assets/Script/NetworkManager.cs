using UnityEngine;
using OscJack;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// OSC communication manager singleton.
/// </summary>
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private NetworkConfig config;
    [SerializeField] private bool useExternalTextConfig = true;
    [SerializeField] private string externalConfigFileName = "NetworkConfig.txt";

    [Header("References")]
    [SerializeField] private PlayerTracker playerTracker;

    [Header("Send Control")]
    [Tooltip("Enable or disable OSC send")]
    public bool sendEnabled = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    private OscClient _client;
    private string _devicePath;
    private float _lastSendTime;
    private string _targetIP;
    private int _sendPort;
    private float _sendInterval;

    private const string DefaultTargetIP = "127.0.0.1";
    private const int DefaultSendPort = 17200;
    private const float DefaultSendInterval = 0.033f;

    public OscClient Client => _client;
    public string DevicePath => _devicePath;
    public string TargetIP => _targetIP;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureExternalConfigFileExists();
        ResolveRuntimeConfig();
        InitializeNetwork();
    }

    void Update()
    {
        if (_client == null || playerTracker == null) return;
        if (!sendEnabled) return;
        if (Time.time - _lastSendTime < _sendInterval) return;

        SendPlayerData();
        _lastSendTime = Time.time;
    }

    void OnDestroy()
    {
        _client?.Dispose();
        if (Instance == this) Instance = null;
    }

    #region Send Control

    public void EnableSend() => sendEnabled = true;
    public void DisableSend() => sendEnabled = false;
    public void SetSendEnabled(bool enabled) => sendEnabled = enabled;

    #endregion

    #region Network Init

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
        EnsureExternalConfigFileExists();
        ResolveRuntimeConfig();

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

        _client = new OscClient(_targetIP, _sendPort);
        Debug.Log($"[NetworkManager] Initialized - Target: {_targetIP}:{_sendPort}");

        using var broadcast = new OscClient("255.255.255.255", _sendPort);
        broadcast.Send("/setAddress/VRnotrame", _devicePath?.TrimStart('/') ?? "unknown");
    }

    private void ResolveRuntimeConfig()
    {
        _targetIP = config != null ? config.TargetIP : DefaultTargetIP;
        _sendPort = config != null ? config.SendPort : DefaultSendPort;
        _sendInterval = config != null ? config.SendInterval : DefaultSendInterval;

        if (!useExternalTextConfig) return;

        string configPath = GetExistingExternalConfigPath();
        if (string.IsNullOrEmpty(configPath)) return;

        bool targetResolved = false;

        try
        {
            foreach (var rawLine in File.ReadAllLines(configPath))
            {
                if (string.IsNullOrWhiteSpace(rawLine)) continue;

                string line = rawLine.Trim();
                if (line.StartsWith("#")) continue;

                if (TryParseKeyValue(line, out string key, out string value))
                {
                    if (!targetResolved && (key == "target_ip" || key == "targetip"))
                    {
                        if (TryParseIPv4(value, out string ip))
                        {
                            _targetIP = ip;
                            targetResolved = true;
                        }
                        continue;
                    }

                    if (key == "send_port" || key == "sendport")
                    {
                        if (int.TryParse(value, out int port) && port > 0 && port <= 65535)
                        {
                            _sendPort = port;
                        }
                        continue;
                    }

                    if (key == "send_interval" || key == "sendinterval")
                    {
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float interval) && interval > 0f)
                        {
                            _sendInterval = interval;
                        }
                        continue;
                    }
                }

                if (!targetResolved && TryParseIPv4(line, out string plainIp))
                {
                    _targetIP = plainIp;
                    targetResolved = true;
                }
            }

            if (enableDebugLog) Debug.Log($"[NetworkManager] Loaded external config: {configPath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[NetworkManager] Failed to read external config ({configPath}): {ex.Message}");
        }
    }

    private static bool TryParseKeyValue(string line, out string key, out string value)
    {
        key = null;
        value = null;

        int idx = line.IndexOf('=');
        if (idx <= 0 || idx >= line.Length - 1) return false;

        key = line.Substring(0, idx).Trim().ToLowerInvariant();
        value = line.Substring(idx + 1).Trim();
        return true;
    }

    private static bool TryParseIPv4(string input, out string ipv4)
    {
        ipv4 = null;
        if (!IPAddress.TryParse(input, out IPAddress parsed)) return false;
        if (parsed.AddressFamily != AddressFamily.InterNetwork) return false;

        ipv4 = parsed.ToString();
        return true;
    }

    private string GetExistingExternalConfigPath()
    {
        if (string.IsNullOrWhiteSpace(externalConfigFileName)) return null;

        string rootPath = GetProjectRootConfigPath();
        string persistentPath = Path.Combine(Application.persistentDataPath, externalConfigFileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (File.Exists(persistentPath)) return persistentPath;
        if (File.Exists(rootPath)) return rootPath;
#else
        if (File.Exists(rootPath)) return rootPath;
        if (File.Exists(persistentPath)) return persistentPath;
#endif

        return null;
    }

    private void EnsureExternalConfigFileExists()
    {
        if (!useExternalTextConfig) return;
        if (string.IsNullOrWhiteSpace(externalConfigFileName)) return;

        string primaryPath = GetPrimaryConfigPathForCurrentPlatform();
        if (File.Exists(primaryPath)) return;

        string content =
            "# First valid IP line is used as OSC target\n" +
            "# You can use either plain IP or key=value format\n" +
            "# Example: target_ip=192.168.1.10\n" +
            "192.168.10.115\n";

        try
        {
            string dir = Path.GetDirectoryName(primaryPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(primaryPath, content, new UTF8Encoding(false));

            if (enableDebugLog)
            {
                Debug.Log($"[NetworkManager] Generated external config: {primaryPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[NetworkManager] Failed to generate external config ({primaryPath}): {ex.Message}");
        }
    }

    private string GetPrimaryConfigPathForCurrentPlatform()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Path.Combine(Application.persistentDataPath, externalConfigFileName);
#else
        return GetProjectRootConfigPath();
#endif
    }

    private string GetProjectRootConfigPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", externalConfigFileName));
    }

    #endregion

    #region Data Send

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