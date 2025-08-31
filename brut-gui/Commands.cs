#nullable enable
using System;
using System.IO;

using Eto.Forms;

using ResourceUtilityLib;

using Serilog;

namespace BrutGui
{
    public class Commands
    {
        private readonly MainForm form;
        public Commands(MainForm mainForm)
        {
            form = mainForm;
        }

        public void CreateFileCommand_Executed(object? sender, EventArgs e)
        {
            string filename = "";
            SaveFileDialog newDialog = new() { };
            newDialog.Filters.Add("Birthright Resource files|.RES");
            if (newDialog.ShowDialog(form) == DialogResult.Ok)
            {
                filename = newDialog.FileName;
            }
            OpenFileFromFilename(filename);
        }

        public void OpenFileCommand_Executed(object? sender, EventArgs e)
        {
            string filename = "";
            if (sender is Command && ((Command)sender).MenuText != "&Open")
            {
                filename = ((Command)sender).ToolTip;
            }
            else
            {
                OpenFileDialog openDialog = new() { };
                openDialog.Filters.Add("Birthright Resource files|.RES");
                if (openDialog.ShowDialog(form) == DialogResult.Ok)
                {
                    filename = openDialog.FileName;
                }
            }
            OpenFileFromFilename(filename);
        }

        public void OpenFileFromFilename(string filename)
        {
            if (!string.IsNullOrEmpty(filename))
            {
                Log.Information("Loading RES file {filename}", filename);
                if (Globals.resource != null)
                {
                    Globals.resource.Dispose();
                }
                Globals.resource = new ResourceUtility(filename, Globals.logger);
                Globals.resourceName = Path.GetFileName(filename).ToUpper();
                Globals.mru.Add(filename);
                Globals.mru.Save(Path.Combine(Globals.appData, "mru.json"));
                if (form.restorePCX.Checked)
                {
                    Globals.resource.RestorePCX();
                }
                else
                {
                    Globals.resource.RetainBitmap();
                }
                form.ManageFileDependentFields(true);
                form.Menu = form.menuBar = (new Menu(form)).Initialize();
                form.Content = form.InitializeLayout();
            }
        }

        public void CloseFileCommand_Executed(object? sender, EventArgs e)
        {
            Log.Information("Closing RES file {filename}", Globals.resourceName);
            if (Globals.resource != null)
            {
                Globals.resource.Dispose();
                Globals.resource = null;
            }
            Globals.resourceName = null;
            form.ManageFileDependentFields(false);
            form.Menu = form.menuBar = (new Menu(form)).Initialize();
            form.Content = form.InitializeLayout();
        }

        public void AddFileCommand_Executed(object? sender, EventArgs e)
        {
            OpenFileDialog openDialog = new() { };
            string[] types = ResourceUtility.GetSupportedExtensions();
            openDialog.Filters.Add("All files|.*");
            openDialog.Filters.Add("Birthright Resource files|" + String.Join(";.", types));
            openDialog.MultiSelect = true;
            if (openDialog.ShowDialog(form) == DialogResult.Ok)
            {
                foreach (string filename in openDialog.Filenames)
                {
                    Globals.resource.AddFile(filename);
                }
            }
            form.Content = form.InitializeLayout();
        }

        public void RemoveFilesCommand_Executed(object? sender, EventArgs e)
        {
            Globals.resource.RemoveFile(form.selected.filename);
            form.Content = form.InitializeLayout();
        }

        public void ExtractFileCommand_Executed(object? sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new() { };
            string[] types = ResourceUtility.GetSupportedExtensions();
            saveDialog.Filters.Add("All files|.*");
            saveDialog.Filters.Add("Birthright Resource files|" + String.Join(";.", types));
            saveDialog.FileName = ResourceUtility.CharArrayToString(form.selected.filename);
            DialogResult result = saveDialog.ShowDialog(form);
            if (result != DialogResult.Ok || string.IsNullOrWhiteSpace(saveDialog.FileName))
            {
                return;
            }
            Globals.resource.SaveResourceToFile(saveDialog.FileName, Globals.resource.GetResourceData(form.selected));
        }

        public void ExtractAllFilesCommand_Executed(object? sender, EventArgs e)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            SelectFolderDialog saveDialog = new() { };
            string[] types = ResourceUtility.GetSupportedExtensions();
            DialogResult result = saveDialog.ShowDialog(form);
            if (result != DialogResult.Ok || string.IsNullOrWhiteSpace(saveDialog.Directory))
            {
                return;
            }
            Directory.SetCurrentDirectory(saveDialog.Directory);
            Globals.resource.ExtractAll();
            Directory.SetCurrentDirectory(currentDirectory);
        }

        public void TogglePCXRestore_Executed(object? sender, EventArgs e)
        {
            if (Globals.resource != null)
            {
                if (form.restorePCX.Checked)
                {
                    Globals.resource.RetainBitmap();
                }
                else
                {
                    Globals.resource.RestorePCX();
                }
            }
        }

        public void TogglePCXRotation_Executed(object? sender, EventArgs e)
        {
            if (Globals.resource != null)
            {
                if (form.restorePCX.Checked)
                {
                    Globals.resource.EnablePCXRotation();
                }
                else
                {
                    Globals.resource.DisablePCXRotation();
                }
            }
        }

        public void ToggleWAVAutoplay_Executed(object? sender, EventArgs e)
        {
            form.autoplay = !form.autoplay;
            if (sender != null)
            {
                CheckMenuItem menuItem = (CheckMenuItem)sender;
                menuItem.Checked = form.autoplay;
            }
        }

        public void AboutCommand_Executed(object? sender, EventArgs e)
        {
            AboutDialog about = new();
            about.Website = new Uri("https://github.com/Shiryou/brut");
            about.WebsiteLabel = "GitHub";
            about.ShowDialog(form);
        }

        public void QuitCommand_Executed(object? sender, EventArgs e)
        {
            Application.Instance.Quit();
        }
    }
}
