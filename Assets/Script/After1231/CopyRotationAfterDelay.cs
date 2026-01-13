using UnityEngine;

/// <summary>
/// Startから指定時間後に、名前で検索したオブジェクトのY軸ローカル回転を自分のオブジェクトにコピーするスクリプト
/// </summary>
public class CopyRotationAfterDelay : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("回転をコピーする元のオブジェクト名")]
    public string targetObjectName;

    [Tooltip("コピーするまでの遅延時間（秒）")]
    public float delay = 0.5f;

    private void Start()
    {
        Invoke(nameof(FindAndCopyRotation), delay);
    }

    /// <summary>
    /// オブジェクトを検索し、見つかったら親のY軸回転をコピーする
    /// </summary>
    private void FindAndCopyRotation()
    {
        if (string.IsNullOrEmpty(targetObjectName))
        {
            Debug.LogWarning($"[CopyRotationAfterDelay] {gameObject.name}: ターゲットオブジェクト名が設定されていません。");
            return;
        }

        if (transform.parent == null)
        {
            Debug.LogWarning($"[CopyRotationAfterDelay] {gameObject.name}: 親オブジェクトがありません。");
            return;
        }

        GameObject targetObject = GameObject.Find(targetObjectName);

        if (targetObject != null)
        {
            // ターゲットのワールドY回転を取得
            float targetWorldY = targetObject.transform.eulerAngles.y;

            // 親のワールドY回転を取得
            float parentWorldY = transform.parent.eulerAngles.y;

            // 自分のローカルY回転 = ターゲットのワールドY - 親のワールドY
            float selfLocalY = targetWorldY - parentWorldY;

            // 現在の自分のローカル回転を保持しつつ、Y軸のみ変更
            Vector3 currentEuler = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(currentEuler.x, selfLocalY, currentEuler.z);

            Debug.Log($"[CopyRotationAfterDelay] {gameObject.name}: ターゲットワールドY={targetWorldY}, 親ワールドY={parentWorldY}, 自分ローカルY={selfLocalY}");
        }
        else
        {
            Debug.LogWarning($"[CopyRotationAfterDelay] {gameObject.name}: '{targetObjectName}'が見つかりませんでした。");
        }
    }
}
