using System;
using System.Drawing;
using System.Windows.Forms;

namespace AnimalHomeGame_CSharp;

// GameItem represents one object in the game — either an animal or a home.
public class GameItem
{
    // Display name of the item, e.g. "Bird" or "Nest"
    public string Name { get; set; } = "";

    // The TUIO marker ID that controls this animal (-1 means not assigned)
    public int TuioId { get; set; } = -1;

    // The picture that appears on screen for this item
    public PictureBox Picture { get; set; } = null!;

    // Where the animal starts on screen (used to snap it back if dropped wrong)
    public Point OriginalLocation { get; set; }

    // The name of the home this animal belongs to, e.g. "Nest"
    public string TargetHomeName { get; set; } = "";

    // True once the animal has been successfully dropped onto its correct home
    public bool IsMatched { get; set; } = false;
}

// GameLogic contains helper methods that check game rules.
public static class GameLogic
{
    // Returns true if the given TUIO marker ID matches the animal's assigned marker
    public static bool ValidateTuioId(GameItem animal, int symbolId)
    {
        return animal.TuioId == symbolId;
    }

    // Returns true if the animal was dropped near its correct home
    public static bool CheckDropMatch(GameItem animal, GameItem home)
    {
        // First check: is this the right home by name?
        if (animal.TargetHomeName != home.Name)
        {
            return false;
        }

        // Second check: are the two pictures overlapping on screen?
        return animal.Picture.Bounds.IntersectsWith(home.Picture.Bounds);
    }
}
