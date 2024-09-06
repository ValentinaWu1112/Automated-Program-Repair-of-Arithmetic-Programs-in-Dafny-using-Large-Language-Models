using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Dafny;

namespace DafnyDriver.APR;

/*
 * FaultLocalization
 * <fields>
 *  <field> DafnyProgram: the original program (having the AST)
 *  <field> FailPrograms: list of members that fail the verification
 *  <field> DictProgram: It's a dictionary where keys are of type Declaration (can be a Method in Dafny)
 *          and values the program's state computed.
 *  <field> Options: the dafny options to run the boogie verification
 *  <field> Susp_Statement: A dictionary that the key represents a member, and the value is a list of suspicious lines
            associated with the buggy line.
 * <field> folderPath: To change the path for evaluating the fault localization
 * </fields>
 *
 * IMPORTANT:
 * 1. Change the path to the correct directory where your Fault_Loc_List are located.
 *    Look for occurrences of 'folderPath = @"Path\To\Fault_Loc_List"' in the file and update them accordingly.
 * 
 * Ensure all references to 'folderPath = @"Path\To\Fault_Loc_List"' are updated to reflect the new path.
 * Uncomment the WriteFaultLocalization()
 * This is crucial to avoid errors when running the program.
 */
public class FaultLocalization {
  public readonly Program DafnyProgram;
  public List<ICanVerify> FailPrograms;
  public Dictionary<Declaration, ProgramStatement> DictProgram;
  public DafnyOptions Options;
  public Dictionary<Declaration, HashSet<int>> Susp_Statement;
  
  public string folderPath = @"Path\To\Fault_Loc_List";

  public FaultLocalization(Program prog, List<ICanVerify> failPrograms, DafnyOptions options) {
    this.DafnyProgram = prog;
    this.DictProgram = new Dictionary<Declaration, ProgramStatement>();
    this.FailPrograms = failPrograms.Distinct().ToList();;
    this.Options = options;
  }

  /*
   * ComputeProgram: For each failed member, compute the fault localization. Currently, only the method member is computed.
   */
  public void ComputeProgram() {
    foreach (var lst in this.FailPrograms) {
      switch (lst) {
        case Method m:
          var ps = ComputeState(m);
          ps.UpdateEntailment();
          ps.NormalizeEntailmentInvariant();
          this.DictProgram.Add(m, ps);
          break;
        default:
          Console.WriteLine("FaultLocalization.cs: " + lst.GetType());
          break;
      }
    }
    this.VerifyProgram();
  }

  /*
   * VerifyProgram: Verify the program after computing the state and generate the entailments
   */
  public void VerifyProgram() {
    var origProg = DafnyProgram.DefaultModuleDef.AccessibleMembers;
    var verifyProgram = new VerifyProgram(origProg, DictProgram, DafnyProgram, Options);
    verifyProgram.VerificationFile();
    verifyProgram.FaultLocalization();
    Susp_Statement = verifyProgram.GetFault_Localization();
    /*Remove the comment if you need to evaluate the fault localization*/
    //WriteFaultLocalization();
  }

  /*
   * WriteFaultLocalization: Print the fault localization
   */
  public void WriteFaultLocalization() {
    try {
      var filename = Path.ChangeExtension(DafnyProgram.Name, ".txt");
      var fullPath = Path.Combine(folderPath, filename);
      using (StreamWriter writer = new StreamWriter(fullPath)) {
        foreach (var (decl, susp) in Susp_Statement) {
          writer.WriteLine(String.Join("; ", susp));
        }
      }
    } catch (Exception e) {
      Console.WriteLine(e);
      throw;
    }
  }

  /*
   * ComputeState: Enter the method body and compute the state for each statement
   */
  public ProgramStatement ComputeState(Method m) {
    var res = new ProgramStatement(m.Req, m.Ens, m.Ins, m.Outs);
    var body = m.Body.Body;
    foreach (var s in body) {
      res.ComputeState(s);
    }
    return res;
  }

  public Dictionary<Declaration, HashSet<int>> GetSusp_Statement() {
    return Susp_Statement;
  }
}