namespace OneWare.CologneChip.Services;

public sealed class CologneChipConstantService
{
    // Singleton-Instanz
    private static readonly Lazy<CologneChipConstantService> _instance = new Lazy<CologneChipConstantService>(() => new CologneChipConstantService());

    // Private Konstruktor, um Instanziierung von außen zu verhindern
    private CologneChipConstantService() {}

    // Öffentliche Eigenschaft, um auf die Singleton-Instanz zuzugreifen
    public static CologneChipConstantService Instance => _instance.Value;

    // Konstanten als Eigenschaften oder Felder
    public static string CologneChipShortTermModeKey => "cologneChipProgrammerShortTermMode";
    public static string CologneChipLongTermModeKey => "cologneChipProgrammerLongTermMode";
    public static string CologneChipTypeKey => "cologneChipProgrammerType";

    // Weitere Konstanten können hier hinzugefügt werden
    public string GetBuildPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "./build/";
        return $"{path.TrimEnd('/')}/build/";
    }
}