using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Dafny;
using Python.Runtime;

namespace DafnyDriver.APR;

/*
 * APR
 * <fields>
 *  <field> OriginalProgram: Original program of the dafny
 *  <field> DafnyOptions: Options defined to sent to the verifier
 *  <field> FailMethods: The list of failed members (Method, Lemma, Function) of the original program
 *  <field> FaultLocalization: The class to implement the fault localization
 *  <field> Repair: The call to implement the repair with the Mistral LLM
*  <field> Susp_Stmt: A dictionary that the key represents a member, and the value is a list of suspicious lines
          associated with the buggy line.
 * </fields>
 *
 * IMPORTANT:
 * Please note that the type RepairGPT4o in public RepairGPT4o Repair; can be changed if necessary. Ensure that when making this change, 
 * the new type is initialized correctly during the instantiation process to avoid issues.
 */

public class APR {
  public Program OriginalProgram;
  public DafnyOptions DafnyOptions;
  public List<ICanVerify> FailMethods;
  public FaultLocalization FaultLocalization;
  
  /*Customize the model as needed, and ensure proper initialization*/
  public RepairGPT4o Repair;
  public Dictionary<Declaration, HashSet<int>> Susp_Stmt;
  
  public APR(Program originalProgram, List<ICanVerify> failMethods, DafnyOptions dafnyOptions) {
    this.OriginalProgram = originalProgram;
    this.DafnyOptions = dafnyOptions;
    this.FailMethods = failMethods;
    FaultLocalization = new FaultLocalization(this.OriginalProgram, this.FailMethods, this.DafnyOptions);
    Repair = new RepairGPT4o(dafnyOptions, originalProgram); //Here the initialization of model
    this.Execute();
  }

  public void Execute() {
    ExecuteFaultLocalization();
    ExecuteRepair();
  }

  public void ExecuteFaultLocalization() {
    FaultLocalization.ComputeProgram();
    Susp_Stmt = FaultLocalization.GetSusp_Statement();
    Repair.UpdateSusp_Stmt(Susp_Stmt);
  }

  public void ExecuteRepair() {
    Repair.Repair();
  }
}