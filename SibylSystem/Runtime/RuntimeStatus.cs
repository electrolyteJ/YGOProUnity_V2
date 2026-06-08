using System;
using System.Reflection;

public static class RuntimeStatus
{
    private static bool noAccess;

    public static bool NoAccess
    {
        get
        {
            FieldInfo field = FindProgramNoAccessField();
            if (field != null)
            {
                return (bool)field.GetValue(null);
            }

            return noAccess;
        }
        set
        {
            noAccess = value;

            FieldInfo field = FindProgramNoAccessField();
            if (field != null)
            {
                field.SetValue(null, value);
            }
        }
    }

    private static FieldInfo FindProgramNoAccessField()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type programType = assemblies[i].GetType("Program");
            if (programType == null)
            {
                continue;
            }

            FieldInfo field = programType.GetField("noAccess", BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                return field;
            }
        }

        return null;
    }
}
