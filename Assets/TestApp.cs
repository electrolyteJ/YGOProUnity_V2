using System;
using UnityEngine;
/**
 *  <summary>
 * 测试应用程序入口
 * 用于测试和调试目的，包含完整的 Unity MonoBehaviour 生命周期跟踪
 * </summary>
 *  ┌─────────────────────────────────────────────────────┐
    │                  场景加载                            │
    └─────────────────┬───────────────────────────────────┘
                      │
                      ▼
             ┌────────────────┐
             │   Awake()      │ ⭐ 第一个执行（当前已实现）
             │  初始化单例     │
             │  获取组件       │
             └───────┬────────┘
                     │
                     ▼
             ┌────────────────┐
             │  OnEnable()    │ 对象启用时调用
             └───────┬────────┘
                     │
                     ▼
             ┌────────────────┐
             │   Start()      │ ⭐ 第一帧之前（当前已实现）
             │  初始化逻辑     │
             └───────┬────────┘
                     │
                     ▼
        ┌────────────────────────────┐
        │      每帧循环执行：          │
        │                            │
        │  ┌──────────────────┐     │
        │  │  Update()        │ ⭐ 每帧调用（当前已实现）
        │  │  处理输入        │     │
        │  │  游戏逻辑        │     │
        │  └────────┬─────────┘     │
        │           │                │
        │           ▼                │
        │  ┌──────────────────┐     │
        │  │  LateUpdate()    │     │
        │  │  摄像机跟随      │     │
        │  │  平滑移动        │     │
        │  └────────┬─────────┘     │
        │           │                │
        │           ▼                │
        │  ┌──────────────────┐     │
        │  │  OnGUI()         │     │
        │  │  GUI 渲染         │     │
        │  └──────────────────┘     │
        └────────────┬───────────────┘
                     │
                     ▼
        ┌────────────────────────────┐
        │   固定间隔（物理）：         │
        │  FixedUpdate()             │ 用于物理计算
        └────────────┬───────────────┘
                     │
                     ▼
             ┌────────────────┐
             │  OnDisable()   │ 对象禁用时
             └───────┬────────┘
                     │
                     ▼
             ┌────────────────┐
             │  OnDestroy()   │ 对象销毁时
             └────────────────┘
 */
public class TestApp : MonoBehaviour
{
    #region 初始化阶段
    
    private void Awake()
    {
        Debug.Log("========================================");
        Debug.Log("=== TestApp.Awake() 测试启动入口 ===");
        Debug.Log("========================================");
        Debug.Log($"[TestApp] GameObject: {gameObject.name}");
        Debug.Log($"[TestApp] 场景：{gameObject.scene.name}");
        Debug.Log($"[TestApp] 时间：{Time.time}");
        Debug.Log($"[TestApp] 分辨率：{Screen.width}x{Screen.height}");
        Debug.Log($"[TestApp] 帧率：{Application.targetFrameRate} FPS");
        Debug.Log("========================================");
        Debug.Log("[TestApp] 测试环境准备就绪");
        Debug.Log("========================================");
    }

    private void OnEnable()
    {
        Debug.Log("[TestApp.OnEnable()] ✅ 对象已启用");
    }

    private void Start()
    {
        Debug.Log("[TestApp.Start()] ✅ 测试应用程序启动");
        Debug.Log("[TestApp] 按 Esc 键退出测试");
    }

    #endregion

    #region 游戏运行阶段

    private void Update()
    {
        // 按 Esc 键退出测试
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[TestApp.Update()] ⚠️ 检测到退出键，退出测试...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void LateUpdate()
    {
        // 在 Update 之后执行，适合摄像机跟随
        Debug.Log("[TestApp.LateUpdate()] 🔄 每帧 LateUpdate");
    }

    private void FixedUpdate()
    {
        // 固定时间间隔，适合物理计算
        Debug.Log("[TestApp.FixedUpdate()] ⏱️ 固定更新 (物理)");
    }

    private void OnGUI()
    {
        // GUI 事件处理
        if (Event.current.type == EventType.Repaint)
        {
            Debug.Log($"[TestApp.OnGUI()] 🎨 GUI 渲染 - 事件：{Event.current.type}");
        }
    }

    #endregion

    #region 渲染阶段

    private void OnPreCull()
    {
        Debug.Log("[TestApp.OnPreCull()] 📷 摄像机剔除前");
    }

    private void OnWillRenderObject()
    {
        Debug.Log($"[TestApp.OnWillRenderObject()] 👁️ 物体渲染前 - 摄像机：{Camera.current?.name ?? "Unknown"}");
    }

    private void OnPostRender()
    {
        Debug.Log("[TestApp.OnPostRender()] 📷 摄像机渲染后");
    }

    private void OnRenderObject()
    {
        Debug.Log("[TestApp.OnRenderObject()] 🎨 渲染物体");
    }

    #endregion

    #region 禁用/销毁阶段

    private void OnDisable()
    {
        Debug.Log("[TestApp.OnDisable()] ❌ 对象已禁用");
    }

    private void OnDestroy()
    {
        Debug.Log("[TestApp.OnDestroy()] 💥 对象已销毁");
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[TestApp.OnApplicationQuit()] 🛑 应用程序退出");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"[TestApp.OnApplicationPause()] ⏸️ 应用程序{(pauseStatus ? "暂停" : "恢复")}");
    }

    #endregion
}
