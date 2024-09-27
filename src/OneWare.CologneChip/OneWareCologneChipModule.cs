using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.Essentials.ViewModels;
using Prism.Ioc;
using Prism.Modularity;

using OneWare.UniversalFpgaProjectSystem.Services;

namespace OneWare.CologneChip;

public class OneWareCologneChipModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        containerProvider.Resolve<FpgaService>().RegisterToolchain<CologneChipToolchain>();
        //This example adds a context menu for .vhd files
        containerProvider.Resolve<IProjectExplorerService>().RegisterConstructContextMenu((selected, menuItems) =>
        {
            if (selected is [IProjectFile {Extension: ".vhd"} vhdFile])
            {
                menuItems.Add(new MenuItemViewModel("Hello World")
                {
                    Header = $"Hello World {vhdFile.Header}"
                });
            }
        });
    }
}