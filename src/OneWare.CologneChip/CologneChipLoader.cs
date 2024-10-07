using OneWare.CologneChip.Services;
using OneWare.Essentials.Enums;
using OneWare.Essentials.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Parser;
using OneWare.UniversalFpgaProjectSystem.Services;
using ILogger = OneWare.Essentials.Services.ILogger;

namespace OneWare.CologneChip;

public class CologneChipLoader(IChildProcessService childProcessService, ISettingsService settingsService, ILogger logger)
    : IFpgaLoader
{
    public string Name => "CologneChip";
    
    private enum ProgrammerState
    {
        JTagFlash,
        JTagMemory,
        SpiFlash,
        SpiMemory,
        IllegalState,
    }

    private ProgrammerState GetProgrammerState(UniversalFpgaProjectRoot projectRoot)
    {
        var fpga = projectRoot.GetProjectProperty("Fpga");
        if (fpga == null) return ProgrammerState.IllegalState;
        
        var properties = FpgaSettingsParser.LoadSettings(projectRoot, fpga);
        
        if (settingsService.GetSettingValue<bool>("UniversalFpgaProjectSystem_LongTermProgramming"))
        {
            var longTermMode = properties.GetValueOrDefault(CologneChipConstantService.Instance.CologneChipLongTermModeKey) ?? "JTAG";
            return longTermMode switch
            {
                "JTAG" => ProgrammerState.JTagFlash,
                "SPI" => ProgrammerState.SpiFlash,
                _ => ProgrammerState.IllegalState
            };
        } 
        
        var shortTermMode = properties.GetValueOrDefault(CologneChipConstantService.Instance.CologneChipShortTermModeKey) ?? "JTAG";
        return shortTermMode switch
        {
            "JTAG" => ProgrammerState.JTagMemory,
            "SPI" => ProgrammerState.SpiMemory,
            _ => ProgrammerState.IllegalState
        };
    }
    
    public async Task DownloadAsync(UniversalFpgaProjectRoot project)
    {
        
        var top = project.TopEntity?.Header ?? throw new Exception("TopEntity not set!");
        var topName = top.Split(".").First();
        var outputDir = project.FullPath;
        
        
        var state = GetProgrammerState(project);
        
        List<string> fpgaArgs = [];
        switch (state)
        {
            case ProgrammerState.JTagFlash:
                fpgaArgs = ["-b", "gatemate_evb_jtag", "-f", "--verify", $"{topName}_00.cfg.bit"];
                break;
            case ProgrammerState.JTagMemory:
                fpgaArgs = ["-b", "gatemate_evb_jtag", "-m", $"{topName}_00.cfg.bit"];
                break;
            case ProgrammerState.SpiFlash:
                fpgaArgs = ["-b", "gatemate_evb_spi", $"{topName}_00.cfg.bit"];
                break;
            case ProgrammerState.SpiMemory:
                fpgaArgs = ["-b", "gatemate_evb_spi", "-m", $"{topName}_00.cfg.bit"];
                break;
            case ProgrammerState.IllegalState:
                logger.Error("IllegalState");
                throw new Exception("IllegalState");
        }
        
        await childProcessService.ExecuteShellAsync("openFPGALoader", fpgaArgs,
            outputDir, "Running Quartus programmer (Short-Term)...", AppState.Loading, true);
    }
}