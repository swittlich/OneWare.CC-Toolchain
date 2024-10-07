using Avalonia.Media;
using Avalonia.Threading;
using OneWare.Essentials.Converters;
using OneWare.Essentials.Enums;
using OneWare.Essentials.Models;
using OneWare.Essentials.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Parser;

namespace OneWare.CologneChip.Services;

public class CologneChipService(
    IChildProcessService childProcessService,
    ILogger logger,
    IOutputService outputService,
    IDockService dockService)
{
    public async Task<bool> SynthAsync(UniversalFpgaProjectRoot project, FpgaModel fpgaModel)
    {
        try
        {
            var properties = FpgaSettingsParser.LoadSettings(project, fpgaModel.Fpga.Name);
            var top = project.TopEntity?.Header ?? throw new Exception("TopEntity not set!");
            
            var buildDir = Path.Combine(project.FullPath, "build");
            Directory.CreateDirectory(buildDir);

            dockService.Show<IOutputService>();
            
            var start = DateTime.Now;

            var yosysSynthTool = properties.GetValueOrDefault("yosysToolchainYosysSynthTool") ??
                                 throw new Exception("Yosys Tool not set!");
            
            var (topName, topLanguage) = (top.Split('.').First(), top.Split('.').Last());

            List<string> yosysArguments = [];
            List<string> includedExtensions = [];
            
            switch (topLanguage)
            {
                case "vhd":
                    outputService.WriteLine("VHDL Compiling...\n===============");
                    yosysArguments = ["-q", "-p", $"ghdl --warn-no-binding -C --ieee=synopsys ./../{top} -e {topName}; {yosysSynthTool} -nomx8 -top {topName} -vlog {topName}_synth.v"];
                    includedExtensions = [];
                    break;
                case "v": 
                    outputService.WriteLine("Verilog Compiling...\n==============");
                    yosysArguments = ["-q", "-p", $"{yosysSynthTool} -nomx8 -top {topName} -vlog {topName}_synth.v"];
                    includedExtensions = [".v", ".sv"];
                    break;
            }
            
            var includedFiles = project.Files
                .Where(x => includedExtensions.Contains(x.Extension))
                .Where(x => !project.CompileExcluded.Contains(x))
                .Where(x => !project.TestBenches.Contains(x))
                .Select(x => $"./../{x.RelativePath}");
            
            yosysArguments.AddRange(properties.GetValueOrDefault("yosysToolchainYosysFlags")?.Split(' ',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? []);
            yosysArguments.AddRange(includedFiles);

            List<string> prArguments = ["-i", $"{topName}_synth.v", "-o", topName, $"-ccf ./../project.ccf -cCP"]; 
            
            var (success, _) = await childProcessService.ExecuteShellAsync("yosys", yosysArguments, $"{project.FullPath}/build",
                "Running yosys...", AppState.Loading, true, x =>
                {
                    if (x.StartsWith("Error:"))
                    {
                        logger.Error(x);
                        return false;
                    }

                    outputService.WriteLine(x);
                    return true;
                });
            
            success = success && (await childProcessService.ExecuteShellAsync("p_r", prArguments,
                $"{project.FullPath}/build", $"Running P_R...", AppState.Loading, true, null, s =>
                {
                    Dispatcher.UIThread.Post(() => { outputService.WriteLine(s); });
                    return true;
                })).success;
            
            var compileTime = DateTime.Now - start;
            if (success)
                outputService.WriteLine(
                    $"==================\n\nCompilation finished after {(int)compileTime.TotalMinutes:D2}:{compileTime.Seconds:D2}\n");
            else
                outputService.WriteLine(
                    $"==================\n\nCompilation failed after {(int)compileTime.TotalMinutes:D2}:{compileTime.Seconds:D2}\n",
                    Brushes.Red);

            return success;
        }
        catch (Exception e)
        {
            logger.Error(e.Message, e);
            return false;
        }
    }
    public async Task CreateNetListJsonAsync(IProjectFile verilog)
    {
        await childProcessService.ExecuteShellAsync("yosys", [
                "-p", "hierarchy -auto-top; proc; opt; memory -nomap; wreduce -memx; opt_clean", "-o",
                $"{verilog.Header}.json", verilog.Header
            ],
            Path.GetDirectoryName(verilog.FullPath)!, "Create Netlist...");
    }
    
}