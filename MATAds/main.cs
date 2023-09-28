using System;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

static void Main()
{
    Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\\Datastores");
    Console.WriteLine("System is running on " + Environment.ProcessorCount + " Processors avaible");

}

static void CommandChecker()
{
    Console.WriteLine("type \"help\" for a list of commands");
    while (true)
    {
        string ?command = Console.ReadLine();
        switch (command)
        {
            case "exit":
                Console.WriteLine("shutting down, please do not shutdown program");
                RequestHandler.acceptingConnections = false;
                EditDatastore.QueueWorker();
                Console.WriteLine("started queue worker in command thingy");
                while (RequestHandler.Queue.Count > 0)
                {
                    Thread.Sleep(20);
                }

                EditDatastore.QueueWorker();

                Console.WriteLine("saved all entries");
                Environment.Exit(0);
                break;
            case "lock":
                Console.WriteLine("Locked Datastore, no new Connections will be processed");
                RequestHandler.acceptingConnections = false;
                break;
            case "unlock":
                Console.WriteLine("Unlocked Datastore, new Connections will be processed again");
                RequestHandler.acceptingConnections = true;
                break;
            case "help":
                Console.WriteLine("following commands are avaible:\nexit - shut down system\nlock - decline all new requests\nunlock - accept new requests");
                break;
        }
    }
}

new Thread(CommandChecker).Start();

app.MapPost("/createDatastore", RequestHandler.CreateDatastore);
app.MapGet("/Entry", RequestHandler.GetValue);
app.MapPost("/Entry", RequestHandler.SetValue);

Main();
app.Run("http://127.0.0.1:8000/");