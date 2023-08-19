using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveObject
{

    [Header("Game Information")]
    public Hero hero;
    public CampaignSave campaignSave;
    public SerializableMapObject mapObject;
    public int money;
    public int xp;
    [Header("Game Background Information")]
    public bool visitedShopBefore;
    public bool visitedUpgradeBefore;
    public List<string> tutorialsPlayed;
    public List<DialogueName> mapDialoguesPlayed;
    public List<Encounter> loadedEncounters;

}

public static class SaveLoadManager
{

    private static string savePath = Application.persistentDataPath + "/SaveInfo/";

    public static void Save(SaveObject saveObject, string fileName)
    {

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            Debug.Log("Save directory not found! Creating new directory...");
        }

        File.WriteAllText(savePath + fileName, JsonUtility.ToJson(saveObject));
        Debug.Log("Slot saved into current file.");

    }

    public static void Load(string fileName)
    {

        if (!DoesSaveExist(fileName))
        {
            Debug.Log("Error: Save slot not found, but Load() function was still called in SaveLoadManager.cs!");
            return;
        }

        string savedText = File.ReadAllText(savePath + fileName);
        SaveObject so = JsonUtility.FromJson<SaveObject>(savedText);
        Debug.Log("Loading save slot (" + fileName + ").");
        GameManager.SetChosenHero(so.hero);
        GameManager.SetGameScene(so.mapObject.currScene);
        GameManager.SetMoney(so.money);
        GameManager.SetXP(so.xp);
        GameManager.SetCampaignSave(so.campaignSave);
        if (so.campaignSave != null) GameManager.SetGameScene(so.campaignSave.currScene);
        GameManager.SetMapObject(so.mapObject);
        GameManager.SetPlayedDialogues(so.mapDialoguesPlayed, so.tutorialsPlayed, so.visitedShopBefore, so.visitedUpgradeBefore);
        GameManager.SetSeenEnemies(so.loadedEncounters);
        GameManager.saveFileName = fileName;

    }

    // Returns true if a save file already exists; else false.
    public static bool DoesSaveExist(string fileName)
    {
        // If we don't have a save file, we don't have a save.
        if (!File.Exists(savePath + fileName)) { return false; }
        // Try and take some stuff from the save to see if it's valid.
        string savedText = File.ReadAllText(savePath + fileName);
        SaveObject so = JsonUtility.FromJson<SaveObject>(savedText);
        if (so.hero.currentRelics.Count > 0 && so.hero.currentRelics[0] == null)
        {
            return false;
        }
        // If the checks pass, return true!
        return true;
    }

}
