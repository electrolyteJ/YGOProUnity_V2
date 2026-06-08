using UnityEngine;

/// <summary>
/// Placing this script on the game object will make that game object pan with mouse movement.
/// </summary>

[AddComponentMenu("NGUI/Examples/Pan With Mouse")]
public class PanWithMouse : MonoBehaviour
{

    Transform mTrans;
    Quaternion mStart;
    Vector2 mRot = Vector2.zero;

    void Start()
    {
        mTrans = transform;
        mStart = mTrans.localRotation;
    }
    Vector2 lastPos = Vector3.zero;
    void Update()
    {
        float delta = RealTime.deltaTime;
        Vector3 pos;
        float halfWidth = Screen.width * 0.5f;
        float halfHeight = Screen.height * 0.5f;
        // Simplified for NGUI asmdef isolation - use standard Input API
        if (Input.GetMouseButtonDown(0))
        {
            lastPos = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            pos = -UICamera.lastEventPosition + lastPos;
        }
        else
        {
            pos = Vector3.zero;
        }
        float x = Mathf.Clamp((pos.x) / halfWidth * 1f, -1f, 1f);
        float y = Mathf.Clamp((pos.y) / halfHeight * 1f, -1f, 1f);
        mRot = Vector2.Lerp(mRot, new Vector2(x, y), delta * 5f);

        mTrans.localRotation = mStart * Quaternion.Euler(-mRot.y * 6f, mRot.x * 8f, 0f);
    }
}

