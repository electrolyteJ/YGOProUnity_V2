using System;
using System.Collections.Generic;
using System.Reflection;
using App.Boot;
using App.Core;
using UnityEditor;
using UnityEngine;
using CoreAppContext = App.Core.AppContext;

public static class Task2BootBridgeBatchTest
{
    public static void Run()
    {
        int exitCode = 0;

        try
        {
            VerifyBootOwnsStartupOrder();
            VerifyProgramStartAssignsInstanceBeforeDelegating();
            Debug.Log("Task2BootBridgeBatchTest OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            exitCode = 1;
        }
        finally
        {
            EditorApplication.Exit(exitCode);
        }
    }

    private static void VerifyBootOwnsStartupOrder()
    {
        CoreAppContext context = new CoreAppContext(
            new TestLogger(),
            new TestEventBus(),
            new TestConfig(),
            new TestStorage(),
            new TestNetworkPlatform(),
            new TestPlatformPaths());
        AppBootstrap bootstrap = new AppBootstrap(context);
        List<string> order = new List<string>();

        bootstrap.Start(new AppSceneStartup(
            delegate { order.Add("prepare"); },
            delegate { order.Add("legacy"); },
            delegate { order.Add("complete"); }));

        string actualOrder = string.Join(">", order.ToArray());
        if (actualOrder != "prepare>legacy>complete")
        {
            throw new Exception("Expected AppBootstrap/AppSceneEntry to own startup order, got: " + actualOrder);
        }
    }

    private static void VerifyProgramStartAssignsInstanceBeforeDelegating()
    {
        GameObject ownerObject = new GameObject("Task2BootBridgeBatchTest.Program");
        Program owner = ownerObject.AddComponent<Program>();
        FieldInfo startupBridgeField = typeof(Program).GetField("StartupBridge", BindingFlags.Static | BindingFlags.NonPublic);
        if (startupBridgeField == null)
        {
            throw new Exception("Program.StartupBridge seam was not found.");
        }

        Action<Program, Action> originalBridge = (Action<Program, Action>)startupBridgeField.GetValue(null);
        bool wasDelegated = false;
        bool sawAssignedInstance = false;
        bool sawInitializeDelegate = false;
        Program delegatedOwner = null;

        startupBridgeField.SetValue(null, (Action<Program, Action>)delegate (Program forwardedOwner, Action legacyInitialize)
        {
            wasDelegated = true;
            delegatedOwner = forwardedOwner;
            sawAssignedInstance = Program.I() == forwardedOwner;
            sawInitializeDelegate = legacyInitialize != null;
        });

        try
        {
            MethodInfo startMethod = typeof(Program).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            if (startMethod == null)
            {
                throw new Exception("Program.Start was not found.");
            }

            startMethod.Invoke(owner, null);
        }
        finally
        {
            startupBridgeField.SetValue(null, originalBridge);
            UnityEngine.Object.DestroyImmediate(ownerObject);
        }

        if (!wasDelegated)
        {
            throw new Exception("Program.Start did not delegate to the startup bridge.");
        }

        if (!ReferenceEquals(delegatedOwner, owner))
        {
            throw new Exception("Program.Start delegated with the wrong owner instance.");
        }

        if (!sawAssignedInstance)
        {
            throw new Exception("Program.Start did not assign Program.instance before delegating.");
        }

        if (!sawInitializeDelegate)
        {
            throw new Exception("Program.Start did not forward the legacy initialize delegate.");
        }
    }

    private sealed class TestLogger : IAppLogger
    {
        public void Log(string message)
        {
        }

        public void LogWarning(string message)
        {
        }

        public void LogError(string message)
        {
        }
    }

    private sealed class TestEventBus : IAppEventBus
    {
        public void Publish<TEvent>(TEvent appEvent)
        {
        }

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
        }
    }

    private sealed class TestConfig : IAppConfig
    {
        public string Get(string key, string defaultValue)
        {
            return defaultValue;
        }
    }

    private sealed class TestStorage : IFileStorage
    {
        public bool FileExists(string path)
        {
            return false;
        }

        public string ReadAllText(string path)
        {
            return string.Empty;
        }

        public void WriteAllText(string path, string contents)
        {
        }
    }

    private sealed class TestNetworkPlatform : INetworkPlatform
    {
        public bool IsAvailable
        {
            get { return true; }
        }
    }

    private sealed class TestPlatformPaths : IPlatformPaths
    {
        public string ProjectRoot
        {
            get { return "/tmp"; }
        }

        public string GetDirectoryPath(string directoryName)
        {
            return directoryName;
        }

        public string GetProjectRootFilePath(params string[] segments)
        {
            return string.Join("/", segments);
        }

        public string GetFilePath(string directoryName, params string[] segments)
        {
            return directoryName + "/" + string.Join("/", segments);
        }
    }
}
