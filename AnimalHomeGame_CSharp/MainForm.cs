using System;
using System.Drawing;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using System.Windows.Forms;

namespace AnimalHomeGame_CSharp;

// MainForm is the first screen the player sees.
// It scans for nearby Bluetooth devices to automatically identify the player.
public partial class MainForm : Form
{
    private Label statusLabel;
    private Label instructionsLabel;
    private DeviceWatcher deviceWatcher;

    // Tracks whether we already found a device so we don't authenticate twice
    private bool isAuthenticated = false;

    public MainForm()
    {
        InitializeComponent();
        SetupGUI();
        SetupBluetoothWatcher();
    }

    // Build and arrange all the labels on screen
    private void SetupGUI()
    {
        this.Text = "Animal Home Game - Authentication";
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.WhiteSmoke;

        statusLabel = new Label();
        statusLabel.Text = "Scanning for your Bluetooth device...";
        statusLabel.Font = new Font("Segoe UI", 20, FontStyle.Bold);
        statusLabel.ForeColor = Color.DimGray;
        statusLabel.AutoSize = false;
        statusLabel.TextAlign = ContentAlignment.MiddleCenter;
        statusLabel.Dock = DockStyle.Top;
        statusLabel.Height = 150;
        statusLabel.Padding = new Padding(0, 50, 0, 0);

        instructionsLabel = new Label();
        instructionsLabel.Text = "How it works:\n\n1. Make sure your phone or device's Bluetooth is turned ON and is 'Discoverable'.\n2. Keep your device nearby.\n3. We will automatically detect you!\n\nIf you are new, we will instantly create your profile.\nIf you are returning, you will be logged right in.";
        instructionsLabel.Font = new Font("Segoe UI", 12, FontStyle.Regular);
        instructionsLabel.ForeColor = Color.DarkSlateGray;
        instructionsLabel.AutoSize = false;
        instructionsLabel.TextAlign = ContentAlignment.MiddleCenter;
        instructionsLabel.Dock = DockStyle.Fill;
        instructionsLabel.Padding = new Padding(20);

        this.Controls.Add(instructionsLabel);
        this.Controls.Add(statusLabel);
    }

    // Start the Windows Bluetooth device watcher so we detect nearby devices
    private void SetupBluetoothWatcher()
    {
        // The specific properties we want to read from each discovered device
        string[] requestedProperties = {
            "System.ItemNameDisplay",
            "System.Devices.Aep.DeviceAddress",
            "System.Devices.Aep.IsConnected"
        };

        // This filter string tells Windows to look only for Bluetooth LE devices
        string bluetoothFilter = "(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")";

        deviceWatcher = DeviceInformation.CreateWatcher(
            bluetoothFilter,
            requestedProperties,
            DeviceInformationKind.AssociationEndpoint);

        // Subscribe to the Added event so we are notified when a device is found
        deviceWatcher.Added += DeviceWatcher_Added;
        deviceWatcher.Start();
    }

    // This method is called (on a background thread) each time a nearby Bluetooth device is found.
    private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
    {
        // If we already authenticated someone, ignore further devices
        if (isAuthenticated)
        {
            return;
        }

        // Skip devices that have no name
        if (string.IsNullOrWhiteSpace(deviceInfo.Name))
        {
            return;
        }

        // Use a lock so that if two devices are found at the same moment,
        // only the first one gets processed.
        lock (this)
        {
            if (isAuthenticated)
            {
                return;
            }
            isAuthenticated = true;

            // Stop scanning — we found our player
            if (deviceWatcher.Status == DeviceWatcherStatus.Started)
            {
                deviceWatcher.Stop();
            }
        }

        // UI updates must happen on the main thread.
        // InvokeRequired is true when we are on a background thread.
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(CallAuthenticateOnUIThread));
        }
        else
        {
            AuthenticateDevice(deviceInfo);
        }

        // Local helper so we can pass it cleanly to Invoke without a lambda
        void CallAuthenticateOnUIThread()
        {
            AuthenticateDevice(deviceInfo);
        }
    }

    // Look up the detected device in our player list and either log them in
    // or create a new account for them.
    private void AuthenticateDevice(DeviceInformation deviceInfo)
    {
        List<UserProfile> profiles = ProfileManager.LoadProfiles();

        // Search the list for a profile that matches this Bluetooth device ID
        UserProfile? existingProfile = FindProfileByDeviceId(profiles, deviceInfo.Id);

        UserProfile activeProfile;

        if (existingProfile != null)
        {
            // Returning player — use their existing profile
            activeProfile = existingProfile;
            statusLabel.Text = "Welcome back, " + existingProfile.PlayerName + "!";
            instructionsLabel.Text = "Automatic Sign-In Successful.\nYour Role: " + existingProfile.Role + "\n\nGetting everything ready for you...";
            statusLabel.ForeColor = Color.Green;
            instructionsLabel.ForeColor = Color.Black;
        }
        else
        {
            // New player — create an account.
            // The very first player to register becomes the Admin.
            string newRole;
            if (profiles.Count == 0)
            {
                newRole = "Admin";
            }
            else
            {
                newRole = "User";
            }

            UserProfile newProfile = new UserProfile();
            newProfile.PlayerName = deviceInfo.Name;
            newProfile.BluetoothDeviceId = deviceInfo.Id;
            newProfile.Role = newRole;

            profiles.Add(newProfile);
            ProfileManager.SaveProfiles(profiles);

            activeProfile = newProfile;
            statusLabel.Text = "Account Created for " + newProfile.PlayerName + "!";
            instructionsLabel.Text = "Automatic Sign-Up Successful.\nYour Role: " + newProfile.Role + "\n\nGetting everything ready for you...";
            statusLabel.ForeColor = Color.Blue;
            instructionsLabel.ForeColor = Color.Black;
        }

        // Wait 3 seconds so the player can read the welcome message, then open the game
        System.Windows.Forms.Timer delayTimer = new System.Windows.Forms.Timer();
        delayTimer.Interval = 3000;
        delayTimer.Tick += new EventHandler(LaunchGameAfterDelay);
        delayTimer.Tag = activeProfile; // Store the profile so the tick handler can use it
        delayTimer.Start();
    }

    // Called when the 3-second welcome timer fires — opens the game screen
    private void LaunchGameAfterDelay(object? sender, EventArgs e)
    {
        System.Windows.Forms.Timer delayTimer = (System.Windows.Forms.Timer)sender!;
        delayTimer.Stop();

        // Retrieve the player profile we stored on the timer
        UserProfile activeProfile = (UserProfile)delayTimer.Tag!;

        GamePlayForm gamePlayForm = new GamePlayForm(activeProfile, this);
        gamePlayForm.Show();
        this.Hide();
    }

    // Look for a player profile that has the same Bluetooth device ID.
    // Returns null if no matching profile is found.
    private UserProfile? FindProfileByDeviceId(List<UserProfile> profiles, string deviceId)
    {
        foreach (UserProfile profile in profiles)
        {
            if (profile.BluetoothDeviceId == deviceId)
            {
                return profile;
            }
        }
        return null;
    }

    // Called by GamePlayForm when the player logs out.
    // Resets the screen and starts scanning again for the next player.
    public void ResetScanner()
    {
        isAuthenticated = false;

        statusLabel.Text = "Scanning for your Bluetooth device...";
        statusLabel.ForeColor = Color.DimGray;
        instructionsLabel.Text = "How it works:\n\n1. Make sure your phone or device's Bluetooth is turned ON and is 'Discoverable'.\n2. Keep your device nearby.\n3. We will automatically detect you!\n\nIf you are new, we will instantly create your profile.\nIf you are returning, you will be logged right in.";
        instructionsLabel.ForeColor = Color.DarkSlateGray;

        // Only restart the watcher if it has stopped
        if (deviceWatcher != null && deviceWatcher.Status != DeviceWatcherStatus.Started)
        {
            deviceWatcher.Start();
        }
    }
}
