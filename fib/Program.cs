
using System.CommandLine;
using System.IO;

Dictionary<string, List<string>> langs = new Dictionary<string, List<string>>
{
    { "csharp", new List<string> { ".cs", ".csx" } },
    { "java", new List<string> { ".java" } },
    { "python", new List<string> { ".py", ".pyw" } },
    { "javascript", new List<string> { ".js", ".jsx", ".mjs", ".cjs" } },
    { "typescript", new List<string> { ".ts", ".tsx", ".mts", ".cts" } },
    { "html", new List<string> { ".html", ".htm" } },
    { "css", new List<string> { ".css" } }
};

var sorts = new List<string>() { "name", "type" };

static bool checkLangValidity(string? language, Dictionary<string, List<string>> langs)
{
    if (string.IsNullOrWhiteSpace(language))
    {
        Console.WriteLine("Language cannot be empty.");
        return false;
    }

    language = language.Trim().ToLowerInvariant();

    if (language != "all" && !langs.ContainsKey(language))
    {
        Console.WriteLine($"Invalid language. Supported values: {string.Join(", ", langs.Keys)}, all");
        return false;
    }

    return true;
}

static bool checkOutputValidity(FileInfo? output)
{
    if (output == null || string.IsNullOrWhiteSpace(output.FullName))
    {
        Console.WriteLine("Please type an output path.");
        return false;
    }
    try
    {
        string fullPath = Path.GetFullPath(output.FullName);
        string? directory = Path.GetDirectoryName(fullPath);

        if (directory == null || !Directory.Exists(directory))
        {
            Console.WriteLine("The output directory does not exist.");
            return false;
        }

        return true;
    }
    catch (ArgumentException)
    {
        Console.WriteLine("The output path is invalid.");
        return false;
    }
    catch (NotSupportedException)
    {
        Console.WriteLine("The output path is not supported.");
        return false;
    }
    catch (PathTooLongException)
    {
        Console.WriteLine("The output path is too long.");
        return false;
    }
}
//יצירת ארגומנטים
var bundleNote = new Option<bool>("--note", "-n");
var bundleRemove = new Option<bool>("--remove-empty-lines", "-r");
var bundleAuthor = new Option<string>("--author", "-a");
var bundleOutput = new Option<FileInfo>("--output", "-o") { Description = "path and name for the file" };
var bundleSort = new Option<string>("--sort", "-s")
{
    DefaultValueFactory = _ => "name"
};
var bundleLanguage = new Option<string>("--language", "-l")
{
    Description = "Programming languages to include, or 'all'",
    Required = true
};

//הוספת ארגומנטים
var bundleCommand = new Command("bundle", "bundling files");
bundleCommand.Options.Add(bundleOutput);
bundleCommand.Options.Add(bundleLanguage);
bundleCommand.Options.Add(bundleNote);
bundleCommand.Options.Add(bundleSort);
bundleCommand.Options.Add(bundleRemove);
bundleCommand.Options.Add(bundleAuthor);

//הפעולה עצמה
bundleCommand.SetAction((parseResult) =>
{
    try
    {
        string? language = parseResult.GetValue(bundleLanguage);
        FileInfo? fileInfo = parseResult.GetValue(bundleOutput);
        language = language.Trim().ToLowerInvariant();
        bool? note = parseResult.GetValue(bundleNote);
        bool? remove = parseResult.GetValue(bundleRemove);
        string? author = parseResult.GetValue(bundleAuthor);
        string? sort = parseResult.GetValue(bundleSort);
        if (!checkLangValidity(language, langs)) return;
        if (!checkOutputValidity(fileInfo)) return;
        if (!sorts.Contains(sort) || sort == null)
        {
            Console.WriteLine("the sort is not valid!");
            return;
        }
        string currentDirectory = Directory.GetCurrentDirectory();
        string[] files = Directory.GetFiles(currentDirectory);
        List<string> extensions;
        if (language == "all")
            extensions = langs.Values.SelectMany(x => x).ToList();
        else
            extensions = langs[language];

        if (sort == "name")
        {
            Array.Sort(files, (a, b) =>
                string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            Array.Sort(files, (a, b) => string.Compare(Path.GetExtension(a), Path.GetExtension(b), StringComparison.OrdinalIgnoreCase));
        }
        using (StreamWriter writer = new StreamWriter(fileInfo.FullName))
        {
            if (!string.IsNullOrWhiteSpace(author))
                writer.WriteLine("//author: " + author);
            foreach (string file in files)
            {
                if (extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase) && Path.GetFullPath(file) != Path.GetFullPath(fileInfo.FullName))
                {
                    if (note == true)
                        writer.WriteLine("//source: " + Path.GetRelativePath(currentDirectory, file));
                    if (remove == true)
                    {
                        using (StreamReader reader = new StreamReader(file))
                        {
                            string? line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                    writer.WriteLine(line);
                            }
                        }
                    }
                    else
                    {
                        string context = File.ReadAllText(file);
                        writer.WriteLine(context);
                    }
                    writer.WriteLine("----------------------------------------");
                }
            }
        }

        Console.WriteLine("the file created succssfully!!!");
        Console.WriteLine($"selected language is: {language}");
    }
    catch (UnauthorizedAccessException)
    {
        Console.Error.WriteLine(
            "Error: You do not have permission to access or write to this path.");
    }
    catch (DirectoryNotFoundException)
    {
        Console.Error.WriteLine(
            "Error: The specified directory does not exist.");
    }
    catch (IOException e)
    {
        Console.Error.WriteLine($"Error while reading or writing files: {e.Message}");
    }
    catch (ArgumentException e)
    {
        Console.Error.WriteLine($"Error: Invalid file path. {e.Message}");
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"Unexpected error: {e.Message}");
    }
});

var createFileCommand = new Command("create-rsp", "creating a file of arguments for bundling");


//שורש הפעולה
var rootCommand = new RootCommand("command for bundling files");
rootCommand.Subcommands.Add(bundleCommand);
rootCommand.Subcommands.Add(createFileCommand);



//create-rsp
createFileCommand.SetAction((parseResult) =>
{
    Console.WriteLine("which language do you prefer?\nyou can choose one from this list:\n" + string.Join(", ", langs.Keys) + "\nor type 'all' to select all languages");
    string lang = Console.ReadLine();
    while(!checkLangValidity(lang,langs))
    {
        Console.WriteLine("which language do you prefer?\nyou can choose one from this list:\n" + string.Join(", ", langs.Keys) + "\nor type 'all' to select all languages");
        lang = Console.ReadLine();
    }
    Console.WriteLine("what is the name of the file? you can type a name or a full directory");
    Console.WriteLine("if there is a space in the name of the file, you can turn the directory into '' ");
    Console.WriteLine("Enter the output file name or full path:");
    string? path = Console.ReadLine();
    while (string.IsNullOrWhiteSpace(path))
    {
        Console.WriteLine("Output path cannot be empty. Please try again:");
        path = Console.ReadLine();
    }
    FileInfo output = new FileInfo(path);
    while (!checkOutputValidity(output))
    {
        Console.WriteLine("Enter a valid output file path:");
        path = Console.ReadLine();
        while (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine("Output path cannot be empty. Please try again:");
            path = Console.ReadLine();
        }
        output = new FileInfo(path);
    }
    Console.WriteLine("do you want to save the source in the title? type 'y' or 'n' ");
    string note = Console.ReadLine();
    while(note != "y" && note!="n")
    {
        Console.WriteLine("do you want to save the source in the title? type 'y' or 'n' ");
        note = Console.ReadLine();
    }
    Console.WriteLine("how do you want to sort the files? type 'name' or 'type' ");
    string sort= Console.ReadLine();
    if(sort != "name"&&sort!="type")
        Console.WriteLine("the sort is not recognized, the files will be sorted by the name");
    Console.WriteLine("to remove empty lines press 'y'");
    string remove= Console.ReadLine();
    Console.WriteLine("to write your name in the begining type it");
    string author= Console.ReadLine();
    Console.WriteLine("Enter a name for the RSP file:");
    string? rspName = Console.ReadLine();
    while (string.IsNullOrWhiteSpace(rspName))
    {
        Console.WriteLine("The file name cannot be empty. Try again:");
        rspName = Console.ReadLine();
    }
    if (!rspName.EndsWith(".rsp", StringComparison.OrdinalIgnoreCase))
    {
        rspName += ".rsp";
    }
    Console.WriteLine("well done! now you can run the command: 'dotnet @"+rspName+' for bundling the files");
    using (StreamWriter writer = new StreamWriter(rspName))
    {
        writer.WriteLine("bundle");
        writer.WriteLine("-l");
        writer.WriteLine(lang);
        writer.WriteLine("-o");
        writer.WriteLine($"\"{output.FullName}\"");
        if (note == "y")
        {
            writer.WriteLine("-n");
        }
        if (sort == "type") 
        {
            writer.WriteLine("-s");
            writer.WriteLine("type");
        }
        if(remove == "y")
            writer.WriteLine("-r");
        if(!string.IsNullOrWhiteSpace(author))
        {
            writer.WriteLine("-a");
            writer.WriteLine($"\"{author}\"");
        }
    }

});

await rootCommand.Parse(args).InvokeAsync();

//לסדר את ההערות לפי שפות
//לתת אפשרות לשם לקובץ RSP
