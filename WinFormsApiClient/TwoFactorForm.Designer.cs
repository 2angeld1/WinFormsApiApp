using MaterialSkin.Controls;
using System;
using MaterialSkin;

namespace WinFormsApiClient
{
    partial class TwoFactorForm
    {
        private MaterialSkin.Controls.MaterialTextBox tfaCodeTextBox;
        private MaterialSkin.Controls.MaterialButton verifyButton;
        private MaterialSkin.Controls.MaterialButton cancelButton;
        private MaterialSkin.Controls.MaterialLabel tfaMessageLabel;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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

        private void InitializeComponent()
        {
            this.tfaCodeTextBox = new MaterialSkin.Controls.MaterialTextBox();
            this.verifyButton = new MaterialSkin.Controls.MaterialButton();
            this.cancelButton = new MaterialSkin.Controls.MaterialButton();
            this.tfaMessageLabel = new MaterialSkin.Controls.MaterialLabel();
            this.SuspendLayout();
            // 
            // tfaCodeTextBox
            // 
            this.tfaCodeTextBox.AnimateReadOnly = false;
            this.tfaCodeTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tfaCodeTextBox.Depth = 0;
            this.tfaCodeTextBox.Font = new System.Drawing.Font("Roboto", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.tfaCodeTextBox.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.tfaCodeTextBox.Hint = "Código de verificación";
            this.tfaCodeTextBox.LeadingIcon = null;
            this.tfaCodeTextBox.Location = new System.Drawing.Point(75, 100);
            this.tfaCodeTextBox.MaxLength = 6;
            this.tfaCodeTextBox.MouseState = MaterialSkin.MouseState.OUT;
            this.tfaCodeTextBox.Multiline = false;
            this.tfaCodeTextBox.Name = "tfaCodeTextBox";
            this.tfaCodeTextBox.Size = new System.Drawing.Size(250, 50);
            this.tfaCodeTextBox.TabIndex = 0;
            this.tfaCodeTextBox.Text = "";
            this.tfaCodeTextBox.TrailingIcon = null;
            // 
            // verifyButton
            // 
            this.verifyButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.verifyButton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.verifyButton.Depth = 0;
            this.verifyButton.HighEmphasis = true;
            this.verifyButton.Icon = null;
            this.verifyButton.Location = new System.Drawing.Point(75, 170);
            this.verifyButton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.verifyButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.verifyButton.Name = "verifyButton";
            this.verifyButton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.verifyButton.Size = new System.Drawing.Size(95, 36);
            this.verifyButton.TabIndex = 1;
            this.verifyButton.Text = "Verificar";
            this.verifyButton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.verifyButton.UseAccentColor = false;
            this.verifyButton.Click += new System.EventHandler(this.verifyButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.cancelButton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.cancelButton.Depth = 0;
            this.cancelButton.HighEmphasis = true;
            this.cancelButton.Icon = null;
            this.cancelButton.Location = new System.Drawing.Point(230, 170);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.cancelButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.cancelButton.Size = new System.Drawing.Size(95, 36);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancelar";
            this.cancelButton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.cancelButton.UseAccentColor = false;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // tfaMessageLabel
            this.tfaMessageLabel.Depth = 0;
            this.tfaMessageLabel.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.tfaMessageLabel.Location = new System.Drawing.Point(25, 65);
            this.tfaMessageLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.tfaMessageLabel.Name = "tfaMessageLabel";
            this.tfaMessageLabel.Size = new System.Drawing.Size(350, 35); // Aumentado la altura de 23 a 35
            this.tfaMessageLabel.TabIndex = 3;
            this.tfaMessageLabel.Text = "Por favor, ingrese el código de verificación.";
            this.tfaMessageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TwoFactorForm
            // 
            this.ClientSize = new System.Drawing.Size(400, 230);
            this.Controls.Add(this.tfaMessageLabel);
            this.Controls.Add(this.tfaCodeTextBox);
            this.Controls.Add(this.verifyButton);
            this.Controls.Add(this.cancelButton);
            this.MaximizeBox = false;
            this.Name = "TwoFactorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Verificación de dos factores";
            this.Load += new System.EventHandler(this.TwoFactorForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}