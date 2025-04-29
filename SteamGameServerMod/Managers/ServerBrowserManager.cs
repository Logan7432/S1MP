using System;
using System.Collections;
using UnityEngine;
using SteamGameServerMod.Logging;
using SteamGameServerMod.Client.UI;

namespace SteamGameServerMod.Client
{
    public class ClientManager
    {
        private ServerBrowserUI _serverBrowserUI;
        private bool _initialized = false;

        public IEnumerator Initialize()
        {
            Log.LogInfo("Initializing Client Manager...");

            // Wait for a scene to be loaded
            yield return new WaitForSeconds(1.0f);

            // Create UI outside of try/catch to avoid yield in try/catch
            _serverBrowserUI = new ServerBrowserUI();
            yield return _serverBrowserUI.Initialize();

            try
            {
                // Register keybinds, hooks, etc.
                RegisterKeybinds();

                _initialized = true;
                Log.LogInfo("Client Manager initialized successfully");
            }
            catch (Exception ex)
            {
                Log.LogError($"Error initializing Client Manager: {ex}");
            }
        }

        public void Update()
        {
            if (!_initialized)
                return;

            _serverBrowserUI?.Update();

            // Check for server browser toggle key press
            if (Input.GetKeyDown(KeyCode.F8))
            {
                ToggleServerBrowser();
            }
        }

        private void RegisterKeybinds()
        {
            // Register any global keybinds needed
            Log.LogInfo("Registered client keybinds");
        }

        private void ToggleServerBrowser()
        {
            if (_serverBrowserUI != null)
            {
                _serverBrowserUI.ToggleUI();
            }
        }

        public void Shutdown()
        {
            _serverBrowserUI?.Cleanup();
            Log.LogInfo("Client Manager shut down");
        }
    }
}