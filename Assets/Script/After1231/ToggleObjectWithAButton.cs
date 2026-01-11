using UnityEngine;
using TMPro;
/// <summary>
/// Quest 3のAボタンを押すと指定したGameObjectのアクティブ状態を切り替えるスクリプト
/// </summary>
public class ToggleObjectWithAButton : MonoBehaviour
{
    [Header("トグル対象のオブジェクト")]
    [Tooltip("Aボタンで表示/非表示を切り替えるGameObject")]
    public GameObject targetObject;

    [Header("設定")]
    [Tooltip("初期状態でオブジェクトをアクティブにするか")]
    public bool startActive = true;

    private bool wasButtonPressed = false;

    public TextMeshPro text;

    void Start()
    {
        // 初期状態を設定
        if (targetObject != null)
        {
            targetObject.SetActive(startActive);
        }
    }

    void Update()
    {
        // Aボタンの状態を取得（右コントローラー）
        bool isButtonPressed = OVRInput.Get(OVRInput.Button.One);

        // ボタンが押された瞬間のみ処理（GetDownの代わりに手動で検出）
        if (isButtonPressed && !wasButtonPressed)
        {
            ToggleTarget();
        }

        wasButtonPressed = isButtonPressed;
    }

    public void SetReady(int ready)
    {
        if (ready == 0)
        {
            text.text = "ErrorConnection";
        }
        else
        {
            text.text = "OK";
        }
    }

    /// <summary>
    /// 対象オブジェクトのアクティブ状態を切り替える
    /// </summary>
    public void ToggleTarget()
    {
        if (targetObject != null)
        {
            bool newState = !targetObject.activeSelf;
            targetObject.SetActive(newState);
            Debug.Log($"[ToggleObjectWithAButton] {targetObject.name} を {(newState ? "アクティブ" : "非アクティブ")} に切り替えました");
        }
        else
        {
            Debug.LogWarning("[ToggleObjectWithAButton] targetObjectが設定されていません");
        }
    }

    /// <summary>
    /// 対象オブジェクトを強制的にアクティブにする
    /// </summary>
    public void ActivateTarget()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(true);
            Debug.Log($"[ToggleObjectWithAButton] {targetObject.name} をアクティブにしました");
        }
    }

    /// <summary>
    /// 対象オブジェクトを強制的に非アクティブにする
    /// </summary>
    public void DeactivateTarget()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(false);
            Debug.Log($"[ToggleObjectWithAButton] {targetObject.name} を非アクティブにしました");
        }
    }
}
