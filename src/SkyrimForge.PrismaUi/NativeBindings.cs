using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SkyrimForge.PrismaUi;

internal static unsafe partial class NativeBindings
{
    private const string native = "SkyrimForge.PrismaUi.Native.dll";

    // The default search omits this assembly's folder. A static constructor runs before the first import.
    static NativeBindings()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeBindings).Assembly, Resolve);
    }

    private static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != native)
        {
            return 0;
        }

        string? directory = Path.GetDirectoryName(typeof(NativeBindings).Assembly.Location);
        if (string.IsNullOrEmpty(directory))
        {
            return 0;
        }

        return NativeLibrary.TryLoad(Path.Combine(directory, native), out nint handle) ? handle : 0;
    }

    [LibraryImport(native, EntryPoint = "SFP_Acquire")]
    internal static partial int SFP_Acquire();

    [LibraryImport(native, EntryPoint = "SFP_SetHandlers")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SFP_SetHandlers(
        delegate* unmanaged<ulong, void> onDomReady,
        delegate* unmanaged<uint, byte*, void> onJsResult,
        delegate* unmanaged<uint, byte*, void> onJsListener,
        delegate* unmanaged<ulong, byte, byte*, void> onConsoleMessage);

    [LibraryImport(native, EntryPoint = "SFP_SlotCount")]
    internal static partial uint SFP_SlotCount();

    [LibraryImport(native, EntryPoint = "SFP_CreateView", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial ulong SFP_CreateView(string htmlPath, [MarshalAs(UnmanagedType.U1)] bool wantDomReady);

    [LibraryImport(native, EntryPoint = "SFP_Invoke", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void SFP_Invoke(ulong view, string script, uint slot);

    [LibraryImport(native, EntryPoint = "SFP_InteropCall", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void SFP_InteropCall(ulong view, string functionName, string argument);

    [LibraryImport(native, EntryPoint = "SFP_RegisterJSListener", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void SFP_RegisterJSListener(ulong view, string functionName, uint slot);

    [LibraryImport(native, EntryPoint = "SFP_HasFocus")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SFP_HasFocus(ulong view);

    [LibraryImport(native, EntryPoint = "SFP_Focus")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SFP_Focus(ulong view, [MarshalAs(UnmanagedType.U1)] bool pauseGame, [MarshalAs(UnmanagedType.U1)] bool disableFocusMenu);

    [LibraryImport(native, EntryPoint = "SFP_Unfocus")]
    internal static partial void SFP_Unfocus(ulong view);

    [LibraryImport(native, EntryPoint = "SFP_Show")]
    internal static partial void SFP_Show(ulong view);

    [LibraryImport(native, EntryPoint = "SFP_Hide")]
    internal static partial void SFP_Hide(ulong view);

    [LibraryImport(native, EntryPoint = "SFP_IsHidden")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SFP_IsHidden(ulong view);

    [LibraryImport(native, EntryPoint = "SFP_GetScrollingPixelSize")]
    internal static partial int SFP_GetScrollingPixelSize(ulong view);

    [LibraryImport(native, EntryPoint = "SFP_SetScrollingPixelSize")]
    internal static partial void SFP_SetScrollingPixelSize(ulong view, int pixelSize);

    [LibraryImport(native, EntryPoint = "SFP_IsValid")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SFP_IsValid(ulong view);

    [LibraryImport(native, EntryPoint = "SFP_Destroy")]
    internal static partial void SFP_Destroy(ulong view);

    [LibraryImport(native, EntryPoint = "SFP_SetOrder")]
    internal static partial void SFP_SetOrder(ulong view, int order);

    [LibraryImport(native, EntryPoint = "SFP_GetOrder")]
    internal static partial int SFP_GetOrder(ulong view);

    [LibraryImport(native, EntryPoint = "SFP_CreateInspectorView")]
    internal static partial void SFP_CreateInspectorView(ulong view);

    [LibraryImport(native, EntryPoint = "SFP_SetInspectorVisibility")]
    internal static partial void SFP_SetInspectorVisibility(ulong view, [MarshalAs(UnmanagedType.U1)] bool visible);

    [LibraryImport(native, EntryPoint = "SFP_IsInspectorVisible")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SFP_IsInspectorVisible(ulong view);

    [LibraryImport(native, EntryPoint = "SFP_SetInspectorBounds")]
    internal static partial void SFP_SetInspectorBounds(ulong view, float topLeftX, float topLeftY, uint width, uint height);

    [LibraryImport(native, EntryPoint = "SFP_HasAnyActiveFocus")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SFP_HasAnyActiveFocus();

    [LibraryImport(native, EntryPoint = "SFP_RegisterConsoleCallback")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SFP_RegisterConsoleCallback(ulong view, [MarshalAs(UnmanagedType.U1)] bool wanted);
}
