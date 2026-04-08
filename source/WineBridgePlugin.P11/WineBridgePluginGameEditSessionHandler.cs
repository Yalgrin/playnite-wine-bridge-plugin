using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Playnite;

namespace WineBridgePlugin;

// This will get serialized using JSON if you are storing it via custom
// collection made using GetCustomCollection library API. Using .NET's built-in System.Text.Json serializer.
// So you need to make sure the object is serializable and members you don't want to save
// must be decorated with [JsonIgnore] attribute.
public partial class WineBridgePluginData : LibraryObject
{
    // [ObservableProperty] is from MVVM toolkit that will automatically generate bindable TestProperty property for this field.
    [ObservableProperty] private bool testProperty;

    public WineBridgePluginData GetCopy()
    {
        return new()
        {
            Id = Id,
            Name = Name,
            TestProperty = TestProperty
        };
    }
}

public class WineBridgePluginGameEditSessionSection : GameEditSessionSection
{
    public WineBridgePluginGameEditSessionSection(string sectionName, FrameworkElement view) :
        base(sectionName, view)
    {
        // GameEditSessionSection has "ShowChangeNotif", set this to true to indicate that specific view has pending changes.
        // This is the same notification that Playnite shows for built-in fields.
    }
}

public class WineBridgePluginGameEditSessionHandler : GameEditSessionHandler
{
    private readonly Game game;
    private readonly WineBridgePluginPlugin plugin;

    public WineBridgePluginData EditData { get; }

    public WineBridgePluginGameEditSessionHandler(Game game, WineBridgePluginPlugin plugin)
    {
        this.game = game;
        this.plugin = plugin;

        // There are many way how to handle these data edit scenarios.
        // This is just an example using built-in support for custom collections.
        // You can handle this any other way you want, store the data in different format or place.

        var existing = plugin.DataCollection.Get(game.Id);
        if (existing is not null)
        {
            // If you are using data collection with memory cache enabled, then you want to get a copy here.
            // Otherwise, you would be editing your "live" instance potentially already used elsewhere,
            // and made it harder to cancel changed.
            EditData = existing.GetCopy();
        }
        else
        {
            EditData = new WineBridgePluginData();
        }
    }

    public override async Task<List<GameEditSessionSection>> GetEditSectionsAsync(GetEditSectionsAsyncArgs args)
    {
        return
        [
            new WineBridgePluginGameEditSessionSection("WineBridgePlugin", new WineBridgePluginGameEditView
            {
                DataContext = this // Set this to whatever you want your view to bind into
            })
        ];
    }

    public override async Task BeginEditAsync(BeginEditArgs args)
    {
        // This gets called when game edit session is started
    }

    public override async Task EndEditAsync(EndEditArgs args)
    {
        // This gets called when game edit session ends by user saving the data.
        var existing = plugin.DataCollection.Get(game.Id);
        if (existing is null)
            await plugin.DataCollection.AddAsync(EditData);
        else
            await plugin.DataCollection.UpdateAsync(EditData);
    }

    public override async Task CancelEditAsync(CancelEditArgs args)
    {
        // This gets called when game edit session is canceled.
        // Meaning user closed game edit view without saving any changes.
    }

    public override bool GetHasUnsavedChanges(GetHasUnsavedChangesArgs args)
    {
        // Return here whether there were any changes made since this edit session started.
        // If you return true, the users will get warned about unsaved changes when cancelling out of the sessions.

        // Usually you will either need to track all changes made on your editing object.
        // Or compare the original state with current one.
        return false;
    }

    public override async Task<ICollection<string>> VerifyDataAsync(VerifyDataArgs args)
    {
        // This gets called when user is about to save data changes.
        // You can do state validation here and return any errors/bad state if you find some.
        // Returning empty collection indicates that everything is ok and data can be saved.
        return [];
    }

    // Following methods are related to handling of metadata download support for your fields.
    // If your plugin reported any metadata field support via GetMetadataDataSupportDescriptors
    // and some metadata plugin provides data for your data field, these will get called.

    // GetCurrentValuePreviewArgsAsync and GetNewValuePreviewArgsAsync are used on
    // metadata comparison view, to allow user to choose between currently assigned and new metadata.
    public override async Task<GameDataDiffPreview?> GetCurrentValuePreviewArgsAsync(GetCurrentValuePreviewArgs args)
    {
        // Use args.DataId to know for which data field is this request for.
        return null;
    }

    public override async Task<GameDataDiffPreview?> GetNewValuePreviewArgsAsync(GetNewValuePreviewArgs args)
    {
        return null;
    }

    // This is used by Playnite to potentially skip metadata diff dialog completely,
    // if old and new data are the same.
    public override async Task<bool> GetIsNewValueDifferentAsync(GetIsNewValueDifferentArgs args)
    {
        // args.CurrentValue has data you returned from GetCurrentValuePreviewArgsAsync.
        // args.NewValue has data you returned from GetNewValuePreviewArgsAsync.
        return true;
    }

    public override async Task ApplyDataAsync(ApplyDataArgs args)
    {
        // args.CurrentValue has data you returned from GetCurrentValuePreviewArgsAsync.
        // args.NewValue has data you returned from GetNewValuePreviewArgsAsync.

        // args.PrimarySelection defines whether current or new value was selected by a user.
        // However, you can of course combine data from current and new values if your data field allows that
        // and your previews have that option. For example, like some Playnite fields, like genres,
        // where a user can choose some values from current selection and some new ones.
    }
}