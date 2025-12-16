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
///     JSON-serializable data which is retrieved on initialization and stored in a dedicated file on shutdown.
/// </summary>
[DataContract]
public class ModPersistentSaveData
{
    private static readonly string PathToDataFolder = Path.Combine(Application.persistentDataPath, "ModData");

    internal static readonly List<ModPersistentSaveData> RegisteredInstances = [];

    [DataMember]
    private Dictionary<string, object> Data = [];

    private readonly string modID;
    private readonly bool isGlobal;

    /// <summary>
    ///     Creates a new persistent data for this mod.
    /// </summary>
    /// <param name="isGlobal">If true, all save slots will share the same data file. Otherwise, each slot has a separate file in its own folder.</param>
    /// <param name="manualSave">If true, ModLib will not automatically save the stored data. Otherwise, the data is stored in a JSON file upon closing the game.</param>
    public ModPersistentSaveData(bool isGlobal = false, bool manualSave = false)
    {
        modID = Registry.GetMod(Assembly.GetCallingAssembly()).Plugin.GUID;
        this.isGlobal = isGlobal;

        LoadFromFile();

        if (!manualSave)
            RegisteredInstances.Add(this);
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
                Core.Logger.LogError($"Failed to retrieve data for {modID}! {ex}");
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
    public void SetData(string key, object data) => Data[key] = data;

    internal void SaveToFile()
    {
        try
        {
            string pathToFile = GetPathToFile();

            DataContractJsonSerializer serializer = new(typeof(ModPersistentSaveData), Data.Values.Select(static o => o.GetType()));

            using MemoryStream ms = new();

            serializer.WriteObject(ms, this);

            string data = Encoding.UTF8.GetString(ms.ToArray());

            if (!string.IsNullOrWhiteSpace(data))
            {
                if (!isGlobal)
                    Directory.CreateDirectory(Path.GetDirectoryName(pathToFile));

                File.WriteAllText(pathToFile, data);

                Core.Logger.LogDebug($"Saving data from {modID} to {pathToFile.Replace(Application.persistentDataPath, "")}");
            }
            else
            {
                Core.Logger.LogDebug($"Data is empty; Not saving to file. ({modID})");
            }
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"Failed to save persistent data for {modID}! {ex}");
        }
    }

    internal void LoadFromFile()
    {
        try
        {
            string pathToFile = GetPathToFile();

            if (!File.Exists(pathToFile)) return;

            string data = File.ReadAllText(pathToFile);

            if (string.IsNullOrWhiteSpace(data)) return;

            DataContractJsonSerializer serializer = new(typeof(ModPersistentSaveData));

            using MemoryStream ms = new(Encoding.UTF8.GetBytes(data));

            Data = ((ModPersistentSaveData)serializer.ReadObject(ms)).Data;

            Core.Logger.LogDebug($"Retrieved data from {modID}!");
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"Failed to load persistent data for {modID}! {ex}");
        }
    }

    private string GetPathToFile() =>
        isGlobal
            ? Path.Combine(PathToDataFolder, Registry.SanitizeModName(modID) + ".json")
            : Path.Combine(PathToDataFolder, Custom.rainWorld?.options?.saveSlot.ToString() ?? "0", Registry.SanitizeModName(modID) + ".json");
}