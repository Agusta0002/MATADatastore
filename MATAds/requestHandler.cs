public class RequestHandler
{
    public static object CreateDatastore(string DatastoreName, string DatastoreKey)
    {
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

        response["value"] = editDatastore.searchDatastore(DatastoreName, Entry);
        if (response["value"] == null) 
        {
            response["message"] = $"No entry has been found for {Entry}";
            return Results.Json(response, null, "application/json", 404);
        }

        return Results.Json(response, null, "application/json", 200);
    }

    public static object SetValue(string DatastoreName, string DatastoreKey, string Entry, string Value)
    {
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

        //goes into the external function
        if (editDatastore.setDatastore(DatastoreName, Entry, Value))
        {
            return Results.Json(response, null, "application/json", 200);
        }

        response["message"] = "something went wrong while saving the data";
        return Results.Json(response, null, "application/json", 500);

    }
}