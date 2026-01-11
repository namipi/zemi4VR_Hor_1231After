using UnityEngine;

public class ResetLoadScene : MonoBehaviour
{
    public void Reset()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }
}
