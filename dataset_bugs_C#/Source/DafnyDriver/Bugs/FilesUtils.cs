using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DafnyDriver.Bugs;

public class FilesUtils {
  
  public static string ReplacePartOfFile(string filePath, int startLine, int startColumn, int endLine, int endColumn, string replacement)
  {
    List<string> lines = new List<string>(File.ReadAllLines(filePath));

    if (startLine < 1 || startLine > lines.Count || endLine < 1 || endLine > lines.Count)
    {
      throw new ArgumentException("Start or end line is out of range.");
    }

    StringBuilder modifiedContent = new StringBuilder();

    for (int i = 0; i < lines.Count; i++)
    {
      if (i + 1 < startLine || i + 1 > endLine)
      {
        modifiedContent.AppendLine(lines[i]);
      }
      else if (i + 1 == startLine && i + 1 == endLine)
      {
        string modifiedLine = lines[i].Substring(0, startColumn - 1) +
                              replacement +
                              lines[i].Substring(endColumn);
        modifiedContent.AppendLine(modifiedLine);
      }
      else if (i + 1 == startLine)
      {
        string modifiedLine = lines[i].Substring(0, startColumn - 1) + replacement;
        modifiedContent.AppendLine(modifiedLine);
      }
      else if (i + 1 == endLine)
      {
        string modifiedLine = lines[i].Substring(endColumn);
        modifiedContent.AppendLine(modifiedLine);
      }
    }

    return modifiedContent.ToString();
  }
  
  public static void WriteCodeToFile(string directoryPath, string fileName, string code)
  {
    // Combine directory path and file name to get the full file path
    var correctDir =  directoryPath.Replace('<', '(').Replace('>', ')');
    if (!Directory.Exists(correctDir)) {
      Directory.CreateDirectory(correctDir);
    }
    string filePath = Path.Combine(correctDir, fileName);

    try
    {
      // Write the code content to the file
      File.WriteAllText(filePath, code);
      Console.WriteLine($"Code successfully written to {filePath}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"An error occurred while writing to {filePath}: {ex.Message}");
    }
  }
}