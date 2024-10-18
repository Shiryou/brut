using System;
using System.IO;

using Eto.Forms;

using ResourceUtilityLib;

namespace BrutGui
{
    public class Commands
    {
        private readonly MainForm form;
        public Commands(MainForm mainForm)
        {
            form = mainForm;
        }

        public void OpenFileCommand_Executed(object? sender, EventArgs e)
        {
            OpenFileDialog openDialog = new() { };
            openDialog.Filters.Add("Birthright Resource files|.RES");
            if (openDialog.ShowDialog(form) == DialogResult.Ok)
            {
                if (Globals.resource != null)
                {
                    Globals.resource.Dispose();
                }
                Globals.resource = new ResourceUtility(openDialog.FileName);
                Globals.resourceName = Path.GetFileName(openDialog.FileName).ToUpper();
                if (form.restore)
                {
                    Globals.resource.RestorePCX();
                }
                else
                {
                    Globals.resource.RetainBitmap();
                }
                form.ManageFileDependentFields(true);
            }
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
            saveDialog.ShowDialog(form);
            Globals.resource.SaveResourceToFile(saveDialog.FileName, Globals.resource.GetResourceData(form.selected));
        }

        public void ExtractAllFilesCommand_Executed(object? sender, EventArgs e)
        {
            SelectFolderDialog saveDialog = new() { };
            string[] types = ResourceUtility.GetSupportedExtensions();
            saveDialog.ShowDialog(form);
            foreach (ResourceHeader item in Globals.resource.ListContents())
            {
                string filePath = saveDialog.Directory + Path.DirectorySeparatorChar + ResourceUtility.CharArrayToString(item.filename);
                Globals.resource.SaveResourceToFile(filePath, Globals.resource.GetResourceData(item));
            }
        }

        public void TogglePCXRestore_Executed(object? sender, EventArgs e)
        {
            CheckMenuItem menuItem = (CheckMenuItem)sender;
            form.restore = !form.restore;
            menuItem.Checked = form.restore;
            if (Globals.resource != null)
            {
                if (!menuItem.Checked)
                {
                    Globals.resource.RetainBitmap();
                }
                else
                {
                    Globals.resource.RestorePCX();
                }
            }
        }

        public void ToggleWAVAutoplay_Executed(object? sender, EventArgs e)
        {
            CheckMenuItem menuItem = (CheckMenuItem)sender;
            form.autoplay = !form.autoplay;
            menuItem.Checked = form.autoplay;
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
