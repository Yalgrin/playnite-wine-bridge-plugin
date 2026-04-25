using Playnite.Markup;

namespace Playnite;

public class LocalizedString : LocStringMarkup
{
    public LocalizedString() : base(WineBridgePlugin.WineBridgePlugin.Id)
    {
    }

    public LocalizedString(string stringId) : base(WineBridgePlugin.WineBridgePlugin.Id, stringId)
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