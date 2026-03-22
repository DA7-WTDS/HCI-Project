using System;
using TCD.System.TUIO;

namespace AnimalHomeGame_CSharp;

// This class connects to the TUIO server and listens for physical markers.
// It fires events (OnObjectAdded, OnObjectUpdated, OnObjectRemoved) so other
// parts of the game can react when a marker is placed, moved, or lifted.
public class TuioHandler : TuioListener, IDisposable
{
    // The TUIO client that reads marker data from the network
    private TuioClient? client;

    // These are callbacks (methods to call) when a marker event happens.
    // Other classes assign methods to these so they get notified.
    public Action<int, float, float>? OnObjectAdded;
    public Action<int, float, float>? OnObjectUpdated;
    public Action<int, float, float>? OnObjectRemoved;

    // Start listening for TUIO markers on the given network port (default 3333)
    public void Start(int port = 3333)
    {
        try
        {
            client = new TuioClient(port);
            client.addTuioListener(this);
            client.connect();
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(
                "Could not start TUIO listener on port " + port + ".\n" + ex.Message,
                "TUIO Error",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
        }
    }

    // Stop listening and disconnect from the TUIO server
    public void Stop()
    {
        try
        {
            if (client != null)
            {
                client.removeTuioListener(this);
                client.disconnect();
            }
        }
        catch
        {
            // Ignore any errors during disconnect
        }
        finally
        {
            client = null;
        }
    }

    // Called by the TUIO library when a new marker is placed on the surface
    public void addTuioObject(TuioObject obj)
    {
        if (OnObjectAdded != null)
        {
            OnObjectAdded(obj.getSymbolID(), obj.getX(), obj.getY());
        }
    }

    // Called by the TUIO library when an existing marker is moved
    public void updateTuioObject(TuioObject obj)
    {
        if (OnObjectUpdated != null)
        {
            OnObjectUpdated(obj.getSymbolID(), obj.getX(), obj.getY());
        }
    }

    // Called by the TUIO library when a marker is lifted off the surface
    public void removeTuioObject(TuioObject obj)
    {
        if (OnObjectRemoved != null)
        {
            OnObjectRemoved(obj.getSymbolID(), obj.getX(), obj.getY());
        }
    }

    // These cursor/touch methods are required by the TuioListener interface
    // but we do not use them in this game, so they are left empty.
    public void addTuioCursor(TuioCursor cursor) { }
    public void updateTuioCursor(TuioCursor cursor) { }
    public void removeTuioCursor(TuioCursor cursor) { }
    public void refresh(TuioTime time) { }

    // Dispose cleans up resources — it simply stops the connection
    public void Dispose()
    {
        Stop();
    }
}
