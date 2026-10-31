using SkyrimForge;
using SkyrimForge.Game;
using SkyrimForge.PrismaUi;
using System.Threading.Tasks;

namespace PrismaUiPlugin;

/// <summary>Creates a view, calls its JavaScript, logs the calls its page makes, and destroys it when the slash key is pressed.</summary>
public sealed class PrismaUiPlugin(PrismaUiApi prisma, IGameEvents events, ILog log) : IPlugin
{
    private PrismaView? view;

    /// <inheritdoc/>
    public void Start()
    {
        events.OnInputLoaded.Subscribe(_ => CreateView());
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kSlash, ModifierKeys.None).Subscribe(_ => DestroyView());
        log.Write("PrismaUiPlugin started. The slash key destroys its view.");
    }

    private void CreateView()
    {
        view = prisma.CreateViewOrNull("PrismaUiPlugin/index.html");
        if (view != null)
        {
            view.RegisterJSListener("sendToPlugin", argument => log.Write($"The page sent \"{argument}\"."));
            view.OnDomReady.Subscribe(OnDomReadyAsync);
        }
    }

    private async Task OnDomReadyAsync(DomReadyEventArgs args)
    {
        args.View.InteropCall("setStatus", "Hello from PrismaUiPlugin.");
        string title = await args.View.Invoke("document.title");
        log.Write($"The page's title is \"{title}\".");
    }

    private void DestroyView()
    {
        view?.Destroy();
        view = null;
    }
}
