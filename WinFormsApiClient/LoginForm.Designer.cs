using MaterialSkin.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;

namespace WinFormsApiClient
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        // MaterialForm controles
        private MaterialSkin.Controls.MaterialTextBox emailTextBox;
        private MaterialSkin.Controls.MaterialTextBox passwordTextBox;
        private MaterialSkin.Controls.MaterialButton loginButton;
        private FontAwesome.Sharp.IconButton ShowPasswordButton;

        // Controles para el nuevo diseño
        private Panel logoPanel;        // Panel izquierdo (45%)
        private Panel formPanel;        // Panel derecho (55%)
        private PictureBox logoPictureBox; // Imagen principal en panel izquierdo
        private Label welcomeLabel;     // Etiqueta de bienvenida
        private Label subtitleLabel;    // Subtítulo de bienvenida

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
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
            this.components = new System.ComponentModel.Container();

            // Inicializar todos los controles
            this.emailTextBox = new MaterialSkin.Controls.MaterialTextBox();
            this.passwordTextBox = new MaterialSkin.Controls.MaterialTextBox();
            this.loginButton = new MaterialSkin.Controls.MaterialButton();
            this.ShowPasswordButton = new FontAwesome.Sharp.IconButton();
            this.logoPanel = new Panel();
            this.formPanel = new Panel();
            this.logoPictureBox = new PictureBox();
            this.welcomeLabel = new Label();
            this.subtitleLabel = new Label();

            // Configuración para poder modificar controles dentro de paneles
            this.logoPanel.SuspendLayout();
            this.formPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.SuspendLayout();

            #region Configuración del formulario principal

            // LoginForm
            this.ClientSize = new System.Drawing.Size(800, 400); // Tamaño horizontal 800x400
            this.MaximizeBox = false;
            this.Name = "LoginForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "ECM Central - Inicio de Sesión";
            this.Padding = new Padding(3, 64, 3, 3); // Espacio para la barra de título

            #endregion

            #region Panel Izquierdo con Logo (45% del ancho)

            // logoPanel
            this.logoPanel.BackColor = Color.Transparent;
            this.logoPanel.Dock = DockStyle.Left;
            this.logoPanel.Location = new Point(3, 64);
            this.logoPanel.Name = "logoPanel";
            this.logoPanel.Size = new Size(360, 333);
            this.logoPanel.TabIndex = 0;

            // logoPictureBox - Imagen principal (illustration.png)
            this.logoPictureBox.BackColor = Color.Transparent;
            this.logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.logoPictureBox.Location = new Point(30, 30);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new Size(300, 260);
            this.logoPictureBox.TabIndex = 0;
            this.logoPictureBox.TabStop = false;

            // Añadir controles al panel izquierdo
            this.logoPanel.Controls.Add(this.logoPictureBox);

            #endregion

            #region Panel Derecho con Formulario (55% del ancho)

            // formPanel
            this.formPanel.BackColor = Color.White;
            this.formPanel.Dock = DockStyle.Fill;
            this.formPanel.Location = new Point(363, 64); // Debajo de la barra de título, después del panel izquierdo
            this.formPanel.Name = "formPanel";
            this.formPanel.Size = new Size(434, 333);
            this.formPanel.TabIndex = 1;
            this.formPanel.Padding = new Padding(20);

            // welcomeLabel - Título de bienvenida
            this.welcomeLabel.AutoSize = true;
            this.welcomeLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.welcomeLabel.Location = new Point(30, 30);
            this.welcomeLabel.Name = "welcomeLabel";
            this.welcomeLabel.Size = new Size(200, 30);
            this.welcomeLabel.TabIndex = 0;
            this.welcomeLabel.Text = "";

            // subtitleLabel - Subtítulo
            this.subtitleLabel.AutoSize = true;
            this.subtitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.subtitleLabel.ForeColor = Color.Gray;
            this.subtitleLabel.Location = new Point(31, 60);
            this.subtitleLabel.Name = "subtitleLabel";
            this.subtitleLabel.Size = new Size(200, 20);
            this.subtitleLabel.TabIndex = 1;
            this.subtitleLabel.Text = "Inicia sesión para continuar";

            // emailTextBox
            this.emailTextBox.AnimateReadOnly = false;
            this.emailTextBox.BorderStyle = BorderStyle.None;
            this.emailTextBox.Depth = 0;
            this.emailTextBox.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            this.emailTextBox.Hint = "Correo electrónico";
            this.emailTextBox.LeadingIcon = null;
            this.emailTextBox.Location = new Point(30, 100);
            this.emailTextBox.MaxLength = 50;
            this.emailTextBox.MouseState = MaterialSkin.MouseState.OUT;
            this.emailTextBox.Multiline = false;
            this.emailTextBox.Name = "emailTextBox";
            this.emailTextBox.Size = new Size(370, 50);
            this.emailTextBox.TabIndex = 2;
            this.emailTextBox.Text = "";
            this.emailTextBox.TrailingIcon = null;

            // passwordTextBox
            this.passwordTextBox.AnimateReadOnly = false;
            this.passwordTextBox.BorderStyle = BorderStyle.None;
            this.passwordTextBox.Depth = 0;
            this.passwordTextBox.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            this.passwordTextBox.Hint = "Contraseña";
            this.passwordTextBox.LeadingIcon = null;
            this.passwordTextBox.Location = new Point(30, 170);
            this.passwordTextBox.MaxLength = 50;
            this.passwordTextBox.MouseState = MaterialSkin.MouseState.OUT;
            this.passwordTextBox.Multiline = false;
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Password = true;
            this.passwordTextBox.Size = new Size(370, 50);
            this.passwordTextBox.TabIndex = 3;
            this.passwordTextBox.Text = "";
            this.passwordTextBox.TrailingIcon = null;

            // ShowPasswordButton
            this.ShowPasswordButton.BackColor = Color.Transparent;
            this.ShowPasswordButton.FlatAppearance.BorderSize = 0;
            this.ShowPasswordButton.FlatStyle = FlatStyle.Flat;
            this.ShowPasswordButton.IconChar = FontAwesome.Sharp.IconChar.Eye;
            this.ShowPasswordButton.IconColor = Color.Gray;
            this.ShowPasswordButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.ShowPasswordButton.IconSize = 24;
            this.ShowPasswordButton.Location = new Point(350, 230);
            this.ShowPasswordButton.Name = "ShowPasswordButton";
            this.ShowPasswordButton.Size = new Size(50, 30);
            this.ShowPasswordButton.TabIndex = 4;
            this.ShowPasswordButton.UseVisualStyleBackColor = false;
            this.ShowPasswordButton.Click += new EventHandler(this.ShowPasswordButton_Click);

            // loginButton
            this.loginButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.loginButton.Density = MaterialButton.MaterialButtonDensity.Default;
            this.loginButton.Depth = 0;
            this.loginButton.HighEmphasis = true;
            this.loginButton.Icon = null;
            this.loginButton.Location = new Point(30, 270);
            this.loginButton.Margin = new Padding(4, 6, 4, 6);
            this.loginButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.loginButton.Name = "loginButton";
            this.loginButton.NoAccentTextColor = Color.Empty;
            this.loginButton.Size = new Size(370, 36);
            this.loginButton.TabIndex = 5;
            this.loginButton.Text = "INICIAR SESIÓN";
            this.loginButton.Type = MaterialButton.MaterialButtonType.Contained;
            this.loginButton.UseAccentColor = false;
            this.loginButton.Click += new EventHandler(this.LoginButton_Click);

            // Añadir controles al panel derecho
            this.formPanel.Controls.Add(this.welcomeLabel);
            this.formPanel.Controls.Add(this.subtitleLabel);
            this.formPanel.Controls.Add(this.emailTextBox);
            this.formPanel.Controls.Add(this.passwordTextBox);
            this.formPanel.Controls.Add(this.ShowPasswordButton);
            this.formPanel.Controls.Add(this.loginButton);

            #endregion

            // Añadir los paneles principales al formulario (el orden es importante)
            this.Controls.Add(this.formPanel);
            this.Controls.Add(this.logoPanel);

            // Finalizar
            this.logoPanel.ResumeLayout(false);
            this.formPanel.ResumeLayout(false);
            this.formPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.ResumeLayout(false);
        }
    }
}