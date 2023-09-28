public class editDatastore
{
    public static Dictionary<Object, Object[]> Cache = new();
    public static Int128 StorageFileBreakSize = 250000;
    public static int MaxAllowedThreads = 16;
    public static void threadPortion(string DatastoreName, string Entry, Int32 fileRange, Int32 fileStart, AutoResetEvent autoResetEvent)
    {
        try {
            for (int i = fileStart - 1 ; i <= fileRange + fileStart + 1; i++)
            {
                if (File.Exists(Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}\\storageFile{i}.dbsf"))
                {
                    using (StreamReader streamReader = new StreamReader(Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}\\storageFile{i}.dbsf"))
                    {
                        while (streamReader.Peek() >= 0)
                        {
                            char[] fileEntry = new char[streamReader.Read()];
                            streamReader.Read(fileEntry, 0, fileEntry.Length);
                            if (Entry == new string(fileEntry))
                            {
                                Cache[Entry] = new Object[] { streamReader.ReadLine(), Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}\\storageFile{i}.dbsf" };
                                autoResetEvent.Set();
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
                }
            }
        } catch {return;}
    }
    public static bool updateFile(string path, string Entry, string Value)
    {
        try {
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
        } catch {return false;}
        return true;
    }

    public static object? searchDatastore(string DatastoreName, string Entry)
    {
        Int32 fileAmount = Directory.GetFiles(Directory.GetCurrentDirectory() + $"\\Datastores\\{DatastoreName}").Count() - 1;

        WaitHandle[] waitHandle = new WaitHandle[editDatastore.MaxAllowedThreads];

        for (int i = 0; i < editDatastore.MaxAllowedThreads; i++)
        {
            
            var fileRange = (int)Math.Ceiling((float)fileAmount / (float)editDatastore.MaxAllowedThreads);
            var fileStart = (int) ((float)fileAmount / (float)editDatastore.MaxAllowedThreads * (float)i);

            AutoResetEvent autoResetEvent = new(false);
            ThreadPool.QueueUserWorkItem(state => threadPortion(DatastoreName, Entry, fileRange, fileStart, autoResetEvent), waitHandle[i]);
            waitHandle[i] = autoResetEvent;
        }

        WaitHandle.WaitAny(waitHandle, 1000);

        object returner = null;

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

        WaitHandle[] waitHandle = new WaitHandle[editDatastore.MaxAllowedThreads];

        for (int i = 0; i < editDatastore.MaxAllowedThreads; i++)
        {
            var fileRange = (int)Math.Ceiling((float)fileAmount / (float)editDatastore.MaxAllowedThreads);
            var fileStart = (int)((float)fileAmount / (float)editDatastore.MaxAllowedThreads * (float)i);

            AutoResetEvent autoResetEvent = new(false);
            ThreadPool.QueueUserWorkItem(state => threadPortion(DatastoreName, Entry, fileRange, fileStart, autoResetEvent), waitHandle[i]);
            waitHandle[i] = autoResetEvent;

        }

        WaitHandle.WaitAny(waitHandle, 1000);

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
}