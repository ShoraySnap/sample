using System;
using System.Drawing;
using System.Windows.Forms;

public class FamilyUploadForm : Form
{
    public bool UploadFamily { get; private set; } = false;
    public bool SkipThisFamily { get; private set; } = false;
    public bool SkipAllFamilies { get; private set; } = false;

    public FamilyUploadForm()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "Assets Not Found";
        Size = new Size(400, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;

        Label mainInstructionLabel = new Label
        {
            Text = $"Missing RFAs",
            AutoSize = true,
            Location = new Point(10, 20),
            Font = new Font(Font.FontFamily, 12)
        };

        Label mainContentLabel = new Label
        {
            Text = "Your project uses assets that were not found...",
            AutoSize = true,
            Location = new Point(10, 50)
        };

        Panel separatorPanel = new Panel
        {
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(10, 80),
            Size = new Size(this.ClientSize.Width - 20, 2)

        };

        // Dummy data for demonstration purposes
        CheckBox uploadCheckbox = new CheckBox { Text = "Option 1: Upload missing family", Location = new Point(10, 90), AutoSize = true };
        uploadCheckbox.CheckedChanged += (sender, e) => UploadFamily = uploadCheckbox.Checked;

        CheckBox skipThisCheckbox = new CheckBox { Text = "Option 2: Skip this family", Location = new Point(10, 120), AutoSize = true };
        skipThisCheckbox.CheckedChanged += (sender, e) => SkipThisFamily = skipThisCheckbox.Checked;

        CheckBox skipAllCheckbox = new CheckBox { Text = "Option 3: Skip all missing families", Location = new Point(10, 150), AutoSize = true };
        skipAllCheckbox.CheckedChanged += (sender, e) => SkipAllFamilies = skipAllCheckbox.Checked;

        Button closeButton = new Button { Text = "Close", Location = new Point(this.ClientSize.Width - 80, this.ClientSize.Height - 30) };
        closeButton.Click += (sender, e) => Close();

        Controls.Add(mainInstructionLabel);
        Controls.Add(mainContentLabel);
        Controls.Add(separatorPanel);
        Controls.Add(uploadCheckbox);
        Controls.Add(skipThisCheckbox);
        Controls.Add(skipAllCheckbox);
    }
}