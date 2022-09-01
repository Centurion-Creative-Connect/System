using CenturionCC.System.Gun;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class HolsterCommand : BoolCommandHandler
    {
        private const string ResultString = "{0}: {1}";
        [SerializeField]
        private Transform holsterRoot;
        [SerializeField]
        private Transform hipsFollower;
        [SerializeField]
        private Transform playerFollower;
        [SerializeField]
        private GunHolster[] holsters;
        private bool _isHolsterEditable;

        [UdonSynced] [FieldChangeCallback(nameof(IsHolsterEnabled))]
        private bool _isHolsterEnabled;
        private bool _isHolsterHips = true;
        [UdonSynced] [FieldChangeCallback(nameof(IsHolsterVisible))]
        private bool _isHolsterVisible;

        public bool IsHolsterEnabled
        {
            get => _isHolsterEnabled;
            set
            {
                _isHolsterEnabled = value;
                foreach (var holster in holsters)
                {
                    if (holster == null) continue;
                    holster.IsHolsterActive = value;
                }
            }
        }

        public bool IsHolsterVisible
        {
            get => _isHolsterVisible;
            set
            {
                _isHolsterVisible = value;
                foreach (var holster in holsters)
                {
                    if (holster == null) continue;
                    holster.IsVisible = value;
                }
            }
        }

        public bool IsHolsterEditable
        {
            get => _isHolsterEditable;
            set
            {
                _isHolsterEditable = value;
                foreach (var holster in holsters)
                {
                    if (holster == null) continue;
                    holster.IsEditable = value;
                }
            }
        }

        public bool IsHolsterHips
        {
            get => _isHolsterHips;
            set
            {
                _isHolsterHips = value;
                holsterRoot.SetParent(_isHolsterHips ? hipsFollower : playerFollower);
            }
        }

        public override string Label => "Holster";
        public override string Usage => "<command> <enable|edit|visible|follow|apply>";
        public override string Description => "Configures holster settings.";

        public override bool OnBoolCommand(NewbieConsole console, string label, ref string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0) return false;
            switch (vars[0])
            {
                case "en":
                case "enabled":
                case "enable":
                {
                    if (vars.Length >= 2)
                        IsHolsterEnabled = ConsoleParser.TryParseBoolean(vars[1], IsHolsterEnabled);

                    console.Println(string.Format(ResultString, "isEnabled", IsHolsterEnabled));
                    return IsHolsterEnabled;
                }
                case "apply":
                {
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                    RequestSerialization();
                    console.Println("<color=green>Applied changes globally!</color>");
                    return true;
                }
                case "ed":
                case "edit":
                {
                    if (vars.Length >= 2)
                        IsHolsterEditable = ConsoleParser.TryParseBoolean(vars[1], IsHolsterEditable);

                    console.Println(string.Format(ResultString, "isEditable", IsHolsterEditable));
                    return IsHolsterEditable;
                }
                case "v":
                case "vi":
                case "visible":
                {
                    if (vars.Length >= 2)
                        IsHolsterVisible = ConsoleParser.TryParseBoolean(vars[1], IsHolsterVisible);

                    console.Println(string.Format(ResultString, "isVisible", IsHolsterVisible));
                    return IsHolsterVisible;
                }
                case "f":
                case "fo":
                case "follow":
                {
                    if (vars.Length >= 2)
                        IsHolsterHips = ConsoleParser.TryParseBoolean(vars[1], IsHolsterHips);

                    console.Println(string.Format(ResultString, "following", IsHolsterHips ? "hips" : "player"));
                    return IsHolsterHips;
                }
                default:
                    console.PrintUsage(this);
                    return false;
            }
        }
    }
}