using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Dafny;

using static DafnyDriver.APR.UtilsString;

namespace DafnyDriver.APR;

/*
 * RepairLlama3
 *
 * <summary>
 *   To perform the repair using the LLama3 model
 * </summary>
 * 
 * <fields>
 *  <field> DafnyOptions: the dafny options to run the boogie verification
 *  <field> OriginalProgram: Original program of the dafny
 *  <field> OriginalFileName: the name of the Dafny program file
 *  <field> Susp_Stmt: A dictionary that the key represents a member, and the value is a list of suspicious lines
            associated with the buggy line.
 *  <field> repair: the flag indicating whether the candidate fix is correct or not
 *  <field> Models: the LLM
 *  <field> model: the model name of the LLM
 *  <field> check: the flag indicating whether the content provided by the model is valid
 *  <field> FilePath: The path of the dafny file (created to verify the candidate fix)
 *  <field> repairDir: To change the path for evaluating the repair
 * </fields>
 *
 * IMPORTANT:
 * 1. Change the path to the correct directory where your Repair\Llama3\Result are located.
 *    Look for occurrences of 'repairDir = @"Path\To\Repair\Llama3\Result"' in the file and update them accordingly.
 * 
 * Ensure all references to 'repairDir = @"Path\To\Repair\Llama3\Result"' are updated to reflect the new path.
 * Uncomment the RepairToFile
 * This is crucial to avoid errors when running the program.
 */

public class RepairLlama3 {
  public DafnyOptions DafnyOptions;
  public Program OriginalProgram;
  public string OriginalFileName;
  public Dictionary<Declaration, HashSet<int>> Susp_Stmt;
  public bool repair = false;

  public APIModels Models;
  public string model = "LM Studio Community/Meta-Llama-3-8B-Instruct-GGUF";

  public string check = null;
  public string FilePath;

  public string repairDir = @"Path\To\Repair\Llama3\Result";

  public RepairLlama3(DafnyOptions dafnyOptions, Program program) {
    this.DafnyOptions = dafnyOptions;
    this.OriginalProgram = program;
    this.OriginalFileName = program.Name;

    Models = new APIModels(model, "lm-studio", "http://localhost:1234/v1");
  }
  
  public void UpdateSusp_Stmt(Dictionary<Declaration, HashSet<int>> susp) {
    Susp_Stmt = susp;
  }

  public void Repair() {
    foreach (var (decl,lstBuggyLine) in Susp_Stmt) {
      foreach (var buggyLine in lstBuggyLine) {
        switch (decl) {
          case Method m:
            var count = 0;
            while (count < 3) {
              var code = FileWithBug(buggyLine);
              Candidate(code, buggyLine).Wait();
              if (check!=null) {
                VerifyCheckFile().Wait();
                if (repair) {
                  Console.WriteLine(buggyLine + ": " + check);
                  /*Remove the comment if you need to evaluate the repair*/
                  RepairToFile(Path.ChangeExtension(this.OriginalFileName, ".txt"), check, count, buggyLine);
                  return;
                }
              }
              count++;
            }
            break;
        }
      }
    }
  }

  public async Task Candidate(string code, int buggyLine) {
    try {
      check = null;
      var candLLM = await Models.FixCandidateAsync(code, model);
      var cand = ExtractExpression(candLLM);
      check = cand;
    
      Uri uriPath = new Uri(OriginalProgram.FullName);
      var code_correct = ReplaceLineAndGetContent(uriPath.LocalPath, buggyLine, cand);
    
      StringToFileCheck("check.dfy", code_correct);
    } catch (Exception e) {
      Console.WriteLine(e);
      throw;
    }
    
  }
  
  public string ExtractExpression(string respString) {
    string pattern = @"'''(.*?)'''|```(.*?)```|`(.*?)`|""(.*?)""";
    Regex regex = new Regex(pattern, RegexOptions.Singleline);
    Match match = regex.Match(respString);

    if (match.Success) {
      for (int i = 1; i < match.Groups.Count; i++) {
        if (match.Groups[i].Success && !string.IsNullOrEmpty(match.Groups[i].Value)) {
          return match.Groups[i].Value;
        }
      }
    }
            
    // If no match is found, return the original string
    return respString;
  }
  
  public async Task VerifyCheckFile() {
    repair = false;
    var compilation = MyCliCompilation.Create(DafnyOptions,
      FilePath);
    compilation.Start();
    await compilation.VerifyAllAndPrintSummary();
    var verify = compilation.GetFailMethods();
    if (compilation.GetErrorCount() > 0 || verify.Count > 0) {
      repair = false;
    }
    else if (verify.Count == 0) {
      repair = true;
    } 
  }

  public string FileWithBug(int buggyLine) {
    Uri uriPath = new Uri(OriginalProgram.FullName);
    var code = MarkBuggyLine(uriPath.LocalPath, buggyLine);
    return code;
  }
  
  public void StringToFileCheck(string fileName, string content) {
    string currentDirectory = Directory.GetCurrentDirectory();
    FilePath = Path.Combine(currentDirectory, fileName);
    File.WriteAllText(FilePath, content);
  }
  
  public void RepairToFile(string fileName, string content, int count, int buggyLine) {
    var filePath = Path.Combine(repairDir, fileName);
    var contentFile = string.Concat(count, "\n", buggyLine, "\n\n", content);
    File.WriteAllText(filePath, contentFile);
  }
}