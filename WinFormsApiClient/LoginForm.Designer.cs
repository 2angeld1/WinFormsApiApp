using MaterialSkin.Controls;
using System;
using MaterialSkin;

namespace WinFormsApiClient
{
    partial class LoginForm
    {
        private MaterialSkin.Controls.MaterialTextBox emailTextBox;
        private MaterialSkin.Controls.MaterialTextBox passwordTextBox;
        private MaterialSkin.Controls.MaterialButton loginButton;
        private FontAwesome.Sharp.IconButton ShowPasswordButton;


        private void InitializeComponent()
        {
            this.emailTextBox = new MaterialSkin.Controls.MaterialTextBox();
            this.passwordTextBox = new MaterialSkin.Controls.MaterialTextBox();
            this.loginButton = new MaterialSkin.Controls.MaterialButton();
            this.ShowPasswordButton = new FontAwesome.Sharp.IconButton();
            this.SuspendLayout();
            // 
            // emailTextBox
            // 
            this.emailTextBox.AnimateReadOnly = false;
            this.emailTextBox.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.emailTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.emailTextBox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.emailTextBox.Depth = 0;
            this.emailTextBox.Font = new System.Drawing.Font("Roboto", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.emailTextBox.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.emailTextBox.Hint = "Correo electrónico";
            this.emailTextBox.LeadingIcon = null;
            this.emailTextBox.Location = new System.Drawing.Point(100, 49);
            this.emailTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.emailTextBox.MaxLength = 50;
            this.emailTextBox.MouseState = MaterialSkin.MouseState.OUT;
            this.emailTextBox.Multiline = false;
            this.emailTextBox.Name = "emailTextBox";
            this.emailTextBox.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.emailTextBox.Size = new System.Drawing.Size(241, 50);
            this.emailTextBox.TabIndex = 0;
            this.emailTextBox.Text = "";
            this.emailTextBox.TrailingIcon = null;
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.AnimateReadOnly = false;
            this.passwordTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.passwordTextBox.Depth = 0;
            this.passwordTextBox.Font = new System.Drawing.Font("Roboto", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.passwordTextBox.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.passwordTextBox.Hint = "Contraseña";
            this.passwordTextBox.LeadingIcon = null;
            this.passwordTextBox.Location = new System.Drawing.Point(100, 100);
            this.passwordTextBox.MaxLength = 50;
            this.passwordTextBox.MouseState = MaterialSkin.MouseState.OUT;
            this.passwordTextBox.Multiline = false;
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Password = true;
            this.passwordTextBox.Size = new System.Drawing.Size(241, 50);
            this.passwordTextBox.TabIndex = 1;
            this.passwordTextBox.Text = "";
            this.passwordTextBox.TrailingIcon = null;
            // 
            // loginButton
            // 
            this.loginButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.loginButton.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.loginButton.Depth = 0;
            this.loginButton.HighEmphasis = true;
            this.loginButton.Icon = null;
            this.loginButton.Location = new System.Drawing.Point(100, 160);
            this.loginButton.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.loginButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.loginButton.Name = "loginButton";
            this.loginButton.NoAccentTextColor = System.Drawing.Color.Empty;
            this.loginButton.Size = new System.Drawing.Size(128, 36);
            this.loginButton.TabIndex = 2;
            this.loginButton.Text = "Iniciar sesión";
            this.loginButton.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.loginButton.UseAccentColor = false;
            this.loginButton.Click += new System.EventHandler(this.LoginButton_Click);
            // 
            // ShowPasswordButton
            // 
            this.ShowPasswordButton.BackColor = System.Drawing.SystemColors.Highlight;
            this.ShowPasswordButton.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.ShowPasswordButton.IconChar = FontAwesome.Sharp.IconChar.Eye;
            this.ShowPasswordButton.IconColor = System.Drawing.Color.White;
            this.ShowPasswordButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.ShowPasswordButton.Location = new System.Drawing.Point(235, 160);
            this.ShowPasswordButton.Name = "ShowPasswordButton";
            this.ShowPasswordButton.Size = new System.Drawing.Size(51, 36);
            this.ShowPasswordButton.TabIndex = 4;
            this.ShowPasswordButton.UseVisualStyleBackColor = false;
            this.ShowPasswordButton.Click += new System.EventHandler(this.ShowPasswordButton_Click);
            // 
            // LoginForm
            // 
            this.ClientSize = new System.Drawing.Size(400, 250);
            this.Controls.Add(this.emailTextBox);
            this.Controls.Add(this.passwordTextBox);
            this.Controls.Add(this.ShowPasswordButton);
            this.Controls.Add(this.loginButton);
            this.MaximizeBox = false;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Login";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

    }
}