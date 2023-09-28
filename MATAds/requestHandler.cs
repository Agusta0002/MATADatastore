using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;

public class RequestHandler
{
    public static bool acceptingConnections = true;
    public static bool LoggingEnabled = true;
    public static List<object[]>? Queue = new();

    public static bool QueueWorkerWorking = false;
    public static bool DatastoreSearchHappening = false;

    public static int MaxAllowedProcessors = 16; 

    public static object CreateDatastore(string DatastoreName, string DatastoreKey)
    {
        if (!acceptingConnections) { return Results.Json(null, null, "application/json", 423); }

        Console.Write("DatastoreName:");
        Console.WriteLine(DatastoreName);
        Console.Write("DatastoreKey:");
        Console.WriteLine(DatastoreKey);

        Dictionary<string, string> response = new();

        try
        {
            if (Directory.Exists(Directory.GetCurrentDirectory() + @"\\Datastores\\" + DatastoreName))
            {
                response.Add("message", "unable to create, datastore already exists");
                return Results.Json(response, null, "application/json", 409);
            }
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\\Datastores\\" + DatastoreName);

            File.AppendAllText(Directory.GetCurrentDirectory() + @"\\Datastores\\" + DatastoreName + @"\\DatastoreInformation.dbif", $"{DatastoreKey}\n{DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString()}");

        }
        catch (Exception ex)
        {
            File.AppendAllText(Directory.GetCurrentDirectory() + @"\\DatastoreCreationLogs.txt", "\n\n[" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "]: " + ex.ToString());

            response.Add("message", "unable to create datastore, retry later");
            return Results.Json(response, null, "application/json", 500);
        }

        response.Add("message", "successfully created datastore");
        return Results.Json(response, null, "application/json", 201);
    }
    public static object GetValue(string DatastoreName, string DatastoreKey, string Entry)
    {

        if (!acceptingConnections) { return Results.Json(null, null, "application/json", 423); }

        DatastoreSearchHappening = true;
        Dictionary<string, object> response = new();

        if (!Directory.Exists(Directory.GetCurrentDirectory() + @"\\Datastores\\" + DatastoreName))
        {
            response["message"] = "no matching datastore found";
            DatastoreSearchHappening = false;
            return Results.Json(response, null, "application/json", 404);
        }

        if (File.ReadLines(Directory.GetCurrentDirectory() + @"\\Datastores\\" + DatastoreName + @"\\DatastoreInformation.dbif").First() != DatastoreKey)
        {
            response["message"] = "the datastore key is incorrect";
            return Results.Json(response, null, "application/json", 403);
        }

        response["value"] = EditDatastore.searchDatastore(DatastoreName, Entry);

        if (response["value"] == null) 
        {
            response["message"] = $"No entry has been found for {Entry}";
            DatastoreSearchHappening = false;

            return Results.Json(response, null, "application/json", 404);
        }  
        
        DatastoreSearchHappening = false;

        return Results.Json(response, null, "application/json", 200);
    }

    

    public static object SetValue(string DatastoreName, string DatastoreKey, string Entry, string Value)
    {
        if (!acceptingConnections) { return Results.Json(null, null, "application/json", 423); }

        Dictionary<string, object> response = new();

        if (!Directory.Exists(Directory.GetCurrentDirectory() + @"\\Datastores\\" + DatastoreName))
        {
            response["message"] = "no matching datastore found";
            return Results.Json(response, null, "application/json", 404);
        }

        if (File.ReadLines(Directory.GetCurrentDirectory() + @"\\Datastores\\" + DatastoreName + @"\\DatastoreInformation.dbif").First() != DatastoreKey)
        {
            response["message"] = "the datastore key is incorrect";
            return Results.Json(response, null, "application/json", 403);
        }
        
        Queue.Add(new Object[] { DatastoreName, Entry, Value });

        Console.WriteLine($"Added to Queue: {Entry}: {Value} for {DatastoreName}");
        try
        {
            new Thread(delegate () { EditDatastore.QueueWorker(); }).Start();
        }
        catch (Exception e) {
            File.AppendAllText(Directory.GetCurrentDirectory() + @"\\DatastoreWritingLogs.txt", $"\n\n[{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}]: {e}");
            return Results.Json(null, null, "application/json", 500); 
        }
        

        return Results.Json(null, null, "application/json", 202);
    }
    
    
}
