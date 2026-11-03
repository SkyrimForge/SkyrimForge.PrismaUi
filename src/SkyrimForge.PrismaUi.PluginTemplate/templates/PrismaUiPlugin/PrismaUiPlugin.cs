using SkyrimForge;
using SkyrimForge.Events;
using SkyrimForge.Game;
using SkyrimForge.PrismaUi;

namespace PrismaUiPlugin;

/// <summary>Creates a view, calls its JavaScript, logs the calls its page makes, and destroys it when the End key is pressed.</summary>
public sealed class PrismaUiPlugin(PrismaUiApi prisma, IGameEvents events, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        // Views should be created during OnInputLoaded or some later event.
        events.OnInputLoaded.Subscribe(async _ =>
        {
            PrismaView view = prisma.CreateView("PrismaUiPlugin/index.html");
            view.RegisterJSListener("sendToPlugin", argument => log.Write($"The page sent \"{argument}\"."));
            view.OnDomReady.Subscribe(async args =>
            {
                args.View.InteropCall("setStatus", "Hello from PrismaUiPlugin.");
                string title = await args.View.Invoke("document.title");
                log.Write($"The page's title is \"{title}\".");
            });
            await events.OnKeyDown(BSKeyboardDevice.Keys.Key.kEnd, ModifierKeys.None).WaitForNextAsync();
            view.Destroy();
        });
        log.Write("PrismaUiPlugin started. The End key destroys its view.");
    }
}
