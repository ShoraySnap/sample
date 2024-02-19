using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection.Emit;
using System.Windows.Controls;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Forms.Button;
using CheckBox = System.Windows.Forms.CheckBox;
using Color = System.Drawing.Color;
using Form = System.Windows.Forms.Form;
using Label = System.Windows.Forms.Label;
using ListView = System.Windows.Forms.ListView;
using ListViewItem = System.Windows.Forms.ListViewItem;
using Panel = System.Windows.Forms.Panel;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using View = System.Windows.Forms.View;

namespace TrudeImporter
{
    internal class UploadInterface
    {
        public static bool AskUserForFamilyUpload(string familyName, ref bool skipAllMissingFamilies)
        {
            if (skipAllMissingFamilies)
            {
                return false;
            }

            FamilyUploadForm uploadForm = new FamilyUploadForm();

            // Show dialog center parent if possible 
            DialogResult result = uploadForm.ShowDialog();

            if (uploadForm.SkipAllFamilies)
            {
                skipAllMissingFamilies = true;
            }

            return uploadForm.UploadFamily;
        }

        public static Family UploadAndLoadFamily(string familyName, string directoryPath, ref bool skipAllMissingFamilies, Document doc)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Select Family File For " + familyName;
            openFileDialog.Filter = "Revit Families (*.rfa)|*.rfa";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                string sourcePath = openFileDialog.FileName;
                string destinationPath = Path.Combine(directoryPath, Path.GetFileName(sourcePath));

                if (!Path.GetFileNameWithoutExtension(sourcePath).Equals(familyName, StringComparison.OrdinalIgnoreCase))
                {
                    TaskDialog mainDialog = new TaskDialog("Incorrect Family");
                    mainDialog.MainInstruction = $"The selected family file does not match the expected family name.";
                    mainDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Upload Again");
                    mainDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Skip this family.");
                    
                    TaskDialogResult tResult = mainDialog.Show();

                    if (tResult == TaskDialogResult.CommandLink1)
                    {
                        return UploadAndLoadFamily(familyName, directoryPath, ref skipAllMissingFamilies, doc);
                    }
                    else
                    {
                        return null;
                    }
                }

                try
                {
                    File.Copy(sourcePath, destinationPath, true);
                    if (doc.LoadFamily(destinationPath, out Family family))
                    {
                        // Check if the loaded family is the correct one
                        if (family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase))
                        {
                            // REMOVE THIS LINE IN PRODUCTION FOR TESTING PURPOSES ONLY
                            //File.Delete(destinationPath);
                            return family;
                        }
                        else
                        {
                            TaskDialog.Show("Error", $"The loaded family '{family.Name}' does not match the expected family name '{familyName}'.");
                            // Delete the copied family file if it's incorrect
                            File.Delete(destinationPath);
                        }
                    }
                    else
                    {
                        TaskDialog.Show("Error", "The family file could not be loaded into the Revit document. Please try again or contact support.");
                    }
                }
                catch (Exception e)
                {
                    TaskDialog.Show("Error", "Failed to load the family file.\n" + e.Message);
                }
            }

            return null;
        }
    }

    internal class UserDescretion
    {
        public static bool PromptUserForFloatingWindow()
        {
            TaskDialog mainDialog = new TaskDialog("Floating Window Detected");
            mainDialog.MainInstruction = "A window has been detected floating above the ground level.";
            mainDialog.MainContent = "Do you want to continue placing the window at this position?";
            mainDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Yes, place the window.");
            mainDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "No, do not place the window.");

            TaskDialogResult tResult = mainDialog.Show();

            return tResult == TaskDialogResult.CommandLink1;
        }
    }

    internal class FamilyUploadForm : Form
    {
        public bool UploadFamily { get; private set; } = false;
        public bool SkipThisFamily { get; private set; } = false;
        public bool SkipAllFamilies { get; private set; } = false;

        public FamilyUploadForm()
        {
            InitializeComponents();
            foreach (var item in GlobalVariables.MissingDoorFamiliesCount)
            {
                System.Diagnostics.Debug.WriteLine("Missing Family: " + item.Key + " Count: " + item.Value);
            }
        }

        private void InitializeComponents()
        {
            Text = "Assets Not Found";
            Size = new Size(640, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            //this.Icon = new Icon("");
            //this.ControlBox = false;
            Label mainInstructionLabel = new Label
            {
                Text = $"Missing RFAs",
                AutoSize = true,
                Location = new Point(10, 20),
                Font = new Font(Font.FontFamily, 15)
            };

            Label mainContentLabel = new Label
            {   MaximumSize = new Size(this.ClientSize.Width - 20, 0),
                Text = "Your project uses assets that were not found while reconciling the Snaptrude file to Revit.  Please link the missing assets. Any unlinked assets will not show up in the resulting model in Revit.",
                AutoSize = true,
                Location = new Point(10, 50)
            };

            Panel separatorPanel = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(10, 80),
                Size = new Size(this.ClientSize.Width - 20, 2)
            };

            var missingDoorFamiliesCount = GlobalVariables.MissingDoorFamiliesCount;

            Controls.Add(mainInstructionLabel);
            Controls.Add(mainContentLabel);
            Controls.Add(separatorPanel);

            ListView familyList = new ListView();
            familyList.Bounds = new Rectangle(new Point(1, 1), new Size(this.ClientSize.Width - 20, 280));
            familyList.View = View.Details;
            familyList.CheckBoxes = true;
            familyList.FullRowSelect = true;
            familyList.GridLines = true;
            familyList.Columns.Add("Family Name", -2, HorizontalAlignment.Center);
            familyList.Columns.Add("Number of Elements", -2, HorizontalAlignment.Center);
            familyList.HeaderStyle = ColumnHeaderStyle.None;
            familyList.Location = new Point(10, 115);

            Panel customHeaderPanel = new Panel
            {
                Location = new Point(10, 90),
                Size = new Size(this.ClientSize.Width - 20, 25),
                BackColor = Color.LightGray
            };

            CheckBox headerCheckBox = new CheckBox
            {
                Text = "Select All",
                AutoSize = true,
                Parent = customHeaderPanel,
                Location = new Point(5, 5),
                BackColor = Color.LightGray,
                Checked = true
            };

            headerCheckBox.Click += (sender, e) =>
            {
                foreach (ListViewItem item in familyList.Items)
                {
                    item.Checked = headerCheckBox.Checked;
                }
            };

            Label totalCountLabel = new Label
            {
                Text = $"Total Missing Families: {missingDoorFamiliesCount.Count}",
                AutoSize = true,
                Parent = customHeaderPanel,
                Location = new Point(customHeaderPanel.Width - 195, 5),
                BackColor = Color.LightGray
            };

            string familyName = "";
            foreach (var item in missingDoorFamiliesCount)
            {
                familyName = $"{item.Key}";
                ListViewItem cell = new ListViewItem(familyName);
                cell.SubItems.Add(item.Value.ToString());
                cell.Checked = true;
                familyList.Items.Add(cell);
            }
            customHeaderPanel.Controls.Add(headerCheckBox);
            customHeaderPanel.Controls.Add(totalCountLabel);
            Controls.Add(customHeaderPanel);
            Controls.Add(familyList);

            Panel footerPanel = new Panel
            {
                Location = new Point(0, this.ClientSize.Height - 80),
                Size = new Size(this.ClientSize.Width, 40),
                BackColor = Color.LightGray
            };
            Label checkedCountLabel = new Label
            {
                Text = $"Checked Families: {familyList.CheckedItems.Count}",
                AutoSize = true,
                Location = new Point(10, 10),
                Parent = footerPanel
            };

            Button skipAllButton = new Button
            {
                Text = "Skip All",
                Location = new Point(footerPanel.Width - 240, 5),
                Size = new Size(100, 30)
            };
            skipAllButton.Click += (sender, e) =>
            {
                MessageBox.Show("Skipped all.");
                Close();
            };
            footerPanel.Controls.Add(skipAllButton);
            Button nextButton = new Button
            {
                Text = "Next",
                Location = new Point(footerPanel.Width - 120, 5),
                Size = new Size(100, 30)
            };
            nextButton.Click += (sender, e) =>
            {
                MessageBox.Show("Proceeding to Next.");
                Close();
            };
            footerPanel.Controls.Add(nextButton);
            familyList.ItemChecked += (sender, e) => {
                if (checkedCountLabel != null)
                {
                    checkedCountLabel.Text = $"{familyList.CheckedItems.Count} Selected";
                }
            };
            Controls.Add(footerPanel);
        }
    }
}

