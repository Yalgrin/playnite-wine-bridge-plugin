using Playnite.Markup;
using WineBridgePlugin;

namespace Playnite;

public class LocalizedString : LocStringMarkup
{
    public LocalizedString() : base(WineBridgePluginPlugin.Id)
    {
    }

    public LocalizedString(string stringId) : base(WineBridgePluginPlugin.Id, stringId)
    {
    }
}

public static partial class Loc
{
    public static IPlayniteApi Api = null!;

    public static string GetString(string stringId)
    {
        return Api.GetLocalizedString(stringId);
    }

    public static string GetString(string stringId, params (string name, object value)[] args)
    {
        return Api.GetLocalizedString(stringId, args);
    }

    public static bool IsStringId(string id)
    {
        return LocId.StringIds.Contains(id);
    }
}