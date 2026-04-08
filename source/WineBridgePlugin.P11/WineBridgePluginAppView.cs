using Playnite;

namespace WineBridgePlugin;

public class WineBridgePluginTestAppView : AppViewItem
{
    public WineBridgePluginTestAppView()
    {
        View = new WineBridgePluginAppView
        {
            DataContext = this
        };
    }

    public override async Task ActivateViewAsync(ActivateViewAsyncArgs args)
    {
        // This gets called when the view is activated.
    }

    public override async Task DeactivateViewAsync(DeactivateViewAsyncArgs args)
    {
        // This gets called when the view is de-activated.
    }
}