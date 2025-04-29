using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Explicitly using UI Text to avoid ambiguity
using SteamGameServerMod.Logging;
using SteamGameServerMod.Client.Models;
using SteamGameServerMod.Settings;

namespace SteamGameServerMod.Client.UI
{
    public class ServerBrowserUI
    {
        private AssetBundle _assetBundle;
        private GameObject _menuObject;
        private CanvasGroup _canvasGroup;
        private GameObject _directConnectPage;
        private GameObject _serverListPage;
        private GameObject _recentServersPage;
        private GameObject _settingsPage;
        private GameObject _connectUI;
        private GameObject _serverListParent;
        private GameObject _serverListItemPrefab;
        private GameObject _noServerText;
        private GameObject _recentServersListParent;
        private GameObject _noHistoryText;
        private InputField _serversSearchbar;
        private List<ServerData> _lastServerRefresh = new List<ServerData>();
        private bool _showSelfHosted = true;

        private Dictionary<string, Button> _positionButtons = new Dictionary<string, Button>();
        private bool _isVisible = false;

        public IEnumerator Initialize()
        {
            Log.LogInfo("Initializing Server Browser UI...");

            // Load the asset bundle - outside of try/catch so we don't have yield in try block
            TextAsset bundleAsset = Resources.Load<TextAsset>("dedicated_server");
            if (bundleAsset == null)
            {
                // Try to load from embedded resource
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("SteamGameServerMod.Assets.dedicated_server.assets"))
                {
                    if (stream != null)
                    {
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);
                        _assetBundle = AssetBundle.LoadFromMemory(data);
                    }
                }
            }
            else
            {
                _assetBundle = AssetBundle.LoadFromMemory(bundleAsset.bytes);
            }

            if (_assetBundle == null)
            {
                Log.LogError("Failed to load asset bundle for server browser");
                yield break;
            }

            try
            {
                // Create the UI
                CreateUI();
                Log.LogInfo("Server Browser UI initialized");
            }
            catch (Exception ex)
            {
                Log.LogError($"Error initializing Server Browser UI: {ex}");
            }

            yield return null;
        }

        private void CreateUI()
        {
            // Instantiate the main menu prefab
            GameObject prefab = _assetBundle.LoadAsset<GameObject>("ServerBrowserMenu");
            _menuObject = GameObject.Instantiate(prefab);
            GameObject.DontDestroyOnLoad(_menuObject);
            _menuObject.name = "ServerBrowserMenu";

            // Setup canvas group for fading
            _canvasGroup = _menuObject.GetComponent<CanvasGroup>();
            HideUI(_canvasGroup, true);

            // Find UI elements
            _directConnectPage = FindGOByName(_menuObject, "DirectConnectPage");
            _serverListPage = FindGOByName(_menuObject, "ServerListPage");
            _recentServersPage = FindGOByName(_menuObject, "RecentServersPage");
            _settingsPage = FindGOByName(_menuObject, "SettingsPage");
            _connectUI = FindGOByName(_menuObject, "ServerConnectInfo");

            // Server list elements
            _serverListParent = FindGOByName(_serverListPage, "Content");
            _serverListItemPrefab = _assetBundle.LoadAsset<GameObject>("ServerEntry");
            _noServerText = FindGOByName(_serverListPage, "NoServersText");

            // Recent servers elements
            _recentServersListParent = FindGOByName(_recentServersPage, "Content");
            _noHistoryText = FindGOByName(_recentServersPage, "NoServersText");

            // Search elements
            _serversSearchbar = FindGOByName(_serverListPage, "Searchbar").GetComponent<InputField>();
            _serversSearchbar.onValueChanged.AddListener((text) => UpdateServerSearch());

            // Setup tabs
            SetupTabs();

            // Setup direct connect
            SetupDirectConnect();

            // Hide UI by default
            HideUI(_canvasGroup, true);
            HideUI(_connectUI.GetComponent<CanvasGroup>(), true);

            // Set default tab to server list
            SetPageEnabled(_serverListPage);

            // Initial server refresh
            RefreshServerList();
        }

        private void SetupTabs()
        {
            // Find tab buttons
            GameObject tabDirectConnect = FindGOByName(_menuObject, "TabDirectConnect");
            GameObject tabServerList = FindGOByName(_menuObject, "TabServerList");
            GameObject tabRecentServers = FindGOByName(_menuObject, "TabRecentServers");
            GameObject tabSettings = FindGOByName(_menuObject, "TabSettings");

            // Add listeners
            tabDirectConnect.GetComponent<Button>().onClick.AddListener(() => {
                SetPageEnabled(_directConnectPage);
                UpdateSelectedTab(tabDirectConnect);
            });

            tabServerList.GetComponent<Button>().onClick.AddListener(() => {
                SetPageEnabled(_serverListPage);
                UpdateSelectedTab(tabServerList);
            });

            tabRecentServers.GetComponent<Button>().onClick.AddListener(() => {
                SetPageEnabled(_recentServersPage);
                UpdateSelectedTab(tabRecentServers);
                RefreshHistoryList();
            });

            tabSettings.GetComponent<Button>().onClick.AddListener(() => {
                SetPageEnabled(_settingsPage);
                UpdateSelectedTab(tabSettings);
            });

            // Default to server list tab
            UpdateSelectedTab(tabServerList);
        }

        private void SetupDirectConnect()
        {
            InputField ipField = FindGOByName(_directConnectPage, "IPField").GetComponent<InputField>();

            // Load saved IP if available
            if (PlayerPrefs.HasKey("S1_DirectConnectIP"))
            {
                ipField.text = PlayerPrefs.GetString("S1_DirectConnectIP");
            }

            // Add listeners
            ipField.onSubmit.AddListener((ip) => {
                PlayerPrefs.SetString("S1_DirectConnectIP", ip);
                JoinServer(ip);
            });

            Button connectButton = FindGOByName(_directConnectPage, "ConnectButton").GetComponent<Button>();
            connectButton.onClick.AddListener(() => {
                PlayerPrefs.SetString("S1_DirectConnectIP", ipField.text);
                JoinServer(ipField.text);
            });
        }

        public void ToggleUI()
        {
            if (_isVisible)
            {
                HideUI(_canvasGroup, false);
                _isVisible = false;
            }
            else
            {
                ShowUI(_canvasGroup, false);
                _isVisible = true;
            }
        }

        public void Update()
        {
            // Handle UI updates and input
        }

        private void SetPageEnabled(GameObject page)
        {
            // Hide all pages first
            HideUI(_directConnectPage.GetComponent<CanvasGroup>(), false);
            HideUI(_serverListPage.GetComponent<CanvasGroup>(), false);
            HideUI(_recentServersPage.GetComponent<CanvasGroup>(), false);
            HideUI(_settingsPage.GetComponent<CanvasGroup>(), false);

            // Show the requested page
            ShowUI(page.GetComponent<CanvasGroup>(), false);
        }

        private void UpdateSelectedTab(GameObject selectedTab)
        {
            // Update tab visual states
            foreach (Transform child in selectedTab.transform.parent)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = child.gameObject != selectedTab;
                }
            }
        }

        private void RefreshServerList()
        {
            // Show loading message
            SetNoServerText("Retrieving servers...");

            // Clear current servers
            foreach (Transform child in _serverListParent.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            // Start server fetch
            ServerFetcher fetcher = new ServerFetcher(ServerSettings.ServerListUrl);
            fetcher.GetServers().ContinueWith(task => {
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    if (task.IsFaulted)
                    {
                        SetNoServerText("Error retrieving servers. Please try again.");
                        Log.LogError($"Error fetching servers: {task.Exception}");
                        return;
                    }

                    var servers = task.Result;
                    _lastServerRefresh = servers;

                    if (servers.Count == 0)
                    {
                        SetNoServerText("No servers found.");
                        return;
                    }

                    // Create server entries
                    foreach (var server in servers)
                    {
                        CreateServerEntry(server);
                    }

                    SetNoServerText(null);
                    UpdateServerSearch();
                });
            });
        }

        private void RefreshHistoryList()
        {
            // Implement recent server history functionality
        }

        private void CreateServerEntry(ServerData server)
        {
            GameObject serverEntry = GameObject.Instantiate(_serverListItemPrefab, _serverListParent.transform);
            serverEntry.name = server.servername + (server.steam ? " (Steam)" : "");

            // Set server info
            serverEntry.transform.Find("ServerName").GetComponent<Text>().text = server.servername;
            serverEntry.transform.Find("Players").GetComponent<Text>().text = $"{server.players}/{server.maxplayers}";
            serverEntry.transform.Find("Days").GetComponent<Text>().text = server.days.ToString();
            serverEntry.transform.Find("IngameTime").GetComponent<Text>().text = server.FormattedTime;
            serverEntry.transform.Find("Whitelisted").GetComponent<Text>().text = server.whitelisted ? "<color=#ba3636>Yes</color>" : "No";

            // Handle partner text if available
            Transform partnerText = serverEntry.transform.Find("PartnerText");
            if (partnerText != null)
            {
                if (string.IsNullOrWhiteSpace(server.partnertext))
                {
                    partnerText.gameObject.SetActive(false);
                }
                else
                {
                    partnerText.GetComponent<Text>().text = server.partnertext;
                }
            }

            // Add connect button listener
            Button connectButton = serverEntry.transform.Find("Connect").GetComponent<Button>();
            connectButton.onClick.AddListener(() => {
                JoinServer($"{server.ip}:{server.port}", server.servername);
            });
        }

        private void UpdateServerSearch()
        {
            string searchText = _serversSearchbar.text.ToLower();
            int visibleCount = 0;

            foreach (Transform child in _serverListParent.transform)
            {
                bool isSteamServer = child.name.ToLower().EndsWith("(steam)");
                bool matchesSearch = child.name.ToLower().Contains(searchText);

                if (isSteamServer && !_showSelfHosted)
                {
                    child.gameObject.SetActive(false);
                }
                else if (matchesSearch)
                {
                    child.gameObject.SetActive(true);
                    visibleCount++;
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }

            if (visibleCount == 0)
            {
                SetNoServerText("No servers found matching your search.");
            }
            else
            {
                SetNoServerText(null);
            }
        }

        private void SetNoServerText(string text)
        {
            if (_noServerText != null)
            {
                Text textComponent = _noServerText.GetComponent<Text>();
                if (textComponent != null)
                {
                    textComponent.text = text;
                    _noServerText.SetActive(!string.IsNullOrEmpty(text));
                }
            }
        }

        private void JoinServer(string serverAddress, string serverName = null)
        {
            // Extract IP and port
            string[] parts = serverAddress.Split(':');
            string ip = parts[0];
            int port = parts.Length > 1 ? int.Parse(parts[1]) : 27015;

            // Show connecting UI
            ShowConnectUI($"Connecting to {serverName ?? serverAddress}...");

            // Initialize connection
            ConnectionManager.ConnectToServer(ip, port, success => {
                if (success)
                {
                    HideUI(_connectUI.GetComponent<CanvasGroup>(), false);
                    // Hide the server browser
                    HideUI(_canvasGroup, false);
                    _isVisible = false;
                }
                else
                {
                    ShowConnectUI($"Failed to connect to {serverName ?? serverAddress}. Server may be offline or unreachable.");
                }
            });
        }

        private void ShowConnectUI(string message)
        {
            ShowUI(_connectUI.GetComponent<CanvasGroup>(), false);
            Text messageText = _connectUI.GetComponentInChildren<Text>();
            if (messageText != null)
            {
                messageText.text = message;
            }

            // Add close button listener
            Button closeButton = FindGOByName(_connectUI, "CloseButton").GetComponent<Button>();
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                HideUI(_connectUI.GetComponent<CanvasGroup>(), false);
                ConnectionManager.CancelConnection();
            });
        }

        public void Cleanup()
        {
            if (_menuObject != null)
            {
                GameObject.Destroy(_menuObject);
                _menuObject = null;
            }

            if (_assetBundle != null)
            {
                _assetBundle.Unload(true);
                _assetBundle = null;
            }
        }

        // Helper methods
        private void ShowUI(CanvasGroup canvasGroup, bool instant = false)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = instant ? 1f : 0f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;

                if (!instant)
                {
                    // Animate fade in
                    LeanTween.value(canvasGroup.gameObject, canvasGroup.alpha, 1f, 0.25f)
                        .setOnUpdate((float val) => { canvasGroup.alpha = val; });
                }
            }
        }

        private void HideUI(CanvasGroup canvasGroup, bool instant = false)
        {
            if (canvasGroup != null)
            {
                if (instant)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                }
                else
                {
                    // Animate fade out
                    LeanTween.value(canvasGroup.gameObject, canvasGroup.alpha, 0f, 0.25f)
                        .setOnUpdate((float val) => { canvasGroup.alpha = val; })
                        .setOnComplete(() => {
                            canvasGroup.blocksRaycasts = false;
                            canvasGroup.interactable = false;
                        });
                }
            }
        }

        private GameObject FindGOByName(GameObject parent, string name)
        {
            Transform targetTransform = FindTransformByName(parent.transform, name);
            return targetTransform?.gameObject;
        }

        private Transform FindTransformByName(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                Transform found = FindTransformByName(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}