using System.Windows.Media;
using Playnite;
using Fonts = Playnite.Fonts;

namespace WineBridgePlugin;

public class WineBridgePluginPlugin : Plugin
{
    // This creates logger for current class names after the class.
    // You can also specify custom name in GetLogger("custom name") call.
    private static readonly ILogger logger = LogManager.GetLogger();

    // Same ID as the one specified in "extension.toml" manifest.
    // You will need to reference this when comparing some objects and requests
    // that have specific plugin owner. To check if it's relevant to you.
    public const string Id = "Yalgrin.WineBridgePlugin";

    // null! is safe here since we are assigning it in InitializeAsync.
    // Unless you try to access the API in plugin constructor, which you shouldn't,
    // this is fine and removes extra null checks later.
    public static IPlayniteApi PlayniteApi { get; private set; } = null!;

    // ID for testing custom game data property from WineBridgePluginData;
    // This is going to be used for metadata support, game edit, grouping, sorting etc.
    // Any place where you are going to reference any work done with this specific data.
    public const string TestPropertyId = "WineBridgePlugin.TestPropery";

    public IPluginLibraryCollection<WineBridgePluginData> DataCollection = null!;

    public WineBridgePluginPlugin()
    {
        // Used only for cases where you also implement GetPluginGameDataPresenter for themes to display your data.
        // XamlId = "WineBridgePlugin";

        // If you are implementing library integration features, set appropriate features via LibrarySettings property.
        // LibrarySettings = new LibrarySupport();

        // If you are implementing metadata provider features, set appropriate features via MetadataSettings property.
        // MetadataSettings = new MetadataSupport();

        // If you want to provide icons for your plugin, just make sure following are available in plugin folder.
        // png, jpg and jpeg are supported:
        // - pluginicon.png: general plugin icon used at various places
        // - libraryicon.png: used at places referencing library integration features
        // - clienticon.png: used at places referencing the original/external library client (GOG Galaxy, Steam client etc.)

        // If you have readme.md in plugin folder, Playnite will display it in addons view when opening section for your plugin.
    }

    // Since a lot of methods in the API now have asynchronous signature (they expect Task as return value),
    // but we are not doing async work here that would require await keyword, you might get a warning from the compiler.
    //
    // To fix it, you have generally three options:
    //  - ignore/suppress the warning (it's generally non-issue in our use case in these override methods)
    //  - await completed task somewhere in the method: "await Task.CompletedTask;"
    //  - return completed task from the method: `return Task.CompletedTask;` or using Task.FromResult() if the method returns a value.
    //
    // I personally recommend one of the first two options.
    // Here's good blog post explaining why the third option might not be a good idea:
    // https://blog.stephencleary.com/2016/12/eliding-async-await.html


    // Any initialization related to the SDK and Playnite functionality should be handled here.
    public override async Task InitializeAsync(InitializeArgs args)
    {
        PlayniteApi = args.Api;

        // This is needed this for built-in localization system to work.
        // See LocalizationExample() method for more info.
        Loc.Api = args.Api;

        // This will return sqlite backed collection for any data you want to store.
        // The collection has the same functionality as built-in collection.
        // The two arguments specify:
        // - cacheData: when enabled all items will be held in memory and all read operation will use that cache instead of making sqlite queries.
        // - multiType: in case you want to store multiple data types into one collection. All data types still have to inherit from <T>
        DataCollection = PlayniteApi.Library.GetCustomCollection<WineBridgePluginData>(true, false);

        // Unlike with built-in collection that send data updates via On*CollectionChange callbacks,
        // or custom collections we need to register to specific event handler.
        DataCollection.CollectionChanged += DataCollectionOnCollectionChanged;

        // Useful folders:
        // This is recommended folder where you should store any user related and generated files.
        var pluginDir = PlayniteApi.UserDataDir;

        // Installation directory of your plugin, in case you need to reference files from your installation package.
        // This folder gets completely removed during plugin update, don't store any user data here.
        var installDir = args.PluginInstallDir;
    }

    // Following are descriptions of specific method features implementations.

    // You should remove any methods you are actually not implementing.

    public override async ValueTask DisposeAsync()
    {
        // If you need to gracefully dispose of some resources on application shutdown, do it here.
    }

    public override async Task<CalculateGameInstallSizeResult?> CalculateGameInstallSizeAsync(
        CalculateGameInstallSizeArgs args)
    {
        // Implement this method if this is library integration plugin, and you want to provide custom way of calculating game installation size.
        // If the method is missing, Playnite will calculate install size by calculating size of game's installation folder.
        return null;
    }

    public override GetInstallationDirectoryResult? GetInstallationDirectory(GetInstallationDirectoryArgs args)
    {
        // This is currently used only for "Open installation location" feature.
        // Implement this method if you are implementing library plugin and assigned installation directory
        // is not a standard location Playnite could open on its own, or is "dynamic" one.
        // Return full directory path.
        return null;
    }

    public override async Task<CollectDiagnosticDataArgsAsyncResult?> CollectDiagnosticDataArgsAsync(
        CollectDiagnosticDataArgs args)
    {
        // Implement this method if you want to gather custom data when user generates diagnostics data for your plugin.
        // This can be run manually by user from addons view or on crash dialog that detected your plugin to be the source of the crash.
        // If the method is missing, Playnite collects extension log.
        return null;
    }

    // Certain functionality related to adding UI items, like menu items or sidebar items, are now split into two method.
    // First, Playnite will call Get*Descriptors method which tells Playnite all the items your plugin can provide
    // for specific item type. These descriptors are used primarily for customization views where a user can choose where
    // specific items should be displayed and in what order.
    // Second, when it's time to actually load the item into the UI, Get* method is called
    // with an argument for specific item ID.
    // Following two method examples are for game menu items, but it works the same for app menu, sidebar items, app views etc.
    public override ICollection<MenuItemDescriptor> GetGameMenuItemDescriptors(GetGameMenuItemDescriptorsArgs args)
    {
        return
        [
            new MenuItemDescriptor("game.menu.item.id", "Test item"),
            // You can also specify subsection for the item, which used in settings views.
            new MenuItemDescriptor("game.menu.other.item.id", "Some other test", "Test section"),
        ];
    }

    // This will get called every time a menu item is to be loaded in the UI.
    public override ICollection<MenuItemImpl>? GetGameMenuItems(GetGameMenuItemsArgs args)
    {
        if (args.ItemId == "game.menu.other.item.id")
        {
            // GetGameMenuItemsArgs args contains some useful info, like for use case in which menu items are being loaded for:
            // main game list, keyboard search results, detail view menu etc.
            // And also contains games in selection, in case you need to return different items based on number of selected games.

            // Icons for various items in the SDK are of UIIcon type.
            // You can either implement custom icon, or use some predefined types via UIIcon.From* methods.
            // This example creates an icon using font symbol from https://www.nerdfonts.com project.
            // Playnite comes bundled with NerdFonts and IcoFonts, but you can use any FontFamily here.
            var icon = UIIcon.FromFontIcon("f0668", Fonts.NerdFont, new SolidColorBrush(Colors.DeepPink));

            return
            [
                new MenuItemImpl("Some other test", async () =>
                {
                    // This gets executed when menu item is clicked
                    await PlayniteApi.Dialogs.ShowMessageAsync("called from plugin");
                }, icon: icon)
            ];
        }

        return null;
    }

    // AddGameMenu works exactly the same as GameMenu related methods.
    // You can use this to add new items to "Add game" main menu and add game sidebar item.
    public override ICollection<MenuItemDescriptor> GetAddGameMenuItemDescriptors(
        GetAddGameMenuItemDescriptorsArgs args)
    {
        return [];
    }

    public override ICollection<MenuItemImpl>? GetAddGameMenuItems(GetAddGameMenuItemsArgs args)
    {
        return null;
    }

    // AppMenu works exactly the same as GameMenu related methods.
    // Use can use this to add new items to the main menu and tray menu.
    public override ICollection<MenuItemDescriptor>? GetAppMenuItemDescriptors(GetAppMenuItemDescriptorsArgs args)
    {
        return [];
    }

    public override ICollection<MenuItemImpl>? GetAppMenuItems(GetAppMenuItemsArgs args)
    {
        return null;
    }

    // Return list of all possible application views. Application view can replace the entire Playnite view.
    // Built-in example being library view and downloads view. Playnite also provides some built-in things for there,
    // like generated sidebar items and view menu options, for user to switch into the view.
    public override ICollection<AppViewItemDescriptor>? GetAppViewItemDescriptors(GetAppViewItemDescriptorsArgs args)
    {
        return
        [
            new AppViewItemDescriptor(
                "test.view.id",
                "Test view",
                // Icon used for sidebar item:
                (iconArgs) => UIIcon.FromFontIcon("f0668", Fonts.NerdFont),
                // Icon used for when the view is activated:
                (iconArgs) => UIIcon.FromFontIcon("f0668", Fonts.NerdFont, new SolidColorBrush(Colors.DeepPink)))
        ];
    }

    public override AppViewItem? GetAppViewItem(GetAppViewItemsArgs args)
    {
        if (args.ViewId == "test.view.id")
            return new WineBridgePluginTestAppView();

        return null;
    }

    // Implement this if you want to provide custom view and functionality for game edit dialog.
    // If you want to allow users to change your game data that way.
    public override async Task<GameEditSessionHandler?> GetGameEditHandlerAsync(GetGameEditHandlerArgs args)
    {
        // In case you want to only support game edit scenarios for a single game
        if (args.Games.Count == 1)
        {
            // Check WineBridgePluginGameEditSessionHandler implementation for more details.
            return new WineBridgePluginGameEditSessionHandler(args.Games[0], this);
        }

        return null;
    }

    // Implement this if you want to provide settings view functionality for your plugin that will be shown on addons views.
    public override async Task<PluginSettingsHandler?> GetSettingsHandlerAsync(GetSettingsHandlerArgs args)
    {
        // Check WineBridgePluginSettingsHandler implementation for more details.
        return new WineBridgePluginSettingsHandler(this);
    }

    // Similar with menu items, this is split between descriptors, items user is given as a choice to group by,
    // and the actual implementations returned by GetGameGrouper.
    // This also works the same for sorting, filtering and exploring so only the grouping example is here.
    // This example provides groping based on TestProperty custom data from WineBridgePluginData.
    public override ICollection<GameGrouperDescriptor> GetGameGrouperDescriptors(GetGameGrouperDescriptorsArgs args)
    {
        return
        [
            new GameGrouperDescriptor(TestPropertyId, "Test property")
        ];
    }

    public override GameGrouper? GetGameGrouper(GetGameGroupersArgs args)
    {
        if (args.ItemId == TestPropertyId)
            return new TestPropertyGrouper(this);

        return null;
    }

    // Sorting support for out TestProperty data field. Similar concepts like with grouping support.
    public override ICollection<GameSorterDescriptor> GetGameSorterDescriptors(GetGameSorterDescriptorsArgs args)
    {
        return
        [
            new GameSorterDescriptor(TestPropertyId, "Test property")
        ];
    }

    public override GameSorter? GetGameSorter(GetGameSortersArgs args)
    {
        if (args.ItemId == TestPropertyId)
            return new TestPropertySorter(this);

        return null;
    }

    // Filter support, again very similar to grouping and sorting stuff.
    public override ICollection<GameFiltererDescriptor> GetGameFilterDescriptors(GetGameFiltereDescriptorsArgs args)
    {
        return
        [
            new GameFiltererDescriptor(TestPropertyId, "Test property")
        ];
    }

    public override GameFilterer? GetGameFilterer(GetGameFilterersArgs args)
    {
        if (args.ItemId == TestPropertyId)
            return new TestPropertyGameFilterer(this, args);

        return null;
    }

    // Exploring support for out TestProperty data field. Similar concepts like with grouping support.
    public override ICollection<GameExplorerDescriptor> GetGameExplorerDescriptors(GetGameExplorerDescriptorsArgs args)
    {
        return
        [
            new GameExplorerDescriptor(TestPropertyId, "Test property")
        ];
    }

    public override GameExplorer? GetGameExplorer(GetGameExplorersArgs args)
    {
        if (args.ItemId == TestPropertyId)
            return new TestPropertyGameExplorer();

        return null;
    }

    // GetMetadataDataSupportDescriptors and GetGameMetadataSessionHandler are used in cases if you want
    // other plugins to provide data for your plugin via built-in metadata download features.
    // GetMetadataDataSupportDescriptors specifies what "data fields" can be downloaded.
    public override ICollection<MetadataDataSupportDescriptor> GetMetadataDataSupportDescriptors(
        GetMetadataDataSupportDescriptorsArgs args)
    {
        return
        [
            // MetadataDataSupportDescriptor can also specify whether this data field can:
            // - merge data from multiple sources
            // - apply data on top of existing data
            new MetadataDataSupportDescriptor(TestPropertyId, "Test property")
        ];
    }

    // This will be requested during bulk metadata download and controls how you actually save
    // data from metadata plugins for your data fields.
    // Data handling during metadata download via game edit view is handled by GameEditSessionHandler.
    // See WineBridgePluginGameEditSessionHandler implementation for more details on that.
    public override GameMetadataSessionHandler? GetGameMetadataSessionHandler(GetGameMetadataSessionHandlerArgs args)
    {
        if (args.DataId == TestPropertyId)
            return new TestPropertyGameMetadataSessionHandler(this);

        return null;
    }

    // This is the default method that gets called if your plugin is reporting any LibrarySettings.
    public override async Task<List<ImportableGame>> GetGamesAsync(LibraryGetGamesArgs args)
    {
        // Return list of importable games here. Playnite will take of automatically updating existing
        // entries with new data like play time, install state etc.
        return [];
    }

    // If your plugin reports LibrarySettings.HasCustomGameImport, this will get called instead of GetGamesAsync.
    public override async Task<List<Game>> ImportGamesAsync(ImportGamesArgs args)
    {
        // You are responsible for the entire library update process. Importing new entries, updating existing ones etc.
        // Playnite is not making any automatic changes during this process to library data.
        // Return all newly imported games so metadata download for newly added games can be properly processed later.
        return [];
    }

    public override async Task OpenClientAsync(OpenClientArgs args)
    {
        // This will get called if LibrarySettings.CanOpenOriginalClient is set.
        // You should open the original client for this library if relevant, like Galaxy client for GOG games.
    }

    public override async Task ShutdownClientAsync(ShutdownClientArgs args)
    {
        // This will get called if LibrarySettings.CanCloseOriginalClient is set and
        // option to close the original client after game exits is enabled by a user.
        // Close the original for this library client that's used to run games and my still run be running.
    }

    // Same method are available for other built-in collection, like for example OnGenreCollectionChange.
    public override async Task OnGameCollectionChange(DataCollectionChangeArgs<Game> args)
    {
        // This is called when data in the collection are changed.
        // args.AddedItems, args.RemovedItems, args.UpdatedItems
    }

    // Handler for our custom collection. Practically the same functionality as with the Game collection handler in previous example.
    private async Task DataCollectionOnCollectionChanged(DataCollectionChangeArgs<WineBridgePluginData> data,
        object sender, CancellationToken cancellationToken)
    {
    }

    public override ICollection<SearchItem>? GetGlobalSearchItems(GetGlobalSearchItemsArgs args)
    {
        // Implement this if you want to support generic action in keyboard search.
        // Ideally cache the items since this can get called quite often.
        return [];
    }

    // This will get called when game is being started. If your plugin knows how to start the game,
    // because you are a library plugin and you imported this game, or you are just providing alternative
    // ways of running games, this is where you do it.
    public override async Task<List<PlayController>> GetPlayActionsAsync(GetPlayActionsArgs args)
    {
        // You can also use prepared controllers in case you need simple startup and tracking procedure.
        // So you don't have to implement the entire PlayController from scratch.
        var controller = new AutomaticFilePlayController(new FileGameAction
        {
            Path = "some path",
            Arguments = "some argument",
            TrackingOptions = new()
            {
                // tracking options
            }
        });

        // This would be standard check if you are a library plugin, and you want to check if your game is being launched.
        if (args.Game.LibraryId == Id)
        {
        }

        return [];
    }

    public override async Task<List<InstallController>> GetInstallActionsAsync(GetInstallActionsArgs args)
    {
        // Implement this if you know how to install args.Game.
        return [];
    }

    public override async Task<List<UninstallController>> GetUninstallActionsAsync(GetUninstallActionsArgs args)
    {
        // Implement this if you know how to uninstall args.Game.
        return [];
    }

    // Implement this method if you are implementing metadata provider via MetadataSettings.
    public override async Task<MetadataProvider?> GetMetadataProviderAsync(GetMetadataProviderArgs args)
    {
        // This requests downloader instance for each metadata session.
        // A session being bulk metadata download initiated by a user or during library update,
        // or manual download from game edit view.
        // args can be used to know more info about the session, including list of games that will be updated.
        return new WineBridgePluginMetadataProvider();
    }

    // Implement if you want to provide sidebar items.
    public override ICollection<SidebarItemDescriptor>? GetSidebarItemDescriptors(GetSidebarItemDescriptorsArgs args)
    {
        return
        [
            new SidebarItemDescriptor("sidebar.item.id", "Test item")
            {
                SuggestedPosition = SideBarPosition.Top,
                SupportedPositions = SideBarPosition.All
            }
        ];
    }

    public override ICollection<SidebarItem>? GetSidebarItems(GetSidebarItemsArgs args)
    {
        // Unlike with menu items, you have to implement your own SidebarItem and return it here.
        // args.Position if you need to return different implementations based on item position.

        // BasicSidebarItem is built-in implementation that provides some basic features
        // This might be enough if you want simple icon based sidebar item.
        if (args.Id == "sidebar.item.id")
            return
            [
                new BasicSidebarItem(
                    UIIcon.FromFontIcon("f01e5", Fonts.NerdFont),
                    async (clickArgs) => await PlayniteApi.Dialogs.ShowMessageAsync("Message from sidebar item!")),

                // There's also overload that allows you to create items with a popup on click.
                // This example creates sidebar item with context menu popup.
                new BasicSidebarItem(
                    UIIcon.FromFontIcon("f011b", Fonts.NerdFont),
                    new SidebarItemPopup((menuArgs) =>
                    [
                        new MenuItemImpl(
                            "Test item",
                            async () => await PlayniteApi.Dialogs.ShowMessageAsync("What does the dog say?"),
                            icon: UIIcon.FromFontIcon("f0a43", Fonts.NerdFont)),
                        new MenuItemImpl(
                            "Test item",
                            async () => await PlayniteApi.Dialogs.ShowMessageAsync("What does the fox say?"),
                            icon: UIIcon.FromFontIcon("e855", Fonts.IcoFont)),
                    ]))
            ];

        return null;
    }

    // Built-in localization uses Fluent (https://projectfluent.org/) files that should be stored in "Localization" sub-folder.
    // en_US.ftl is used as default English file, and you can have multiple *en_US.ftl default, for example if you want to include
    // some shared strings from different project.
    public void LocalizationExample()
    {
        // Get string by a key
        var example1 = Loc.GetString("example_string");

        // You can also generate keys for type safety using toolbox.
        // To do that, run Toolbox with ftlgen arguments:
        // Toolbox.exe ftlgen "path_where_ftl_files_are" "path_where_gen_output_should_be"

        // This will generate LocId class that you can then use this way:
        var example2 = Loc.GetString(LocId.example_string);

        // For strings that accept parameter(s):
        var example3 = Loc.GetString(LocId.example_string_param, ("param", "param value"));

        // Check WineBridgePluginSettingsView.xaml to see how to use this in XAML views.
    }

    // Sometimes when calling method from background threads you may need to explicitly run things on the UI thread.
    // This is mainly needed if you are doing things that touch UI components in some way.
    public void UIDispatchedExample()
    {
        // To execute stuff on the main thread use UIDispatched methods.
        UIDispatcher.Invoke(() => logger.Warn("Called on the UI thread explicitly."));
    }

    public async Task LibraryOperationsActions()
    {
        // These examples are on Game collection but the same apply to all, including custom ones.

        // Get item
        var example1 = PlayniteApi.Library.Games.Get("gameId");

        // Iterate through all items
        foreach (var game in PlayniteApi.Library.Games)
        {
        }

        // Add new item
        var newGame = new Game("New game");
        await PlayniteApi.Library.Games.AddAsync(newGame);

        // Update item
        newGame.Name = "new name";
        await PlayniteApi.Library.Games.UpdateAsync(newGame);

        // UpdateAsync has extra overloads you use for batch updates, one example:
        await PlayniteApi.Library.Games.UpdateAsync(
            // List of games to update
            ["gameID1", "gameID2"],
            // Update action that changes game data
            (g) =>
            {
                g.LastPlayedDate = DateTimeOffset.Now;
                g.CriticScore = 20;
            });


        // Remove itm
        await PlayniteApi.Library.Games.RemoveAsync(newGame.Id);

        // AddAsync, RemoveAsync and UpdateAsync also accept multiple items for bulk changes.
        // Each change method call can, and often does, force certain things in the UI to reload,
        // so making single call with multiple items is preferred over multiple one item calls.

        // If you want to make large amount of different type of changes in one go, use MakeBulkChangesAsync method.
        await PlayniteApi.Library.Games.MakeBulkChangesAsync(
            [], // Items to add
            [], // Items to update
            []); // Items to remove
    }

    public async Task BackgroundOperationsExample()
    {
        // This puts new operation into background queue.
        // When and if the operation will be started is controlled by Playnite a user.
        PlayniteApi.AddBackgroundOperation(new TestBackgroundOperation());
    }
}