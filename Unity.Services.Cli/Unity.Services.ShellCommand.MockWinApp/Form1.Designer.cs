namespace Unity.Services.ShellCommand.MockWinApp;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        lbl_description = new System.Windows.Forms.Label();
        SuspendLayout();
        //
        // lbl_description
        //
        lbl_description.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        lbl_description.Location = new System.Drawing.Point(12, 9);
        lbl_description.Name = "lbl_description";
        lbl_description.Size = new System.Drawing.Size(776, 432);
        lbl_description.TabIndex = 0;
        lbl_description.Text = "I\'m a Mock app and I do nothing";
        //
        // Form1
        //
        AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(800, 450);
        Controls.Add(lbl_description);
        Text = "ShellCommand.MockApp";
        WindowState = System.Windows.Forms.FormWindowState.Minimized;
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label lbl_description;

    #endregion
}
