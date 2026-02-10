using UnityEngine;

public class Koshi : MonoBehaviour
{
    public GameObject Head;

    void Update()
    {
        Vector3 pos = Head.transform.position;
        pos.y -= 0.8f;
        transform.position = pos;

        // Y軸回転のみを反映
        float yRotation = Head.transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }
}
