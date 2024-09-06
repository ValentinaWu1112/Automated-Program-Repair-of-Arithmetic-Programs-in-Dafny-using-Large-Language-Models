using System;
using System.Collections.Generic;
using Microsoft.Dafny;

namespace DafnyDriver.APR;

public enum StmtStates {
  BREAK,
  CONTINUE,
  RETURN, 
  REQUIRE,
  ENSURE,
  ELSE,
  DEFAULT, 
  INVARIANT
}

/*
 * StateCondition
 *
 * <fields>
 *  <field> Condition: the condition of the state in the statement
 *  <field> stmt: the statement related to the condition
 *  <field> IsVerified: used in the fault localization to classify whether the statement verifies the entailment.
 *  <field> Update: to identify  if a statement is a type of update
 *  <field> ControlFlow: to control the flow of execution of a break or continue
 *  <field> StmtState: to identify the type of the statement
 * </fields>
 */

public class StateCondition {
  public AttributedExpression Condition;
  public Statement stmt;
  public bool IsVerified;
  public bool Update = false;
  public bool ControlFlow = false;
  public StmtStates StmtState = StmtStates.DEFAULT;

  public StateCondition(AttributedExpression c, Statement s) {
    this.Condition = c;
    this.stmt = s;
    this.IsVerified = false;
    Update = false;
  }

  public StateCondition(AttributedExpression c) {
    this.Condition = c;
    this.IsVerified = false;
    Update = false;
  }

  public StateCondition(String ss) {
    SetStmtState(ss);
  }

  public void SetStmtState(String ss) {
    switch (ss) {
      case "BREAK":
        this.StmtState = StmtStates.BREAK;
        break;
      case "CONTINUE":
        this.StmtState = StmtStates.CONTINUE;
        break;
      case "RETURN":
        this.StmtState = StmtStates.RETURN;
        break;
      case "REQUIRE":
        this.StmtState = StmtStates.REQUIRE;
        break;
      case "ELSE":
        this.StmtState = StmtStates.ELSE;
        break;
      case "INVARIANT":
        this.StmtState = StmtStates.INVARIANT;
        break;
    }
  }

  public StateCondition(AttributedExpression c, bool isVerified, bool update, Statement stmt) {
    this.Condition = c;
    this.IsVerified = isVerified;
    this.Update = update;
    this.stmt = stmt;
  }

  public void SetUpdate(bool upt) {
    this.Update = upt;
  }

  public void SetControlFlow(bool cf) {
    this.ControlFlow = cf;
  }

  public void SetIsVerified(bool ver) {
    this.IsVerified = ver;
  }

  public bool GetIsVerified() {
    return this.IsVerified;
  }

  public Statement GetStmt() {
    return this.stmt;
  }
}