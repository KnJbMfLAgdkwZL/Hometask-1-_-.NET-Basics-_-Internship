using Microsoft.Extensions.Configuration;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Hometask_1___.NET_Basics___Internship.Logic.Interfaces;
using Newtonsoft.Json;

namespace Hometask_1___.NET_Basics___Internship.Logic;

public class CsvEtlService : IEtlService
{
    private readonly List<string> _processingFiles = new();

    private string _inputFolder = "";
    private string _outputFolder = "";

    private bool GetConfig()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.json");
            var config = configuration.Build();

            if (string.IsNullOrEmpty(config["InputFolder"]) || string.IsNullOrEmpty(config["OutputFolder"]))
            {
                return false;
            }

            _inputFolder = config["InputFolder"];
            _outputFolder = config["OutputFolder"];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public async Task Start(CancellationToken cancelToken)
    {
        if (GetConfig())
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            while (await timer.WaitForNextTickAsync())
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                var files = Directory.GetFiles(_inputFolder)
                    .Where(s => s.ToLower().EndsWith(".csv") || s.ToLower().EndsWith(".txt"))
                    .Where(file => !_processingFiles.Contains(file)).ToList();

                foreach (var file in files.Where(_ => !cancelToken.IsCancellationRequested))
                {
                    Parse(file);
                }
            }
        }
    }

    private void Parse(string file)
    {
        _processingFiles.Add(file);

        var json = ReadAndTransform(file);
        Save(json, file);

        _processingFiles.Remove(file);
    }

    private static string ReadAndTransform(string file)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ", ",
            PrepareHeaderForMatch = (args) => args.Header.ToLower()
        };
        using var reader = new StreamReader(file);
        using var csv = new CsvReader(reader, config);
        var rows = csv.GetRecords<dynamic>();

        return Transform(rows);
    }

    private static string Transform(IEnumerable<dynamic> rows)
    {
        var settings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Error = (sender, args) => { args.ErrorContext.Handled = true; },
        };
        return JsonConvert.SerializeObject(rows, Formatting.Indented, settings);
    }

    private static string RandomString(int length)
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private void Save(string json, string file)
    {
        var date = DateTime.Now.ToShortDateString();
        var dirOut = $"{_outputFolder}/{date}";
        if (!Directory.Exists(dirOut))
        {
            Directory.CreateDirectory(dirOut);
        }

        var doneDir = $"{_inputFolder}/Done";
        if (!Directory.Exists(doneDir))
        {
            Directory.CreateDirectory(doneDir);
        }

        var fileId = RandomString(6);
        var fileName = Path.GetFileName(file);
        var fileType = Path.GetExtension(file);
        var newName = $"{fileId}_{fileName}";

        var outputPath = $"{dirOut}/{newName.Replace(fileType, ".json")}";
        File.WriteAllText(outputPath, json);

        File.Move(file, $"{doneDir}/{newName}");
    }
}