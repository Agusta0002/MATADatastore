using System;
using System.IO;

public class EditDatastore
{
    public static Dictionary<Object, Object[]> Cache = new();
    public static Int128 StorageFileBreakSize = 250000;

    public static void ThreadPortion(string DatastoreName, string Entry, Int32 fileRange, Int32 fileStart, AutoResetEvent autoResetEvent)
    {
        bool FoundEntry = false;
        for (int i = fileStart - 1 ; i <= fileRange + fileStart + 1; i++)
        {
            
            if (File.Exists(Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}\\storageFile{i}.dbsf"))
            {
                try {
                    using (StreamReader streamReader = new StreamReader(Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}\\storageFile{i}.dbsf"))
                    {
                        while (streamReader.Peek() >= 0)
                        {
                            char[] fileEntry = new char[streamReader.Read()];
                            streamReader.Read(fileEntry, 0, fileEntry.Length);
                            if (Entry == new string(fileEntry))
                            {
                                FoundEntry = true;
                                Cache[Entry] = new Object[] { streamReader.ReadLine(), Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}\\storageFile{i}.dbsf" };
                                streamReader.Close();
                                break;
                            }
                            else
                            {
                                
                                streamReader.ReadLine();
                            }
                        }
                        streamReader.Close();
                    }
                } catch {
                    return;
                }
            }
        }
        if (FoundEntry) { autoResetEvent.Set();}
    }

    public static bool updateFile(string path, string Entry, string Value)
    {
        Console.WriteLine("Rewriting File");
        StreamReader streamReader = new StreamReader(path);
        StreamWriter streamWriter = new StreamWriter(path+"c");

        while (streamReader.Peek() >= 0)
        {
            char[] fileEntry = new char[streamReader.Read()];
            streamReader.Read(fileEntry, 0, fileEntry.Length);

            if (Entry != new string(fileEntry))
            {
                var line = new string(Convert.ToChar(fileEntry.Length) + new string(fileEntry) + streamReader.ReadLine());
                streamWriter.WriteLine(line);
            }
            else
            {
                var line = Value;
                streamReader.ReadLine();
                streamWriter.WriteLine(line);
            }
        }
        

        streamReader.Close();
        streamWriter.Close();

        File.Delete(path);
        File.Move(path + "c", path);
        return false;
    }

    public static object? searchDatastore(string DatastoreName, string Entry)
    {
        Int32 fileAmount = Directory.GetFiles(Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}").Count() - 1;

        WaitHandle[] waitHandle = new WaitHandle[RequestHandler.MaxAllowedProcessors];

        for (int i = 0; i < RequestHandler.MaxAllowedProcessors; i++)
        {
            var fileRange = (int)Math.Ceiling((float)fileAmount / (float) RequestHandler.MaxAllowedProcessors);
            var fileStart = (int) ((float)fileAmount / (float)RequestHandler.MaxAllowedProcessors * (float)i);

            AutoResetEvent autoResetEvent = new(false);
            ThreadPool.QueueUserWorkItem(state => ThreadPortion(DatastoreName, Entry, fileRange, fileStart, autoResetEvent), waitHandle[i]);
            waitHandle[i] = autoResetEvent;
        }


        WaitHandle.WaitAny(waitHandle, (int) 100);

        object? returner = null;

        //TODO: cleanup code snipped
        if (Cache.ContainsKey(Entry))
        {
            returner = (string) Cache[Entry][0];
        }

        Cache.Remove(Entry);
        return returner;
    }

    public static bool setDatastore(string DatastoreName, string Entry, string Value)
    {
        Int32 fileAmount = Directory.GetFiles(Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}").Count() - 1;

        WaitHandle[] waitHandle = new WaitHandle[RequestHandler.MaxAllowedProcessors];

        for (int i = 0; i < RequestHandler.MaxAllowedProcessors; i++)
        {
            var fileRange = (int)Math.Ceiling((float)fileAmount / (float) RequestHandler.MaxAllowedProcessors);
            var fileStart = (int)((float)fileAmount / (float)RequestHandler.MaxAllowedProcessors * (float)i);

            AutoResetEvent autoResetEvent = new(false);

            ThreadPool.QueueUserWorkItem(state => ThreadPortion(DatastoreName, Entry, fileRange, fileStart, autoResetEvent), waitHandle[i]);

            waitHandle[i] = autoResetEvent;

        }
 
        WaitHandle.WaitAny(waitHandle, (int) 1000);

        if (!Cache.ContainsKey(Entry))
        {
            if (fileAmount == 0)
            {
                string line = (char) Entry.Length + Entry + Value + "\n";
                File.AppendAllText((Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}\\storageFile{fileAmount}.dbsf"), line);
            } 
            else
            {
                if (new FileInfo(Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}\\storageFile{fileAmount-1}.dbsf").Length >= StorageFileBreakSize)
                {
                    string line = (char)Entry.Length + Entry + Value + "\n";
                    File.AppendAllText((Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}\\storageFile{fileAmount}.dbsf"), line);
                }
                else
                {
                    string line = (char)Entry.Length + Entry + Value + "\n";
                    File.AppendAllText((Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}\\storageFile{fileAmount - 1}.dbsf"), line);
                }
            }
        }
        else
        {
            updateFile((string)Cache[Entry][1], Entry, (char)Entry.Length + Entry + Value);
        }
        return true;
    }

    public static bool QueueWorker()
    {
        if (RequestHandler.QueueWorkerWorking) { Console.WriteLine("Queue worker already working"); return false; }
        Console.WriteLine("Starting Queue Worker");
        RequestHandler.QueueWorkerWorking = true;
        

        int QueueLength = RequestHandler.Queue.Count;
        Console.WriteLine($"there are {QueueLength} elements in the queue");
        while (QueueLength > 0) {
            
            try {
                setDatastore((string)RequestHandler.Queue[QueueLength - 1][0], (string)RequestHandler.Queue[QueueLength - 1][1], (string)RequestHandler.Queue[QueueLength - 1][2]);
                RequestHandler.Queue.RemoveAt(QueueLength - 1);
            }
            catch (Exception e)
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + @"\\DatastoreWritingLogs.txt", $"\n\n[{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}]: {e} for {RequestHandler.Queue[QueueLength - 1]}");
            }
            QueueLength--;
        }

        RequestHandler.QueueWorkerWorking = false;
        Console.WriteLine("Stopped Queue Worker");
        return true;
    }
}
