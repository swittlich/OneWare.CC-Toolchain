using OneWare.CologneChip.Services;
using OneWare.Essentials.Controls;
using OneWare.Essentials.Models;
using OneWare.Essentials.ViewModels;
using OneWare.Settings.ViewModels;
using OneWare.Settings.ViewModels.SettingTypes;
using OneWare.UniversalFpgaProjectSystem.Fpga;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Parser;

namespace OneWare.CologneChip.ViewModels;

public class CologneChipLoaderSettingsViewModel  : FlexibleWindowViewModelBase
{
    private readonly UniversalFpgaProjectRoot _projectRoot;
    private readonly IFpga _fpga;
    
    private readonly ComboBoxSetting _shortTermModeSetting;
    private readonly ComboBoxSetting _longTermModeSetting;
    private readonly Dictionary<string, string> _settings;
    public SettingsCollectionViewModel SettingsCollection { get; } = new("CologneChip Loader Settings")
    {
        ShowTitle = false
    };

    public CologneChipLoaderSettingsViewModel(UniversalFpgaProjectRoot projectRoot, IFpga fpga)
    {
        _projectRoot = projectRoot;
        _fpga = fpga;
        
        Title = "CologneChip Loader Settings";
        Id = "CologneChip Loader Settings";
        
        var defaultProperties = fpga.Properties;
        _settings = FpgaSettingsParser.LoadSettings(projectRoot, fpga.Name);
        
        
        
        _shortTermModeSetting = new ComboBoxSetting("Short Term Mode", "Mode to use for Short Term Programming",
            defaultProperties.GetValueOrDefault(CologneChipConstantService.Instance.CologneChipShortTermModeKey) ?? "", ["JTAG", "SPI"]);
        
        _longTermModeSetting = new ComboBoxSetting("Long Term Mode", "Mode to use for Long Term Programming",
            defaultProperties.GetValueOrDefault(CologneChipConstantService.Instance.CologneChipLongTermModeKey) ?? "", ["JTAG", "SPI"]);
        
        if (_settings.TryGetValue(CologneChipConstantService.Instance.CologneChipShortTermModeKey, out var qPstMode))
            _shortTermModeSetting.Value = qPstMode;
        
        if (_settings.TryGetValue(CologneChipConstantService.Instance.CologneChipLongTermModeKey, out var qPstOperation))
            _longTermModeSetting.Value = qPstOperation;

        
        SettingsCollection.SettingModels.Add(new ComboBoxSettingViewModel(_shortTermModeSetting));
        SettingsCollection.SettingModels.Add(new ComboBoxSettingViewModel(_longTermModeSetting));

    }
    
    
    public void Save(FlexibleWindow flexibleWindow)
    {
        _settings[CologneChipConstantService.Instance.CologneChipShortTermModeKey] = _shortTermModeSetting.Value.ToString()!;
        _settings[CologneChipConstantService.Instance.CologneChipLongTermModeKey] = _longTermModeSetting.Value.ToString()!;
        
        FpgaSettingsParser.SaveSettings(_projectRoot, _fpga.Name, _settings);
        
        Close(flexibleWindow);
    }
    
    public void Reset()
    {
        foreach (var setting in SettingsCollection.SettingModels)
        {
            setting.Setting.Value = setting.Setting.DefaultValue;
        }
    }
}