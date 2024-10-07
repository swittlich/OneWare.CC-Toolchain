using System.Text.RegularExpressions;
using OneWare.CologneChip.Services;
using OneWare.Essentials.Services;
using OneWare.UniversalFpgaProjectSystem.Models;
using OneWare.UniversalFpgaProjectSystem.Services;
using Prism.Ioc;


namespace OneWare.CologneChip;

public class CologneChipToolchain(CologneChipService cologneChipService, ILogger logger) : IFpgaToolchain
{

    public string Name => "CologneChip";
    
    private static string Pattern => @"NET\s+""(?<pinName>[^""]+)""\s+Loc\s+=\s+""(?<pinLocation>[^""]+)""(?:\s*\|\s*(?<constraints>[^;]*))?;";

    public void OnProjectCreated(UniversalFpgaProjectRoot project)
    {
        //TODO Add gitignore defaults
    }

    public void LoadConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        try
        {
            var files = Directory.GetFiles(project.RootFolderPath);
            var pcfPath = files.FirstOrDefault(x => Path.GetExtension(x) == ".ccf");
            if (pcfPath != null)
            {
                var pcf = File.ReadAllText(pcfPath);
                var lines = pcf.Split('\n');
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("NET"))
                    {
                        // remove comments
                        var index = trimmedLine.IndexOf('#');
                        var result = (index >= 0) ? trimmedLine.Substring(0, index).Trim() : trimmedLine;
                        
                        var match = Regex.Match(result, Pattern);
                        
                        if (match.Success)
                        {
                            // Extrahiere pin-name, pin-location und opt.-constraints
                            var pinName = match.Groups["pinName"].Value;
                            var pinLocation = match.Groups["pinLocation"].Value;
                            var constraints = match.Groups["constraints"].Success ? match.Groups["constraints"].Value : null;
                            
                            var signal = pinName;
                            var pin = pinLocation;

                            if (fpga.PinModels.TryGetValue(pin, out var pinModel) &&
                                fpga.NodeModels.TryGetValue(signal, out var signalModel))
                                fpga.Connect(pinModel, signalModel);

                            // fpga.Constraint(constraints);
                            
                            // Ausgabe
                            Console.WriteLine($"Pin Name: {pinName}");
                            Console.WriteLine($"Pin Location: {pinLocation}");
                            Console.WriteLine($"Constraints: {constraints}");
                        }
                        else
                        {
                            ContainerLocator.Container.Resolve<ILogger>().Warning("CCF Line invalid: " + trimmedLine);
                        }
                    }
                }
            }
        } 
        catch (Exception e)
        {
            ContainerLocator.Container.Resolve<ILogger>().Error(e.Message, e);
        }
    }

    public void SaveConnections(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        var pcfPath = Path.Combine(project.FullPath, "project.ccf");

        try
        {
            var pcf = "";
            if (File.Exists(pcfPath))
            {
                var existingPcf = File.ReadAllText(pcfPath);
                existingPcf = RemoveLine(existingPcf, "NET");
                pcf = existingPcf.Trim();
            }

            foreach (var conn in fpga.PinModels.Where(x => x.Value.ConnectedNode is not null))
                pcf += $"\nNET \"{conn.Value.ConnectedNode!.Node.Name}\" Loc =  \"{conn.Value.Pin.Name}\";";
            pcf = pcf.Trim() + '\n';

            File.WriteAllText(pcfPath, pcf);
        }
        catch (Exception e)
        {
            ContainerLocator.Container.Resolve<ILogger>().Error(e.Message, e);
        }
    }

    public Task<bool> CompileAsync(UniversalFpgaProjectRoot project, FpgaModel fpga)
    {
        return cologneChipService.SynthAsync(project, fpga);
    }
    
    private string RemoveLine(string file, string find)
    {
        var startIndex = file.IndexOf(find, StringComparison.Ordinal);
        while (startIndex > -1)
        {
            var endIndex = file.IndexOf('\n', startIndex);
            if (endIndex == -1) endIndex = file.Length - 1;
            file = file.Remove(startIndex, endIndex - startIndex + 1);
            startIndex = file.IndexOf(find, startIndex, StringComparison.Ordinal);
        }

        return file;
    }
}