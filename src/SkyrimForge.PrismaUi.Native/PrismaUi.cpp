// C++, so the compiler checks every signature and vtable index.
#include "PrismaUI_API.h"

#include <array>
#include <cstdint>
#include <utility>

// The binding passes only a view that SFP_CreateView returned and a slot below SFP_SlotCount.
// SFP_CreateView succeeds only after SFP_Acquire and SFP_SetHandlers, so the exports omit null and bounds checks.
namespace
{
    enum class Acquisition : std::int32_t
    {
        Version2 = 0,
        Version1 = 1,
        NotLoaded = 2,
        NoEntryPoint = 3,
        NoInterface = 4
    };

    PRISMA_UI_API::IVPrismaUI1* g_api = nullptr;
    PRISMA_UI_API::IVPrismaUI2* g_api2 = nullptr;

    void (*g_onDomReady)(std::uint64_t view) = nullptr;
    void (*g_onJsResult)(std::uint32_t slot, const char* result) = nullptr;
    void (*g_onJsListener)(std::uint32_t slot, const char* argument) = nullptr;
    void (*g_onConsoleMessage)(std::uint64_t view, std::uint8_t level, const char* message) = nullptr;

    constexpr std::uint32_t slotCount = 512;

    // Prisma UI's callbacks receive no context, so each slot needs its own function.
    template <std::uint32_t Slot>
    void JsResultStub(const char* result)
    {
        g_onJsResult(Slot, result);
    }

    template <std::uint32_t Slot>
    void JsListenerStub(const char* argument)
    {
        g_onJsListener(Slot, argument);
    }

    // MSVC cannot expand a pack through a template template parameter over a value.
    template <std::uint32_t... Slots>
    constexpr std::array<PRISMA_UI_API::JSCallback, sizeof...(Slots)> MakeJsResultStubs(std::integer_sequence<std::uint32_t, Slots...>)
    {
        return { &JsResultStub<Slots>... };
    }

    template <std::uint32_t... Slots>
    constexpr std::array<PRISMA_UI_API::JSListenerCallback, sizeof...(Slots)> MakeJsListenerStubs(std::integer_sequence<std::uint32_t, Slots...>)
    {
        return { &JsListenerStub<Slots>... };
    }

    const std::array<PRISMA_UI_API::JSCallback, slotCount> jsResultStubs =
        MakeJsResultStubs(std::make_integer_sequence<std::uint32_t, slotCount>{});

    const std::array<PRISMA_UI_API::JSListenerCallback, slotCount> jsListenerStubs =
        MakeJsListenerStubs(std::make_integer_sequence<std::uint32_t, slotCount>{});

    void DomReadyStub(PrismaView view)
    {
        g_onDomReady(view);
    }

    void ConsoleMessageStub(PrismaView view, PRISMA_UI_API::ConsoleMessageLevel level, const char* message)
    {
        g_onConsoleMessage(view, static_cast<std::uint8_t>(level), message);
    }
}

// Runs the steps of RequestPluginAPI separately, to distinguish the failures.
extern "C" __declspec(dllexport) std::int32_t SFP_Acquire()
{
    const HMODULE prisma = GetModuleHandleW(L"PrismaUI.dll");
    if (prisma == nullptr)
    {
        return static_cast<std::int32_t>(Acquisition::NotLoaded);
    }

    const auto request = reinterpret_cast<PRISMA_UI_API::RequestPluginAPIFunc>(GetProcAddress(prisma, "RequestPluginAPI"));
    if (request == nullptr)
    {
        return static_cast<std::int32_t>(Acquisition::NoEntryPoint);
    }

    g_api2 = static_cast<PRISMA_UI_API::IVPrismaUI2*>(request(PRISMA_UI_API::InterfaceVersion::V2));
    g_api = g_api2 != nullptr ? g_api2 : static_cast<PRISMA_UI_API::IVPrismaUI1*>(request(PRISMA_UI_API::InterfaceVersion::V1));

    if (g_api == nullptr)
    {
        return static_cast<std::int32_t>(Acquisition::NoInterface);
    }
    return static_cast<std::int32_t>(g_api2 != nullptr ? Acquisition::Version2 : Acquisition::Version1);
}

// False if handlers are already installed, meaning the binding is loaded more than once.
extern "C" __declspec(dllexport) bool SFP_SetHandlers(
    void (*onDomReady)(std::uint64_t),
    void (*onJsResult)(std::uint32_t, const char*),
    void (*onJsListener)(std::uint32_t, const char*),
    void (*onConsoleMessage)(std::uint64_t, std::uint8_t, const char*))
{
    if (g_onDomReady != nullptr)
    {
        return false;
    }

    g_onDomReady = onDomReady;
    g_onJsResult = onJsResult;
    g_onJsListener = onJsListener;
    g_onConsoleMessage = onConsoleMessage;
    return true;
}

extern "C" __declspec(dllexport) std::uint32_t SFP_SlotCount()
{
    return slotCount;
}

// Also requests the view's console messages, which a Prisma UI that predates IVPrismaUI2 cannot send.
extern "C" __declspec(dllexport) std::uint64_t SFP_CreateView(const char* htmlPath)
{
    const PrismaView view = g_api->CreateView(htmlPath, &DomReadyStub);
    if (view != 0 && g_api2 != nullptr)
    {
        g_api2->RegisterConsoleCallback(view, &ConsoleMessageStub);
    }
    return view;
}

extern "C" __declspec(dllexport) void SFP_Invoke(std::uint64_t view, const char* script, std::uint32_t slot)
{
    g_api->Invoke(view, script, jsResultStubs[slot]);
}

extern "C" __declspec(dllexport) void SFP_InteropCall(std::uint64_t view, const char* functionName, const char* argument)
{
    g_api->InteropCall(view, functionName, argument);
}

extern "C" __declspec(dllexport) void SFP_RegisterJSListener(std::uint64_t view, const char* functionName, std::uint32_t slot)
{
    g_api->RegisterJSListener(view, functionName, jsListenerStubs[slot]);
}

extern "C" __declspec(dllexport) bool SFP_HasFocus(std::uint64_t view)
{
    return g_api->HasFocus(view);
}

extern "C" __declspec(dllexport) bool SFP_Focus(std::uint64_t view, bool pauseGame, bool disableFocusMenu)
{
    return g_api->Focus(view, pauseGame, disableFocusMenu);
}

extern "C" __declspec(dllexport) void SFP_Unfocus(std::uint64_t view)
{
    g_api->Unfocus(view);
}

extern "C" __declspec(dllexport) void SFP_Show(std::uint64_t view)
{
    g_api->Show(view);
}

extern "C" __declspec(dllexport) void SFP_Hide(std::uint64_t view)
{
    g_api->Hide(view);
}

extern "C" __declspec(dllexport) bool SFP_IsHidden(std::uint64_t view)
{
    return g_api->IsHidden(view);
}

extern "C" __declspec(dllexport) std::int32_t SFP_GetScrollingPixelSize(std::uint64_t view)
{
    return g_api->GetScrollingPixelSize(view);
}

extern "C" __declspec(dllexport) void SFP_SetScrollingPixelSize(std::uint64_t view, std::int32_t pixelSize)
{
    g_api->SetScrollingPixelSize(view, pixelSize);
}

extern "C" __declspec(dllexport) bool SFP_IsValid(std::uint64_t view)
{
    return g_api->IsValid(view);
}

extern "C" __declspec(dllexport) void SFP_Destroy(std::uint64_t view)
{
    g_api->Destroy(view);
}

extern "C" __declspec(dllexport) void SFP_SetOrder(std::uint64_t view, std::int32_t order)
{
    g_api->SetOrder(view, order);
}

extern "C" __declspec(dllexport) std::int32_t SFP_GetOrder(std::uint64_t view)
{
    return g_api->GetOrder(view);
}

extern "C" __declspec(dllexport) void SFP_CreateInspectorView(std::uint64_t view)
{
    g_api->CreateInspectorView(view);
}

extern "C" __declspec(dllexport) void SFP_SetInspectorVisibility(std::uint64_t view, bool visible)
{
    g_api->SetInspectorVisibility(view, visible);
}

extern "C" __declspec(dllexport) bool SFP_IsInspectorVisible(std::uint64_t view)
{
    return g_api->IsInspectorVisible(view);
}

extern "C" __declspec(dllexport) void SFP_SetInspectorBounds(std::uint64_t view, float topLeftX, float topLeftY, std::uint32_t width, std::uint32_t height)
{
    g_api->SetInspectorBounds(view, topLeftX, topLeftY, width, height);
}

// PrismaUiApi.HasAnyActiveFocus is static, so this can precede SFP_Acquire.
extern "C" __declspec(dllexport) bool SFP_HasAnyActiveFocus()
{
    return g_api != nullptr && g_api->HasAnyActiveFocus();
}
