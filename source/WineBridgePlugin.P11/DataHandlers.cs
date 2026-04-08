using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Playnite;

namespace WineBridgePlugin;

public class TestPropertyGrouper : GameGrouper
{
    private readonly WineBridgePluginPlugin plugin;

    // We can prepare groups in advance here since we have simple bool properly.
    private static readonly List<List<GameGroup>> groups =
    [
        [new GameGroup("1", "Test property true")],
        [new GameGroup("2", "Test property false")],
        [new GameGroup("3", "None")]
    ];

    // affectedDataIds are linked to DataChangedAsync method you can call from your plugin.
    // Calling DataChangedAsync with an ID set in affectedDataIds will tell Playnite that the list might need
    // regrouping if grouping by one of the affectedDataIds is currently being applied.
    // This works the same way for sorting as well if you need to call for the list re-sort.
    public TestPropertyGrouper(WineBridgePluginPlugin plugin) : base([WineBridgePluginPlugin.TestPropertyId])
    {
        this.plugin = plugin;
    }

    public override List<GameGroup>? GetGroups(GetGroupsArgs args)
    {
        // You can return multiple groups here for one game, in case you want to show that game
        // as being part of multiple groups in the game list.
        var data = plugin.DataCollection.Get(args.Game.Id);
        if (data is null)
            return groups[2];

        if (data.TestProperty)
            return groups[0];

        return groups[1];
    }

    // This expects the same int value as standard IComparable implementations:
    // Less than zero: GroupA precedes GroupB in the sort order.
    // Zero: GroupA instance occurs in the same position in the sort order as GroupB.
    // Greater than zero: GroupA instance follows GroupB in the sort order.
    public override int CompareGroups(CompareGroupsArgs args)
    {
        // This controls how groups themselves are sorted in the game list.
        // Very simple way of doing this is to have group IDs in some sortable format and just sort by those.
        return args.GroupA.Id.CompareTo(args.GroupB.Id, StringComparison.Ordinal);
    }

    // You can use these two if you need to do some warmup of the data before grouping is actually done.
    public override void BeginGrouping(BeginGroupingArgs args)
    {
    }

    public override void EndGrouping(EndGroupingArgs args)
    {
    }
}

public class TestPropertySorter : GameSorter
{
    public TestPropertySorter(WineBridgePluginPlugin plugin) : base([WineBridgePluginPlugin.TestPropertyId])
    {
    }

    // Not very applicable to out case since our TestProperty is bool value.
    // But do standard sort order comparison with int result, like for normal IComparable implementations:
    // Less than zero: GroupA precedes GroupB in the sort order.
    // Zero: GroupA instance occurs in the same position in the sort order as GroupB.
    // Greater than zero: GroupA instance follows GroupB in the sort order.
    public override int CompareForSort(CompareArgs args)
    {
        // args.GameA and args.GameB are games currently being compared for sorting.
        return 0;
    }

    // You can use these two if you need to do some warmup of the data before sorting is actually done.
    public override void BeginSort(BeginSortArgs args)
    {
    }

    public override void EndSort(EndSortArgs args)
    {
    }
}

public partial class TestPropertyGameFiltererSettings : ObservableObject
{
    [ObservableProperty] private bool? state;
}

public class TestPropertyGameFilterer : GameFilterer
{
    private readonly WineBridgePluginPlugin plugin;
    public TestPropertyGameFiltererSettings Settings { get; } = new();

    public TestPropertyGameFilterer(WineBridgePluginPlugin plugin, Plugin.GetGameFilterersArgs args) : base(args)
    {
        this.plugin = plugin;
        if (args.Settings is not null)
        {
            // Load previously settings that we previously serialized using SerializeSettings.
            // For example if you serialized your settings into string JSON, you'll get that JSON string here.
        }

        // We need to know where a user changed filer settings so we can tell Playnite to re-filter game list.
        // This might get more complex depending on how complex your filterer (settings) is.
        // We can just listen to properties changing on our settings class in this example.
        Settings.PropertyChanged += SettingsOnPropertyChanged;

        View = new TestPropertyFilterView
        {
            DataContext = this
        };

        // This tells Playnite whether filterer has some settings applied and is considered to be "active".
        IsActive = Settings.State != null;
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        IsActive = Settings.State != null;
        // Tell Playnite that re-filtering is needed.
        if (IsActive)
            FilterChangedAsync(new FilterChangedArgs());
    }

    public override bool Filter(FilterGameArgs args)
    {
        // Return true if game passed filter criteria and therefore should appear in the game list.
        return true;
    }

    public override void ClearFilter(ClearFilterArgs args)
    {
        // This gets called whe a user request filter to be reset to no filtering state.
        Settings.State = null;

        // We also then need to tell Playnite to reload the list, but that gets automatically
        // called in our case via SettingsOnPropertyChanged.
    }

    // You can use these two if you need to do some warmup of the data before filtering is actually done.
    public override void BeginFiltering(BeginFilteringArgs args)
    {
    }

    public override void EndFiltering(EndFilteringArgs args)
    {
    }

    // This will get called when Playnite is about to save filter's settings.
    // Usually when a user is saving specific view configuration or by Playnite when last used
    // filtering is saved.
    public override SerializeSettingsResult? SerializeSettings(SerializeSettingsArgs args)
    {
        // Return string serialized settings, for example in JSON format.
        // These will get later passed on via a constructor so you can load them,
        // if filterer instance is set to be from previous state.
        return null;
    }

    // This will get called when data explorer requests specific settings to be applied.
    public override void ApplyExplorerFilter(ApplyExplorerFilterArgs args)
    {
        // args.FilterData has filtering data requested to be applied.
        // If you are not providing exploring support for fields, don't implement this method.

        // Our TestPropertyGameExplorer sends bool value which will be in args.FilterData.Data
        if (args.FilterData.Data is bool boolVal)
            Settings.State = boolVal;
    }
}

public class TestPropertyGameExplorer : GameExplorer
{
    // Our test property is bool value so we know all possible items in advance.
    private static readonly List<GameExplorerItem> items =
    [
        new GameExplorerItem("explorer.id.1", "True",
            // This defines what filterer will be requested when user selects this explorer and
            // what value will be passed into it to filter by.
            new GameExplorerFilterData(WineBridgePluginPlugin.TestPropertyId, true)),
        new GameExplorerItem("explorer.id.2", "False",
            new GameExplorerFilterData(WineBridgePluginPlugin.TestPropertyId, false))
    ];

    public override List<GameExplorerItem> GetExplorableItems(GetExplorableItemsArgs args)
    {
        // Return all explorable items here.
        // If you need to reload the items, call ItemsListChangedAsync method.
        // Playnite will request new items by calling this method again.
        return items;
    }
}

public class TestPropertyGameMetadataSessionHandler : GameMetadataSessionHandler
{
    private readonly WineBridgePluginPlugin plugin;

    public TestPropertyGameMetadataSessionHandler(WineBridgePluginPlugin plugin)
    {
        this.plugin = plugin;
    }

    public override bool HasValue(HasValueArgs args)
    {
        // Return true here if specific args.Game already has any data for this data field.
        // This is used for cases where users chooses to only download and apply metadata when they are missing.
        return plugin.DataCollection.Get(args.Game.Id)?.TestProperty is not null;
    }

    public override async Task<bool> ApplyValueAsync(ApplyValueAsyncArgs args)
    {
        // This method is called when metadata source provided single value to be applied
        if (args.Value is bool value)
        {
            var existing = plugin.DataCollection.Get(args.Game.Id);
            if (existing is null)
            {
                var newData = new WineBridgePluginData
                {
                    Id = args.Game.Id,
                    TestProperty = value
                };

                await plugin.DataCollection.AddAsync(newData);
            }
            else
            {
                existing.TestProperty = value;
                await plugin.DataCollection.UpdateAsync(existing);
            }

            return true;
        }

        return false;
    }

    public override async Task<bool> ApplyValuesAsync(ApplyValuesAsyncArgs args)
    {
        // This method will be called you metadata descriptor for this plugin reported it supports
        // merging values from multiple sources and multiple data are actually available.
        return true;
    }
}