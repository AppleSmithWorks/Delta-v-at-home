using Content.Server._DV.CBURN; 
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Configuration;

namespace Content.Server.Chat.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SetCburnCommand : IConsoleCommand
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
    
        public string Command => "setcburn";
        public string Description => "Toggles CBURN spawning";
        public string Help => "setcburn";
    
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var current = _cfg.GetCVar(CBurnCVars.CburnSpawnDisabled);
            _cfg.SetCVar(CBurnCVars.CburnSpawnDisabled, !current);
        
            var status = current ? "enabled" : "disabled";
            shell.WriteLine($"CBURN spawning is now {status}");
        }
    }
}
