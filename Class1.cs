using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using KSP.UI.Screens;

[KSPAddon(KSPAddon.Startup.Flight, false)]
public class ControllerSupportMod : MonoBehaviour
{
    public static ControllerSupportMod Instance;  // Static reference to access the instance

    private Gamepad gamepad;

    // Settings variables
    private float vibrationStrength = 1.0f;  // Default vibration strength (1.0 is full power)
    private bool showSettingsWindow = false; // To toggle the settings window

    private const string settingsFilePath = "GameData/ControllerSupportMod/settings.cfg";

    // Constants for vibration strength range
    private const float VIBRATION_MIN = 0.0f;
    private const float VIBRATION_MAX = 2.0f;

    void Awake()
    {
        // Ensure there's only one instance of the mod
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Destroy if another instance already exists
            return;
        }

        DontDestroyOnLoad(gameObject); // Keep this object across scenes
    }

    void Start()
    {
        LoadSettings();
        Debug.Log("Throttle Vibration Mod Initialized.");
    }

    void Update()
    {
        // Check if a gamepad is connected
        gamepad = Gamepad.current;
        if (gamepad == null)
        {
            return;
        }

        // Ensure we have a valid active vessel
        if (FlightGlobals.ActiveVessel != null)
        {
            // Get the vessel's current G-force (geeforce)
            float geeforce = (float)FlightGlobals.ActiveVessel.geeForce;

            // Scale the vibration intensity based on geeforce and vibration strength setting
            float vibrationIntensity = Mathf.Clamp01(geeforce * vibrationStrength);

            // Apply vibration to the controller
            gamepad.SetMotorSpeeds(vibrationIntensity, vibrationIntensity);
        }
    }

    void OnDestroy()
    {
        // Stop vibration when the mod is destroyed
        if (gamepad != null)
        {
            gamepad.SetMotorSpeeds(0, 0);
        }
    }

    // Load settings from the config file
    void LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            ConfigNode config = ConfigNode.Load(settingsFilePath);
            if (config.HasValue("vibrationStrength"))
            {
                vibrationStrength = float.Parse(config.GetValue("vibrationStrength"));
            }
        }
    }

    // Save the settings to a config file
    void SaveSettings()
    {
        ConfigNode config = new ConfigNode();
        config.AddValue("vibrationStrength", vibrationStrength);
        config.Save(settingsFilePath);
        Debug.Log("Settings saved.");
    }

    // Create a settings window for vibration intensity
    void OnGUI()
    {
        if (showSettingsWindow)
        {
            // Create the window with a slider
            GUILayout.Window(123456, new Rect(100, 100, 300, 150), SettingsWindow, "Throttle Vibration Settings");
        }
    }

    // The content of the settings window
    void SettingsWindow(int windowID)
    {
        GUILayout.Label("Adjust Controller Vibration Power");

        // Slider for vibration strength (0.0 to 2.0)
        vibrationStrength = GUILayout.HorizontalSlider(vibrationStrength, VIBRATION_MIN, VIBRATION_MAX);
        GUILayout.Label("Vibration Strength: " + vibrationStrength.ToString("F2"));

        // Button to close the settings window
        if (GUILayout.Button("Close"))
        {
            showSettingsWindow = false;
        }

        // Save settings button
        if (GUILayout.Button("Save Settings"))
        {
            SaveSettings();
        }

        // Make the window draggable
        GUI.DragWindow();
    }

    // Create a button on the mod panel in the main menu
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    private class ModButtonCreator : MonoBehaviour
    {
        private ApplicationLauncherButton settingsButton;

        void Start()
        {
            // Register for GameEvents to ensure ApplicationLauncher is ready
            if (ApplicationLauncher.Ready)
            {
                CreateSettingsButton();
            }
            else
            {
                GameEvents.onGUIApplicationLauncherReady.Add(OnApplicationLauncherReady);
            }
        }

        // Called when the Application Launcher is ready
        private void OnApplicationLauncherReady()
        {
            // Remove the event listener to avoid being called multiple times
            GameEvents.onGUIApplicationLauncherReady.Remove(OnApplicationLauncherReady);

            // Now that the launcher is ready, create the settings button
            CreateSettingsButton();
        }

        // Create the mod panel button
        private void CreateSettingsButton()
        {
            // Ensure the settings button is not created more than once
            if (settingsButton == null)
            {
                settingsButton = ApplicationLauncher.Instance.AddModApplication(
                    onTrue: () => ToggleSettingsWindow(), // When button is clicked, toggle window visibility
                    onHover: null,
                    onHoverOut: null,
                    onFalse: null, // No action when button is deactivated
                    onEnable: OnButtonEnable, // Action when the button is enabled
                    onDisable: OnButtonDisable, // Action when the button is disabled
                    visibleInScenes: ApplicationLauncher.AppScenes.ALWAYS,
                    texture: new Texture2D(32, 32) // Icon (you can use a custom icon here)
                );
            }
        }

        // Show or hide the settings window when the button is clicked
        private void ToggleSettingsWindow()
        {
            ControllerSupportMod.Instance.showSettingsWindow = !ControllerSupportMod.Instance.showSettingsWindow;
        }

        // Callback when the button is enabled
        private void OnButtonEnable()
        {
            Debug.Log("Throttle Vibration Mod Button Enabled");
        }

        // Callback when the button is disabled
        private void OnButtonDisable()
        {
            Debug.Log("Throttle Vibration Mod Button Disabled");
        }

        // Cleanup when the button is removed
        void OnDestroy()
        {
            if (settingsButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(settingsButton);
            }
        }
    }
}
