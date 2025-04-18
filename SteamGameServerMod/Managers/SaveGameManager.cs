using System.IO.Compression;
using System.Reflection;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using SteamGameServerMod.Logging;
using Steamworks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SteamGameServerMod.Managers;

public static class SaveGameManager
{
    /// <summary>
    /// Initiates a new <see cref="SaveInfo"/> instance.
    /// </summary>
    /// <param name="saveName"></param>
    /// <param name="gameVersion"></param>
    public static void CreateNewSave(string saveName, string gameVersion)
    {
        var savePath = Path.Combine(SaveManager.Instance.IndividualSavesContainerPath, saveName);
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        CopyDefaultSaveToFolder(savePath);

        var gameJsonPath = Path.Combine(savePath, "Game.json");
        File.WriteAllText(gameJsonPath, new GameData
        {
            Seed                = Random.Range(0, int.MaxValue),
            OrganisationName    = saveName,                //< @TODO: Will be multiple organizations (Create custom save manager for this)
            Settings            = new(),
            GameVersion         = gameVersion
        }.GetJson());

        var metadataJsonPath = Path.Combine(savePath, "Metadata.json");
        var metadata = new MetaData(new(DateTime.Now), new(DateTime.Now), Application.version, Application.version, false);
        File.WriteAllText(metadataJsonPath, metadata.GetJson());

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SteamGameServerMod.Assets.Player_0.zip");
        if (stream == null)
        {
            Log.LogError("Failed to load stream for Player_0.zip for new save");
            return;
        }

        var zipArchive = new ZipArchive(stream);
        zipArchive.ExtractToDirectory(Path.Combine(savePath, "Players"));

        // Override the PlayerCode
        var playerJsonPath = Path.Combine(savePath, "Players", "Player_0", "Player.json");
        var playerData = ReadJsonData<PlayerData>(playerJsonPath);
        playerData.PlayerCode = SteamUser.GetSteamID().ToString();
        File.WriteAllText(playerJsonPath, playerData.GetJson());
    }

    /// <summary>
    /// Reads a <see cref="SaveInfo"/> instance from the folder :) (Tyler PLEASE IMPROVE THIS CODE OR I WILL SCREAM FOR CRYING OUT LOUD, THANK YOU <3)
    /// </summary>
    /// <param name="saveName"></param>
    /// <returns></returns>
    public static SaveInfo GetSave(string saveName)
    {
        var savePath = Path.Combine(SaveManager.Instance.IndividualSavesContainerPath, saveName);
        if (!Directory.Exists(savePath))
            CreateNewSave(saveName, Application.version);

        var metaData = ReadJsonData<MetaData>(Path.Combine(savePath, "MetaData.json")) ?? new(new(DateTime.Now), new(DateTime.Now), Application.version, Application.version, false);
        var gameData = ReadJsonData<GameData>(Path.Combine(savePath, "Game.json")) ?? new()
        {
            Seed                = Random.Range(0, int.MaxValue),
            OrganisationName    = saveName,                //< @TODO: Will be multiple organizations (Create custom save manager for this)
            Settings            = new(),
            GameVersion         = Application.version
        };
        var moneyData = ReadJsonData<MoneyData>(Path.Combine(savePath, "Money.json")) ?? new(100.0f, 0.0f, 0.0f, 10000f);
        return new(savePath, 0, gameData.OrganisationName, metaData.CreationDate.GetDateTime(), metaData.LastPlayedDate.GetDateTime(), moneyData.Networth, metaData.LastSaveVersion, metaData);
    }

    static void CopyDefaultSaveToFolder(string folderPath)
    {
        CopyDirectory(Path.Combine(Application.streamingAssetsPath, "DefaultSave"), folderPath, true);
    }

    static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory and check if it exists
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        var dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (!recursive)
            return;

        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir, true);
        }
    }

    static T ReadJsonData<T>(string jsonFilePath) where T : class
    {
        if (!File.Exists(jsonFilePath))
            return null;

        var fileText = File.ReadAllText(jsonFilePath);
        return string.IsNullOrEmpty(fileText) ? null : JsonUtility.FromJson<T>(fileText);
    }
}
