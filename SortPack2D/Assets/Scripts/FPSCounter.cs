using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    float deltaTime = 0.0f;
    float fps = 0f;
    float ms = 0f;

    GUIStyle style;
    Rect rect;

    float updateInterval = 0.25f; // update mỗi 0.25s
    float timer = 0f;

    void Start()
    {

        Application.targetFrameRate = 120;
        QualitySettings.vSyncCount = 0;

        int w = Screen.width, h = Screen.height;

        rect = new Rect(10, 10, w, h * 2 / 100);

        style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 50;
        style.normal.textColor = Color.white;
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        timer += Time.unscaledDeltaTime;

        // CHỈ cập nhật FPS mỗi 0.25s (không gây lag)
        if (timer >= updateInterval)
        {
            ms = deltaTime * 1000f;
            fps = 1f / deltaTime;
            timer = 0f;
        }
    }

    void OnGUI()
    {
        GUI.Label(rect, $"{ms:0.0} ms ({fps:0} FPS)", style);
    }
}
