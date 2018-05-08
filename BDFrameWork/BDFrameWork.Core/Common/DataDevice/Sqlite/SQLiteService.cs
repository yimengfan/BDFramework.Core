using System;
using SQLite4Unity3d;
using UnityEngine;
#if !UNITY_EDITOR
using System.Collections;
using System.IO;
#endif
using System.Collections.Generic;

public class SQLiteService
{
    //db connect
    public SQLiteConnection DB { get; private set; }

    public SQLiteService(string DatabaseName)
    {
#if UNITY_EDITOR
        var dbPath = string.Format(@"Assets/StreamingAssets/{0}", DatabaseName);
#else
// check if file exists in Application.persistentDataPath
        var filepath = string.Format("{0}/{1}", Application.persistentDataPath, DatabaseName);

        if (!File.Exists(filepath))
        {
            Debug.Log("Database not in Persistent path");
            // if it doesn't ->
            // open StreamingAssets directory and load the db ->

#if UNITY_ANDROID 
            var loadDb =
new WWW("jar:file://" + Application.dataPath + "!/assets/" + DatabaseName);  // this is the path to your StreamingAssets in android
            while (!loadDb.isDone) { }  // CAREFUL here, for safety reasons you shouldn't let this while loop unattended, place a timer and error check
            // then save to Application.persistentDataPath
            File.WriteAllBytes(filepath, loadDb.bytes);
#elif UNITY_IOS
                 var loadDb =
Application.dataPath + "/Raw/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy(loadDb, filepath);
#elif UNITY_WP8
                var loadDb =
Application.dataPath + "/StreamingAssets/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy(loadDb, filepath);

#elif UNITY_WINRT
		var loadDb =
Application.dataPath + "/StreamingAssets/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
		// then save to Application.persistentDataPath
		File.Copy(loadDb, filepath);
		
#elif UNITY_STANDALONE_OSX
		var loadDb =
Application.dataPath + "/Resources/Data/StreamingAssets/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
		// then save to Application.persistentDataPath
		File.Copy(loadDb, filepath);
#else
	var loadDb = Application.dataPath + "/StreamingAssets/" + DatabaseName;  
            // this is the path to your StreamingAssets in iOS
	// then save to Application.persistentDataPath
	File.Copy(loadDb, filepath);

#endif

          // Debug.Log("Database written");
        }

        var dbPath = filepath;
#endif
        DB = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
      //  Debug.Log("Final PATH: " + dbPath);
    }


    public void Close()
    {
        DB.Close();
    }

    public void CreateDB<T>()
    {
        DB.DropTable<T>();
        DB.CreateTable<T>();
    }

    public void CreateDBByType(Type t)
    {
        
        DB.DropTableByType(t);
        DB.CreateTableByType(t);
    }
    
    public void InsertTable(System.Collections.IEnumerable objects)
    {
        DB.InsertAll(objects);
    }
    
//    public void CreateDB<T>()
//    {
//        DB.DropTable<T>();
//        DB.CreateTable<T>();
//
//        DB.InsertAll(new[]
//        {
//            new Person
//            {
//                Id = 1,
//                Name = "Tom",
//                Surname = "Perez",
//                Age = 56
//            },
//            new Person
//            {
//                Id = 2,
//                Name = "Fred",
//                Surname = "Arthurson",
//                Age = 16
//            },
//            new Person
//            {
//                Id = 3,
//                Name = "John",
//                Surname = "Doe",
//                Age = 25
//            },
//            new Person
//            {
//                Id = 4,
//                Name = "Roberto",
//                Surname = "Huertas",
//                Age = 37
//            }
//        });
//    }

    public TableQuery<T> GetTable<T>() where T : new()
    {
        return DB.Table<T>();
    }
//    public IEnumerable<Person> GetPersons()
//    {
//        return DB.Table<Person>();
//    }

    public IEnumerable<Person> GetPersonsNamedRoberto()
    {
        return DB.Table<Person>().Where(x => x.Name == "Roberto" && x.Age == 11);
    }

    public Person GetJohnny()
    {
        return DB.Table<Person>().Where(x => x.Name == "Johnny").FirstOrDefault();
    }

    public Person CreatePerson()
    {
        var p = new Person
        {
            Name = "Johnny",
            Surname = "Mnemonic",
            Age = 21
        };
        DB.Insert(p);
        return p;
    }
}