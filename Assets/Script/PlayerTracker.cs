
using UnityEngine;

/// <summary>
/// プレイヤーの位置・回転を追跡
/// </summary>
public class PlayerTracker : MonoBehaviour
{
    [Header("追跡対象")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("基準座標系（オプション）")]
    [SerializeField] private Transform referenceFrame;

    [Header("受信データ適用先")]
    public GameObject targetObject;

    [Tooltip("targetObjectが未設定の場合、この名前でFindする")]
    public string targetObjectName = "Poppins";

    [Header("スムージング設定")]
    [Tooltip("スムージング時間（ミリ秒）")]
    [SerializeField] private float smoothTimeMs = 100f;

    [Header("ブレンド設定")]
    [Tooltip("PlayerTrackerの適用率 (0=デフォルト位置, 0.5=50%ブレンド, 1=PlayerTracker100%)")]
    [Range(0f, 1f)]
    public float blendRatio = 0f;

    // スムージング用の内部変数
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private Vector3 _velocityPosition;
    private bool _hasTarget = false;

    // デフォルト位置・回転を保存
    private Vector3 _defaultLocalPosition;
    private Quaternion _defaultLocalRotation;
    private bool _defaultCaptured = false;

    /// <summary>
    /// トラッキングデータ構造体
    /// </summary>
    public readonly struct TrackingData
    {
        public readonly Vector3 headPos;
        public readonly Vector3 leftHandPos;
        public readonly Vector3 rightHandPos;
        public readonly Quaternion headRot;
        public readonly Quaternion leftHandRot;
        public readonly Quaternion rightHandRot;

        public TrackingData(Vector3 head, Vector3 left, Vector3 right, Quaternion hRot, Quaternion leftRot, Quaternion rightRot)
        {
            headPos = head;
            leftHandPos = left;
            rightHandPos = right;
            headRot = hRot;
            leftHandRot = leftRot;
            rightHandRot = rightRot;
        }
    }

    /// <summary>
    /// 現在のトラッキングデータを取得
    /// </summary>
    public TrackingData GetTrackingData()
    {
        if (referenceFrame != null)
        {
            return new TrackingData(
                ToLocal(head.position),
                ToLocal(leftHand.position),
                ToLocal(rightHand.position),
                ToLocalRotation(head.rotation),
                ToLocalRotation(leftHand.rotation),
                ToLocalRotation(rightHand.rotation)
            );
        }

        return new TrackingData(
            head.position,
            leftHand.position,
            rightHand.position,
            head.rotation,
            leftHand.rotation,
            rightHand.rotation
        );
    }

    private Vector3 ToLocal(Vector3 worldPos)
        => referenceFrame.InverseTransformPoint(worldPos) * 2f;

    private Quaternion ToLocalRotation(Quaternion worldRot)
        => Quaternion.Inverse(referenceFrame.rotation) * worldRot;

    /// <summary>
    /// 外部から基準座標系を設定
    /// </summary>
    public void SetReferenceFrame(Transform frame)
    {
        referenceFrame = frame;
        Debug.Log($"[PlayerTracker] ReferenceFrame set to {frame?.name}");
    }

    /// <summary>
    /// 外部からtargetObjectを設定（PlayerIndicatorReferenceから呼ばれる）
    /// </summary>
    public void SetTargetObject(GameObject target)
    {
        targetObject = target;

        if (targetObject != null && referenceFrame != null)
        {
            // デフォルト位置・回転を保存（SetupTargetObjectで位置がリセットされる前に）
            _defaultLocalPosition = targetObject.transform.localPosition;
            _defaultLocalRotation = targetObject.transform.localRotation;
            _defaultCaptured = true;

            SetupTargetObject();
            targetObject.SetActive(false);
            Debug.Log($"[PlayerTracker] PlayerIndicator set as targetObject (default pos: {_defaultLocalPosition})");
        }
        else if (targetObject != null)
        {
            Debug.Log($"[PlayerTracker] PlayerIndicator set, waiting for ReferenceFrame");
        }
    }

    void Update()
    {
        // targetObjectが未設定の場合、名前で検索
        if (targetObject == null && !string.IsNullOrEmpty(targetObjectName))
        {
            var found = GameObject.Find(targetObjectName);
            if (found != null)
            {
                targetObject = found;
                _defaultLocalPosition = targetObject.transform.localPosition;
                _defaultLocalRotation = targetObject.transform.localRotation;
                _defaultCaptured = true;
                Debug.Log($"[PlayerTracker] targetObject '{targetObjectName}' を自動取得 (default pos: {_defaultLocalPosition})");
            }
        }

        if (!_hasTarget || targetObject == null) return;

        // デフォルト位置がまだ保存されていない場合は保存
        if (!_defaultCaptured)
        {
            _defaultLocalPosition = targetObject.transform.localPosition;
            _defaultLocalRotation = targetObject.transform.localRotation;
            _defaultCaptured = true;
        }

        float smoothTime = smoothTimeMs / 1000f; // ミリ秒を秒に変換

        // blendRatioに基づいてデフォルト位置とPlayerTracker位置をブレンド
        Vector3 blendedTargetPosition = Vector3.Lerp(_defaultLocalPosition, _targetPosition, blendRatio);
        Quaternion blendedTargetRotation = Quaternion.Slerp(_defaultLocalRotation, _targetRotation, blendRatio);

        // 位置のスムージング
        targetObject.transform.localPosition = Vector3.SmoothDamp(
            targetObject.transform.localPosition,
            blendedTargetPosition,
            ref _velocityPosition,
            smoothTime
        );

        // 回転のスムージング（Slerpを使用）
        float rotationSpeed = smoothTime > 0f ? Time.deltaTime / smoothTime : 1f;
        targetObject.transform.localRotation = Quaternion.Slerp(
            targetObject.transform.localRotation,
            blendedTargetRotation,
            Mathf.Clamp01(rotationSpeed * 3f) // 回転は少し早めに追従
        );
    }

    /// <summary>
    /// targetObjectをreferenceFrameの子要素に設定
    /// </summary>
    public void SetupTargetObject()
    {
        if (targetObject == null || referenceFrame == null) return;

        targetObject.transform.SetParent(referenceFrame);
        targetObject.transform.localPosition = Vector3.zero;
        targetObject.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 受信した文字列データをパースしてtargetObjectに適用
    /// フォーマット: headX#headY#headZ@leftX#leftY#leftZ@rightX#rightY#rightZ%headRotX#headRotY#headRotZ#headRotW@...
    /// Y座標は0固定、回転はY軸のみ適用
    /// </summary>
    public void ApplyReceivedTransform(string transformData)
    {
        Debug.Log(targetObject);
        Debug.Log(referenceFrame);
        if (targetObject == null || referenceFrame == null || string.IsNullOrEmpty(transformData)) return;
        try
        {
            // 位置と回転を%で分割
            string[] posRotSplit = transformData.Split('%');
            if (posRotSplit.Length < 2) return;

            string positionData = posRotSplit[0];
            string rotationData = posRotSplit[1];

            // 位置データを@で分割（head, leftHand, rightHand）
            string[] positions = positionData.Split('@');
            if (positions.Length < 1) return;

            // 頭の位置データを#で分割
            string[] headPosValues = positions[0].Split('#');
            if (headPosValues.Length < 3) return;

            float headX = float.Parse(headPosValues[0]);
            float headZ = float.Parse(headPosValues[2]);

            // Y座標は0に固定
            Vector3 localPos = new Vector3(-headX, 0f, headZ);

            // 回転データを@で分割（head, leftHand, rightHand）
            string[] rotations = rotationData.Split('@');
            if (rotations.Length < 1) return;

            // 頭の回転データを#で分割（x, y, z, w）
            string[] headRotValues = rotations[0].Split('#');
            if (headRotValues.Length < 4) return;

            float rotX = float.Parse(headRotValues[0]);
            float rotY = float.Parse(headRotValues[1]);
            float rotZ = float.Parse(headRotValues[2]);
            float rotW = float.Parse(headRotValues[3]);

            Quaternion fullRotation = new Quaternion(rotX, rotY, rotZ, rotW);

            // Y軸回転のみを抽出
            Vector3 euler = fullRotation.eulerAngles;
            Quaternion yOnlyRotation = Quaternion.Euler(0f, euler.y, 0f);

            // ターゲット値を設定（実際の適用はUpdate()でスムージングされる）
            _targetPosition = localPos;
            _targetRotation = yOnlyRotation;
            _hasTarget = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerTracker] Transform parse error: {e.Message}");
        }
    }
}
