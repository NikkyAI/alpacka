using Microsoft.Extensions.CommandLineUtils;

namespace GitMC.CLI.Commands
{
    public class CommandInstall : CommandLineApplication
    {
        public CommandInstall()
        {
            Name = "install";
            Description = "install a gitmc pack";
            
            Commands.Add(new CommandMultiMC());
            Commands.Add(new CommandServer());
        }
    }
}