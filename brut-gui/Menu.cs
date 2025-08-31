using System.Linq;

using Eto.Forms;

namespace BrutGui
{
    internal class Menu
    {
        public MainForm form;
        public Commands commands;
        public Menu(MainForm mainForm)
        {
            form = mainForm;
            commands = new(form);
        }

        public SubMenuItem BuildFileMenu()
        {
            // create menu
            var createFileCommand = new Command
            {
                MenuText = "&New",
                Shortcut = Application.Instance.CommonModifier | Keys.N,
                ID = "CreateFileCommand"
            };
            createFileCommand.Executed += commands.CreateFileCommand_Executed;

            var openFileCommand = new Command
            {
                MenuText = "&Open",
                Shortcut = Application.Instance.CommonModifier | Keys.O,
                ID = "OpenFileCommand"
            };
            openFileCommand.Executed += commands.OpenFileCommand_Executed;

            var addFileCommand = new Command
            {
                MenuText = "&Add a file",
                Shortcut = Application.Instance.CommonModifier | Keys.A,
                ID = "AddFileCommand"
            };
            addFileCommand.Executed += commands.AddFileCommand_Executed;
            addFileCommand.Enabled = false;
            form.file_write_dependent.Add(addFileCommand);

            var removeFileCommand = new Command
            {
                MenuText = "&Remove a file",
                Shortcut = Application.Instance.CommonModifier | Keys.R,
                ID = "RemoveFileCommand"
            };
            removeFileCommand.Executed += commands.RemoveFilesCommand_Executed;
            removeFileCommand.Enabled = false;
            form.selected_write_dependent.Add(removeFileCommand);

            var extractFileCommand = new Command
            {
                MenuText = "&Extract a file",
                Shortcut = Application.Instance.CommonModifier | Keys.E,
                ID = "ExtractFileCommand"
            };
            extractFileCommand.Executed += commands.ExtractFileCommand_Executed;
            extractFileCommand.Enabled = false;
            form.selected_dependent.Add(extractFileCommand);

            var extractAllCommand = new Command
            {
                MenuText = "&Extract all files",
                Shortcut = Application.Instance.CommonModifier | Keys.X,
                ID = "ExtractAllCommand"
            };
            extractAllCommand.Executed += commands.ExtractAllFilesCommand_Executed;
            extractAllCommand.Enabled = false;
            form.file_dependent.Add(extractAllCommand);

            // create menu
            var closeFileCommand = new Command
            {
                MenuText = "&Close",
                Shortcut = Application.Instance.CommonModifier | Keys.C,
                ID = "CloseFileCommand"
            };
            closeFileCommand.Executed += commands.CloseFileCommand_Executed;
            closeFileCommand.Enabled = false;
            form.file_dependent.Add(closeFileCommand);

            var quitCommand = new Command
            {
                MenuText = "&Quit",
                Shortcut = Application.Instance.CommonModifier | Keys.Q,
                ID = "QuitCommand"
            };
            quitCommand.Executed += commands.QuitCommand_Executed;

            SubMenuItem fileMenu = new() { Text = "&File" }; //the & is used in Windows only, ignored in Mac, and stripped out in Linux
            fileMenu.Items.Add(createFileCommand);
            fileMenu.Items.Add(openFileCommand);
            fileMenu.Items.Add(new SeparatorMenuItem());
            fileMenu.Items.Add(addFileCommand);
            fileMenu.Items.Add(removeFileCommand);
            fileMenu.Items.Add(extractFileCommand);
            fileMenu.Items.Add(extractAllCommand);
            fileMenu.Items.Add(new SeparatorMenuItem());
            fileMenu.Items.Add(closeFileCommand);
            if (Globals.mru.Count() > 0)
            {
                fileMenu.Items.Add(new SeparatorMenuItem());
                foreach (string item in Globals.mru.ToArray())
                {
                    int cutoff = new int[] { item.Length - 45, 0 }.Max();
                    int new_length = item.Length - cutoff;
                    string shortened = item;

                    if (new_length < item.Length)
                    {
                        shortened = "..." + item.Substring(cutoff, new_length);
                    }

                    Command menuItem = new Command { MenuText = shortened, ToolTip = item };
                    menuItem.Executed += commands.OpenFileCommand_Executed;
                    fileMenu.Items.Add(menuItem);
                }
            }
            fileMenu.Items.Add(new SeparatorMenuItem());
            fileMenu.Items.Add(quitCommand);

            return fileMenu;
        }

        public SubMenuItem BuildSettingsMenu()
        {
            form.restorePCX = new()
            {
                Text = "Attempt &PCX recovery",
                Shortcut = Application.Instance.CommonModifier | Keys.P,
                ID = "RecoverPCX",
                Checked = form.restorePCX.Checked
            };
            form.restorePCX.CheckedChanged += commands.TogglePCXRestore_Executed;
            form.rotatePCX = new()
            {
                Text = "&Rotate PCX files",
                Shortcut = Application.Instance.CommonModifier | Keys.R,
                ID = "RotatePCX",
                Checked = form.rotatePCX.Checked
            };
            form.rotatePCX.CheckedChanged += commands.TogglePCXRotation_Executed;
            CheckMenuItem autoplay = new()
            {
                Text = "Autoplay &WAV files",
                Shortcut = Application.Instance.CommonModifier | Keys.W,
                ID = "AutoplayWAV"
            };
            autoplay.Checked = form.autoplay;
            autoplay.CheckedChanged += commands.ToggleWAVAutoplay_Executed;

            SubMenuItem settingsMenu = new() { Text = "&Options" };
            settingsMenu.Items.Add(form.restorePCX);
            settingsMenu.Items.Add(form.rotatePCX);
            settingsMenu.Items.Add(autoplay);

            return settingsMenu;
        }

        public MenuItem BuildHelpMenu()
        {
            var aboutCommand = new Command
            {
                MenuText = "&About BRUT",
                ID = "AboutCommand"
            };
            aboutCommand.Executed += commands.AboutCommand_Executed;

            SubMenuItem helpMenu = new() { Text = "&Help" };
            helpMenu.Items.Add(aboutCommand);
            return helpMenu;
        }

        public MenuBar Initialize()
        {
            return new MenuBar
            {
                Items =
                {
                   BuildFileMenu(),
                   BuildSettingsMenu(),
                   BuildHelpMenu()
                },
                ApplicationItems = { }
            };
        }
    }
}
