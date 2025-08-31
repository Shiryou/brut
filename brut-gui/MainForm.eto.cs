using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Eto.Forms;

using ImageMagick;

using Microsoft.Xna.Framework.Audio;

using ResourceUtilityLib;

using Serilog;

namespace BrutGui
{
    partial class MainForm : Form
    {
        public TableLayout fileManager = new()
        {
            Spacing = new Eto.Drawing.Size(0, 5),
            Padding = new Eto.Drawing.Padding(0, 0, 0, 5)
        };
        public List<object> file_dependent = new();
        public List<object> file_write_dependent = new();
        public List<object> selected_dependent = new();
        public List<object> selected_write_dependent = new();
        public ListBox listBox = new();
        public Label fileInfo = new();
        public ImageView previewPcx = new();
        public Button previewWav = new();
        public ResourceHeader selected;
        public Commands commands;
        public MenuBar menuBar;
        public CheckMenuItem rotatePCX = new()
        {
            Checked = false
        };
        public CheckMenuItem restorePCX = new()
        {
            Checked = true
        };
        public bool autoplay = false;

        void InitializeComponent()
        {
            commands = new(this);

            // sets the client (inner) size of the window for your content
            this.ClientSize = new Eto.Drawing.Size(200, 200);
            this.Size = new Eto.Drawing.Size(1000, 600);
            this.Title = "Birthright Resource Utility";
            Menu = menuBar = (new Menu(this)).Initialize();

            Content = InitializeLayout();
        }

        public DynamicLayout InitializeLayout()
        {
            listBox.Items.Clear();
            TableLayout layout = InitializePanels();

            DynamicLayout rootLayout = new()
            {
                Spacing = new Eto.Drawing.Size(0, 5)
            };
            if (Globals.resource == null)
            {
                fileInfo.Text = "No resource file loaded.";
                ManageFileDependentFields(false);
            }
            else
            {
                fileInfo.Text = GetArchiveInfo();
                ManageFileDependentFields(true);

                foreach (var file in Globals.resource.ListContents())
                {
                    listBox.Items.Add(new ListItem()
                    {
                        Text = ResourceUtility.CharArrayToString(file.filename)
                    });
                }
                listBox.SelectedValueChanged += ShowFileInfo;
                ManageSelectedDependentFields(false);
            }

            rootLayout.Add(layout);
            return rootLayout;
        }

        public TableLayout InitializePanels()
        {
            fileManager = new();
            TableLayout layout = new()
            {
                Spacing = new Eto.Drawing.Size(5, 0)
            };
            if (Globals.resource == null)
            {
                this.Title = "Birthright Resource Utility";
            }
            else
            {
                this.Title = "Birthright Resource Utility - " + Globals.resourceName;

                if (Globals.resource.IsReadOnly())
                {
                    this.Title += " (Read-Only)";
                }
            }
            fileManager.Rows.Add(new TableRow(new TableCell(fileInfo, true))
            {
                ScaleHeight = true
            });

            previewWav.Text = "Preview Audio";
            Command previewWavCmd = new();
            previewWavCmd.Executed += PreviewWAV;
            previewWav.Command = previewWavCmd;
            previewWav.Height = 50;
            previewWav.Width = 50;
            previewWav.Visible = false;

            fileManager.Rows.Add(new TableRow(new TableCell(null, true))
            {
                ScaleHeight = true
            });

            previewPcx.Image = null;
            fileManager.Rows.Add(new TableRow(new TableCell(previewPcx, true))
            {
                ScaleHeight = true
            });

            TableLayout audioControls = new()
            {
                Spacing = new Eto.Drawing.Size(5, 5),
                Padding = new Eto.Drawing.Padding(5, 5)
            };
            audioControls.Rows.Add(new TableRow(new TableCell() { ScaleWidth = true }, new TableCell(previewWav, false), new TableCell() { ScaleWidth = true }));
            fileManager.Rows.Add(new TableRow(new TableCell(audioControls, true)));

            fileManager.Rows.Add(new TableRow(new TableCell(BuildFileControlButtons(), true)));
            layout.Rows.Add(new TableRow(new TableCell(listBox, true), new TableCell(fileManager, true)));

            return layout;
        }

        public string GetArchiveInfo()
        {
            return String.Format(
                "Resource File\n" + 
                "Version: {0}\n" +
                "Resources: {1}",
                Globals.resource.FileVersion(),
                Globals.resource.Count());
        }

        public string GetFileInfo()
        {
            string metadata = String.Format(
                "Resource\n" +
                "Filename: {2}\n" +
                "Compression: {3}\n" +
                "Hashed with ID: {4}\n" +
                "Rotated: {5}\n",
                Globals.resource.FileVersion(),
                Globals.resource.Count(),
                ResourceUtility.CharArrayToString(selected.filename),
                ResourceUtility.GetCompressionType(selected),
                ResourceUtility.UsesIDHash(selected),
                (selected.flags & 2) == 2
            );
            if (selected.cbCompressedData != selected.cbUncompressedData)
            {
                metadata += String.Format("File size: {0}\nCompressed size: {1}", FormatFileSize(selected.cbUncompressedData), FormatFileSize(selected.cbCompressedData));
            }
            else
            {
                metadata += String.Format("File size: {0}", FormatFileSize(selected.cbUncompressedData));
            }

            Log.Information("{0} {1}", selected.extension, ResourceUtility.GetSupportedExtensions()[selected.extension]);
            switch (ResourceUtility.GetSupportedExtensions()[selected.extension])
            {
                case "PCX":
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        metadata += "\n\nPCX previews are currently only available on Windows builds due to technical issues.\nPlease extract the file(s) and view them with an image viewer with PCX support.";
                    }
                    break;
                case "WAV":
                    break;
                default:
                    metadata += "\n\nThis format currently doesn't support previews.";
                    break;
            }
            return metadata;
        }

        #nullable enable
        public void ShowFileInfo(object? sender, EventArgs e)
        {
            previewPcx.Image = null;
            previewWav.Visible = false;
            string selectedValue = listBox.SelectedValue?.ToString() ?? string.Empty;
            if (listBox.SelectedValue == null)
            {
                ManageSelectedDependentFields(false);
                selected = new();
                previewPcx.Image = null;
                previewWav.Visible = false;
                return;
            }

            ManageSelectedDependentFields(true);
            try
            {
                selected = Globals.resource.GetFileInformation(selectedValue);
            }
            catch (FileNotFoundException)
            {
                HashAlgorithm alg = Globals.resource.GetHashType();
                if (alg == HashAlgorithm.HashCrc)
                {
                    Globals.resource.UseIDHash();
                }
                else
                {
                    Globals.resource.UseCRCHash();
                }
                selected = Globals.resource.GetFileInformation(selectedValue);
            }

            switch (ResourceUtility.GetSupportedExtensions()[selected.extension])
            {
                case "PCX":
                    PreviewPCX();
                    break;

                case "WAV":
                    previewWav.Visible = true;
                    if (autoplay)
                    {
                        PreviewWAV();
                    }
                    break;
                default:
                    break;
            }

            string archive_metadata = GetArchiveInfo();
            string file_metadata = GetFileInfo();

            fileInfo.Text = archive_metadata + "\n\n" + file_metadata;
            Log.Information(file_metadata);
        }
        #nullable restore

        private string FormatFileSize(uint len)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return String.Format("{0:0.##} {1}", len, sizes[order]);
        }

        private void PreviewPCX()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Convert from internal bitmap back to PCX and then to standard Bitmap
                byte[] data;
                if (!Globals.resource.GetRestoreSetting())
                {
                    Globals.resource.RestorePCX();
                    data = Globals.resource.GetResourceData(selected);
                    Globals.resource.RetainBitmap();
                }
                else
                {
                    data = Globals.resource.GetResourceData(selected);
                }
                MagickImage image = new(data, MagickFormat.Pcx);
                image.Format = MagickFormat.Bmp;
                previewPcx.Image = new Eto.Drawing.Bitmap(image.ToByteArray());
            }
            else
            {
                previewPcx.Image = null;
            }
        }

        #nullable enable
        private void PreviewWAV(object? sender = null, EventArgs? e = null)
        {
            MemoryStream soundStream = new(Globals.resource.GetResourceData(selected));
            SoundEffect.FromStream(soundStream).Play();
        }
        #nullable restore

        public TableLayout BuildFileControlButtons()
        {
            TableLayout buttonContainer = new()
            {
                Spacing = new Eto.Drawing.Size(5, 0)
            };
            Button add = new();
            add.Text = "Add a file";
            Command addButtonCmd = new();
            addButtonCmd.Executed += commands.AddFileCommand_Executed;
            add.Command = addButtonCmd;
            add.Height = 50;
            file_write_dependent.Add(add);

            Button remove = new();
            remove.Text = "Remove a file";
            Command removeButtonCmd = new();
            removeButtonCmd.Executed += commands.RemoveFilesCommand_Executed;
            remove.Command = removeButtonCmd;
            remove.Height = 50;
            selected_write_dependent.Add(remove);

            Button extract = new();
            extract.Text = "Extract selected";
            Command extractButtonCmd = new();
            extractButtonCmd.Executed += commands.ExtractFileCommand_Executed;
            extract.Command = extractButtonCmd;
            extract.Height = 50;
            selected_dependent.Add(extract);

            Button extractAll = new();
            extractAll.Text = "Extract all";
            Command extractAllButtonCmd = new();
            extractAllButtonCmd.Executed += commands.ExtractAllFilesCommand_Executed;
            extractAll.Command = extractAllButtonCmd;
            extractAll.Height = 50;
            file_dependent.Add(extractAll);

            buttonContainer.Rows.Add(new TableRow(new TableCell() { ScaleWidth = true }, new TableCell(add), new TableCell(remove), new TableCell(extract), new TableCell(extractAll), new TableCell() { ScaleWidth = true }));
            return buttonContainer;
        }

        public void ManageFileDependentFields(bool enabled)
        {
            bool writeable = enabled && !Globals.resource.IsReadOnly();

            if (!enabled)
            {
                ManageSelectedDependentFields(false);
            }

            foreach (object item in file_dependent)
            {
                if (item is Control)
                {
                    ((Control)item).Enabled = enabled;
                }
                else if (item is Command)
                {
                    ((Command)item).Enabled = enabled;
                }
                else if (item is MenuItem)
                {
                    ((MenuItem)item).Enabled = enabled;
                }
            }

            foreach (object item in file_write_dependent)
            {
                if (item is Control)
                {
                    ((Control)item).Enabled = writeable;
                }
                else if (item is Command)
                {
                    ((Command)item).Enabled = writeable;
                }
                else if (item is MenuItem)
                {
                    ((MenuItem)item).Enabled = writeable;
                }
            }
        }

        public void ManageSelectedDependentFields(bool enabled)
        {
            bool writeable = enabled && !Globals.resource.IsReadOnly();

            foreach (object item in selected_dependent)
            {
                if (item is Control)
                {
                    ((Control)item).Enabled = enabled;
                }
                else if (item is Command)
                {
                    ((Command)item).Enabled = enabled;
                }
            }

            foreach (object item in selected_write_dependent)
            {
                if (item is Control)
                {
                    ((Control)item).Enabled = writeable;
                }
                else if (item is Command)
                {
                    ((Command)item).Enabled = writeable;
                }
                else if (item is MenuItem)
                {
                    ((MenuItem)item).Enabled = writeable;
                }
            }
        }
    }
}
