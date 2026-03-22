using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AnimalHomeGame_CSharp;

// GamePlayForm is the actual game screen where players drag animals to their homes.
// Animals are moved by placing physical TUIO markers (or by mouse for testing).
public class GamePlayForm : Form
{
    // The player who is currently playing
    private readonly UserProfile currentUser;

    // Reference to the login screen so we can show it again when the game closes
    private readonly MainForm parentScanner;

    // Handles communication with the TUIO sensor
    private TuioHandler tuioHandler;

    // The TUIO marker ID that triggers a logout when placed
    private const int LOGOUT_MARKER_ID = 5;

    // Maps each TUIO marker ID to the animal it controls
    // Example: animalById[1] gives the Dog's GameItem
    private readonly Dictionary<int, GameItem> animalById = new Dictionary<int, GameItem>();

    // List of all home zones (Nest, Doghouse, Water, Farm)
    private readonly List<GameItem> homes = new List<GameItem>();

    // Tracks which animals are currently being held by a TUIO marker
    // Key = marker ID, Value = the animal being moved
    private readonly Dictionary<int, GameItem> grabbedAnimals = new Dictionary<int, GameItem>();

    // The animal currently being dragged by the mouse (null if none)
    private GameItem? mouseDragItem = null;

    // Stores where on the image the mouse clicked, so the drag looks natural
    private Point mouseOffset;

    // The bar at the top that shows messages like "Wrong home!" or "You win!"
    private Label feedbackLabel = null!;

    // A small debug bar at the bottom showing the last TUIO event received
    private Label debugLabel = null!;

    // Defines all four animals: name, TUIO marker ID, image file, and target home name
    private static readonly (string name, int tuioId, string image, string home)[] AnimalDefs =
    {
        ("Bird",  0, "bird.jpeg",  "Nest"),
        ("Dog",   1, "dog.jpeg",   "Doghouse"),
        ("Fish",  2, "fish.jpeg",  "Water"),
        ("Farm",  3, "farm.jpeg",  "Farm"),
    };

    // Defines all four home zones: name and image file
    private static readonly (string name, string image)[] HomeDefs =
    {
        ("Nest",     "nest.jpeg"),
        ("Doghouse", "doghouse.jpeg"),
        ("Water",    "water.jpeg"),
        ("Farm",     "farm.jpeg"),
    };

    public GamePlayForm(UserProfile profile, MainForm mainForm)
    {
        currentUser = profile;
        parentScanner = mainForm;
        tuioHandler = new TuioHandler();
        InitializeComponent();
        SetupGUI();
        SetupTuio();
    }

    // Build all the visual elements: background, buttons, animals, and homes
    private void SetupGUI()
    {
        this.Text = "Animal Home Game - Playing";
        this.Size = new Size(1024, 768);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormClosed += GamePlayForm_FormClosed;
        this.DoubleBuffered = true; // Reduces flicker when images are moved

        // Load the background image if it exists
        string bgPath = GetAssetPath("background.jpeg");
        if (File.Exists(bgPath))
        {
            this.BackgroundImage = Image.FromFile(bgPath);
            this.BackgroundImageLayout = ImageLayout.Stretch;
        }
        else
        {
            this.BackColor = Color.DarkGreen;
        }

        // Back button — closes the game and returns to the login screen
        Button backButton = new Button();
        backButton.Text = "← Back";
        backButton.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        backButton.BackColor = Color.FromArgb(180, 80, 80);
        backButton.ForeColor = Color.White;
        backButton.Size = new Size(110, 40);
        backButton.Location = new Point(10, 10);
        backButton.Cursor = Cursors.Hand;
        backButton.FlatStyle = FlatStyle.Flat;
        backButton.FlatAppearance.BorderSize = 0;
        backButton.Click += BackButton_Click;
        this.Controls.Add(backButton);

        // Feedback label — displays messages at the top of the screen
        feedbackLabel = new Label();
        feedbackLabel.Text = "Place the correct TUIO marker on each animal to unlock it!";
        feedbackLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        feedbackLabel.ForeColor = Color.White;
        feedbackLabel.BackColor = Color.FromArgb(160, 20, 20, 20);
        feedbackLabel.AutoSize = false;
        feedbackLabel.TextAlign = ContentAlignment.MiddleCenter;
        feedbackLabel.Size = new Size(700, 42);
        feedbackLabel.Location = new Point((this.ClientSize.Width - 700) / 2, 10);
        this.Controls.Add(feedbackLabel);

        // Debug label — shows the last raw TUIO event at the bottom of the screen
        debugLabel = new Label();
        debugLabel.Text = "TUIO: waiting...";
        debugLabel.Font = new Font("Segoe UI", 9);
        debugLabel.ForeColor = Color.LightGray;
        debugLabel.BackColor = Color.FromArgb(130, 0, 0, 0);
        debugLabel.AutoSize = false;
        debugLabel.TextAlign = ContentAlignment.MiddleLeft;
        debugLabel.Size = new Size(300, 22);
        debugLabel.Location = new Point(10, this.ClientSize.Height - 30);
        this.Controls.Add(debugLabel);

        // Layout constants for placing animals (left side) and homes (right side)
        int itemHeight = 100;
        int itemWidth  = 110;
        int startY     = 100; // Y position of the first row
        int spacingY   = 130; // Vertical distance between rows
        int leftX      = 40;  // X position for animals
        int rightX     = 860; // X position for homes

        // Create a picture and label for each animal
        for (int i = 0; i < AnimalDefs.Length; i++)
        {
            string name       = AnimalDefs[i].name;
            int    tuioId     = AnimalDefs[i].tuioId;
            string imageFile  = AnimalDefs[i].image;
            string targetHome = AnimalDefs[i].home;
            int    y          = startY + i * spacingY;

            // Label above the animal picture showing its name and marker number
            Label animalLabel = new Label();
            animalLabel.Text = name + "  [Marker #" + tuioId + "]";
            animalLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            animalLabel.ForeColor = Color.White;
            animalLabel.BackColor = Color.FromArgb(120, 0, 0, 0);
            animalLabel.TextAlign = ContentAlignment.MiddleCenter;
            animalLabel.Size = new Size(itemWidth, 20);
            animalLabel.Location = new Point(leftX, y - 22);
            this.Controls.Add(animalLabel);

            // Picture box for the animal image
            PictureBox animalPic = new PictureBox();
            animalPic.Size = new Size(itemWidth, itemHeight);
            animalPic.Location = new Point(leftX, y);
            animalPic.SizeMode = PictureBoxSizeMode.StretchImage;
            animalPic.BorderStyle = BorderStyle.FixedSingle;
            animalPic.Cursor = Cursors.Hand;
            animalPic.Tag = name;

            string path = GetAssetPath(imageFile);
            if (File.Exists(path))
            {
                animalPic.Image = Image.FromFile(path);
            }
            else
            {
                animalPic.BackColor = Color.LightGray;
            }

            // Hook up mouse events so the player can also drag with the mouse
            animalPic.MouseDown += AnimalPic_MouseDown;
            animalPic.MouseMove += AnimalPic_MouseMove;
            animalPic.MouseUp   += AnimalPic_MouseUp;

            // Create the data object for this animal
            GameItem animalItem = new GameItem();
            animalItem.Name = name;
            animalItem.TuioId = tuioId;
            animalItem.Picture = animalPic;
            animalItem.OriginalLocation = new Point(leftX, y);
            animalItem.TargetHomeName = targetHome;
            animalItem.IsMatched = false;

            // Register it in the dictionary so TUIO events can find it by marker ID
            animalById[tuioId] = animalItem;

            this.Controls.Add(animalPic);
            animalPic.BringToFront();
            animalLabel.BringToFront();
        }

        // Create a picture and label for each home zone
        for (int i = 0; i < HomeDefs.Length; i++)
        {
            string homeName   = HomeDefs[i].name;
            string imageFile  = HomeDefs[i].image;
            int    y          = startY + i * spacingY;

            Label homeLabel = new Label();
            homeLabel.Text = homeName;
            homeLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            homeLabel.ForeColor = Color.White;
            homeLabel.BackColor = Color.FromArgb(120, 0, 0, 0);
            homeLabel.TextAlign = ContentAlignment.MiddleCenter;
            homeLabel.Size = new Size(itemWidth, 20);
            homeLabel.Location = new Point(rightX, y - 22);
            this.Controls.Add(homeLabel);

            PictureBox homePic = new PictureBox();
            homePic.Size = new Size(itemWidth, itemHeight);
            homePic.Location = new Point(rightX, y);
            homePic.SizeMode = PictureBoxSizeMode.StretchImage;
            homePic.BorderStyle = BorderStyle.Fixed3D;
            homePic.Tag = homeName;

            string path = GetAssetPath(imageFile);
            if (File.Exists(path))
            {
                homePic.Image = Image.FromFile(path);
            }
            else
            {
                homePic.BackColor = Color.LightGray;
            }

            GameItem homeItem = new GameItem();
            homeItem.Name = homeName;
            homeItem.Picture = homePic;
            homeItem.OriginalLocation = new Point(rightX, y);

            homes.Add(homeItem);
            this.Controls.Add(homePic);
            homeLabel.BringToFront();
        }

        // Make sure the overlay controls stay on top of the animal/home pictures
        backButton.BringToFront();
        feedbackLabel.BringToFront();
        debugLabel.BringToFront();
    }

    // Connect the TUIO event callbacks and start listening
    private void SetupTuio()
    {
        tuioHandler.OnObjectAdded   += HandleTuioAdded;
        tuioHandler.OnObjectUpdated += HandleTuioUpdated;
        tuioHandler.OnObjectRemoved += HandleTuioRemoved;
        tuioHandler.Start();
    }

    // --- TUIO Event Handlers ---

    // Called when a physical marker is placed on the surface
    private void HandleTuioAdded(int symbolId, float normX, float normY)
    {
        // Run on the UI thread because we are updating visual elements
        SafeInvoke(HandleTuioAddedOnUIThread, symbolId, normX, normY);
    }

    private void HandleTuioAddedOnUIThread(int symbolId, float normX, float normY)
    {
        debugLabel.Text = "TUIO: Added ID=" + symbolId + "  x=" + normX.ToString("F2") + " y=" + normY.ToString("F2");

        // Special case: marker #5 triggers logout
        if (symbolId == LOGOUT_MARKER_ID)
        {
            ShowFeedback("👋 Logging out...", Color.DodgerBlue);

            // Wait 1.5 seconds so the player can see the message, then close the form
            System.Windows.Forms.Timer logoutTimer = new System.Windows.Forms.Timer();
            logoutTimer.Interval = 1500;
            logoutTimer.Tick += LogoutTimer_Tick;
            logoutTimer.Start();
            return;
        }

        // Check if this marker ID belongs to any animal
        bool markerFound = animalById.TryGetValue(symbolId, out GameItem? animal);
        if (!markerFound || animal == null)
        {
            ShowFeedback("❌ Marker #" + symbolId + " is not assigned to any animal!", Color.Red);
            return;
        }

        // Ignore if the animal is already matched
        if (animal.IsMatched)
        {
            ShowFeedback(animal.Name + " is already home — no need to move it!", Color.Gold);
            return;
        }

        // Mark the animal as grabbed and move it to the marker's position
        grabbedAnimals[symbolId] = animal;
        animal.Picture.BorderStyle = BorderStyle.Fixed3D;
        MoveAnimalToMarker(animal, normX, normY);
        ShowFeedback("✅ Marker #" + symbolId + " grabbed " + animal.Name + ". Move it to its home!", Color.DarkGreen);
    }

    // Called each time a held marker moves across the surface
    private void HandleTuioUpdated(int symbolId, float normX, float normY)
    {
        SafeInvoke(HandleTuioUpdatedOnUIThread, symbolId, normX, normY);
    }

    private void HandleTuioUpdatedOnUIThread(int symbolId, float normX, float normY)
    {
        debugLabel.Text = "TUIO: Move ID=" + symbolId + "  x=" + normX.ToString("F2") + " y=" + normY.ToString("F2");

        // Only move animals that are currently grabbed
        bool isGrabbed = grabbedAnimals.TryGetValue(symbolId, out GameItem? animal);
        if (!isGrabbed || animal == null)
        {
            return;
        }

        MoveAnimalToMarker(animal, normX, normY);
    }

    // Called when a marker is lifted off the surface
    private void HandleTuioRemoved(int symbolId, float normX, float normY)
    {
        SafeInvoke(HandleTuioRemovedOnUIThread, symbolId, normX, normY);
    }

    private void HandleTuioRemovedOnUIThread(int symbolId, float normX, float normY)
    {
        debugLabel.Text = "TUIO: Removed ID=" + symbolId;

        // Check if we were tracking this marker
        bool wasGrabbed = grabbedAnimals.TryGetValue(symbolId, out GameItem? animal);
        if (!wasGrabbed || animal == null)
        {
            return;
        }

        // Remove the marker from the grabbed list
        grabbedAnimals.Remove(symbolId);

        // Try to snap the animal to its home, or send it back to the start
        TrySnapOrReturn(animal);
    }

    // Called when the logout timer fires — closes the game form
    private void LogoutTimer_Tick(object? sender, EventArgs e)
    {
        System.Windows.Forms.Timer logoutTimer = (System.Windows.Forms.Timer)sender!;
        logoutTimer.Stop();
        this.Close();
    }

    // --- Animal Movement ---

    // Move the animal picture so it is centered on the marker's screen position
    private void MoveAnimalToMarker(GameItem animal, float normX, float normY)
    {
        Point screenPt = NormToScreen(normX, normY);

        int newX = screenPt.X - animal.Picture.Width / 2;
        int newY = screenPt.Y - animal.Picture.Height / 2;

        animal.Picture.Location = new Point(newX, newY);
        animal.Picture.BringToFront();
    }

    // Convert TUIO normalized coordinates (0.0–1.0) to actual screen pixels
    private Point NormToScreen(float nx, float ny)
    {
        int screenX = (int)(nx * this.ClientSize.Width);
        int screenY = (int)(ny * this.ClientSize.Height);
        return new Point(screenX, screenY);
    }

    // --- Drop Logic ---

    // Check whether the animal is over its correct home. If yes, snap it in place.
    // If no, send it back to its starting position.
    private void TrySnapOrReturn(GameItem animal)
    {
        animal.Picture.BorderStyle = BorderStyle.FixedSingle;

        foreach (GameItem home in homes)
        {
            if (GameLogic.CheckDropMatch(animal, home))
            {
                // Center the animal picture inside the home picture
                int snapX = home.Picture.Left + (home.Picture.Width  - animal.Picture.Width)  / 2;
                int snapY = home.Picture.Top  + (home.Picture.Height - animal.Picture.Height) / 2;

                animal.Picture.Location = new Point(snapX, snapY);
                animal.IsMatched = true;

                ShowFeedback("🎉 " + animal.Name + " is home!", Color.Gold);
                CheckWinCondition();
                return;
            }
        }

        // No matching home found — send the animal back to where it started
        ReturnToOrigin(animal);
        ShowFeedback("❌ Wrong home! " + animal.Name + " returned to start.", Color.OrangeRed);
    }

    // Move the animal back to its original starting position
    private void ReturnToOrigin(GameItem animal)
    {
        animal.Picture.Location = animal.OriginalLocation;
    }

    // Check if every animal has been matched. If so, the player wins.
    private void CheckWinCondition()
    {
        bool allMatched = true;

        foreach (GameItem animal in animalById.Values)
        {
            if (!animal.IsMatched)
            {
                allMatched = false;
                break;
            }
        }

        if (allMatched)
        {
            feedbackLabel.Text = "🏆 All animals are home! You win!";
            feedbackLabel.BackColor = Color.FromArgb(200, 20, 120, 20);
            MessageBox.Show("🎉 Congratulations! All animals found their homes!", "You Win!",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // --- Mouse Drag Support (for testing without a TUIO device) ---

    // Record which animal the player clicked on, and where on it they clicked
    private void AnimalPic_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!(sender is PictureBox pic))
        {
            return;
        }

        GameItem? animal = FindAnimalByPic(pic);
        if (animal == null || animal.IsMatched)
        {
            return;
        }

        mouseDragItem = animal;
        mouseOffset = new Point(e.X, e.Y);
        animal.Picture.BringToFront();
        ShowFeedback("[Mouse] Dragging " + animal.Name + "…", Color.White);
    }

    // Move the animal picture to follow the mouse while dragging
    private void AnimalPic_MouseMove(object? sender, MouseEventArgs e)
    {
        if (mouseDragItem == null || e.Button != MouseButtons.Left)
        {
            return;
        }

        Point newLoc = mouseDragItem.Picture.Location;
        newLoc.Offset(e.X - mouseOffset.X, e.Y - mouseOffset.Y);
        mouseDragItem.Picture.Location = newLoc;
    }

    // When the mouse button is released, try to snap the animal to a home
    private void AnimalPic_MouseUp(object? sender, MouseEventArgs e)
    {
        if (mouseDragItem == null)
        {
            return;
        }

        GameItem animal = mouseDragItem;
        mouseDragItem = null;
        TrySnapOrReturn(animal);
    }

    // Search through all animals and return the one whose picture matches the given PictureBox.
    // Returns null if no match is found.
    private GameItem? FindAnimalByPic(PictureBox pic)
    {
        foreach (GameItem animal in animalById.Values)
        {
            if (animal.Picture == pic)
            {
                return animal;
            }
        }
        return null;
    }

    // --- Feedback Label ---

    // Update the feedback bar with a message and a background color based on the result type
    private void ShowFeedback(string message, Color color)
    {
        // The feedback can be called from a background thread, so we must check
        if (feedbackLabel.InvokeRequired)
        {
            feedbackLabel.Invoke(new Action(ShowFeedbackOnUIThread));
            return;
        }
        ShowFeedbackOnUIThread();

        void ShowFeedbackOnUIThread()
        {
            feedbackLabel.Text = message;
            // Darken the color so the text is readable on top of it
            feedbackLabel.BackColor = Color.FromArgb(190, color.R / 3, color.G / 3, color.B / 3);
        }
    }

    // --- Utility ---

    // SafeInvoke ensures a method runs on the UI thread.
    // TUIO events arrive on a background thread, so any UI update must be marshalled.
    private void SafeInvoke(Action<int, float, float> uiAction, int symbolId, float normX, float normY)
    {
        if (this.IsDisposed || !this.IsHandleCreated)
        {
            return;
        }

        if (this.InvokeRequired)
        {
            // We are on a background thread — schedule the action on the UI thread
            this.Invoke(new Action(RunOnUIThread));
        }
        else
        {
            uiAction(symbolId, normX, normY);
        }

        void RunOnUIThread()
        {
            uiAction(symbolId, normX, normY);
        }
    }

    // Build the full file path for an asset (image) file
    private static string GetAssetPath(string filename)
    {
        // First try the release output folder
        string path = Path.Combine(Application.StartupPath, "Assets", filename);
        if (File.Exists(path))
        {
            return path;
        }

        // Fall back to the project source folder (used when running from Visual Studio)
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Assets", filename);
    }

    // --- Event Handlers for Form Lifecycle ---

    // Close button on the form header
    private void BackButton_Click(object? sender, EventArgs e)
    {
        this.Close();
    }

    // Called when this form is closed — stop TUIO and show the login screen again
    private void GamePlayForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        tuioHandler.Stop();
        tuioHandler.Dispose();

        if (parentScanner != null && !parentScanner.IsDisposed)
        {
            parentScanner.ResetScanner();
            parentScanner.Show();
        }
    }

    // --- Boilerplate (required by the Windows Forms designer) ---

    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (components != null)
            {
                components.Dispose();
            }

            if (tuioHandler != null)
            {
                tuioHandler.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        this.ClientSize = new Size(1024, 768);
        this.Name = "GamePlayForm";
        this.ResumeLayout(false);
    }
}
