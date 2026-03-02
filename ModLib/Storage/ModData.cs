using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using RWCustom;
using UnityEngine;

namespace ModLib.Storage;

/// <summary>
///     Mod-unique persistent data which is automatically retrieved and saved to disk by ModLib.
/// </summary>
[DataContract]
public class ModData
{
    internal static readonly List<ModData> StoredInstances = [];

    /// <summary>
    ///     The path to the folder where data files are stored. This property is read-only.
    /// </summary>
    protected static string PathToDataFolder
    {
        get
        {
            field ??= Path.Combine(Application.persistentDataPath, "ModData");

            return field;
        }
    }

    /// <summary>
    ///     The dictionary where all data of this instance is stored.
    /// </summary>
    [DataMember]
    protected internal Dictionary<string, object> Data { get; set; } = [];

    /// <summary>
    ///     The identifier of the mod owning this data holder. This property is read-only.
    /// </summary>
    public string ModID { get; }

    /// <summary>
    ///     Whether or not this data holder instance is shared between all save slots of the game. This property is read-only.
    /// </summary>
    public bool IsGlobal { get; }

    /// <summary>
    ///     Whether or not this data holder instance is automatically managed by ModLib for saving and retrieving data. This property is read-only.
    /// </summary>
    public bool AutoSave { get; }

    /// <summary>
    ///     The name of the file where data will be stored. This property is read-only.
    /// </summary>
    protected string? SaveFileName { get; }

    /// <summary>
    ///     Creates a new persistent data for this mod.
    /// </summary>
    /// <param name="saveFileName">If specified, overrides the default file name for the stored data.</param>
    /// <param name="isGlobal">If true, all three save slots will share the same data file. Otherwise, each slot will have its own save file.</param>
    /// <param name="autoSave">If true, ModLib will load any previously stored data on construction, and save it to a JSON file on shutdown.</param>
    public ModData(string? saveFileName = "", bool isGlobal = false, bool autoSave = true)
    {
        ModID = Registry.GetMod(Assembly.GetCallingAssembly()).Plugin.GUID;

        IsGlobal = isGlobal;
        AutoSave = autoSave;

        if (autoSave)
        {
            SaveFileName = string.IsNullOrWhiteSpace(saveFileName)
                ? Registry.SanitizeModName(ModID)
                : saveFileName;

            LoadFromFile();
        }

        StoredInstances.Add(this);
    }

    /// <summary>
    ///     Retrieves the stored data with the given key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The data associated with the given key.</returns>
    /// <exception cref="ArgumentNullException">key is null</exception>
    /// <exception cref="KeyNotFoundException">No data was found with the given key.</exception>
    public object GetData(string key) => Data[key];

    /// <summary>
    ///     Retrieves the stored data with the given key.
    /// </summary>
    /// <typeparam name="T">The type of the data to be retrieved</typeparam>
    /// <param name="key">The key to search for.</param>
    /// <returns>The data associated with the given key.</returns>
    /// <exception cref="ArgumentNullException">key is null</exception>
    /// <exception cref="KeyNotFoundException">No data was found with the given key.</exception>
    /// <exception cref="InvalidCastException">The stored data is not of the given type.</exception>
    public T GetData<T>(string key) => (T)Data[key];

    /// <summary>
    ///     Determines if the given key has any data associated to it in the persistent data.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>
    ///     <c>true</c> if any data is bound to the given key, <c>false</c> otherwise.
    ///     This method returns <c>false</c> if key is null.
    /// </returns>
    public bool HasData(string key) => !string.IsNullOrEmpty(key) && Data.ContainsKey(key);

    /// <summary>
    ///     Attempts to safely retrieve the stored data for the given key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="data">The stored data for the given key.</param>
    /// <returns>
    ///     <c>true</c> if valid data was found for the given key, <c>false</c> otherwise.
    ///     This method returns <c>false</c> if key is null.
    /// </returns>
    public bool TryGetData(string key, out object data)
    {
        if (string.IsNullOrEmpty(key) || !Data.TryGetValue(key, out data))
        {
            data = default!;
            return false;
        }

        return data is not null;
    }

    /// <summary>
    ///     Attempts to safely retrieve the stored data for the given key.
    /// </summary>
    /// <typeparam name="T">The type of the retrieved data.</typeparam>
    /// <param name="key">The key to search for.</param>
    /// <param name="data">The stored data for the given key.</param>
    /// <returns>
    ///     <c>true</c> if valid data was found for the given key, <c>false</c> otherwise.
    ///     This method returns <c>false</c> if key is null.
    /// </returns>
    public bool TryGetData<T>(string key, out T data)
    {
        if (string.IsNullOrEmpty(key) || !Data.TryGetValue(key, out object objData))
        {
            data = default!;
            return false;
        }

        try
        {
            data = (T)objData;
            return data is not null;
        }
        catch (Exception ex)
        {
            if (ex is not InvalidCastException)
                Core.Logger.LogError($"Failed to retrieve data for {ModID}! {ex}");
        }

        data = default!;
        return false;
    }

    /// <summary>
    ///     Adds a new key-value pair to the persistent data.
    /// </summary>
    /// <param name="key">The key for the new data.</param>
    /// <param name="data">The data to be stored.</param>
    /// <exception cref="ArgumentNullException">key is null</exception>
    /// <exception cref="ArgumentException">A stored data already exists for the given key</exception>
    public void AddData(string key, object data) => Data.Add(key, data);

    /// <summary>
    ///     Clears all stored data.
    /// </summary>
    public void ClearData() => Data.Clear();

    /// <summary>
    ///     Sets the stored data of the given key to the provided value.
    /// </summary>
    /// <param name="key">The key the data will be bound to.</param>
    /// <param name="data">The data to be stored.</param>
    /// <exception cref="ArgumentNullException">key is null</exception>
    public void SetData(string key, object data) => Data[key] = data;

    /// <summary>
    ///     Saves this instance's data to its respective save file.
    /// </summary>
    protected internal virtual void SaveToFile()
    {
        try
        {
            string pathToFile = GetPathToFile();

            DataContractJsonSerializer serializer = new(typeof(ModData), Data.Values.Select(static o => o.GetType()));

            using MemoryStream ms = new();

            serializer.WriteObject(ms, this);

            string data = Encoding.UTF8.GetString(ms.ToArray());

            if (!string.IsNullOrWhiteSpace(data))
            {
                if (!IsGlobal)
                    Directory.CreateDirectory(Path.GetDirectoryName(pathToFile));

                File.WriteAllText(pathToFile, data);

                Core.Logger.LogDebug($"Saving data from {ModID} to {pathToFile.Replace(Application.persistentDataPath, "")}");
            }
            else
            {
                Core.Logger.LogDebug($"Data is empty; Not saving to file. ({ModID})");
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"Failed to save persistent data for {ModID}! {ex}");
        }
    }

    /// <summary>
    ///     Loads this mod's data from its JSON file, if any.
    /// </summary>
    protected internal virtual void LoadFromFile()
    {
        try
        {
            string pathToFile = GetPathToFile();

            if (!File.Exists(pathToFile)) return;

            string data = File.ReadAllText(pathToFile);

            if (string.IsNullOrWhiteSpace(data)) return;

            DataContractJsonSerializer serializer = new(typeof(ModData));

            using MemoryStream ms = new(Encoding.UTF8.GetBytes(data));

            Data = ((ModData)serializer.ReadObject(ms)).Data;

            Core.Logger.LogDebug($"Retrieved data from {ModID}!");
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"Failed to load persistent data for {ModID}! {ex}");
        }
    }

    /// <summary>
    ///     Retrieves the path to the file where data will be stored/retrieved.
    /// </summary>
    /// <returns>The full path to the file where data will be stored/retrieved.</returns>
    protected virtual string GetPathToFile() =>
        IsGlobal
            ? Path.Combine(PathToDataFolder, $"{SaveFileName}.json")
            : Path.Combine(PathToDataFolder, Custom.rainWorld?.options?.saveSlot.ToString() ?? "0", $"{SaveFileName}.json");
}