using Playnite;

namespace WineBridgePlugin;

public class WineBridgePluginMetadataProviderProviderGameSession : MetadataProviderGameSession
{
    public WineBridgePluginMetadataProviderProviderGameSession(Game game) : base(game)
    {
    }

    public override async Task<object?> GetDataAsync(GetDataArgs dataArgs)
    {
        // This is where Playnite will ask your plugin to return data for specific data field for a game.
        // What IDs you'll get here depends on what you returned as supported via MetadataSettings
        // on your plugin class. For built-in data fields, you can use static BuiltInGameDataId.
        // If you are supporting data for other plugins, you will need to get their IDs from plugin developer.

        // Since this also expects plain object type to be returned, what type of data each
        // data field supports is to be documented by owner of the data field.
        // For Playnite's built-in data fields, see documentation here: TODO

        // An example for built-in description field:
        if (dataArgs.DataId == "Playnite.Description") // Same as BuiltInGameDataId.Description
        {
            // Playnite's description field supports GameDescription and string types as metadata values.
            // So returning either of these is valid:

            // return "Description test";
            // return new GameDescription("Description test", GameDescriptionFormat.Markdown);
        }

        return null;
    }
}

public class WineBridgePluginMetadataProvider : MetadataProvider
{
    public override async Task<MetadataProviderGameSession?> CreateGameSessionAsync(CreateGameMetadataSessionArgs args)
    {
        // This gets called for each game and returned MetadataProviderGameSession is disposed when Playnite is done with it.
        return new WineBridgePluginMetadataProviderProviderGameSession(args.Game);
    }
}