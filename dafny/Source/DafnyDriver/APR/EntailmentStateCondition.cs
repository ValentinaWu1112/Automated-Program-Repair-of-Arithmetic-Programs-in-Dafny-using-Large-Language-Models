using System.Collections.Generic;
using System.Linq;
using Microsoft.Dafny;

namespace DafnyDriver.APR;

/*
 * EntailmentStateCondition
 *
 * <summary>
 *  Representation of entailment of the class StateCondition
 * </summary
 * 
 * <fields>
 *  <field> LHSCondition: conditions of the left-hand side to entail the RHSCondition
 *  <field> RHSCondition: consitions of the right-hand side
 *  <field> VerificationStmt: To control the verification statements (assert, assume, calc, etc.)
 *  <field> IsInvariant: if is a EntailmentStateCondition to verify invariants
 *  <field> IsInvariant: if is a entailment to verify invariants
 * </fields>
 */
public class EntailmentStateCondition {
  public List<StateCondition> LHSCondition;
  public List<StateCondition> RHSCondition;
  public List<Statement> VerificationStmt = new List<Statement>();
  public bool IsInvariant;

  public EntailmentStateCondition() {
    this.LHSCondition = new List<StateCondition>();
    this.RHSCondition = new List<StateCondition>();
  }

  public void AddVerificationStmt(Statement s) {
    if(!VerificationStmt.Contains(s)) {
      VerificationStmt.Add(s);
    }
  }
  public void AddLHSCondition(StateCondition e) {
    this.LHSCondition.Add(e);
  }
  
  public void AddRHSCondition(StateCondition e) {
    this.RHSCondition.Add(e);
  }

  public void SetRHSCondition(List<StateCondition> l) {
    RHSCondition = l.ToList();
  }

  public void SetInvariant(bool i) {
    this.IsInvariant = i;
  }
}