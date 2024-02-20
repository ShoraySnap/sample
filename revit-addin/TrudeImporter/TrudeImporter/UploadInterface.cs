using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
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
    internal class FamilyUploadForm : Form
    {
        public bool UploadFamily { get; private set; } = false;
        public bool SkipThisFamily { get; private set; } = false;
        public bool SkipAllFamilies { get; private set; } = false;

        public Form SelectFamiliesForm { get; private set; }

        public Form UploadFamiliesForm { get; private set; }


        public IDictionary<string, (bool IsChecked, int NumberOfElements, string path)> missingDoorFamiliesCount = GlobalVariables.MissingDoorFamiliesCount;

        public FamilyUploadForm()
        {
            SelectFamilies();
        }

        private void SelectFamilies()
        {
            Text = "Assets Not Found";
            Size = new Size(640, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            
            SelectFamiliesForm = this;

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
                Text = "RFA Name",
                AutoSize = true,
                Parent = customHeaderPanel,
                Location = new Point(5, 5),
                BackColor = Color.LightGray,
                Checked = true
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
                cell.SubItems.Add(item.Value.NumberOfElements.ToString());
                cell.Checked = item.Value.IsChecked;
                cell.Tag =item.Key;
                familyList.Items.Add(cell);
            }

            headerCheckBox.Click += (sender, e) =>
            {if (headerCheckBox.Checked)
                {
                    foreach (ListViewItem item in familyList.Items)
                    {
                        item.Checked = true;
                    }
                }
                else
                {
                    foreach (ListViewItem item in familyList.Items)
                    {
                        item.Checked = false;
                    }
                }
            };

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
                Text = "Link missing RFAs",
                Location = new Point(footerPanel.Width - 120, 5),
                Size = new Size(100, 30)
            };
            nextButton.Click += (sender, e) =>
            {
                foreach (var missingFamily in GlobalVariables.MissingDoorFamiliesCount)
                {
                    System.Diagnostics.Debug.WriteLine("Family: " + missingFamily.Key + "\nCount: " + missingFamily.Value.NumberOfElements + "\nCheck:" + missingFamily.Value.IsChecked + "\nPath: " + missingFamily.Value.path);
                }
                UploadFamiliesForm?.Dispose();
                this.Hide();
                UploadFamilies();
            };
            this.FormClosing += (sender, e) =>
            {
                e.Cancel = true; // Prevents form from closing
                this.Hide(); // Hides the form instead
            };
            footerPanel.Controls.Add(nextButton);
            familyList.ItemChecked += (sender, e) =>
            {
                if (e.Item != null && e.Item.Tag != null)
                {
                    familyName = e.Item.Tag.ToString();
                    bool isChecked = e.Item.Checked;

                    // Update GlobalVariables to reflect new checked state
                    if (GlobalVariables.MissingDoorFamiliesCount.ContainsKey(familyName))
                    {
                        var count = GlobalVariables.MissingDoorFamiliesCount[familyName].NumberOfElements;
                        var path = GlobalVariables.MissingDoorFamiliesCount[familyName].path;
                        GlobalVariables.MissingDoorFamiliesCount[familyName] = (isChecked, count, path);
                    }
                }

                int checkedItemsCount = familyList.CheckedItems.Count;
                if (checkedItemsCount > 0)
                {
                    checkedCountLabel.Text = $"{checkedItemsCount} Selected";
                    nextButton.Enabled = true;
                }
                else
                {
                    checkedCountLabel.Text = "Select an RFA to take action";
                    nextButton.Enabled = false;
                }
                headerCheckBox.Checked = false;
                if (checkedItemsCount == missingDoorFamiliesCount.Count)
                {
                    checkedCountLabel.Text = "All Families Selected";
                    nextButton.Enabled = true;
                    headerCheckBox.Checked = true;
                }
                
            };
            Controls.Add(footerPanel);
        }

        private void UploadFamilies()
        {
            Form newDialog = new Form
            {
                Text = "Assets Not Found",
                Size = new Size(640, 560),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false,
            };
            UploadFamiliesForm = newDialog;
            UploadFamiliesForm.FormClosing += (sender, e) =>
            {
                e.Cancel = true; // Prevents actual closing
                UploadFamiliesForm.Hide(); // Hides instead of closing
            };
            Label mainInstructionLabel = new Label
            {
                Text = $"Link RFAs",
                AutoSize = true,
                Location = new Point(10, 20),
                Font = new Font(Font.FontFamily, 15)
            };

            Label mainContentLabel = new Label
            {
                MaximumSize = new Size(this.ClientSize.Width - 20, 0),
                Text = "Select folder containing RFA files OR link them manually below.",
                AutoSize = true,
                Location = new Point(10, 50)
            };

            Panel separatorPanel = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(10, 80),
                Size = new Size(this.ClientSize.Width - 20, 2)
            };

            newDialog.Controls.Add(mainInstructionLabel);
            newDialog.Controls.Add(mainContentLabel);
            newDialog.Controls.Add(separatorPanel);

            Panel customHeaderPanel = new Panel
            {
                Location = new Point(10, 90),
                Size = new Size(this.ClientSize.Width - 20, 25),
                BackColor = Color.LightGray
            };

            Label headerCheckBox = new Label
            {
                Text = "RFA Name",
                AutoSize = true,
                Parent = customHeaderPanel,
                Location = new Point(5, 5),
                BackColor = Color.LightGray
            };
            var linkedFiles = 0;
            var filesToLink = 0;
            //count checked items in missingDoorFamiliesCount
            foreach (var item in missingDoorFamiliesCount)
            {
                if (item.Value.IsChecked)
                {
                    filesToLink++;
                }
            }
            Label totalCountLabel = new Label
            {
                Text = $"Linked {linkedFiles} of {filesToLink}",
                AutoSize = true,
                Parent = customHeaderPanel,
                Location = new Point(customHeaderPanel.Width - 195, 5),
                BackColor = Color.LightGray
            };

            newDialog.Controls.Add(customHeaderPanel);
            customHeaderPanel.Controls.Add(headerCheckBox);
            customHeaderPanel.Controls.Add(totalCountLabel);


            ListView familyList = new ListView();
            familyList.Bounds = new Rectangle(new Point(1, 1), new Size(this.ClientSize.Width - 20, 280));
            familyList.View = View.Details;
            familyList.CheckBoxes = true;
            familyList.FullRowSelect = true;
            familyList.GridLines = false;
            familyList.Columns.Add("Family Name", -2, HorizontalAlignment.Center);
            familyList.Columns.Add("Number of Elements", -2, HorizontalAlignment.Center);
            familyList.HeaderStyle = ColumnHeaderStyle.None;
            familyList.Location = new Point(10, 115);
            familyList.CheckBoxes = false;

            ListViewExtender extender = new ListViewExtender(familyList);
            ListViewButtonColumn buttonAction = new ListViewButtonColumn(1);
            buttonAction.FixedWidth = true;
            

            extender.AddColumn(buttonAction);

            string familyName = "";
            foreach (var item in missingDoorFamiliesCount)
            {
                if (item.Value.IsChecked)
                {
                    familyName = $"{item.Key}";
                    ListViewItem cell = new ListViewItem(familyName);
                    cell.SubItems.Add("Upload");
                    cell.Tag = item.Key;
                    familyList.Items.Add(cell);
                }
            }

            
            //this.FormClosed += (sender2, e2) => {
            //    MessageBox.Show("Original Form Closed.");
            //};

            newDialog.Controls.Add(familyList);
            Panel footerPanel = new Panel
            {
                Location = new Point(0, this.ClientSize.Height - 80),
                Size = new Size(this.ClientSize.Width, 40),
                BackColor = Color.LightGray
            };

            Label linkMessage = new Label
            {
                Text = $"Link assets to proceed",
                AutoSize = true,
                Location = new Point(10, 10),
                Parent = footerPanel
            };

            Button backButton = new Button
            {
                Text = "Back",
                Location = new Point(footerPanel.Width - 240, 5),
                Size = new Size(100, 30)
            };
            backButton.Click += (sender, e) =>
            {
                this.Show(); // Show the Select Families Form again
                UploadFamiliesForm.Hide(); // Hide the current form properly
            };

            Button doneButton = new Button
            {
                Text = "Done",
                Location = new Point(footerPanel.Width - 120, 5),
                Size = new Size(100, 30),
                Enabled = false
            };
            
            
            void OnButtonActionClick(object sender, ListViewColumnMouseEventArgs e)
            {
                familyName = e.Item.Text;
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Title = "Select Family File For " + familyName;
                openFileDialog.Filter = "Revit Families (*.rfa)|*.rfa";
                openFileDialog.RestoreDirectory = true;
                bool? uploadResult = openFileDialog.ShowDialog();

                if (uploadResult == true) // Check if user selected a file
                {
                    string sourcePath = openFileDialog.FileName;
                    if (GlobalVariables.MissingDoorFamiliesCount.ContainsKey(familyName) && !string.IsNullOrEmpty(sourcePath))
                    {
                        var isCheck = GlobalVariables.MissingDoorFamiliesCount[familyName].IsChecked;
                        var count = GlobalVariables.MissingDoorFamiliesCount[familyName].NumberOfElements;
                        GlobalVariables.MissingDoorFamiliesCount[familyName] =
                            (isCheck, count, sourcePath);
                        e.SubItem.Text = "Uploaded";
                        linkedFiles++;
                        totalCountLabel.Text =
                            $"Linked {linkedFiles} of {missingDoorFamiliesCount.Count}";

                        CheckAllUploaded();
                    }
                    void CheckAllUploaded()
                    {
                        doneButton.Enabled =
                            missingDoorFamiliesCount.All(item => item.Value.path != null && item.Value.path != "");
                        linkMessage.Text = doneButton.Enabled ? "All assets linked" : "Link assets to proceed";
                    }
                }
            }
            doneButton.Click += (sender, e) =>
            {
                foreach (var missingFamily in GlobalVariables.MissingDoorFamiliesCount)
                {
                    System.Diagnostics.Debug.WriteLine("Family: " + missingFamily.Key + "\nCount: " + missingFamily.Value.NumberOfElements + "\nCheck:" + missingFamily.Value.IsChecked + "\nPath: " + missingFamily.Value.path);
                }
            };
            buttonAction.Click += OnButtonActionClick;
            footerPanel.Controls.Add(backButton);
            footerPanel.Controls.Add(doneButton);
            newDialog.Controls.Add(footerPanel);
            using (newDialog)
            {
                DialogResult result = newDialog.ShowDialog();
            }
        }
    }
}

