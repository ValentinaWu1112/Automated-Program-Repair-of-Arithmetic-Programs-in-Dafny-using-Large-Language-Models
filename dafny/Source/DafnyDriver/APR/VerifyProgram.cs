using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Dafny;

using static DafnyDriver.APR.UtilsString;

namespace DafnyDriver.APR;

/*
 * VerifyProgram
 *
 * <fields>
 *  <field> OriginalProgram: the members of the original programs
 *  <field> DictProgram: It's a dictionary where keys are of type Declaration (can be a Method in Dafny)
 *          and values the program's state computed.
 *  <field> FailMethods: A list of members that failed the verification, used to address fault localization
 *  <field> FailChecks: Dictionary that the key is the members fails and the value correspond to the FailMethods,
                        We can handle multiple failures in the members in the original program.
 *  <field> FilePath: The path of the dafny file (created to verify the entailments)
 *  <field> Options: The dafny options to run the boogie verification
 *  <field> Fault_Localization: The list of suspicious lines related to the bug for each failed member.
 *  <field> DafnyProgram: the original program (having the AST)
 */
public class VerifyProgram {
  public Dictionary<Declaration, AccessibleMember> OriginalProgram;
  public Dictionary<Declaration, ProgramStatement> DictProgram;
  public List<ICanVerify> FailMethods;
  public Dictionary<Declaration, List<ICanVerify>> FailChecks = new Dictionary<Declaration, List<ICanVerify>>();
  public string FilePath;
  public DafnyOptions Options;
  public Dictionary<Declaration, HashSet<int>> Fault_Localization;
  public Program DafnyProgram;

  public VerifyProgram(Dictionary<Declaration, AccessibleMember> originalProg, Dictionary<Declaration, ProgramStatement> dictProgram, Program p, DafnyOptions options) {
    this.OriginalProgram = originalProg;
    this.DictProgram = dictProgram;
    this.FailMethods = new List<ICanVerify>();
    this.Options = options;
    this.DafnyProgram = p;
    Fault_Localization = new Dictionary<Declaration, HashSet<int>>();
  }

  public async Task VerifyFile() {
    var compilation = MyCliCompilation.Create(Options, FilePath);
    compilation.Start();
    await compilation.VerifyAllAndPrintSummary();
    var resultFailMethods = compilation.GetFailMethods();
    this.FailMethods = resultFailMethods;
  }

  public Dictionary<Declaration, HashSet<int>> GetFault_Localization() {
    return Fault_Localization;
  }

  /*
   * FaultLocalization: For each failed member, compute the list of suspicious lines after verifying the entailments.
   */
  public void FaultLocalization() {
    foreach (var (decl,progStmt) in DictProgram) {
      ComputeResultEntailment(progStmt, FailChecks[decl]);
      var suspStmt = SuspiciousStatementLineLst(progStmt);
      Fault_Localization.Add(decl, suspStmt);
    }
  }

  public HashSet<int> SuspiciousStatementLineLst(ProgramStatement progStmt) {
    var ret = new HashSet<int>();
    var ents = progStmt.GetEntailmentStateConditions();
    foreach (var ent in ents) {
      var stateCond = ent.LHSCondition;
      foreach (var cond in stateCond) {
        if (!cond.GetIsVerified()) {
          var tmp = cond.GetStmt();
          if (tmp == null) {
            continue;
          }
          ret.Add(tmp.RangeToken.line);
        }
      }
    }
    return ret;
  }

  /*
   * ComputeResultEntailment: The order of the entailments in the list corresponds to the naming convention used in the lemmas.
   * For instance, the entailment at index 1 is named check_1, this implies that the lemma is associated with that number.
   * If a lemma’s name does not include the number, it indicates that the entailment has been verified and
   * the associated statements are not considered the buggy line.
   *
   * Conversely, if a lemma’s name contains the number, the statements are suspected to be the buggy line,
   * unless they are pre-conditions or belong to an else condition.
   * In the case of an else condition, verify the associated if-condition; if the if-condition is verified,
   * then it is not the buggy line.
   */
  public void ComputeResultEntailment(ProgramStatement progStmt, List<ICanVerify> failMethod) {
    var ents = progStmt.GetEntailmentStateConditions();
    var failMeth = failMethod.Distinct().ToList();
    failMeth.RemoveAll(obj => !obj.FullDafnyName.Contains("check", StringComparison.OrdinalIgnoreCase));
    var failMethOrd = failMeth.OrderBy(obj => 
    {
      int numero = int.Parse(obj.FullDafnyName.Split('_')[1]);
      return numero;
    }).ToList();
     
     if (failMethOrd.Count <= 0) { 
       return;
     }
     var index_failMethods = 0;
     for (int i = 0; i < ents.Count; i++) {
       if (index_failMethods < failMethOrd.Count) {
         if (failMethOrd[index_failMethods].FullDafnyName.Contains(i.ToString())) {
           var lhs = ents[i].LHSCondition;
           foreach (var l in lhs) {
             var state = l.StmtState;
             switch (state) {
               case StmtStates.REQUIRE:
                 l.SetIsVerified(true);
                 break;
               case StmtStates.ELSE:
                 var stmt = l.stmt;
                 var stmtCont = progStmt.GetProgramStmt().Find(x => x.Stmt.Equals(stmt));
                 var stCond = stmtCont.Condition;
                 var isVer = stCond[0].GetIsVerified();
                 l.SetIsVerified(isVer);
                 break;
             }
           }

           index_failMethods++;
         } 
         else {
           var lhs = ents[i].LHSCondition;
           foreach (var l in lhs) {
             l.SetIsVerified(true);
           }
         }
       }
       else {
         var lhs = ents[i].LHSCondition;
         foreach (var l in lhs) {
           l.SetIsVerified(true);
         }
       }
     }
  }

  public void VerificationFile() {
    foreach (var (decl, progStmt) in DictProgram) {
      string programFile = String.Empty;
      programFile = string.Concat(programFile, EntailmentsToString(decl, progStmt));
      Uri uriPath = new Uri(DafnyProgram.FullName);
      var code = InsertStringAtFile(uriPath.LocalPath, decl.EndToken.line, decl.EndToken.col, programFile);
      StringToFile("check.dfy", code);
      VerifyFile().Wait();
      this.FailChecks.Add(decl, this.FailMethods);
    }
  }

  public string EntailmentsToString(Declaration decl, ProgramStatement progStmt) {
    string programFile = String.Empty;
    var ents = progStmt.GetEntailments();
    for (int i =0; i<ents.Count; i++) {
      var ent = ents[i];
      var ins = progStmt.GetVariables();
      switch (decl) {
        case Method m:
          programFile = string.Concat(programFile, "lemma ");
          var tmpName = "check_" + i + "(";
          programFile = string.Concat(programFile, tmpName);
          var inp = InputToString(ins);
          programFile = string.Concat(programFile, string.Concat(inp, ")\n"));
          programFile = string.Concat(programFile, UtilsString.SpecToString(ent.LHSInvariantCondition, "requires "),
            UtilsString.SpecToString(ent.LHSCondition, "requires "));
          programFile = string.Concat(programFile, UtilsString.SpecToString(ent.RHSCondition, "ensures "));
          var verStmt = ent.GetVerificationStmt();
          programFile = string.Concat(programFile, "{\n", UtilsString.VerificationStmtToString(verStmt), "\n}\n\n");
          break;
      }
    }
    return programFile;
  }

  public string InputToString(List<Formal> ins) {
    string ret = String.Empty;
    for (int j=0; j<ins.Count; j++) {
      var i = ins[j];
      ret = string.Concat(ret, string.Concat(i.Name, ":", i.Type.ToString()));
      if (j < ins.Count - 1) {
        ret = string.Concat(ret, ",");
      }
    }

    return ret;
  }

  public void StringToFile(string fileName, string content) {
    string currentDirectory = Directory.GetCurrentDirectory();
    FilePath = Path.Combine(currentDirectory, fileName);
    File.WriteAllText(FilePath, content);
  }
}