using MaterialSkin.Controls;

namespace WinFormsApiClient
{
    partial class GabineteSelectorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            this.cabinetComboBox = new System.Windows.Forms.ComboBox();
            this.categoryComboBox = new System.Windows.Forms.ComboBox();
            this.subcategoryComboBox = new System.Windows.Forms.ComboBox();
            this.titleTextBox = new MaterialSkin.Controls.MaterialTextBox();
            this.descriptionTextBox = new MaterialSkin.Controls.MaterialTextBox();
            this.confirmButton = new MaterialSkin.Controls.MaterialButton();
            this.cancelButton = new MaterialSkin.Controls.MaterialButton();
            this.cabinetLabel = new MaterialSkin.Controls.MaterialLabel();
            this.categoryLabel = new MaterialSkin.Controls.MaterialLabel();
            this.subcategoryLabel = new MaterialSkin.Controls.MaterialLabel();
            this.titleLabel = new MaterialSkin.Controls.MaterialLabel();
            this.descriptionLabel = new MaterialSkin.Controls.MaterialLabel();
            this.SuspendLayout();
            // 
            // cabinetComboBox
            // 
            this.cabinetComboBox.FormattingEnabled = true;
            this.cabinetComboBox.Location = new System.Drawing.Point(50, 90);
            this.cabinetComboBox.Name = "cabinetComboBox";
            this.cabinetComboBox.Size = new System.Drawing.Size(300, 24);
            this.cabinetComboBox.TabIndex = 0;
            this.cabinetComboBox.SelectedIndexChanged += new System.EventHandler(this.cabinetComboBox_SelectedIndexChanged);
            // 
            // categoryComboBox
            // 
            this.categoryComboBox.FormattingEnabled = true;
            this.categoryComboBox.Location = new System.Drawing.Point(50, 145);
            this.categoryComboBox.Name = "categoryComboBox";
            this.categoryComboBox.Size = new System.Drawing.Size(300, 24);
            this.categoryComboBox.TabIndex = 1;
            this.categoryComboBox.SelectedIndexChanged += new System.EventHandler(this.categoryComboBox_SelectedIndexChanged);
            // 
            // subcategoryComboBox
            // 
            this.subcategoryComboBox.FormattingEnabled = true;
            this.subcategoryComboBox.Location = new System.Drawing.Point(50, 200);
            this.subcategoryComboBox.Name = "subcategoryComboBox";
            this.subcategoryComboBox.Size = new System.Drawing.Size(300, 24);
            this.subcategoryComboBox.TabIndex = 2;
            // 
            // titleTextBox
            // 
            this.titleTextBox.AnimateReadOnly = false;
            this.titleTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.titleTextBox.Depth = 0;
            this.titleTextBox.Font = new System.Drawing.Font("Roboto", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.titleTextBox.Hint = "Título (opcional)";
            this.titleTextBox.Location = new System.Drawing.Point(50, 255);
            this.titleTextBox.MaxLength = 50;
            this.titleTextBox.MouseState = MaterialSkin.MouseState.OUT;
            this.titleTextBox.Multiline = false;
            this.titleTextBox.Name = "titleTextBox";
            this.titleTextBox.Size = new System.Drawing.Size(300, 50);
            this.titleTextBox.TabIndex = 3;
            this.titleTextBox.Text = "";
            // 
            // descriptionTextBox
            // 
            this.descriptionTextBox.AnimateReadOnly = false;
            this.descriptionTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.descriptionTextBox.Depth = 0;
            this.descriptionTextBox.Font = new System.Drawing.Font("Roboto", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.descriptionTextBox.Hint = "Descripción (opcional)";
            this.descriptionTextBox.Location = new System.Drawing.Point(50, 330);
            this.descriptionTextBox.MaxLength = 200;
            this.descriptionTextBox.MouseState = MaterialSkin.MouseState.OUT;
            this.descriptionTextBox.Multiline = true;
            this.descriptionTextBox.Name = "descriptionTextBox";
            this.descriptionTextBox.Size = new System.Drawing.Size(300, 80);
            this.descriptionTextBox.TabIndex = 4;
            this.descriptionTextBox.Text = "";
            // 
            // confirmButton
            // 
            this.confirmButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.confirmButton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.confirmButton.Depth = 0;
            this.confirmButton.HighEmphasis = true;
            this.confirmButton.Icon = null;
            this.confirmButton.Location = new System.Drawing.Point(50, 430);
            this.confirmButton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.confirmButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.confirmButton.Name = "confirmButton";
            this.confirmButton.Size = new System.Drawing.Size(100, 36);
            this.confirmButton.TabIndex = 5;
            this.confirmButton.Text = "Confirmar";
            this.confirmButton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.confirmButton.UseAccentColor = false;
            this.confirmButton.Click += new System.EventHandler(this.confirmButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.cancelButton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.cancelButton.Depth = 0;
            this.cancelButton.HighEmphasis = true;
            this.cancelButton.Icon = null;
            this.cancelButton.Location = new System.Drawing.Point(250, 430);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.cancelButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(100, 36);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancelar";
            this.cancelButton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.cancelButton.UseAccentColor = false;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // cabinetLabel
            // 
            this.cabinetLabel.Depth = 0;
            this.cabinetLabel.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.cabinetLabel.Location = new System.Drawing.Point(50, 70);
            this.cabinetLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.cabinetLabel.Name = "cabinetLabel";
            this.cabinetLabel.Size = new System.Drawing.Size(100, 23);
            this.cabinetLabel.TabIndex = 7;
            this.cabinetLabel.Text = "Gabinete:";
            // 
            // categoryLabel
            // 
            this.categoryLabel.Depth = 0;
            this.categoryLabel.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.categoryLabel.Location = new System.Drawing.Point(50, 125);
            this.categoryLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.categoryLabel.Name = "categoryLabel";
            this.categoryLabel.Size = new System.Drawing.Size(100, 23);
            this.categoryLabel.TabIndex = 8;
            this.categoryLabel.Text = "Categoría:";
            // 
            // subcategoryLabel
            // 
            this.subcategoryLabel.Depth = 0;
            this.subcategoryLabel.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.subcategoryLabel.Location = new System.Drawing.Point(50, 180);
            this.subcategoryLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.subcategoryLabel.Name = "subcategoryLabel";
            this.subcategoryLabel.Size = new System.Drawing.Size(150, 23);
            this.subcategoryLabel.TabIndex = 9;
            this.subcategoryLabel.Text = "Subcategoría:";
            // 
            // titleLabel
            // 
            this.titleLabel.Depth = 0;
            this.titleLabel.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.titleLabel.Location = new System.Drawing.Point(50, 235);
            this.titleLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(100, 23);
            this.titleLabel.TabIndex = 10;
            this.titleLabel.Text = "Título:";
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.Depth = 0;
            this.descriptionLabel.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.descriptionLabel.Location = new System.Drawing.Point(50, 310);
            this.descriptionLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(100, 23);
            this.descriptionLabel.TabIndex = 11;
            this.descriptionLabel.Text = "Descripción:";
            // 
            // GabineteSelectorForm
            // 
            this.ClientSize = new System.Drawing.Size(400, 500);
            this.Controls.Add(this.descriptionLabel);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.subcategoryLabel);
            this.Controls.Add(this.categoryLabel);
            this.Controls.Add(this.cabinetLabel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.confirmButton);
            this.Controls.Add(this.descriptionTextBox);
            this.Controls.Add(this.titleTextBox);
            this.Controls.Add(this.subcategoryComboBox);
            this.Controls.Add(this.categoryComboBox);
            this.Controls.Add(this.cabinetComboBox);
            this.MaximizeBox = false;
            this.Name = "GabineteSelectorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Seleccionar destino";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ComboBox cabinetComboBox;
        private System.Windows.Forms.ComboBox categoryComboBox;
        private System.Windows.Forms.ComboBox subcategoryComboBox;
        private MaterialSkin.Controls.MaterialTextBox titleTextBox;
        private MaterialSkin.Controls.MaterialTextBox descriptionTextBox;
        private MaterialSkin.Controls.MaterialButton confirmButton;
        private MaterialSkin.Controls.MaterialButton cancelButton;
        private MaterialSkin.Controls.MaterialLabel cabinetLabel;
        private MaterialSkin.Controls.MaterialLabel categoryLabel;
        private MaterialSkin.Controls.MaterialLabel subcategoryLabel;
        private MaterialSkin.Controls.MaterialLabel titleLabel;
        private MaterialSkin.Controls.MaterialLabel descriptionLabel;
    }
}