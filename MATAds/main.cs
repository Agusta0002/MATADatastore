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

app.MapPost("/createDatastore", RequestHandler.CreateDatastore);
app.MapGet("/Entry", RequestHandler.GetValue);
app.MapPost("/Entry", RequestHandler.SetValue);

Main();
app.Run("http://127.0.0.1/");