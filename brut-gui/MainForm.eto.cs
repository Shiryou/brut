using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;

using Eto.Forms;

using ImageMagick;

using LibVLCSharp.Shared;

using ResourceUtilityLib;

namespace BrutGui
{
    partial class MainForm : Eto.Forms.Form
    {
        public TableLayout fileManager = new()
        {
            Spacing = new Eto.Drawing.Size(0, 5),
            Padding = new Eto.Drawing.Padding(0, 0, 0, 5)
        };
        public List<object> file_dependent = new();
        public List<object> selected_dependent = new();
        public ListBox listBox = new();
        public Label fileInfo = new();
        public ImageView preview = new();
        public LibVLC LibVLC;
        public MediaPlayer player;
        public ResourceHeader selected;
        public Commands commands;
        public MenuBar menuBar;
        public bool restore = true;
        public bool autoplay = false;

        void InitializeComponent()
        {
            commands = new(this);
            try
            {
                LibVLC = new();
            }
            catch (Exception) { }

            if (LibVLC != null)
            {
                player = new(LibVLC);
            }
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
                fileInfo.Text = String.Format("Version: {0}\nResources: {1}", Globals.resource.FileVersion(), Globals.resource.Count());
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
            }
            fileManager.Rows.Add(new TableRow(new TableCell(fileInfo, true))
            {
                ScaleHeight = true
            });

            preview.Image = null;
            fileManager.Rows.Add(new TableRow(new TableCell(preview, true))
            {
                ScaleHeight = true
            });

            fileManager.Rows.Add(new TableRow(new TableCell(BuildFileControlButtons(), true)));
            layout.Rows.Add(new TableRow(new TableCell(listBox, true), new TableCell(fileManager, true)));

            return layout;
        }

        public void ShowFileInfo(object? sender, EventArgs e)
        {
            if (listBox.SelectedValue == null)
            {
                ManageSelectedDependentFields(false);
                selected = new();
                preview.Image = null;
                return;
            }

            ManageSelectedDependentFields(true);
            try
            {
                selected = Globals.resource.GetFileInformation(listBox.SelectedValue.ToString());
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
                selected = Globals.resource.GetFileInformation(listBox.SelectedValue.ToString());
            }

            string metadata = String.Format(
                "Resource File\n" +
                "Version: {0}\n" +
                "Resources: {1}\n\n" +
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
                metadata += String.Format("File size: {0}\nCompressed size: {1}", selected.cbUncompressedData, selected.cbCompressedData);
            }
            else
            {
                metadata += String.Format("File size: {0}", selected.cbUncompressedData);
            }

            switch (ResourceUtility.GetSupportedExtensions()[selected.extension])
            {
                case "PCX":
                    PreviewPCX(ref metadata);
                    break;

                case "WAV":
                    PreviewWAV(ref metadata);
                    break;
                default:
                    metadata += "\n\nThis format currently doesn't support previews.";
                    break;
            }

            fileInfo.Text = metadata;
        }

        private void PreviewPCX(ref string metadata)
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
                preview.Image = new Eto.Drawing.Bitmap(image.ToByteArray());

                metadata += "\n\nNote: Image previews and extraction currently do not support un-rotating. This will be added in a future update.";
            }
            else
            {
                preview.Image = null;
                metadata += "\n\nPCX previews are currently unavailable on Linux builds due to technical issues.\nPlease extract the file(s) and view them with an image viewer with PCX support.";
            }
        }

        private void PreviewWAV(ref string metadata)
        {
            if (LibVLC == null)
            {
                metadata += "\n\nWAV previews failed to load.";
                return;
            }

            MemoryStream sound = new(Globals.resource.GetResourceData(selected));
            Media media = new Media(LibVLC, new StreamMediaInput(sound));
            player = new(media);
            if (autoplay)
            {
                player.Play();
            }
        }

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
            file_dependent.Add(add);

            Button remove = new();
            remove.Text = "Remove a file";
            Command removeButtonCmd = new();
            removeButtonCmd.Executed += commands.RemoveFilesCommand_Executed;
            remove.Command = removeButtonCmd;
            remove.Height = 50;
            selected_dependent.Add(remove);

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
        }

        public void ManageSelectedDependentFields(bool enabled)
        {
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
        }
    }
}
