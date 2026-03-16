using System;
using System.Drawing;
using System.Collections.Concurrent;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using System.Windows.Forms;



namespace AnimalHomeGame_CSharp;

public partial class MainForm : Form
{
        private System.Windows.Forms.Panel signInPanel;
        private System.Windows.Forms.Panel signUpPanel;

        private Label statusLabel;
        private TextBox nameTextBox;
        private ListBox deviceListBox;
        private Button scanButton;
        private Button registerButton;

        private DeviceWatcher deviceWatcher;
        private ConcurrentDictionary<string, DeviceInformation> discoveredDevices = new();

        public MainForm()
        {
            InitializeComponent();
            SetupGUI();
            SetupBluetoothWatcher();
        }

        private void SetupBluetoothWatcher()
        {
            string[] requestedProperties = { "System.ItemNameDisplay", "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
            
            deviceWatcher = DeviceInformation.CreateWatcher(
                "(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                requestedProperties,
                DeviceInformationKind.AssociationEndpoint);

            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            if (string.IsNullOrWhiteSpace(deviceInfo.Name)) return;
            if (discoveredDevices.TryAdd(deviceInfo.Id, deviceInfo))
            {
                UpdateDeviceList();
            }
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            if (discoveredDevices.TryGetValue(deviceInfoUpdate.Id, out DeviceInformation deviceInfo))
            {
                deviceInfo.Update(deviceInfoUpdate);
                UpdateDeviceList();
            }
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            if (discoveredDevices.TryRemove(deviceInfoUpdate.Id, out _))
            {
                UpdateDeviceList();
            }
        }

        private void UpdateDeviceList()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateDeviceList));
                return;
            }

            deviceListBox.Items.Clear();
            foreach (var device in discoveredDevices.Values)
            {
                deviceListBox.Items.Add($"{device.Name} ({device.Id})");
            }
        }

        private void SetupGUI()
        {
           
            this.Text = "Animal Home Game - Authentication";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;

           
            signInPanel = new System.Windows.Forms.Panel { Dock = DockStyle.Fill };
            signUpPanel = new System.Windows.Forms.Panel { Dock = DockStyle.Fill, Visible = false }; // Hidden by default

            BuildSignInScreen();
            BuildSignUpScreen();

            
            this.Controls.Add(signUpPanel);
            this.Controls.Add(signInPanel);
        }

        private void BuildSignInScreen()
        {
            Label welcomeLabel = new Label { Text = "Welcome to Animal Home", Font = new Font("Segoe UI", 24, FontStyle.Bold), AutoSize = true, Location = new Point(200, 150) };
            
            statusLabel = new Label { Text = "Scanning for your Bluetooth device to sign in...", Font = new Font("Segoe UI", 12, FontStyle.Italic), ForeColor = Color.DimGray, AutoSize = true, Location = new Point(230, 220) };
            
            Button goToSignUpBtn = new Button { Text = "Register New Player", Font = new Font("Segoe UI", 12), Size = new Size(200, 50), Location = new Point(300, 300), Cursor = Cursors.Hand };
            
            
            goToSignUpBtn.Click += (sender, e) => {
                signInPanel.Visible = false;
                signUpPanel.Visible = true;
            };

            signInPanel.Controls.Add(welcomeLabel);
            signInPanel.Controls.Add(statusLabel);
            signInPanel.Controls.Add(goToSignUpBtn);
        }

        private void BuildSignUpScreen()
        {
            Label titleLabel = new Label { Text = "Register New Bluetooth Device", Font = new Font("Segoe UI", 20, FontStyle.Bold), AutoSize = true, Location = new Point(200, 50) };
            
            Label nameLabel = new Label { Text = "Player Name:", Font = new Font("Segoe UI", 12), AutoSize = true, Location = new Point(150, 130) };
            nameTextBox = new TextBox { Font = new Font("Segoe UI", 12), Size = new Size(300, 30), Location = new Point(280, 127) };

            scanButton = new Button { Text = "Scan for Devices", Font = new Font("Segoe UI", 12), Size = new Size(430, 40), Location = new Point(150, 180), Cursor = Cursors.Hand };
            scanButton.Click += (sender, e) => {
                discoveredDevices.Clear();
                deviceListBox.Items.Clear();
                if (deviceWatcher == null) return;
                
                if (deviceWatcher.Status != DeviceWatcherStatus.Started)
                {
                    deviceWatcher.Start();
                    scanButton.Text = "Scanning... (Click to Stop)";
                }
                else
                {
                    deviceWatcher.Stop();
                    scanButton.Text = "Scan for Devices";
                }
            };
            
            deviceListBox = new ListBox { Font = new Font("Segoe UI", 10), Size = new Size(430, 150), Location = new Point(150, 240) };

            registerButton = new Button { Text = "Save & Sign Up", Font = new Font("Segoe UI", 12, FontStyle.Bold), BackColor = Color.LightGreen, Size = new Size(200, 50), Location = new Point(150, 420), Cursor = Cursors.Hand };
            
            Button cancelBtn = new Button { Text = "Cancel", Font = new Font("Segoe UI", 12), BackColor = Color.LightPink, Size = new Size(200, 50), Location = new Point(380, 420), Cursor = Cursors.Hand };
            
            
            cancelBtn.Click += (sender, e) => {
                signUpPanel.Visible = false;
                signInPanel.Visible = true;
                deviceListBox.Items.Clear(); 
                nameTextBox.Clear();
                if (deviceWatcher != null && (deviceWatcher.Status == DeviceWatcherStatus.Started || deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted))
                {
                    deviceWatcher.Stop();
                    scanButton.Text = "Scan for Devices";
                }
            };

            registerButton.Click += (sender, e) => {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    MessageBox.Show("Please enter a player name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (deviceListBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a Bluetooth device to register with your profile.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string selectedDevice = deviceListBox.SelectedItem.ToString();
                
                if (deviceWatcher.Status == DeviceWatcherStatus.Started)
                {
                    deviceWatcher.Stop();
                    scanButton.Text = "Scan for Devices";
                }

                MessageBox.Show($"Successfully registered '{nameTextBox.Text}' with device:\n{selectedDevice}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                cancelBtn.PerformClick();
            };

            signUpPanel.Controls.Add(titleLabel);
            signUpPanel.Controls.Add(nameLabel);
            signUpPanel.Controls.Add(nameTextBox);
            signUpPanel.Controls.Add(scanButton);
            signUpPanel.Controls.Add(deviceListBox);
            signUpPanel.Controls.Add(registerButton);
            signUpPanel.Controls.Add(cancelBtn);
        }
    }
