﻿using MaterialSkin.Controls;
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
        private MaterialButton SubmitButton, SelectFileButton, ResetButton, BtnScanToPortal;
        private OpenFileDialog fileDialog;

        // Nuevos controles para el panel de archivos
        private Panel filePanel;
        private Panel filePanelContainer;
        private PictureBox fileTypeIcon;
        private Label fileNameLabel;
        private Button RemoveFileButton;
        private TableLayoutPanel buttonTablePanel;
        private MaterialLabel fileSelectionLabel;

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
            this.SelectFileButton = new MaterialSkin.Controls.MaterialButton();
            this.fileLabel = new MaterialSkin.Controls.MaterialLabel();
            this.SubmitButton = new MaterialSkin.Controls.MaterialButton();
            this.ResetButton = new MaterialSkin.Controls.MaterialButton();
            this.BtnScanToPortal = new MaterialSkin.Controls.MaterialButton();
            this.fileDialog = new OpenFileDialog();
            this.fileSelectionLabel = new MaterialSkin.Controls.MaterialLabel();

            // Nuevos controles
            this.filePanel = new Panel();
            this.filePanelContainer = new Panel();
            this.fileTypeIcon = new PictureBox();
            this.fileNameLabel = new Label();
            this.RemoveFileButton = new Button();
            this.buttonTablePanel = new TableLayoutPanel();

            this.SuspendLayout();

            // Configuración del fileDialog
            this.fileDialog.Filter = "Archivos permitidos (*.pdf;*.jpg;*.jpeg;*.png;*.webp)|*.pdf;*.jpg;*.jpeg;*.png;*.webp|" +
                                    "Documentos PDF (*.pdf)|*.pdf|" +
                                    "Imágenes (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp";
            this.fileDialog.Title = "Seleccionar documento o imagen";

            // Título descriptivo encima del botón de selección
            this.fileSelectionLabel.Depth = 0;
            this.fileSelectionLabel.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.fileSelectionLabel.Location = new System.Drawing.Point(40, 70);
            this.fileSelectionLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.fileSelectionLabel.Name = "fileSelectionLabel";
            this.fileSelectionLabel.Size = new System.Drawing.Size(420, 23);
            this.fileSelectionLabel.TabIndex = 11;
            this.fileSelectionLabel.Text = "";

            // SelectFileButton - Bajado más
            this.SelectFileButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.SelectFileButton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.SelectFileButton.Depth = 0;
            this.SelectFileButton.HighEmphasis = true;
            this.SelectFileButton.Icon = null;
            this.SelectFileButton.Location = new System.Drawing.Point(40, 100);
            this.SelectFileButton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.SelectFileButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.SelectFileButton.Name = "SelectFileButton";
            this.SelectFileButton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.SelectFileButton.Size = new System.Drawing.Size(420, 40);
            this.SelectFileButton.TabIndex = 0;
            this.SelectFileButton.Text = "SELECCIONAR ARCHIVO";
            this.SelectFileButton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.SelectFileButton.UseAccentColor = true;
            this.SelectFileButton.Click += new System.EventHandler(this.SelectFileButton_Click);

            // Sección de archivo - Bajada más para evitar superposiciones
            // filePanel
            this.filePanel.Location = new System.Drawing.Point(40, 150);
            this.filePanel.Name = "filePanel";
            this.filePanel.Size = new System.Drawing.Size(420, 80);
            this.filePanel.TabIndex = 10;
            this.filePanel.BorderStyle = BorderStyle.None;

            // filePanelContainer
            this.filePanelContainer.BorderStyle = BorderStyle.FixedSingle;
            this.filePanelContainer.BackColor = System.Drawing.Color.WhiteSmoke;
            this.filePanelContainer.Size = new Size(420, 60);
            this.filePanelContainer.Visible = true;
            this.filePanelContainer.Padding = new Padding(5);
            this.filePanelContainer.Location = new Point(0, 0);

            // fileTypeIcon
            this.fileTypeIcon.Size = new Size(32, 32);
            this.fileTypeIcon.Location = new Point(10, 14);
            this.fileTypeIcon.SizeMode = PictureBoxSizeMode.Zoom;

            // fileNameLabel
            this.fileNameLabel.AutoSize = true;
            this.fileNameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.fileNameLabel.Location = new Point(50, 18);
            this.fileNameLabel.MaximumSize = new Size(340, 0);
            this.fileNameLabel.Text = "Seleccione un archivo PDF o imagen";

            // RemoveFileButton
            this.RemoveFileButton.Text = "X";
            this.RemoveFileButton.FlatStyle = FlatStyle.Flat;
            this.RemoveFileButton.Size = new Size(24, 24);
            this.RemoveFileButton.Location = new Point(380, 15);
            this.RemoveFileButton.Cursor = Cursors.Hand;
            this.RemoveFileButton.Click += new EventHandler(this.RemoveFileButton_Click);

            // fileLabel
            this.fileLabel.Depth = 0;
            this.fileLabel.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.fileLabel.Location = new System.Drawing.Point(40, 220);
            this.fileLabel.MouseState = MaterialSkin.MouseState.HOVER;
            this.fileLabel.Name = "fileLabel";
            this.fileLabel.Size = new System.Drawing.Size(420, 19);
            this.fileLabel.TabIndex = 8;
            this.fileLabel.Text = "Ningún archivo seleccionado";
            this.fileLabel.Visible = false;

            // LabelNombre
            this.LabelNombre.Depth = 0;
            this.LabelNombre.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.LabelNombre.Location = new System.Drawing.Point(40, 240);
            this.LabelNombre.MouseState = MaterialSkin.MouseState.HOVER;
            this.LabelNombre.Name = "LabelNombre";
            this.LabelNombre.Size = new System.Drawing.Size(100, 23);
            this.LabelNombre.TabIndex = 2;
            this.LabelNombre.Text = "Título:";

            // titulo - Movido más abajo
            this.titulo.AnimateReadOnly = false;
            this.titulo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.titulo.Depth = 0;
            this.titulo.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.6F);
            this.titulo.LeadingIcon = null;
            this.titulo.Location = new System.Drawing.Point(40, 265);
            this.titulo.MaxLength = 50;
            this.titulo.MouseState = MaterialSkin.MouseState.OUT;
            this.titulo.Multiline = false;
            this.titulo.Name = "titulo";
            this.titulo.Size = new System.Drawing.Size(420, 50);
            this.titulo.TabIndex = 1;
            this.titulo.Text = "";
            this.titulo.TrailingIcon = null;

            // Label1 - Gabinetes
            this.Label1.BackColor = System.Drawing.Color.Transparent;
            this.Label1.Depth = 0;
            this.Label1.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.Label1.Location = new System.Drawing.Point(40, 320);
            this.Label1.MouseState = MaterialSkin.MouseState.HOVER;
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(100, 23);
            this.Label1.TabIndex = 4;
            this.Label1.Text = "Archivador:";

            // dropdown1 - Gabinetes
            this.dropdown1.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.dropdown1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dropdown1.ForeColor = System.Drawing.Color.Black;
            this.dropdown1.Location = new System.Drawing.Point(40, 345);
            this.dropdown1.Name = "dropdown1";
            this.dropdown1.Size = new System.Drawing.Size(420, 24);
            this.dropdown1.TabIndex = 2;
            this.dropdown1.DropDownStyle = ComboBoxStyle.DropDownList;

            // Label2 - Categorías
            this.Label2.Depth = 0;
            this.Label2.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.Label2.Location = new System.Drawing.Point(40, 375);
            this.Label2.MouseState = MaterialSkin.MouseState.HOVER;
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(100, 23);
            this.Label2.TabIndex = 5;
            this.Label2.Text = "Categoría:";

            // dropdown2 - Categorías
            this.dropdown2.BackColor = System.Drawing.Color.White;
            this.dropdown2.ForeColor = System.Drawing.Color.Black;
            this.dropdown2.Location = new System.Drawing.Point(40, 400);
            this.dropdown2.Name = "dropdown2";
            this.dropdown2.Size = new System.Drawing.Size(420, 24);
            this.dropdown2.TabIndex = 3;
            this.dropdown2.DropDownStyle = ComboBoxStyle.DropDownList;

            // Label3 - Subcategorías
            this.Label3.Depth = 0;
            this.Label3.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.Label3.Location = new System.Drawing.Point(40, 430);
            this.Label3.MouseState = MaterialSkin.MouseState.HOVER;
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(100, 23);
            this.Label3.TabIndex = 6;
            this.Label3.Text = "SubCategoría:";

            // dropdown3 - Subcategorías
            this.dropdown3.BackColor = System.Drawing.Color.White;
            this.dropdown3.ForeColor = System.Drawing.Color.Black;
            this.dropdown3.Location = new System.Drawing.Point(40, 455);
            this.dropdown3.Name = "dropdown3";
            this.dropdown3.Size = new System.Drawing.Size(420, 24);
            this.dropdown3.TabIndex = 4;
            this.dropdown3.DropDownStyle = ComboBoxStyle.DropDownList;

            // LabelDesc
            this.LabelDesc.Depth = 0;
            this.LabelDesc.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.LabelDesc.Location = new System.Drawing.Point(40, 485);
            this.LabelDesc.MouseState = MaterialSkin.MouseState.HOVER;
            this.LabelDesc.Name = "LabelDesc";
            this.LabelDesc.Size = new System.Drawing.Size(100, 23);
            this.LabelDesc.TabIndex = 3;
            this.LabelDesc.Text = "Descripción:";

            // desc
            this.desc.AnimateReadOnly = false;
            this.desc.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.desc.Depth = 0;
            this.desc.Font = new System.Drawing.Font("Roboto", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.desc.LeadingIcon = null;
            this.desc.Location = new System.Drawing.Point(40, 510);
            this.desc.MaxLength = 500;
            this.desc.MouseState = MaterialSkin.MouseState.OUT;
            this.desc.Multiline = true;
            this.desc.Name = "desc";
            this.desc.Size = new System.Drawing.Size(420, 70);
            this.desc.TabIndex = 5;
            this.desc.Text = "";
            this.desc.TrailingIcon = null;

            // buttonTablePanel
            this.buttonTablePanel.ColumnCount = 2;
            this.buttonTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.buttonTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.buttonTablePanel.RowCount = 2;
            this.buttonTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.buttonTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            this.buttonTablePanel.Location = new Point(40, 600);
            this.buttonTablePanel.Size = new Size(420, 100);
            this.buttonTablePanel.Padding = new Padding(5);

            // BtnScanToPortal
            this.BtnScanToPortal.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BtnScanToPortal.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.BtnScanToPortal.Depth = 0;
            this.BtnScanToPortal.HighEmphasis = true;
            this.BtnScanToPortal.Icon = null;
            this.BtnScanToPortal.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.BtnScanToPortal.MouseState = MaterialSkin.MouseState.HOVER;
            this.BtnScanToPortal.Name = "BtnScanToPortal";
            this.BtnScanToPortal.NoAccentTextColor = System.Drawing.Color.Empty;
            this.BtnScanToPortal.Size = new System.Drawing.Size(200, 36);
            this.BtnScanToPortal.TabIndex = 6;
            this.BtnScanToPortal.Text = "Scanner";
            this.BtnScanToPortal.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.BtnScanToPortal.UseAccentColor = true;
            this.BtnScanToPortal.Dock = DockStyle.Fill;
            this.BtnScanToPortal.Click += new System.EventHandler(this.BtnScanToPortal_Click);

            // SubmitButton
            this.SubmitButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.SubmitButton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.SubmitButton.Depth = 0;
            this.SubmitButton.HighEmphasis = true;
            this.SubmitButton.Icon = null;
            this.SubmitButton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.SubmitButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.SubmitButton.Name = "SubmitButton";
            this.SubmitButton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.SubmitButton.Size = new System.Drawing.Size(200, 36);
            this.SubmitButton.TabIndex = 8;
            this.SubmitButton.Text = "Enviar";
            this.SubmitButton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.SubmitButton.UseAccentColor = false;
            this.SubmitButton.Dock = DockStyle.Fill;
            this.SubmitButton.Click += new System.EventHandler(this.SubmitButton_Click);

            // ResetButton
            this.ResetButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ResetButton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.ResetButton.Depth = 0;
            this.ResetButton.HighEmphasis = true;
            this.ResetButton.Icon = null;
            this.ResetButton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.ResetButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.ResetButton.Name = "ResetButton";
            this.ResetButton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.ResetButton.Size = new System.Drawing.Size(200, 36);
            this.ResetButton.TabIndex = 9;
            this.ResetButton.Text = "Limpiar";
            this.ResetButton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.ResetButton.UseAccentColor = false;
            this.ResetButton.Dock = DockStyle.Fill;
            this.ResetButton.Click += new System.EventHandler(this.ResetButton_Click);

            // FormularioForm - Formulario agrandado
            this.ClientSize = new System.Drawing.Size(500, 720);
            this.Controls.Add(this.filePanel);
            this.Controls.Add(this.buttonTablePanel);
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
            this.Controls.Add(this.SelectFileButton);
            this.Controls.Add(this.fileLabel);
            this.Controls.Add(this.fileSelectionLabel);
            this.MaximizeBox = false;
            this.Name = "FormularioForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Formulario";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}