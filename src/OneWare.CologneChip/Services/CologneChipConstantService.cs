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
    public string CologneChipShortTermModeKey => "cologneChipProgrammerShortTermMode";
    public string CologneChipLongTermModeKey => "cologneChipProgrammerLongTermMode";

    // Weitere Konstanten können hier hinzugefügt werden
}