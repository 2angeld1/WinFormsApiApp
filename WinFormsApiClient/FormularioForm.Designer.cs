using MaterialSkin.Controls;
using System.Drawing;
using System;
using System.Windows.Forms;
using MaterialSkin;

namespace WinFormsApiClient
{
    partial class FormularioForm
    {
        private MaterialTextBox titulo, desc;
        private MaterialLabel Label1, Label2, Label3, LabelNombre, LabelDesc, fileLabel;
        private ComboBox dropdown1, dropdown2, dropdown3;
        private MaterialButton submitButton, selectFileButton;
        private OpenFileDialog fileDialog;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.titulo = new MaterialSkin.Controls.MaterialTextBox();
            this.desc = new MaterialSkin.Controls.MaterialTextBox();
            this.LabelNombre = new MaterialSkin.Controls.MaterialLabel();
            this.LabelDesc = new MaterialSkin.Controls.MaterialLabel();
            this.Label1 = new MaterialSkin.Controls.MaterialLabel();
            this.Label2 = new MaterialSkin.Controls.MaterialLabel();
            this.Label3 = new MaterialSkin.Controls.MaterialLabel();
            this.dropdown1 = new System.Windows.Forms.ComboBox();
            this.dropdown2 = new System.Windows.Forms.ComboBox();
            this.dropdown3 = new System.Windows.Forms.ComboBox();
            this.selectFileButton = new MaterialSkin.Controls.MaterialButton();
            this.fileLabel = new MaterialSkin.Controls.MaterialLabel();
            this.submitButton = new MaterialSkin.Controls.MaterialButton();
            this.descriptionLabel = new MaterialSkin.Controls.MaterialLabel();
            this.archivadorLabel = new MaterialSkin.Controls.MaterialLabel();
            this.categoriaLabel = new MaterialSkin.Controls.MaterialLabel();
            this.subcategoriaLabel = new MaterialSkin.Controls.MaterialLabel();
            this.ResetButton = new MaterialSkin.Controls.MaterialButton();
            this.BtnScanToPortal = new MaterialSkin.Controls.MaterialButton();
            this.BtnPrintToPortal = new MaterialSkin.Controls.MaterialButton();
            this.fileDialog = new OpenFileDialog();
            this.SuspendLayout();
            // 
            // titulo
            // 
            this.titulo.AnimateReadOnly = false;
            this.titulo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.titulo.Depth = 0;
            this.titulo.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.6F);
            this.titulo.LeadingIcon = null;
            this.titulo.Location = new System.Drawing.Point(40, 212);
            this.titulo.MaxLength = 50;
            this.titulo.MouseState = MaterialSkin.MouseState.OUT;
            this.titulo.Multiline = false;
            this.titulo.Name = "titulo";
            this.titulo.Size = new System.Drawing.Size(328, 50);
            this.titulo.TabIndex = 0;
            this.titulo.Text = "";
            this.titulo.TrailingIcon = null;
            // 
            // desc
            // 
            this.desc.AnimateReadOnly = false;
            this.desc.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.desc.Depth = 0;
            this.desc.Font = new System.Drawing.Font("Roboto", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.desc.LeadingIcon = null;
            this.desc.Location = new System.Drawing.Point(40, 469);
            this.desc.MaxLength = 500;
            this.desc.MouseState = MaterialSkin.MouseState.OUT;
            this.desc.Multiline = false;
            this.desc.Name = "desc";
            this.desc.Size = new System.Drawing.Size(328, 50);
            this.desc.TabIndex = 1;
            this.desc.Text = "";
            this.desc.TrailingIcon = null;
            // 
            // LabelNombre
            // 
            this.LabelNombre.Depth = 0;
            this.LabelNombre.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.LabelNombre.Location = new System.Drawing.Point(54, 186);
            this.LabelNombre.MouseState = MaterialSkin.MouseState.HOVER;
            this.LabelNombre.Name = "LabelNombre";
            this.LabelNombre.Size = new System.Drawing.Size(100, 23);
            this.LabelNombre.TabIndex = 2;
            this.LabelNombre.Text = "Titulo:";
            // 
            // LabelDesc
            // 
            this.LabelDesc.Depth = 0;
            this.LabelDesc.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.LabelDesc.Location = new System.Drawing.Point(54, 443);
            this.LabelDesc.MouseState = MaterialSkin.MouseState.HOVER;
            this.LabelDesc.Name = "LabelDesc";
            this.LabelDesc.Size = new System.Drawing.Size(100, 23);
            this.LabelDesc.TabIndex = 3;
            this.LabelDesc.Text = "Descripción:";
            // 
            // Label1
            // 
            this.Label1.BackColor = System.Drawing.Color.Transparent;
            this.Label1.Depth = 0;
            this.Label1.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.Label1.Location = new System.Drawing.Point(54, 274);
            this.Label1.MouseState = MaterialSkin.MouseState.HOVER;
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(100, 23);
            this.Label1.TabIndex = 4;
            this.Label1.Text = "Archivador:";
            // 
            // Label2
            // 
            this.Label2.Depth = 0;
            this.Label2.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.Label2.Location = new System.Drawing.Point(54, 327);
            this.Label2.MouseState = MaterialSkin.MouseState.HOVER;
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(100, 23);
            this.Label2.TabIndex = 5;
            this.Label2.Text = "Categoría:";
            // 
            // Label3
            // 
            this.Label3.Depth = 0;
            this.Label3.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.Label3.Location = new System.Drawing.Point(54, 380);
            this.Label3.MouseState = MaterialSkin.MouseState.HOVER;
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(100, 23);
            this.Label3.TabIndex = 6;
            this.Label3.Text = "SubCategoría:";
            // 
            // dropdown1
            // 
            this.dropdown1.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.dropdown1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dropdown1.ForeColor = System.Drawing.Color.Black;
            this.dropdown1.Items.AddRange(new object[] {
            "Opción 1",
            "Opción 2",
            "Opción 3"});
            this.dropdown1.Location = new System.Drawing.Point(40, 300);
            this.dropdown1.Name = "dropdown1";
            this.dropdown1.Size = new System.Drawing.Size(328, 24);
            this.dropdown1.TabIndex = 0;
            // 
            // dropdown2
            // 
            this.dropdown2.BackColor = System.Drawing.Color.White;
            this.dropdown2.ForeColor = System.Drawing.Color.Black;
            this.dropdown2.Items.AddRange(new object[] {
            "Opción A",
            "Opción B",
            "Opción C"});
            this.dropdown2.Location = new System.Drawing.Point(40, 353);
            this.dropdown2.Name = "dropdown2";
            this.dropdown2.Size = new System.Drawing.Size(328, 24);
            this.dropdown2.TabIndex = 1;
            // 
            // dropdown3
            // 
            this.dropdown3.BackColor = System.Drawing.Color.White;
            this.dropdown3.ForeColor = System.Drawing.Color.Black;
            this.dropdown3.Items.AddRange(new object[] {
            "Tipo X",
            "Tipo Y",
            "Tipo Z"});
            this.dropdown3.Location = new System.Drawing.Point(40, 406);
            this.dropdown3.Name = "dropdown3";
            this.dropdown3.Size = new System.Drawing.Size(328, 24);
            this.dropdown3.TabIndex = 2;
            // 
            // selectFileButton
            // 
            this.selectFileButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.selectFileButton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.selectFileButton.Depth = 0;
            this.selectFileButton.HighEmphasis = true;
            this.selectFileButton.Icon = null;
            this.selectFileButton.Location = new System.Drawing.Point(151, 128);
            this.selectFileButton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.selectFileButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.selectFileButton.Name = "selectFileButton";
            this.selectFileButton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.selectFileButton.Size = new System.Drawing.Size(97, 36);
            this.selectFileButton.TabIndex = 3;
            this.selectFileButton.Text = "Adjuntar";
            this.selectFileButton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.selectFileButton.UseAccentColor = false;
            this.selectFileButton.Click += new System.EventHandler(this.SelectFileButton_Click);
            // 
            // fileLabel
            // 
            this.fileLabel.AutoSize = true;
            this.fileLabel.Depth = 0;
            this.fileLabel.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.fileLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.fileLabel.Location = new System.Drawing.Point(102, 87);
            this.fileLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.fileLabel.Name = "fileLabel";
            this.fileLabel.Size = new System.Drawing.Size(205, 19);
            this.fileLabel.TabIndex = 4;
            this.fileLabel.Text = "Ningún archivo seleccionado";
            // 
            // submitButton
            // 
            this.submitButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.submitButton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.submitButton.Depth = 0;
            this.submitButton.HighEmphasis = true;
            this.submitButton.Icon = null;
            this.submitButton.Location = new System.Drawing.Point(295, 557);
            this.submitButton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.submitButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.submitButton.Name = "submitButton";
            this.submitButton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.submitButton.Size = new System.Drawing.Size(73, 36);
            this.submitButton.TabIndex = 5;
            this.submitButton.Text = "Enviar";
            this.submitButton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.submitButton.UseAccentColor = false;
            this.submitButton.Click += new System.EventHandler(this.SubmitButton_Click);
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.AutoSize = true;
            this.descriptionLabel.Depth = 0;
            this.descriptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.descriptionLabel.Location = new System.Drawing.Point(50, 50);
            this.descriptionLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(100, 23);
            this.descriptionLabel.TabIndex = 0;
            this.descriptionLabel.Text = "Seleccione las opciones correspondientes en cada campo:";
            // 
            // archivadorLabel
            // 
            this.archivadorLabel.AutoSize = true;
            this.archivadorLabel.Depth = 0;
            this.archivadorLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.archivadorLabel.Location = new System.Drawing.Point(50, 100);
            this.archivadorLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.archivadorLabel.Name = "archivadorLabel";
            this.archivadorLabel.Size = new System.Drawing.Size(100, 23);
            this.archivadorLabel.TabIndex = 0;
            this.archivadorLabel.Text = "Archivador:";
            // 
            // categoriaLabel
            // 
            this.categoriaLabel.AutoSize = true;
            this.categoriaLabel.Depth = 0;
            this.categoriaLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.categoriaLabel.Location = new System.Drawing.Point(50, 170);
            this.categoriaLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.categoriaLabel.Name = "categoriaLabel";
            this.categoriaLabel.Size = new System.Drawing.Size(100, 23);
            this.categoriaLabel.TabIndex = 0;
            this.categoriaLabel.Text = "Categoría:";
            // 
            // subcategoriaLabel
            // 
            this.subcategoriaLabel.AutoSize = true;
            this.subcategoriaLabel.Depth = 0;
            this.subcategoriaLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.subcategoriaLabel.Location = new System.Drawing.Point(50, 240);
            this.subcategoriaLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.subcategoriaLabel.Name = "subcategoriaLabel";
            this.subcategoriaLabel.Size = new System.Drawing.Size(100, 23);
            this.subcategoriaLabel.TabIndex = 0;
            this.subcategoriaLabel.Text = "Subcategoría:";
            // 
            // ResetButton
            // 
            this.ResetButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ResetButton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.ResetButton.Depth = 0;
            this.ResetButton.HighEmphasis = true;
            this.ResetButton.Icon = null;
            this.ResetButton.Location = new System.Drawing.Point(40, 557);
            this.ResetButton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.ResetButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.ResetButton.Name = "ResetButton";
            this.ResetButton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.ResetButton.Size = new System.Drawing.Size(65, 36);
            this.ResetButton.TabIndex = 7;
            this.ResetButton.Text = "Reset";
            this.ResetButton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.ResetButton.UseAccentColor = false;
            // 
            // BtnScanToPortal
            // 
            this.BtnScanToPortal.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BtnScanToPortal.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.BtnScanToPortal.Depth = 0;
            this.BtnScanToPortal.HighEmphasis = true;
            this.BtnScanToPortal.Icon = null;
            this.BtnScanToPortal.Location = new System.Drawing.Point(108, 557);
            this.BtnScanToPortal.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.BtnScanToPortal.MouseState = MaterialSkin.MouseState.HOVER;
            this.BtnScanToPortal.Name = "BtnScanToPortal";
            this.BtnScanToPortal.NoAccentTextColor = System.Drawing.Color.Empty;
            this.BtnScanToPortal.Size = new System.Drawing.Size(88, 36);
            this.BtnScanToPortal.TabIndex = 8;
            this.BtnScanToPortal.Text = "Scanner";
            this.BtnScanToPortal.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.BtnScanToPortal.UseAccentColor = false;
            this.BtnScanToPortal.Click += new System.EventHandler(this.BtnScanToPortal_Click);
            // 
            // BtnPrintToPortal
            // 
            this.BtnPrintToPortal.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BtnPrintToPortal.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.BtnPrintToPortal.Depth = 0;
            this.BtnPrintToPortal.HighEmphasis = true;
            this.BtnPrintToPortal.Icon = null;
            this.BtnPrintToPortal.Location = new System.Drawing.Point(204, 557);
            this.BtnPrintToPortal.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.BtnPrintToPortal.MouseState = MaterialSkin.MouseState.HOVER;
            this.BtnPrintToPortal.Name = "BtnPrintToPortal";
            this.BtnPrintToPortal.NoAccentTextColor = System.Drawing.Color.Empty;
            this.BtnPrintToPortal.Size = new System.Drawing.Size(87, 36);
            this.BtnPrintToPortal.TabIndex = 9;
            this.BtnPrintToPortal.Text = "Imprimir";
            this.BtnPrintToPortal.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.BtnPrintToPortal.UseAccentColor = false;
            this.BtnPrintToPortal.Click += new System.EventHandler(this.BtnPrintToPortal_Click);
            // 
            // FormularioForm
            // 
            this.ClientSize = new System.Drawing.Size(400, 624);
            this.Controls.Add(this.BtnPrintToPortal);
            this.Controls.Add(this.BtnScanToPortal);
            this.Controls.Add(this.ResetButton);
            this.Controls.Add(this.titulo);
            this.Controls.Add(this.desc);
            this.Controls.Add(this.LabelNombre);
            this.Controls.Add(this.LabelDesc);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.dropdown1);
            this.Controls.Add(this.dropdown2);
            this.Controls.Add(this.dropdown3);
            this.Controls.Add(this.selectFileButton);
            this.Controls.Add(this.fileLabel);
            this.Controls.Add(this.submitButton);
            this.MaximizeBox = false;
            this.Name = "FormularioForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Formulario";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MaterialLabel descriptionLabel;
        private MaterialLabel archivadorLabel;
        private MaterialLabel categoriaLabel;
        private MaterialLabel subcategoriaLabel;
        private MaterialButton ResetButton;
        private MaterialButton BtnScanToPortal;
        private MaterialButton BtnPrintToPortal;
    }
}