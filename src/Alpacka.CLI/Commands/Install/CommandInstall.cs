using Microsoft.Extensions.CommandLineUtils;

namespace Alpacka.CLI.Commands
{
    public class CommandInstall : CommandLineApplication
    {
        public CommandInstall()
        {
            Name = "install";
            Description = "install a alpacka pack";
            
            Commands.Add(new CommandMultiMC());
            Commands.Add(new CommandServer());
        }
    }
}