using CenturionCC.System.Utils;
using DerpyNewbie.Logger;
using UdonSharp;
namespace CenturionCC.System.Command.Util
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CenturionDiagnosticCommand : NewbieConsoleCommandHandler
    {
        public override string Label => "CenturionDiagnostic";
        public override string Usage => "<command> <err|warn> <msg>";
        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars.Length != 2) return console.PrintUsage(this);
            switch (vars[0])
            {
                case "err":
                {
                    CenturionDiagnostic.LogError(vars[1]);
                    return ConsoleLiteral.Of(0);
                }
                case "warn":
                {
                    CenturionDiagnostic.LogWarning(vars[1]);
                    return ConsoleLiteral.Of(0);
                }
                default:
                {
                    return console.PrintUsage(this);
                }
            }
        }
    }
}
