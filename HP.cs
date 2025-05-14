// File: HP.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using HarmonyLib;

/// <summary>
/// A 1:1 C# wrapper of all public HarmonyLib APIs.
/// Python will call these static methods to drive Harmony.
/// </summary>
public static class HP
{
    //─── static Harmony instance ─────────────────────────────────────────────────
    /// <summary>Global debug flag (set before any patches).</summary>
    public static bool DEBUG
    {
        get => Harmony.DEBUG;
        set => Harmony.DEBUG = value;
    }

    /// <summary>Your unique Harmony ID.</summary>
    public static string Id => harmony.Id;

    private static readonly Harmony harmony = new Harmony("com.yourname.pht");

    //─── instance-based creation & patching ──────────────────────────────────────
    /// <summary>Create a PatchClassProcessor for a given type.</summary>
    public static PatchClassProcessor CreateClassProcessor(string typeName)
    {
        var type = Type.GetType(typeName) 
                   ?? throw new ArgumentException($"Type not found: {typeName}");
        return harmony.CreateClassProcessor(type);
    }

    /// <summary>Create a PatchProcessor for a given method.</summary>
    public static PatchProcessor CreateProcessor(string targetTypeName, string methodName)
    {
        var original = AccessTools.Method(Type.GetType(targetTypeName), methodName)
                       ?? throw new ArgumentException($"Method not found: {methodName}");
        return harmony.CreateProcessor(original);
    }

    /// <summary>Create a ReversePatcher for a stub method.</summary>
    public static ReversePatcher CreateReversePatcher(
        string targetTypeName,
        string methodName,
        string standinPatchName)
    {
        var original = AccessTools.Method(Type.GetType(targetTypeName), methodName)
                       ?? throw new ArgumentException($"Method not found: {methodName}");
        var standin  = new HarmonyMethod(AccessTools.Method(typeof(HP), standinPatchName));
        return harmony.CreateReversePatcher(original, standin);
    }

    /// <summary>Patch one method (prefix/postfix/transpiler/finalizer).</summary>
    public static MethodInfo Patch(
        string targetTypeName,
        string methodName,
        string prefixPatchName     = null,
        string postfixPatchName    = null,
        string transpilerPatchName = null,
        string finalizerPatchName  = null)
    {
        var target   = Type.GetType(targetTypeName)
                     ?? throw new ArgumentException($"Type not found: {targetTypeName}");
        var original = AccessTools.Method(target, methodName)
                       ?? throw new ArgumentException($"Method not found: {methodName}");

        var prefix     = prefixPatchName     != null
                         ? new HarmonyMethod(AccessTools.Method(typeof(HP), prefixPatchName))
                         : null;
        var postfix    = postfixPatchName    != null
                         ? new HarmonyMethod(AccessTools.Method(typeof(HP), postfixPatchName))
                         : null;
        var transpiler = transpilerPatchName != null
                         ? new HarmonyMethod(AccessTools.Method(typeof(HP), transpilerPatchName))
                         : null;
        var finalizer  = finalizerPatchName  != null
                         ? new HarmonyMethod(AccessTools.Method(typeof(HP), finalizerPatchName))
                         : null;

        return harmony.Patch(original, prefix, postfix, transpiler, finalizer);
    }

    /// <summary>Unpatch a method (all patch types) by this Harmony ID.</summary>
    public static void Unpatch(
        string targetTypeName,
        string methodName,
        string harmonyID = null)
    {
        var original = AccessTools.Method(Type.GetType(targetTypeName), methodName)
                       ?? throw new ArgumentException($"Method not found: {methodName}");
        harmony.Unpatch(original, HarmonyPatchType.All, harmonyID ?? harmony.Id);
    }

    /// <summary>Remove a specific patch method.</summary>
    public static void UnpatchSpecific(
        string targetTypeName,
        string methodName,
        string patchMethodName)
    {
        var original = AccessTools.Method(Type.GetType(targetTypeName), methodName)
                       ?? throw new ArgumentException($"Method not found: {methodName}");
        var patch    = AccessTools.Method(typeof(HP), patchMethodName)
                       ?? throw new ArgumentException($"Patch not found: {patchMethodName}");
        harmony.Unpatch(original, patch);
    }

    /// <summary>Patch all [HarmonyPatch] in this assembly.</summary>
    public static void PatchAllCurrent() => harmony.PatchAll(Assembly.GetExecutingAssembly());

    /// <summary>Patch all [HarmonyPatch] in named assembly.</summary>
    public static void PatchAllFromAssembly(string assemblyName)
    {
        var asm = Assembly.Load(assemblyName)
                  ?? throw new ArgumentException($"Assembly not found: {assemblyName}");
        harmony.PatchAll(asm);
    }

    /// <summary>Patch all uncategorized patches in this assembly.</summary>
    public static void PatchAllUncategorized() => harmony.PatchAllUncategorized();

    /// <summary>Patch all uncategorized in named assembly.</summary>
    public static void PatchAllUncategorizedAssembly(string assemblyName)
    {
        var asm = Assembly.Load(assemblyName)
                  ?? throw new ArgumentException($"Assembly not found: {assemblyName}");
        harmony.PatchAllUncategorized(asm);
    }

    /// <summary>Patch by category in this assembly.</summary>
    public static void PatchCategory(string category) => harmony.PatchCategory(category);

    /// <summary>Patch by category in named assembly.</summary>
    public static void PatchCategoryAssembly(string assemblyName, string category)
    {
        var asm = Assembly.Load(assemblyName)
                  ?? throw new ArgumentException($"Assembly not found: {assemblyName}");
        harmony.PatchCategory(asm, category);
    }

    /// <summary>Unpatch all methods by this Harmony ID.</summary>
    public static void UnpatchAll(string harmonyID = null) => harmony.UnpatchAll(harmonyID);

    /// <summary>Unpatch by category in this assembly.</summary>
    public static void UnpatchCategory(string category) => harmony.UnpatchCategory(category);

    /// <summary>Unpatch by category in named assembly.</summary>
    public static void UnpatchCategoryAssembly(string assemblyName, string category)
    {
        var asm = Assembly.Load(assemblyName)
                  ?? throw new ArgumentException($"Assembly not found: {assemblyName}");
        harmony.UnpatchCategory(asm, category);
    }

    /// <summary>List methods this instance has patched.</summary>
    public static List<string> GetPatchedMethods()
    {
        var list = new List<string>();
        foreach (var mb in harmony.GetPatchedMethods())
            list.Add($"{mb.DeclaringType.FullName}.{mb.Name}");
        return list;
    }

    //─── static/global Harmony utilities ─────────────────────────────────────────
    /// <summary>All patched methods in the AppDomain.</summary>
    public static List<string> GetAllPatchedMethodsGlobal()
    {
        var list = new List<string>();
        foreach (var mb in Harmony.GetAllPatchedMethods())
            list.Add($"{mb.DeclaringType.FullName}.{mb.Name}");
        return list;
    }

    /// <summary>Get method from a stack frame (skip N frames).</summary>
    public static MethodBase GetMethodFromStackframe(int skipFrames)
    {
        var frame = new StackFrame(skipFrames);
        return Harmony.GetMethodFromStackframe(frame);
    }

    /// <summary>Get original method for a replacement.</summary>
    public static MethodBase GetOriginalMethod(MethodInfo replacement)
        => Harmony.GetOriginalMethod(replacement);

    /// <summary>Get original method from a stack frame.</summary>
    public static MethodBase GetOriginalMethodFromStackframe(int skipFrames)
    {
        var frame = new StackFrame(skipFrames);
        return Harmony.GetOriginalMethodFromStackframe(frame);
    }

    /// <summary>Get patch info for a given method.</summary>
    public static Patches GetPatchInfo(string targetTypeName, string methodName)
    {
        var original = AccessTools.Method(Type.GetType(targetTypeName), methodName)
                       ?? throw new ArgumentException($"Method not found: {methodName}");
        return Harmony.GetPatchInfo(original);
    }

    /// <summary>Test if any patches exist for a Harmony ID.</summary>
    public static bool HasAnyPatches(string harmonyID) => Harmony.HasAnyPatches(harmonyID);

    /// <summary>Reverse-patch (duplicate) an original onto your stub.</summary>
    public static MethodInfo ReversePatch(
        string targetTypeName,
        string methodName,
        string standinPatchName,
        string transpilerPatchName = null)
    {
        var original = AccessTools.Method(Type.GetType(targetTypeName), methodName)
                       ?? throw new ArgumentException($"Method not found: {methodName}");
        var standin  = new HarmonyMethod(AccessTools.Method(typeof(HP), standinPatchName));
        MethodInfo transpiler = null;
        if (transpilerPatchName != null)
            transpiler = AccessTools.Method(typeof(HP), transpilerPatchName);
        return Harmony.ReversePatch(original, standin, transpiler);
    }

    /// <summary>Get versions of all active Harmony instances.</summary>
    public static Dictionary<string, Version> VersionInfo(out Version currentVersion)
        => Harmony.VersionInfo(out currentVersion);


    //─── example patch-methods (optional—you can remove or replace these) ─────────
    public static bool ExamplePrefix()      { /* … */ return true; }
    public static void ExamplePostfix()     { /* … */ }
    public static IEnumerable<CodeInstruction> ExampleTranspiler(
        IEnumerable<CodeInstruction> instr) { return instr; }
    public static void ExampleFinalizer(Exception __exception) { /* … */ }
}
