using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

/* 
A class to make a save file system! The whole class will be binary serialized by Google Protobuf + additional encryption and compression

How to create your own data for your game is to write your own .proto file.

This comes with a base .proto file too. Generated class will be "partial", so it will merge nicely with your game's. (As long as you also name the class PlayerData)

Basics are PlayerData.Local.Save and PlayerData.Load
Local means it will load the file in your device.

It's a partial not inheritance, because I want the custom data you want
to include serialized together with basic data in this class by protobuf.
You can edit them if they are useless to you.

Include your game-specific things in the other side of the partial.

You should move commented varialbes to your partial then define those.

Note that if a hacker gain access of your key and iv they probably can
hack your save file. And as you see below, they are plain text in your code.
(hackable)

So change it to something else like server query if you care about security.

*/

public partial class PlayerData {

    //implement this on your partial class

    //public static readonly string playerDataFileName  = "SaveFile.sav";
    //private static readonly byte[] key = Encoding.ASCII.GetBytes("EightChr");
    //private static readonly byte[] iv = Encoding.ASCII.GetBytes("EightChr");

    //implement this on your partial class

    //private readonly ulong shortenAlgorithmX;
    //private readonly ulong shortenAlgorithmM;

    public static readonly int MaxDisplayNameLength = 10; 
    public static readonly string backupSuffix = ".backup";
    private static PlayerData local;

    //Something you might want to know

    //it fetches only once and cache it. Even if you modify your save and save it, this local is the one that you fetches earilier.

    //But of course if you have modify your local before saving, your local must also be current. So there's no need to call the expensive .Load everytime you want your data.

    //There is a LocalReload in case if you want to revert to the binary file, or just have replaced the save via backup.
    public static PlayerData Local
    {
        get
        {
            if(local == null)
            {
                //Load from binary
                local = PlayerData.Load();
            }
            return local;
        }
    }

// These guys are in protobuf
/* 
    private string displayName;
    private string playerId; //GUID string
    private int playerIdHash;
    private string shortPlayerId; //Short form of that GUID string
    private string email;
*/


/// <summary>
/// Converts GUID to more readable name using a simple algorithm.
/// Modify it depending on your game's format.
/// Such case is unlikely, but the real GUID should be kept just for that.
/// </summary>
/// <returns></returns>
    public string FormattedShortPlayerId
    {
        get 
        { 
            if(PlayerId == null)
            {
                return "???-???-???";
            }
            else
            {
                return
                ShortPlayerId.Substring(0,3) +
                "-" +
                ShortPlayerId.Substring(3,3) +
                "-" +
                ShortPlayerId.Substring(6,3)
                ; 
            }
        }
    }

    public string DisplayNameString
    {
        get 
        { 
            if(DisplayName == null || DisplayName == "")
            {
                return "???";
            }
            else
            {
                return DisplayName; 
            }
        }
        set
        {
            if(value.Length > 0 && value.Length <= MaxDisplayNameLength)
            {
                DisplayName = value;
                Save();
            }
        }
    }

    public bool IsInitialized()
    {
        if( DisplayName == null || PlayerId == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public bool IsAttachedOnline()
    {
        if(Email == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void ConnectOnline(string email)
    {
        this.Email = email;
    }

/// <summary>
/// Does not destroy your save file
/// </summary>
    public void Initialize()
    {
        this.DisplayName = "Player" + UnityEngine.Random.Range(0,9999).ToString("0000");
        bool isShortUserIdGood = false;
        while(isShortUserIdGood == false)
        {
            //GUID based user ID generation
            Guid guid = Guid.NewGuid();
            this.PlayerId = guid.ToString();
            this.PlayerIdHash = guid.GetHashCode();
            this.ShortPlayerId = PlayerDataUtility.ShortenGUID(guid,shortenAlgorithmX,shortenAlgorithmM);
            isShortUserIdGood = PlayerDataUtility.IsShortUserIdGood(FormattedShortPlayerId);
        }
        Initialize2();
        Debug.Log("Initialized with Name : " + DisplayName + " ID : " + PlayerId + " SID : " + FormattedShortPlayerId);
        Save();
    }

//Move this to your partial and write something
/* 
    private void Initialize2()
    {

    }
*/

    public void Save()
    {
        SaveAs(playerDataFileName);
    }

    public void Backup()
    {
        SaveAs(playerDataFileName + backupSuffix);
    }

    public void RestoreBackup()
    {
        PlayerDataUtility.ApplySaveFile(Application.persistentDataPath, playerDataFileName + backupSuffix);
    }

    private void SaveAs(string name)
    {
        //Debug.Log("Saved : " + Application.persistentDataPath);
        FileStream file = File.Create(Application.persistentDataPath + "/" + name);
        DESCryptoServiceProvider des = new DESCryptoServiceProvider();
        using (var cryptoStream = new CryptoStream(file, des.CreateEncryptor(key, iv), CryptoStreamMode.Write))
        {
            using (Google.Protobuf.CodedOutputStream cos = new Google.Protobuf.CodedOutputStream(cryptoStream))
            {
                local.WriteTo(cos);
            }
        }
        file.Close();
    }

    public static void LocalReload()
    {
        local = Load();
    }

    private static PlayerData Load()
    {
        //Debug.Log("Loaded : " + Application.persistentDataPath);
        if (File.Exists(Application.persistentDataPath + "/" + playerDataFileName))
        {
            FileStream file = File.Open(Application.persistentDataPath + "/" + playerDataFileName, FileMode.Open);
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            PlayerData loadedData = new PlayerData();
            using (var cryptoStream = new CryptoStream(file, des.CreateDecryptor(key, iv), CryptoStreamMode.Read))
            {
                using (Google.Protobuf.CodedInputStream cos = new Google.Protobuf.CodedInputStream(cryptoStream))
                {
                    loadedData = PlayerData.Parser.ParseFrom(cos);
                }
            }
            file.Close();
            return loadedData;
        }
        else
        {
            return new PlayerData();
        }
    }

    /// <summary>
    /// SUPER DESTRUCTIVE OPERATION please be careful!
    /// </summary>
    public static void Reset()
    {
        local = new PlayerData();
        local.Save();
    }


}
