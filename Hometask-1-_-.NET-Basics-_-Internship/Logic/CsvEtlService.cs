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

    private List<string> _errorLog = new();
    private long _parsedFiles = 0;
    private long _parsedLines = 0;
    private long _foundErrors = 0;
    private bool _midnightErrorLog = false;

    private void ResertError()
    {
        _errorLog = new List<string>();
        _parsedFiles = 0;
        _parsedLines = 0;
        _foundErrors = 0;
        _midnightErrorLog = false;
    }

    void SaveErrors()
    {
        var date = DateTime.Now;
        if (date.Hour == 23 && date.Minute == 59)
        {
            if (!_midnightErrorLog)
            {
                var dateShor = DateTime.Now.ToShortDateString();
                var dirOut = $"{_outputFolder}/{dateShor}";
                if (!Directory.Exists(dirOut))
                {
                    Directory.CreateDirectory(dirOut);
                }

                var settings = new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Error = (sender, args) => { args.ErrorContext.Handled = true; },
                };

                var dyn = new
                {
                    parsed_files = _parsedFiles,
                    parsed_lines = _parsedLines,
                    foundErrors = _foundErrors,
                    invalid_files = JsonConvert.SerializeObject(_errorLog, Formatting.Indented, settings)
                };

                var logJson = JsonConvert.SerializeObject(dyn, Formatting.Indented, settings);

                var outputPath = $"{dirOut}/meta.log";
                File.WriteAllText(outputPath, logJson);

                _midnightErrorLog = true;
            }
        }
        else
        {
            _midnightErrorLog = false;
        }
    }

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
            var errorStr = $"{DateTime.Now}: No config file";
            _errorLog.Add(errorStr);
            _errorLog.Add(e.ToString());

            Console.WriteLine(errorStr);
            Console.WriteLine(e);
            return false;
        }

        return true;
    }


    public async Task Start(CancellationToken cancelToken)
    {
        ResertError();

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
        try
        {
            _processingFiles.Add(file);
            var json = ReadAndTransform(file);
            Save(json, file);
        }
        catch (Exception e)
        {
            var errorStr = $"{DateTime.Now}: Error parce {file}";
            _errorLog.Add(errorStr);
            _errorLog.Add(e.ToString());
            _foundErrors++;

            Console.WriteLine(errorStr);
            Console.WriteLine(e);
        }

        _parsedFiles++;
        _processingFiles.Remove(file);
    }

    private string ReadAndTransform(string file)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ", ",
            PrepareHeaderForMatch = (args) => args.Header.ToLower()
        };
        using var reader = new StreamReader(file);
        using var csv = new CsvReader(reader, config);
        var rows = csv.GetRecords<dynamic>().ToList();

        _parsedLines += rows.Count;

        return Transform(rows);
    }

    private string Transform(IEnumerable<dynamic> rows)
    {
        var settings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Error = (sender, args) => { args.ErrorContext.Handled = true; },
        };
        return JsonConvert.SerializeObject(rows, Formatting.Indented, settings);
    }

    private string RandomString(int length)
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private void Save(string json, string file)
    {
        var dateShor = DateTime.Now.ToShortDateString();
        var dirOut = $"{_outputFolder}/{dateShor}";
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