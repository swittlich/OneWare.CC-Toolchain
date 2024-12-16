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
        PgmJTagMemory,
        PgmSpiMemory,
        PgmSpiFlash,
        PgmJTagFlash,
        DirtyJTagMemory,
        DirtyJTagFlash,
        IllegalState,
    }

    private ProgrammerState GetProgrammerState(Dictionary<string, string> properties)
    {
        var ccType = properties.GetValueOrDefault(CologneChipConstantService.CologneChipTypeKey) ?? "EVB";
        var longTermProgramming =
            settingsService.GetSettingValue<bool>("UniversalFpgaProjectSystem_LongTermProgramming");
        var longTermMode = properties.GetValueOrDefault(CologneChipConstantService.CologneChipLongTermModeKey) ?? "JTAG";
        var shortTermMode = properties.GetValueOrDefault(CologneChipConstantService.CologneChipShortTermModeKey) ?? "JTAG";
        
        switch (longTermProgramming)
        {
            case true when ccType == "EVB":
            {
                return longTermMode switch
                {
                    "JTAG" => ProgrammerState.JTagFlash,
                    "SPI" => ProgrammerState.SpiFlash,
                    _ => ProgrammerState.IllegalState
                };
            }
            case false when ccType == "EVB":
            {
                
                return shortTermMode switch
                {
                    "JTAG" => ProgrammerState.JTagMemory,
                    "SPI" => ProgrammerState.SpiMemory,
                    _ => ProgrammerState.IllegalState
                };
            }
            case true when ccType == "Programmer":
            {
                return longTermMode switch
                {
                    "JTAG" => ProgrammerState.PgmJTagFlash,
                    "SPI" => ProgrammerState.PgmSpiFlash,
                    _ => ProgrammerState.IllegalState
                };
            }
            case false when ccType == "Programmer":
            {
                return shortTermMode switch
                {
                    "JTAG" => ProgrammerState.PgmJTagMemory,
                    "SPI" => ProgrammerState.PgmSpiMemory,
                    _ => ProgrammerState.IllegalState
                };
            }
            case true when ccType == "OlimexEVB":
            {
                return longTermMode switch
                {
                    "JTAG" => ProgrammerState.DirtyJTagFlash,
                    "SPI" => ProgrammerState.DirtyJTagFlash,
                    _ => ProgrammerState.IllegalState
                };
            }
            case false when ccType == "OlimexEVB":
            {
                return shortTermMode switch
                {
                    "JTAG" => ProgrammerState.DirtyJTagMemory,
                    "SPI" => ProgrammerState.DirtyJTagMemory,
                    _ => ProgrammerState.IllegalState
                };
            }
            default:
                return ProgrammerState.IllegalState;
        }
    }
    
    public async Task DownloadAsync(UniversalFpgaProjectRoot project)
    {
        
        var top = project.TopEntity?.Header ?? throw new Exception("TopEntity not set!");
        var topName = top.Split(".").First();
        var outputDir = project.FullPath;
        
        var fpga = project.GetProjectProperty("Fpga");
        var properties = FpgaSettingsParser.LoadSettings(project, fpga!);
        var useWsl = bool.Parse(properties.GetValueOrDefault(CologneChipConstantService.CologneChipSettingsUseWsl) ?? "false");
        
        var state = GetProgrammerState(properties);
        
        List<string> fpgaArgs = [];
        var bitStreamPath = $"{CologneChipConstantService.Instance.GetBuildPath(project.RelativePath)}{topName}_00.cfg.bit";
        
        switch (state)
        {
            case ProgrammerState.JTagFlash:
                fpgaArgs = ["-b", "gatemate_evb_jtag", "-f", "--verify", $"{bitStreamPath}"];
                break;
            case ProgrammerState.JTagMemory:
                fpgaArgs = ["-b", "gatemate_evb_jtag", "-m",  $"{bitStreamPath}"];
                break;
            case ProgrammerState.SpiFlash:
                fpgaArgs = ["-b", "gatemate_evb_spi",  $"{bitStreamPath}"];
                break;
            case ProgrammerState.SpiMemory:
                fpgaArgs = ["-b", "gatemate_evb_spi", "-m",  $"{bitStreamPath}"];
                break;
            case ProgrammerState.PgmJTagMemory:
                fpgaArgs = ["-c", "gatemate_pgm",  $"{bitStreamPath}"];
                break;
            case ProgrammerState.PgmJTagFlash:
                fpgaArgs = ["-c", "gatemate_pgm", "-f", $"{bitStreamPath}"];
                break;
            case ProgrammerState.PgmSpiMemory:
                fpgaArgs = ["-b", "gatemate_pgm_spi",  $"{bitStreamPath}"];
                break;
            case ProgrammerState.PgmSpiFlash:
                fpgaArgs = ["-b", "gatemate_pgm_spi ", "-f", $"{bitStreamPath}"];
                break;
            case ProgrammerState.DirtyJTagMemory:
                fpgaArgs = ["-c", "dirtyJtag",  $"{bitStreamPath}"];
                break;
            case ProgrammerState.DirtyJTagFlash:
                fpgaArgs = ["-c", "dirtyJtag  ", "-f", $"{bitStreamPath}"];
                break;
            case ProgrammerState.IllegalState:
                logger.Error("IllegalState");
                throw new Exception("IllegalState");
        }
        
        if (!useWsl) {
        
            await childProcessService.ExecuteShellAsync("openFPGALoader", fpgaArgs,
            outputDir, "Running OpenFPGALoader (Short-Term)...", AppState.Loading, true);
        }
        else
        {
            var args = fpgaArgs.ToList();
            args.Insert(0, "sudo");
            args.Insert(1, "openFPGALoader");
            await childProcessService.ExecuteShellAsync("wsl", args,
                outputDir, "Running openFPGALoader (Short-Term)...", AppState.Loading, true);
        }
    }
}