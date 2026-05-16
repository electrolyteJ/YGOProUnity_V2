using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public class OcgcoreLoadTest
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate uint MessageHandler(IntPtr pDuel, uint messageType);

    [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
    static extern void set_message_handler(MessageHandler f);

    [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr create_duel(uint seed);

    [DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
    static extern void end_duel(IntPtr pduel);

    static uint DummyHandler(IntPtr pDuel, uint msg) => 0;

    [MenuItem("Tools/Test Ocgcore Load (macOS)")]
    static void TestLoad()
    {
        try
        {
            set_message_handler(DummyHandler);
            Debug.Log("ocgcore loaded: set_message_handler OK");

            IntPtr duel = create_duel(12345);
            Debug.Log("ocgcore loaded: create_duel returned " + (duel != IntPtr.Zero ? "valid" : "NULL"));

            if (duel != IntPtr.Zero)
            {
                end_duel(duel);
                Debug.Log("ocgcore loaded: end_duel OK");
            }

            EditorUtility.DisplayDialog("ocgcore Test",
                "ocgcore loaded successfully!\n\ngame functions work.", "OK");
        }
        catch (DllNotFoundException e)
        {
            Debug.LogError("FAILED: DllNotFoundException - " + e.Message);
            EditorUtility.DisplayDialog("ocgcore Test",
                "FAILED: Library not found!\n\n" + e.Message, "OK");
        }
        catch (EntryPointNotFoundException e)
        {
            Debug.LogError("FAILED: EntryPointNotFoundException - " + e.Message);
            EditorUtility.DisplayDialog("ocgcore Test",
                "FAILED: Function not found! (wrong version?)\n\n" + e.Message, "OK");
        }
        catch (Exception e)
        {
            Debug.LogError("FAILED: " + e.GetType().Name + " - " + e.Message);
            EditorUtility.DisplayDialog("ocgcore Test",
                "FAILED: " + e.GetType().Name + "\n\n" + e.Message, "OK");
        }
    }
}
