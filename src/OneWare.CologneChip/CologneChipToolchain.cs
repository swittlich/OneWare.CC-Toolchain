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
                            // extract pin-name, pin-location and opt.-constraints
                            var signal = match.Groups["pinName"].Value;
                            var pin = match.Groups["pinLocation"].Value;
                            var constraints = match.Groups["constraints"].Success ? match.Groups["constraints"].Value : null;
                            
                            if (fpga.PinModels.TryGetValue(pin, out var pinModel) &&
                                fpga.NodeModels.TryGetValue(signal, out var signalModel))
                                fpga.Connect(pinModel, signalModel);

                            // fpga.Constraint(constraints);
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
            List<string> lines = [];
            List<string> result = [];
            if (File.Exists(pcfPath))
            {
                lines = [..File.ReadAllLines(pcfPath)];
            }
            
            var pinModels = fpga.PinModels.Where(x => x.Value.ConnectedNode is not null).Select(conn => conn.Value).ToList();
            var pinModelsCache = pinModels.ToList();

            foreach (var line in lines)
            {
                if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
                {
                    result.Add(line);
                    continue;
                }

                if (!line.StartsWith("NET")) continue;
                
                var found = false;
                foreach (var pin in pinModels)
                {
                    if (!line.Contains(pin.ConnectedNode!.Node.Name)) continue;
                        
                    var commentIndex = line.IndexOf('#', StringComparison.Ordinal);
                    var comment = commentIndex != -1 ? line[commentIndex..] : string.Empty;
                    
                    var constraintIndex = line.IndexOf('|');
                    var semicolonIndex = line.IndexOf(';');
                    
                    var constraint = constraintIndex != -1 
                                     && constraintIndex < semicolonIndex
                                     && (constraintIndex < commentIndex || commentIndex < 0 )
                        ? line[constraintIndex..semicolonIndex] : string.Empty;
                    
                    var newLine = $"\nNET \"{pin.ConnectedNode!.Node.Name}\" Loc =  \"{pin.Pin.Name}\"";
                    
                    if (constraint != string.Empty) newLine += $" {constraint}";
                    
                    newLine += ";";
                    
                    if (commentIndex != -1) newLine += $" {comment}";
                    
                    result.Add(newLine.Trim());
                    pinModelsCache.Remove(pin);
                    found = true;
                    break;
                }

                if (!found)
                {
                    result.Add($"# {line}");
                }
            }

            result.AddRange(pinModelsCache.Select(pin => $"\nNET \"{pin.ConnectedNode!.Node.Name}\" Loc =  \"{pin.Pin.Name}\";"));
            File.WriteAllLines(pcfPath, result);
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
}