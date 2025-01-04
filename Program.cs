using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using System.Runtime.Versioning;

class Program
{
    enum Language { Korean, English }
    static Language language = Language.Korean;
    static bool autoExit = true;

    class FileOption
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public FileOption(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public override string ToString() => Name;
    }

    static List<FileOption> fileOptions = new()
    {
        new FileOption("Steam ADOFAI", "steam://rungameid/977950"),
        new FileOption("exe ADOFAI", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\A Dance of Fire and Ice\\A Dance of Fire and Ice.exe")
    };

    static void Main()
    {
        LoadSettings();
        ShowMainMenu();
        SaveSettings();
    }

    static void ShowMainMenu()
    {
        while (true)
        {
            DisplayMenuHeader(language == Language.Korean ? "실행할 얼불춤을 선택하세요:" : "Select the ADOFAI to launch:");
            for (int i = 0; i < fileOptions.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {fileOptions[i]}");
            }
            Console.WriteLine(language == Language.Korean ? "0. 설정" : "0. Settings");
            Console.WriteLine(language == Language.Korean ? "X. 종료" : "X. Exit");

            string? input = GetUserInput(language == Language.Korean ? "선택: " : "Choose: ");

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= fileOptions.Count)
            {
                ExecuteFile(fileOptions[choice - 1].Path);
                if (autoExit) break;
            }
            else if (input == "0")
            {
                ShowSettingsMenu();
            }
            else if (input?.Equals("X", StringComparison.OrdinalIgnoreCase) == true)
            {
                ExitProgram();
            }
            else
            {
                DisplayErrorMessage();
            }
        }
    }

    static void ShowSettingsMenu()
    {
        while (true)
        {
            DisplayMenuHeader(language == Language.Korean ? "설정 메뉴를 선택하세요:" : "Select a settings menu option:");
            Console.WriteLine(language == Language.Korean ? "1. 언어 설정 변경" : "1. Change Language");
            Console.WriteLine(language == Language.Korean ? "2. 실행 경로 추가" : "2. Add File Path");
            Console.WriteLine(language == Language.Korean ? "3. 실행 경로 삭제" : "3. Remove File Path");
            Console.WriteLine(language == Language.Korean ? "4. 자동 종료 설정 변경" : "4. Toggle Auto Exit");
            Console.WriteLine(language == Language.Korean ? "0. 이전 메뉴로" : "0. Back to Main Menu");

            string? input = GetUserInput(language == Language.Korean ? "선택: " : "Choose: ");

            switch (input)
            {
                case "1":
                    ChangeLanguage();
                    break;
                case "2":
                    AddFilePath();
                    break;
                case "3":
                    RemoveFilePath();
                    break;
                case "4":
                    ToggleAutoExit();
                    break;
                case "0":
                    return;
                default:
                    DisplayErrorMessage();
                    break;
            }
        }
    }

    static void ChangeLanguage()
    {
        DisplayMenuHeader(language == Language.Korean ? "변경할 언어를 선택하세요:" : "Choose a language to switch to:");
        Console.WriteLine(language == Language.Korean ? "1. 한국어" : "1. Korean");
        Console.WriteLine(language == Language.Korean ? "2. 영어" : "2. English");

        string? input = GetUserInput(language == Language.Korean ? "선택: " : "Choose: ");
        switch (input)
        {
            case "1":
                language = Language.Korean;
                DisplayMessageWithPause("언어가 한국어로 설정되었습니다.", "Language set to Korean.");
                break;
            case "2":
                language = Language.English;
                DisplayMessageWithPause("언어가 영어로 설정되었습니다.", "Language set to English.");
                break;
            default:
                DisplayErrorMessage();
                break;
        }
    }

    static void AddFilePath()
    {
        DisplayMenuHeader(language == Language.Korean ? "새 실행 옵션 추가" : "Add New Launch Option");
        string? name = GetUserInput(language == Language.Korean ? "새 실행 옵션 이름: " : "Enter the name: ");
        if (string.IsNullOrWhiteSpace(name))
        {
            DisplayErrorMessage();
            return;
        }

        string? path = GetUserInput(language == Language.Korean ? "새 실행 경로: " : "Enter the path: ");
        if (string.IsNullOrWhiteSpace(path) || (!File.Exists(path) && !path.StartsWith("steam://")))
        {
            DisplayErrorMessage(language == Language.Korean ? "유효하지 않은 경로입니다." : "Invalid path.");
            return;
        }

        if (fileOptions.Exists(o => o.Name == name || o.Path == path))
        {
            DisplayErrorMessage(language == Language.Korean ? "중복된 이름 또는 경로입니다." : "Duplicate name or path.");
            return;
        }

        fileOptions.Add(new FileOption(name, path));
        DisplayMessageWithPause($"{name}이(가) 추가되었습니다.", $"{name} has been added.");
    }

    static void RemoveFilePath()
    {
        DisplayMenuHeader(language == Language.Korean ? "삭제할 실행 경로를 선택하세요:" : "Select a file path to remove:");
        for (int i = 0; i < fileOptions.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {fileOptions[i]}");
        }
        Console.WriteLine(language == Language.Korean ? "0. 취소" : "0. Cancel");

        string? input = GetUserInput(language == Language.Korean ? "선택: " : "Choose: ");

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= fileOptions.Count)
        {
            var removedOption = fileOptions[choice - 1];
            fileOptions.RemoveAt(choice - 1);
            SaveSettings();
            DisplayMessageWithPause($"{removedOption.Name}이(가) 삭제되었습니다.", $"{removedOption.Name} has been removed.");
        }
        else if (input == "0")
        {
            DisplayMessageWithPause("삭제가 취소되었습니다.", "Removal canceled.");
        }
        else
        {
            DisplayErrorMessage();
        }
    }

    static void ToggleAutoExit()
    {
        autoExit = !autoExit;
        DisplayMessageWithPause(
            autoExit ? "자동 종료가 활성화되었습니다." : "자동 종료가 비활성화되었습니다.",
            autoExit ? "Auto exit enabled." : "Auto exit disabled.");
    }

    [SupportedOSPlatform("windows")]
    static void ExecuteFile(string path)
    {
        try
        {
            if (path.StartsWith("steam://"))
            {
                string steamPath = GetSteamPath();
                string steamExe = Path.Combine(steamPath, "steam.exe");
                if (!File.Exists(steamExe))
                    throw new FileNotFoundException(language == Language.Korean ? "Steam 실행 파일을 찾을 수 없습니다." : "Steam executable not found.");

                string appId = path.Substring(path.LastIndexOf('/') + 1);
                Process.Start(new ProcessStartInfo
                {
                    FileName = steamExe,
                    Arguments = $"-applaunch {appId}",
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            DisplayMessageWithPause(
                language == Language.Korean ? $"실행 중 오류 발생: {ex.Message}" : $"Error while executing: {ex.Message}");
        }
    }

    [SupportedOSPlatform("windows")]
    static string GetSteamPath()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        return key?.GetValue("SteamPath") as string
            ?? throw new InvalidOperationException(language == Language.Korean ? "Steam 경로를 찾을 수 없습니다." : "Steam path not found.");
    }

    [SupportedOSPlatform("windows")]
    static void ExitProgram()
    {
        Environment.Exit(0);
    }

    static void SaveSettings()
    {
        try
        {
            var settings = new
            {
                Language = (int)language,
                AutoExit = autoExit,
                FileOptions = fileOptions
            };
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("settings.json", json);
        }
        catch (Exception ex)
        {
            DisplayMessageWithPause(
                language == Language.Korean ? $"설정 저장 중 오류 발생: {ex.Message}" : $"Error saving settings: {ex.Message}");
        }
    }

    static void LoadSettings()
    {
        if (!File.Exists("settings.json")) return;

        try
        {
            string json = File.ReadAllText("settings.json");
            var settings = JsonSerializer.Deserialize<Settings>(json);

            if (settings != null)
            {
                language = (Language)settings.Language;
                autoExit = settings.AutoExit;
                fileOptions = settings.FileOptions ?? new List<FileOption>();
            }
        }
        catch (Exception ex)
        {
            DisplayMessageWithPause(
                language == Language.Korean ? $"설정 불러오기 중 오류 발생: {ex.Message}" : $"Error loading settings: {ex.Message}");
        }
    }

    static void DisplayMenuHeader(string message)
    {
        Console.Clear();
        Console.WriteLine("=================================");
        Console.WriteLine(message);
    }

    static void DisplayMessageWithPause(string koreanMessage, string englishMessage = null)
    {
        Console.WriteLine(language == Language.Korean ? koreanMessage : (englishMessage ?? koreanMessage));
        Pause();
    }

    static void DisplayErrorMessage()
    {
        DisplayMessageWithPause(
            "잘못된 입력입니다. 다시 시도하세요.",
            "Invalid input. Please try again.");
    }

    static void DisplayErrorMessage(string message)
    {
        DisplayMessageWithPause(message, message);
    }

    static string? GetUserInput(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine();
    }

    static void Pause()
    {
        Console.WriteLine(language == Language.Korean ? "계속하려면 아무 키나 누르세요..." : "Press any key to continue...");
        Console.ReadKey();
    }

    class Settings
    {
        public int Language { get; set; }
        public bool AutoExit { get; set; }
        public List<FileOption>? FileOptions { get; set; }
    }
}
