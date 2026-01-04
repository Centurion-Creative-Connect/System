using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
namespace CenturionCC.System.Gimmick.HardCase
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HardCaseText : UdonSharpBehaviour
    {
        private const int ProgressBarWidth = 45;

        [SerializeField]
        private GameObject screenObject;

        [SerializeField]
        private Text text;

        [SerializeField]
        private string terminalPrefix = @"[<color=green>{0}@a-flag-pc</color>: ";

        [SerializeField] [TextArea(10, 30)]
        private string initScreenMessage =
            "|==============================================================|\n|                                                              |\n|     /$$$$$$        /$$$$$$$$ /$$        /$$$$$$   /$$$$$$    | \n|    /$$__  $$      | $$_____/| $$       /$$__  $$ /$$__  $$   |\n|   | $$  \\ $$      | $$      | $$      | $$  \\ $$| $$  \\__/   |\n|   | $$$$$$$$      | $$$$$   | $$      | $$$$$$$$| $$ /$$$$   |\n|   | $$__  $$      | $$__/   | $$      | $$__  $$| $$|_  $$   |\n|   | $$  | $$      | $$      | $$      | $$  | $$| $$  \\ $$   |\n|   | $$  | $$      | $$      | $$$$$$$$| $$  | $$|  $$$$$$/   |\n|   |__/  |__/      |__/      |________/|__/  |__/ \\______/    |\n|                                                     v0.6.0   | \n|==============================================================|\n  (Pickup keyboard and interact | Press F) to defuse...\n";

        private void Start()
        {
            DrawIdle("root");
        }

        private void OnEnable()
        {
            DrawIdle("root");
        }

        private string FormatUsername(string username)
        {
            return username.ToLowerInvariant().Replace(' ', '_');
        }

        public virtual void DrawIdle(string username)
        {
            username = FormatUsername(username);
            text.text = initScreenMessage + string.Format(terminalPrefix, username, "~");
        }

        public virtual void PlantProgress(string username, float progress, int etaInSeconds)
        {
            username = FormatUsername(username);
            var resultText = initScreenMessage + string.Format(terminalPrefix, "root", "~");
            if (progress > 0.05F)
                resultText += $"su {username}\n" +
                              "Password: \n";

            if (progress > 0.1F) resultText += string.Format(terminalPrefix, username, "/root");

            if (progress > 0.15F)
                resultText += "cd ~\n" +
                              string.Format(terminalPrefix, username, "~");

            if (progress > 0.2F) resultText += "defuse\n";

            if (progress > 0.225F) resultText += "\nSearching for bomb...\n";

            if (progress > 0.25F)
            {
                var prog1 = Mathf.Clamp01((progress - 0.25F) * 1.5F);
                if (prog1 < 0.15F)
                    resultText += "Cooking a defuser\n";
                else if (prog1 < 0.35F)
                    resultText += "Starting up processes\n";
                else if (prog1 < 0.9F)
                    resultText += "Waiting for hook...\n";
                else if (prog1 < 0.98F)
                    resultText += "Plating the dish...\n";
                else
                    resultText += "Completed!\n";

                resultText +=
                    $"{(Mathf.Approximately(prog1, 1F) ? '-' : GetProgressRotating())} {GetProgressBar(prog1)} {prog1:P0} | ETA: {etaInSeconds}s\n";
            }

            if (progress > 0.4F && progress < 0.6F)
            {
                var prog2 = Mathf.Clamp01((progress - 0.4F) * 8F);
                resultText +=
                    "Starting hook process\n" +
                    $"{(Mathf.Approximately(prog2, 1F) ? '-' : GetProgressRotating())} {GetProgressBar(prog2)} {prog2:P0} | ETA: Unknown\n";
            }

            if (progress > 0.6F && progress < 0.9F)
            {
                var prog3 = Mathf.Clamp01((progress - 0.7F) * 8F);
                resultText +=
                    "Hooking up to the bomb\n" +
                    $"{(Mathf.Approximately(prog3, 1F) ? '-' : GetProgressRotating())} {GetProgressBar(prog3)} {prog3:P0} | ETA: Unknown\n";
            }

            text.text = resultText;
        }

        public virtual void PlantReady()
        {
            text.text += "\n<color=green>Defuser is ready. Place near the bomb to defuse...</color>\n";
        }

        public virtual void CannotPlant(string username)
        {
            username = FormatUsername(username);
            PlantProgress(username, 0.23F, 0);
            text.text += "\n<color=red>Could not find bomb near defuser. Stand near the bomb to begin...</color>\n" +
                         string.Format(terminalPrefix, username, "~");
        }

        public virtual void Abort(string username)
        {
            username = FormatUsername(username);
            text.text += "^C" + string.Format(terminalPrefix, username, "~");
        }

        public virtual void DefuseProgress(float progress)
        {
        }

        public static char GetProgressRotating()
        {
            // - \ | /
            var loopedFrames = Time.frameCount % 100;
            return loopedFrames < 25 ? '-' : loopedFrames < 45 ? '\\' : loopedFrames < 65 ? '|' : '/';
        }

        public static string GetProgressBar(float progress, int width = ProgressBarWidth, char completeChar = '\u2588',
                                            char incompleteChar = '\u2591')
        {
            progress = Mathf.Clamp01(progress);
            var complete = Mathf.FloorToInt(width * progress);
            var incomplete = width - complete;
            var result = "[";
            for (var i = 0; i < complete; i++) result += completeChar;

            for (var i = 0; i < incomplete; i++) result += incompleteChar;

            result += "]";
            return result;
        }

        public void SetObjectsVisible(bool visible)
        {
            screenObject.SetActive(visible);
            text.gameObject.SetActive(visible);
        }
    }
}
