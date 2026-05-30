using System;
using System.Reflection;

public static class InterStringBootstrap
{
    public static void Apply(Func<string, string> get)
    {
        Type helperType = FindGameStringHelperType();
        if (helperType == null)
        {
            return;
        }

        SetStaticField(helperType, "xilie", get("系列："));
        SetStaticField(helperType, "opHint", get("*控制权经过转移"));
        SetStaticField(helperType, "licechuwai", get("*里侧表示的除外卡片"));
        SetStaticField(helperType, "biaoceewai", get("*表侧表示的额外卡片"));
        SetStaticField(helperType, "teshuzhaohuan", get("*被特殊召唤出场"));
        SetStaticField(helperType, "yijingqueren", get("卡片展示简表※  "));
        SetStaticField(helperType, "_chaoliang", get("超量："));
        SetStaticField(helperType, "_ewaikazu", get("额外卡组："));
        SetStaticField(helperType, "_fukazu", get("副卡组："));
        SetStaticField(helperType, "_guaishou", get("怪兽："));
        SetStaticField(helperType, "_mofa", get("魔法："));
        SetStaticField(helperType, "_ronghe", get("融合："));
        SetStaticField(helperType, "_lianjie", get("连接："));
        SetStaticField(helperType, "_tongtiao", get("同调："));
        SetStaticField(helperType, "_xianjing", get("陷阱："));
        SetStaticField(helperType, "_zhukazu", get("主卡组："));

        SetStaticField(helperType, "kazu", get("卡组"));
        SetStaticField(helperType, "mudi", get("墓地"));
        SetStaticField(helperType, "chuwai", get("除外"));
        SetStaticField(helperType, "ewai", get("额外"));
        SetStaticField(helperType, "SemiNomi", get("未正规召唤"));
        SetStaticField(helperType, "_wofang", get("我方"));
        SetStaticField(helperType, "_duifang", get("对方"));
    }

    private static Type FindGameStringHelperType()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type helperType = assemblies[i].GetType("GameStringHelper");
            if (helperType != null)
            {
                return helperType;
            }
        }

        return null;
    }

    private static void SetStaticField(Type type, string fieldName, string value)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        if (field != null)
        {
            field.SetValue(null, value);
        }
    }
}
