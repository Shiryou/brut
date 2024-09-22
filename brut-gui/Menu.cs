using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            form.file_dependent.Add(addFileCommand);

            var removeFileCommand = new Command
            {
                MenuText = "&Remove a file",
                Shortcut = Application.Instance.CommonModifier | Keys.R,
                ID = "RemoveFileCommand"
            };
            removeFileCommand.Executed += commands.RemoveFilesCommand_Executed;
            removeFileCommand.Enabled = false;
            form.selected_dependent.Add(removeFileCommand);

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

            var quitCommand = new Command
            {
                MenuText = "&Quit",
                Shortcut = Application.Instance.CommonModifier | Keys.Q,
                ID = "QuitCommand"
            };
            quitCommand.Executed += commands.QuitCommand_Executed;

            SubMenuItem fileMenu = new() { Text = "&File" }; //the & is used in Windows only, ignored in Mac, and stripped out in Linux
            fileMenu.Items.Add(openFileCommand);
            fileMenu.Items.Add(new SeparatorMenuItem());
            fileMenu.Items.Add(addFileCommand);
            fileMenu.Items.Add(removeFileCommand);
            fileMenu.Items.Add(extractFileCommand);
            fileMenu.Items.Add(extractAllCommand);
            fileMenu.Items.Add(new SeparatorMenuItem());
            fileMenu.Items.Add(quitCommand);

            return fileMenu;
        }

        public SubMenuItem BuildHelpMenu()
        {
            var aboutCommand = new Command
            {
                MenuText = "&About",
                Shortcut = Application.Instance.CommonModifier | Keys.A,
                ID = "AboutCommand"
            };

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
                   BuildHelpMenu()
                },
                ApplicationItems = { }
            };
        }
    }
}
