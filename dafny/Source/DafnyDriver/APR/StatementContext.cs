using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dafny;

namespace DafnyDriver.APR;  

enum TypesStatement {
  IF,
  ELIF,
  ELSE,
  LOOP_EXIT,
  LOOP_INDEX,
  DEFAULT
}

/*
 * StatementContext
 *
 * <fields>
 *  <field> StateBefore: The states before executing the statement
 *  <field> StateAfter: The states after executing the statement
 *  <field> Stmt: The statement of the program
 *  <field> Condition: The state after executing the statement
 *  <field> TypeStmt: The type of the statement
 *  <field> Variables: The variables declared throughout the program.
 *  <field> ControlClass: The flag used to control the initialization of the class, specifically with the values defined for the class constructor.
 *  <field> ControlClassExp: The expression representing the class when ControlClass is true
 * </fields>
 */
public class StatementContext {
  /*
   * List<StateCondition> = StateCondition1 /\ StateCondition2 /\ StateCondition3 /\ ....
   * List<List<StateCondtion>> = (StateCondition1 /\ StateCondition2 /\ StateCondition3 /\ ....) \/ (StateCondition4 /\ StateCondition5 /\ StateCondition6 /\ ....)
   * For example: in if-then-else statement, we will have two List<StateCondition>
   */
  public List<List<StateCondition>> StateBefore = new List<List<StateCondition>>();
  public List<List<StateCondition>> StateAfter = new List<List<StateCondition>>();
  public Statement Stmt;
  private TypesStatement TypeStmt = TypesStatement.DEFAULT;
  public List<StateCondition> Condition = new List<StateCondition>();
  public List<Formal> Variables = new List<Formal>();
  private Cloner Cloner = new Cloner();

  public bool ControlClass = false;
  public Expression ControlClassExp;
  
  public StatementContext(IfStmt s, List<List<StateCondition>> stateBefore) {
    this.Stmt = s;
    foreach (var lst in stateBefore) {
      this.StateBefore.Add(lst.ToList());
    }
    this.TypeStmt = TypesStatement.IF;
    this.ComputeStateAfterIfStmt((IfStmt)this.Stmt);
  }
  
  public void ComputeStateAfterIfStmt(IfStmt stmt) {
    foreach (var lst in this.StateBefore) {
      StateAfter.Add(lst.ToList());
    }
    if (stmt.Guard == null) {
      //Probably is the '*' -> Ignore
    } else {
      UpdateStateAfter(new StateCondition(new AttributedExpression(stmt.Guard), this.Stmt));
    }
  }
  
  public StatementContext(UpdateStmt s, List<List<StateCondition>> stateBefore) {
    this.Stmt = s;
    foreach (var lst in stateBefore) {
      this.StateBefore.Add(lst.ToList());
    }
    this.ComputeStateAfterUpdateStmt((UpdateStmt)this.Stmt);
  }
  
  public void ComputeStateAfterUpdateStmt(UpdateStmt stmt) {
    foreach (var lst in this.StateBefore) {
      StateAfter.Add(lst.ToList());
    }
    /*Foreach from the list of expressions on the left: a,b := 0*/
    var childrens = stmt.Children;
    foreach (var child in childrens) {
      switch (child) {
        case AssignStmt tmpChild:
          var lhsAssExpr = tmpChild.Lhs;
          switch (tmpChild.Rhs) {
            case ExprRhs tmpRhsExpr:
              /* Update the value of a variable
               * For example, v:=0
               */
              var rhsExprr = new ExprRhs(Cloner, tmpRhsExpr);
              var expr = new BinaryExpr(this.Stmt.Tok, BinaryExpr.Opcode.Eq, lhsAssExpr, rhsExprr.Expr);
              UpdateStateAfter(expr, update:true);
              UpdateVar(lhsAssExpr);
              break;
            case TypeRhs typeRhs:
              /*
               * Declare new arrays and class
               */
              var nameType = new NameSegment(lhsAssExpr.Tok, lhsAssExpr.ToString(), null);
              var arrayDimension = typeRhs.ArrayDimensions;
              if(arrayDimension != null){ //Array dimension
                for (int j = 0; j < arrayDimension.Count; j++) {
                  var dim = arrayDimension[j];
                  Expression arrayLengthExpr;
                  if (arrayDimension.Count == 1) {
                    arrayLengthExpr = new BinaryExpr(lhsAssExpr.Tok, BinaryExpr.Opcode.Eq, new ExprDotName(lhsAssExpr.Tok, nameType, "Length", null), dim);
                  } else {
                    arrayLengthExpr = new BinaryExpr(lhsAssExpr.Tok, BinaryExpr.Opcode.Eq, new ExprDotName(lhsAssExpr.Tok, nameType, "Length"+j.ToString(), null), dim);
                  }
                  UpdateStateAfter(arrayLengthExpr);
                }
              }
              
              var display = typeRhs.InitDisplay;
              if (display != null) { //array values
                for (int i = 0; i < display.Count; i++) {
                  var ind = display[i];
                  var seqSelE0 = new SeqSelectExpr(ind.Tok, true, nameType, new LiteralExpr(typeRhs.Tok, i), null, null);
                  var tmpBinExp = new BinaryExpr(ind.Tok, BinaryExpr.Opcode.Eq, seqSelE0, ind);

                  UpdateStateAfter(tmpBinExp, update:true);
                }
              }

              var initCall = typeRhs.InitCall;
              if (initCall != null) { //class
                var lExpr = tmpChild.Lhs;
                var ctor = initCall.Method;
                if (ctor.HasPostcondition) {
                  var rhsExpr = ctor.Ens;
                  var argOrig = ctor.Ins;
                  var argSubs = initCall.Args;
                  ControlClass = true;
                  var lstExpr = TransformLst(lExpr, rhsExpr, argOrig, argSubs);
                  foreach (var exp in lstExpr) {
                    var stateC = new StateCondition(new AttributedExpression(exp), this.Stmt);
                    stateC.SetUpdate(true);
                    UpdateStateAfter(stateC);
                    UpdateVar(lExpr);
                  }
                }

                ControlClass = false;
              }
              
              UpdateVar(lhsAssExpr);
              break;
            default:
              Console.WriteLine("StatementContext (ComputeStateAfterUpdateStmt - tmpChild.Rhs): " + tmpChild.Rhs.GetType());
              break;
          }
          
          break;
        case CallStmt tmpChild:
          var lhsCallExpr = tmpChild.Lhs;
          foreach (var lExpr in lhsCallExpr) {
            if (tmpChild.Method.HasPostcondition) {
              var rhsExpr = tmpChild.Method.Ens;
              var argOrig = tmpChild.Method.Ins;
              var argSubs = tmpChild.Args; 
              var lstExpr = TransformLst(lExpr, rhsExpr, argOrig, argSubs);
              foreach (var expr in lstExpr) {
                var stateC = new StateCondition(new AttributedExpression(expr), this.Stmt);
                stateC.SetUpdate(true);
                UpdateStateAfter(stateC);
                UpdateVar(lExpr);
              }
            }
          }

          if(lhsCallExpr.Count <=0 && !(tmpChild.MethodSelect.Obj is StaticReceiverExpr)) {
            var methSel = tmpChild.MethodSelect;
            var methSelObj = tmpChild.MethodSelect.Obj;
            if (tmpChild.Method.HasPostcondition) {
              var rhsExpr = tmpChild.Method.Ens;
              var argOrig = tmpChild.Method.Ins;
              var argSubs = tmpChild.Args;
              ControlClass = true;
              var lstExpr = TransformLst(methSelObj, rhsExpr, argOrig, argSubs);
              foreach (var expr in lstExpr) {
                var stateC = new StateCondition(new AttributedExpression(expr), this.Stmt);
                stateC.SetUpdate(true);
                UpdateStateAfter(stateC);
                UpdateVar(methSelObj);
              }
            }

            ControlClass = false;
          }
          break;
        default:
          Console.WriteLine("StatementContext (ComputeStateAfterUpdateStmt): " + child.GetType());
          break;
      }
    }
    
    if (!(childrens is List<Statement>)) {
      var lhss = stmt.Lhss[0];
      var rhss = stmt.Rhss[0];
      switch (rhss) {
        case ExprRhs tmpRhsExpr:
          var rhsExprr = new ExprRhs(Cloner, tmpRhsExpr);
          var expr = new BinaryExpr(this.Stmt.Tok, BinaryExpr.Opcode.Eq, lhss, rhsExprr.Expr);
          UpdateStateAfter(expr, update:true);
          UpdateVar(lhss);
          break;
        default:
          Console.WriteLine("StatementContext (ComputeStateAfterUpdateStmt - tmpChild.Rhs): " + rhss.GetType());
          break;
         
      }
    }
  }

  public void UpdateVar(Expression lExpr) {
    /*Update variables defined by the user*/
    if(!ControlClass) {
      Variables.Add(new Formal(lExpr.Tok, lExpr.ToString(), lExpr.Type, true, false, null));
    } else {
      lExpr = ControlClassExp;
    }
    
    /*
     * This for loop is for cases:
     * s:= 1+2
     * s:= s+1
     * we need to update to s = 1+2+1, because the verifier does not accept two declaration for the same variable
     */
    for (int i = 0; i < this.StateAfter.Count; i++) {
      var ls = this.StateAfter[i];
      for (int j = 0; j < ls.Count - 1; j++) {
        var l = ls[j];
        if (l.Update) {
          switch (l.Condition.E) {
            case BinaryExpr tmpBinExpr:
              if (tmpBinExpr.E0.ToString().Equals(lExpr.ToString())) {  
                for (int k = j + 1; k < ls.Count - 1; k++) {
                  if (ls[k].Update) {
                    ls[k].Condition = new AttributedExpression(Convert(ls[k].Condition.E, tmpBinExpr.E0.ToString(), tmpBinExpr.E1));
                  }
                }
                ls.Remove(l);
                j--;
                
              }
              if (tmpBinExpr.E1.ToString().Equals(lExpr.ToString())) { 
                for (int k = j + 1; k < ls.Count - 1; k++) {
                  if (ls[k].Update) {
                    ls[k].Condition = new AttributedExpression(Convert(ls[k].Condition.E, tmpBinExpr.E1.ToString(), tmpBinExpr.E0));
                  }
                }
                ls.Remove(l);
                j--;
                
              }
              break;
            default:
              Console.WriteLine("StatementContext (UpdateVar): " + l.Condition.E.GetType());
              break;
          }
        }
      }
    }
  }

  /*
   * Auxiliares functions to update the new value for a variable:
   * check if the expression matches argOrig, and if so, replace it with argSubs.
   */
  public List<Expression> TransformLst(Expression lExpr, List<AttributedExpression> exp, List<Formal> argOrig, List<Expression> argSubs) {
    var ret = new List<Expression>();
    foreach (var attExpr in exp) {
      switch (attExpr.E) {
        case BinaryExpr binExpr:
          var resExpr = new BinaryExpr(new Cloner(), binExpr);
          Expression tmpResExpr = resExpr;
          for (int i = 0; i < argOrig.Count; i++) {
            tmpResExpr = Convert(tmpResExpr, argOrig[i].Name, argSubs[i]);
          }
          resExpr = (BinaryExpr)tmpResExpr;
          if(resExpr.E1.ToString().Equals(binExpr.E1.ToString()) || resExpr.E1.ToString().Equals(binExpr.E0.ToString())) {
            if (ControlClass) {
              var tmpExp1 = new ExprDotName(resExpr.E1.Tok, lExpr, resExpr.E1.ToString(), null);
              ControlClassExp = tmpExp1;
              resExpr.E1 = tmpExp1;
            }else {
              resExpr.E1 = lExpr;
            }
          } else {
            if (ControlClass) {
              var tmpExp0 = new ExprDotName(resExpr.E0.Tok, lExpr, resExpr.E0.ToString(), null);
              ControlClassExp = tmpExp0;
              resExpr.E0 = tmpExp0;
            } else {
              resExpr.E0 = lExpr;
            }
          }
          ret.Add(resExpr);
          break;
      }
      
    }

    return ret;
  }

  public Expression Convert(Expression exp, String argOrig, Expression argSubs) {
    var ret = exp;
    switch (exp) {
      case BinaryExpr binaryExpr:
        var binE0 = Convert(binaryExpr.E0, argOrig, argSubs);
        var binE1 = Convert(binaryExpr.E1, argOrig, argSubs);
        ret = new BinaryExpr(binaryExpr.Tok, binaryExpr.Op, binE0, binE1);
        break;
      case ParensExpression parensExpression:
        var parE = Convert(parensExpression.E, argOrig, argSubs);
        ret = new ParensExpression(parensExpression.Tok, parE);
        break;
      case NameSegment nameSegment:
        if (nameSegment.Name == argOrig) {
          ret = argSubs;
        }
        break;
      case ApplySuffix applySuffix:
        var appLhs = Convert(applySuffix.Lhs, argOrig, argSubs);
        var actBindLst = new List<ActualBinding>();
        foreach (var actualBinding in applySuffix.Bindings.ArgumentBindings) {
          var act = Convert(actualBinding.Actual, argOrig, argSubs);
          actBindLst.Add(new ActualBinding(actualBinding.FormalParameterName, act));
        }
        ret = new ApplySuffix(applySuffix.Tok, applySuffix.AtTok, appLhs, actBindLst, applySuffix.CloseParen);
        break;
      case SeqSelectExpr seqSelectExpr:
        if (seqSelectExpr.ToString() == argOrig) {
          ret = argSubs;
        }
        break;
      case IdentifierExpr identifierExpr:
        if (identifierExpr.Name == argOrig) {
          ret = argSubs;
        }
        break;
      case ExprDotName exprDotName:
        if (exprDotName.ToString().Equals(argOrig)) {
          ret = argSubs;
        }
        break;
      default:
        Console.WriteLine("StatementContext (Convert(Expression)): " + exp.GetType());
        break;
    }
    return ret;
  }
  
  public StatementContext(VarDeclStmt s) {
    this.Stmt = s;
  }
  
  public StatementContext(ForLoopStmt s, List<List<StateCondition>> stateBefore) {
    this.Stmt = s;
    foreach (var lst in stateBefore) {
      this.StateBefore.Add(lst.ToList());
    }
    this.ComputeStateAfterForLoopStmt(s);
  }

  public void ComputeStateAfterForLoopStmt(ForLoopStmt stmt) {
    foreach (var lst in this.StateBefore) {
      var tmpLst = new List<StateCondition>(lst);
      tmpLst.RemoveAll(x => x.StmtState != StmtStates.REQUIRE);
      StateAfter.Add(tmpLst.ToList());
    }

    var e0 = new NameSegment(stmt.LoopIndex.Tok, stmt.LoopIndex.Name, null);
    Expression expr;
    if(stmt.GoingUp) {
      expr = new BinaryExpr(stmt.Tok, BinaryExpr.Opcode.Lt, e0, stmt.End);
    } else {
      expr = new BinaryExpr(stmt.Tok, BinaryExpr.Opcode.Gt, e0, stmt.End);
    }
    UpdateStateAfter(new StateCondition(new AttributedExpression(expr), this.Stmt));
  }

  public StatementContext(ForLoopStmt s, BinaryExpr index, List<List<StateCondition>> stateBefore, bool newVar) {
    this.Stmt = s;
    foreach (var lst in stateBefore) {
      this.StateBefore.Add(lst.ToList());
    }

    this.TypeStmt = TypesStatement.LOOP_INDEX;
    this.ComputeStateAfterForLoopIndexStmt(index, newVar);
  }

  public void ComputeStateAfterForLoopIndexStmt(BinaryExpr index, bool newVar) {
    foreach (var lst in this.StateBefore) {
      StateAfter.Add(lst.ToList());
    }

    if (newVar) {
      Variables.Add(new Formal(index.E0.Tok, index.E0.ToString(), index.E0.Type, true, false, null));
    }
    UpdateStateAfter(new StateCondition(new AttributedExpression(index), this.Stmt));
  }
  public StatementContext(WhileStmt s, List<List<StateCondition>> stateBefore) {
    this.Stmt = s;
    foreach (var lst in stateBefore) {
      this.StateBefore.Add(lst.ToList());
    }

    this.ComputeStateAfterWhileStmt((WhileStmt)this.Stmt);
  }
  
  public void ComputeStateAfterWhileStmt(WhileStmt stmt) {
    foreach (var lst in this.StateBefore) {
      var tmpLst = new List<StateCondition>(lst);
      tmpLst.RemoveAll(x => x.StmtState != StmtStates.REQUIRE);
      StateAfter.Add(tmpLst.ToList());
    }

    if (stmt.Guard == null) {
      
    }else {
      UpdateStateAfter(new StateCondition(new AttributedExpression(stmt.Guard), this.Stmt));
    }
  }
  
  
  public StatementContext(ReturnStmt s) {
    this.Stmt = s;
  }

  public StatementContext(BreakStmt s, List<List<StateCondition>> stateBefore) {
    this.Stmt = s;
    foreach (var lst in stateBefore) {
      this.StateBefore.Add(lst.ToList());
    }

    this.ComputeStateAfterBreakStmt((BreakStmt)this.Stmt);
  }

  public void ComputeStateAfterBreakStmt(BreakStmt stmt) {
    foreach (var lst in this.StateBefore) {
      StateAfter.Add(lst.ToList());
    }

    var sc = new StateCondition("BREAK");
    sc.SetControlFlow(true);
    UpdateStateAfter(sc);
  }

  public StatementContext(AssignSuchThatStmt s, List<List<StateCondition>> stateBefore) {
    this.Stmt = s;
    foreach (var lst in stateBefore) {
      this.StateBefore.Add(lst.ToList());
    }

    this.ComputeStateAfterAssignSuchThatStmt(s);
  }

  public void ComputeStateAfterAssignSuchThatStmt(AssignSuchThatStmt stmt) {
    foreach (var lst in this.StateBefore) {
      StateAfter.Add(lst.ToList());
    }

    var sc = new StateCondition(new AttributedExpression(stmt.Expr), stmt);
    UpdateStateAfter(sc);
  }

  public StatementContext(string typeStmt, List<List<StateCondition>> stateBefore, Expression c, Statement s) {
    this.Stmt = s;
    foreach (var lst in stateBefore) {
      this.StateBefore.Add(lst.ToList());
    }
    switch (typeStmt) {
      case "ELSE":
        ComputeStateAfterDefault();
        this.TypeStmt = TypesStatement.ELSE;
        var sc = new StateCondition(new AttributedExpression(c), this.Stmt);
        sc.SetStmtState("ELSE");
        UpdateStateAfter(sc);
        break;
      case "ELIF":
        ComputeStateAfterDefault();
        UpdateStateAfter(new StateCondition(new AttributedExpression(c), this.Stmt));
        this.TypeStmt = TypesStatement.ELIF;
        break;
      case "LOOP_EXIT":
        /*c is Guard of the loop*/
        ComputeStateAfterLoopExit();
        var notGuard = new UnaryOpExpr(s.Tok, UnaryOpExpr.Opcode.Not, c);
        UpdateStateAfter(new StateCondition(new AttributedExpression(notGuard), this.Stmt));
        this.TypeStmt = TypesStatement.LOOP_EXIT;
        break;
    }
  }

  public void ComputeStateAfterDefault() {
    foreach (var lst in this.StateBefore) {
      StateAfter.Add(lst.ToList());
    }
  }

  /*
   * ComputeStateAfterLoopExit: This function will remove all states within the loop body,
   * as the loop invariants are considered the result once the loop terminates.
   */
  public void ComputeStateAfterLoopExit() {
    foreach (var lst in this.StateBefore) {
      var tmpLst = new List<StateCondition>(lst);
      tmpLst.RemoveAll(x => x.StmtState != StmtStates.REQUIRE);
      StateAfter.Add(tmpLst.ToList());
    } 
  }
  
  public void UpdateStateAfter(StateCondition sc) {
    this.Condition.Add(sc);
    foreach (var stateAnd in this.StateAfter) {
      if (stateAnd.Count == 0) {
        stateAnd.Add(sc);
      } else {
        var checkState = stateAnd[^1];
        if(checkState.StmtState != StmtStates.RETURN && !(checkState.StmtState == StmtStates.BREAK && checkState.ControlFlow)) {
          stateAnd.Add(sc);
        }
      }
    }
  }

  public void UpdateStateAfter(Expression e, bool isVerified=false, bool update=false) {
    switch (e) {
      case BinaryExpr binExpr:
        foreach (var stateAnd in this.StateAfter) {
          var newBinExpr = new BinaryExpr(Cloner, binExpr);
          var sc = new StateCondition(new AttributedExpression(newBinExpr), isVerified, update, this.Stmt);
          if (stateAnd.Count == 0) {
            stateAnd.Add(sc);
          } else {
            var checkState = stateAnd[^1];
            if(checkState.StmtState != StmtStates.RETURN && !(checkState.StmtState == StmtStates.BREAK && checkState.ControlFlow)) {
              stateAnd.Add(sc);
            }
          }
          
          this.Condition.Add(sc);
        }
        /*if(this.StateAfter.Count == 0) */
        break;
    }
  }
  public List<List<StateCondition>> GetStateAfter() {
    return StateAfter;
  }

  public List<Formal> GetVariables() {
    return Variables;
  }
  
}