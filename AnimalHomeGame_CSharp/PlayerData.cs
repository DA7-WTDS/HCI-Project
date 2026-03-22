using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AnimalHomeGame_CSharp;

// Stores the information for one registered player.
// Each player is identified by their Bluetooth device.
public class UserProfile
{
    // The player's display name (taken from the Bluetooth device name)
    public string PlayerName { get; set; } = string.Empty;

    // The unique Bluetooth device ID used to recognize the player
    public string BluetoothDeviceId { get; set; } = string.Empty;

    // Either "Admin" (first registered player) or "User" (everyone else)
    public string Role { get; set; } = string.Empty;
}

// ProfileManager handles reading and writing the player list from/to disk.
// Player data is stored as JSON in a file called "users.json".
public static class ProfileManager
{
    // Path to the file that stores all player profiles
    private const string FilePath = "users.json";

    // Load all player profiles from disk.
    // Returns an empty list if the file does not exist or cannot be read.
    public static List<UserProfile> LoadProfiles()
    {
        // If the file doesn't exist yet, return an empty list
        if (!File.Exists(FilePath))
        {
            return new List<UserProfile>();
        }

        try
        {
            string json = File.ReadAllText(FilePath);

            // Parse the JSON into a list of UserProfile objects
            List<UserProfile>? result = JsonSerializer.Deserialize<List<UserProfile>>(json);

            // If parsing returned null for any reason, return an empty list
            if (result == null)
            {
                return new List<UserProfile>();
            }

            return result;
        }
        catch
        {
            // If anything goes wrong (e.g. corrupted file), return an empty list
            return new List<UserProfile>();
        }
    }

    // Save all player profiles to disk, overwriting the previous file.
    public static void SaveProfiles(List<UserProfile> profiles)
    {
        // Serialize to JSON with indentation so the file is human-readable
        JsonSerializerOptions options = new JsonSerializerOptions();
        options.WriteIndented = true;

        string json = JsonSerializer.Serialize(profiles, options);
        File.WriteAllText(FilePath, json);
    }
}
