using System;
using System.Drawing;
using System.Windows.Forms;

namespace AnimalHomeGame_CSharp;

public partial class GameForm : Form
{
    private UserProfile currentUser;

    public GameForm(UserProfile profile)
    {
        this.currentUser = profile;
        InitializeComponent();
        SetupGUI();
    }

    private void SetupGUI()
    {
        this.Text = "Animal Home Game - Main Menu";
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.WhiteSmoke;
        this.FormClosed += GameForm_FormClosed;

        Panel headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = Color.LightSkyBlue
        };

        Label welcomeLabel = new Label
        {
            Text = $"Welcome back, {currentUser.PlayerName}!\nRole: {currentUser.Role}",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.DarkSlateBlue,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };

        headerPanel.Controls.Add(welcomeLabel);
        this.Controls.Add(headerPanel);

        Panel controlsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(50)
        };

        Button playButton = new Button
        {
            Text = "Play Game",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            BackColor = Color.LightGreen,
            Size = new Size(300, 60),
            Location = new Point(250, 50),
            Cursor = Cursors.Hand
        };
        playButton.Click += (s, e) => 
        {
            MainForm? mainForm = null;
            foreach (Form f in Application.OpenForms)
                if (f is MainForm mf) { mainForm = mf; break; }

            if (mainForm != null)
            {
                GamePlayForm gamePlay = new GamePlayForm(currentUser, mainForm);
                gamePlay.Show();
                this.Hide();
            }
        };

        Button logoutButton = new Button
        {
            Text = "Logout & Switch User",
            Font = new Font("Segoe UI", 14),
            BackColor = Color.LightPink,
            Size = new Size(300, 50),
            Location = new Point(250, currentUser.Role == "Admin" ? 350 : 150),
            Cursor = Cursors.Hand
        };
        logoutButton.Click += (s, e) => this.Close();

        controlsPanel.Controls.Add(playButton);
        controlsPanel.Controls.Add(logoutButton);

        if (currentUser.Role == "Admin")
        {
            Button viewStatsButton = new Button
            {
                Text = "View Global Stats",
                Font = new Font("Segoe UI", 14),
                BackColor = Color.Wheat,
                Size = new Size(300, 50),
                Location = new Point(250, 130),
                Cursor = Cursors.Hand
            };
            viewStatsButton.Click += (s, e) => MessageBox.Show("Opening Stats...", "Admin Only");

            Button manageUsersButton = new Button
            {
                Text = "Manage Users",
                Font = new Font("Segoe UI", 14),
                BackColor = Color.LightYellow,
                Size = new Size(300, 50),
                Location = new Point(250, 200),
                Cursor = Cursors.Hand
            };
            manageUsersButton.Click += (s, e) => MessageBox.Show("Opening User Manager...", "Admin Only");

            Button settingsButton = new Button
            {
                Text = "Game Settings",
                Font = new Font("Segoe UI", 14),
                BackColor = Color.LightGray,
                Size = new Size(300, 50),
                Location = new Point(250, 270),
                Cursor = Cursors.Hand
            };
            settingsButton.Click += (s, e) => MessageBox.Show("Opening Settings...", "Admin Only");

            controlsPanel.Controls.Add(viewStatsButton);
            controlsPanel.Controls.Add(manageUsersButton);
            controlsPanel.Controls.Add(settingsButton);
        }

        this.Controls.Add(controlsPanel);
        controlsPanel.BringToFront();
    }

    private void GameForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        foreach (Form form in Application.OpenForms)
        {
            if (form is MainForm mainForm)
            {
                mainForm.ResetScanner();
                mainForm.Show();
                break;
            }
        }
    }

    private System.ComponentModel.IContainer components = null;

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
        this.SuspendLayout();
        this.ClientSize = new System.Drawing.Size(284, 261);
        this.Name = "GameForm";
        this.ResumeLayout(false);
    }
}
