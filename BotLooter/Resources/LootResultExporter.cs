using BotLooter.Looting;

namespace BotLooter.Resources;

public class LootResultExporter
{
    private readonly SemaphoreSlim _fileAccessSemaphore = new(1);

    private readonly string _filePath;

    public LootResultExporter(string filePath)
    {
        _filePath = filePath;
    }

    public async Task ExportResult(string login, LootResult lootResult)
    {
        await _fileAccessSemaphore.WaitAsync();

        try
        {
            // For now, only export successful loots
            if (!lootResult.Success)
            {
                return;
            }
            
            await File.AppendAllTextAsync(_filePath, $"{login}{Environment.NewLine}");
        }
        finally
        {
            _fileAccessSemaphore.Release();
        }
    }
}