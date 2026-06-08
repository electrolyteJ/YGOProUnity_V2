using System;
using UnityEngine;
public class Program  : MonoBehaviour
{
    private static Program _instance;

    private static MonoBehaviour _monoBehaviour;
    public static void init(MonoBehaviour  monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }
    public Menu menu { get; private set; }
    public Ocgcore ocgcore { get; private set; }
    public Room room { get; private set; }
    public CardDescription cardDescription { get; private set; }
    public SelectServer selectServer { get; private set; }

    /// <summary>No-op compatibility property.</summary>
    public static bool noAccess { get; set; }

    /// <summary>No-op compatibility property.</summary>
    public static GameObject ui_main_3d { get; private set; }

    /// <summary>No-op compatibility property.</summary>
    public static GameObject ui_windows_2d { get; private set; }

    /// <summary>No-op compatibility property.</summary>
    public static GameObject ui_main_2d { get; private set; }

    /// <summary>No-op compatibility property.</summary>
    public static float _padScroll = 0f;

    /// <summary>No-op compatibility property.</summary>
    public static GameObject mod_simple_ngui_background_texture { get; private set; }

    /// <summary>No-op compatibility property.</summary>
    public static GameObject ui_back_ground_2d { get; private set; }


    public void shiftToServant(object servant) { }

    /// <summary>No-op compatibility property.</summary>
    public static bool InputGetMouseButtonDown_0 => UnityEngine.Input.GetMouseButtonDown(0);

    /// <summary>No-op compatibility property.</summary>
    public static bool InputGetMouseButton_0 => UnityEngine.Input.GetMouseButton(0);

    /// <summary>No-op compatibility method.</summary>
    public static GameObject go() => _instance ? _instance.gameObject : null;

    /// <summary>No-op compatibility method.</summary>
    public static void go(int priority, Action callback) { }

    /// <summary>No-op compatibility property.</summary>
    public static float deltaTime => Time.deltaTime;

    /// <summary>No-op compatibility method.</summary>
    public static float TimePassed() => Time.time;
    public static Program I()
    {
        if (_instance == null)
        {
            _instance = FindObjectOfType<Program>();
            if (_instance == null)
            {
                var go = new GameObject("Program");
                _instance = go.AddComponent<Program>();
            }
        }
        return _instance;
    }
}