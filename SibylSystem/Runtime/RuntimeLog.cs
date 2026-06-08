using System;
using UnityEngine;

public static class RuntimeLog
{
    public static void Exception(Exception exception)
    {
        if (exception != null)
        {
            Debug.Log(exception);
        }
    }
}
