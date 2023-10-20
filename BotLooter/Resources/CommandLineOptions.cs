using System.CommandLine;
using System.CommandLine.Parsing;

namespace BotLooter.Resources;

public class CommandLineOptions
{
  public string ConfigFilePath { get; set; } = "BotLooter.Config.json";
}

public class CommandLineParser
{
  public static CommandLineOptions Parse(string[] args)
  {
    var rootCommand = new RootCommand();

    var configFileOption = new Option<string>(
      "--config-file-path",
      getDefaultValue: () => "BotLooter.Config.json",
      description: "Путь к конфигурационному файлу"
    );
 
    rootCommand.AddOption(configFileOption);

    var parseResult = rootCommand.Parse(args);

    var commandLineOptions = new CommandLineOptions();

    if (parseResult.HasOption(configFileOption))
    {
      commandLineOptions.ConfigFilePath  = parseResult.GetValueForOption(configFileOption) ?? "BotLooter.Config.json";
    }

    return commandLineOptions;
  }
}