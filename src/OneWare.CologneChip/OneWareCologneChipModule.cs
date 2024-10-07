using OneWare.CologneChip.ViewModels;
using OneWare.CologneChip.Views;
using OneWare.Essentials.Helpers;
using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.Essentials.ViewModels;
using OneWare.UniversalFpgaProjectSystem.Models;
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
        var settingsService = containerProvider.Resolve<ISettingsService>();
        var defaultCologneChipPath = "./";
        
        containerProvider.Resolve<IWindowService>().RegisterUiExtension("UniversalFpgaToolBar_DownloaderConfigurationExtension", new UiExtension(x =>
        {
            if (x is not UniversalFpgaProjectRoot cm) return null;
            return new CologneChipLoaderWindowExtensionView()
            {
                DataContext = containerProvider.Resolve<CologneChipLoaderWindowExtensionViewModel>((typeof(UniversalFpgaProjectRoot), cm))
            };
        }));
        
        containerProvider.Resolve<FpgaService>().RegisterToolchain<CologneChipToolchain>();
        containerProvider.Resolve<FpgaService>().RegisterLoader<CologneChipLoader>();
        
        settingsService.RegisterTitledPath("Tools", "CologneChip", "CologneChip_Path", "CologneChip Toolchain Path",
            "Sets the path for CologneChip Toolchain", defaultCologneChipPath, null, null, IsCologneChipPathValid);
        
        settingsService.GetSettingObservable<string>("CologneChip_Path").Subscribe(x =>
        {
            if (string.IsNullOrEmpty(x)) return;

            if (!IsCologneChipPathValid(x))
            {
                containerProvider.Resolve<ILogger>().Warning("CologneChip Toolchain path invalid", null, false);
                return;
            }

            var yosys = Path.Combine(x, "bin/yosys");
            var pr = Path.Combine(x, "bin/p_r");
            var openFpgaLoader = Path.Combine(x, "bin/openFPGALoader");
            
            ContainerLocator.Container.Resolve<IEnvironmentService>().SetPath("CC_yosys", yosys);
            ContainerLocator.Container.Resolve<IEnvironmentService>().SetPath("CC_p_r", pr);
            ContainerLocator.Container.Resolve<IEnvironmentService>().SetPath("CC_openFPGALoader", openFpgaLoader);
        });

    }
            
    private static bool IsCologneChipPathValid(string path)
    {
        if (!Directory.Exists(path)) return false;
        
        if (!File.Exists(Path.Combine(path, "VERSION"))) return false;
        if (!File.Exists(Path.Combine(path, "bin", "VERSION"))) return false;
        if (!Directory.Exists(Path.Combine(path, "bin", "yosys"))) return false;
        if (!Directory.Exists(Path.Combine(path, "bin", "p_r"))) return false;
        if (!Directory.Exists(Path.Combine(path, "bin", "openFPGALoader"))) return false;
        
        return true;
    }
}