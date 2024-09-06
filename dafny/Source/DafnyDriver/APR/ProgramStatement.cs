using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Dafny;
using Expression = Microsoft.Dafny.Expression;
using Type = Microsoft.Dafny.Type;

namespace DafnyDriver.APR;

/*
 * ProgramStatement
 *
 * <fields>
 *  <field> ProgramStmt: List of details of each statement in the program
 *  <field> Entailments: List of entailments to prove for fault localization
 *  <field> EntailmentStateConditions: List pf entailments of type 'StateCondition' to prove for fault localization
 *  <field> Req: The constraints of the pre-conditions (requires)
 *  <field> Ens: The constraints of the post-condition (ensures)
 *  <field> StateBefore: Variable to control the current state
 *  <field> Variables: The variables declared throughout the program.
 *  <field> ControlFlow: To control the flow of execution of break and continue
 *  <field> VerificationStmt: To manage the flow of verification statements (e.g., assume, assert, calc, etc.),
 * </fields>
 */
public class ProgramStatement {
  public List<StatementContext> ProgramStmt;
  public List<Entailment> Entailments = new List<Entailment>();
  public List<EntailmentStateCondition> EntailmentStateConditions = new List<EntailmentStateCondition>();
  public List<AttributedExpression> Req;
  public List<AttributedExpression> Ens;
  public List<List<StateCondition>> StateBefore = new List<List<StateCondition>>();
  public List<Formal> Variables;
  public StatementContext ControlFlow;
  public Dictionary<Statement, List<List<StateCondition>>> VerificationStmt = new Dictionary<Statement, List<List<StateCondition>>>();

  public ProgramStatement(List<AttributedExpression> Req, List<AttributedExpression> Ens, List<Formal> Ins, List<Formal> Outs) {
    this.ProgramStmt = new List<StatementContext>();
    this.Req = Req;
    this.Ens = Ens;
    Variables = new List<Formal>(Ins);
    Variables.AddRange(Outs);
    var tmpLst = new List<StateCondition>();
    foreach (var req in this.Req) {
      var tmpState = new StateCondition(req);
      tmpState.SetStmtState("REQUIRE");
      tmpState.SetIsVerified(true);
      tmpLst.Add(tmpState);
    }
    this.StateBefore.Add(tmpLst);
  }

  public List<StatementContext> GetProgramStmt() {
    return this.ProgramStmt;
  }

  public void AddStatementContext(StatementContext sc) {
    this.ProgramStmt.Add(sc);
  }
  
  public void UpdateStateBefore(StatementContext sc) {
    this.StateBefore.Clear();
    this.StateBefore = sc.GetStateAfter().ToList();
  }
  
  public void UpdateStateBefore(List<List<StateCondition>> l1, List<List<StateCondition>> l2) {
    StateBefore.Clear();
    StateBefore = l1.ToList();
    StateBefore.AddRange(l2);
  }

  public void UpdateStateBefore(List<List<StateCondition>> l1) {
    StateBefore.Clear();
    StateBefore = l1.ToList();
  }

  public void UpdateStateBefore(List<AttributedExpression> inv) {
    Cloner cloner = new Cloner();
    foreach (var attE in inv) {
      switch (attE.E) {
        case BinaryExpr binaryExpr:
          var binExp = new BinaryExpr(cloner, binaryExpr);
          var binStateCond = new StateCondition(new AttributedExpression(binExp));
          binStateCond.SetStmtState("INVARIANT");
          if (binExp.Op == BinaryExpr.Opcode.Eq) {
            binStateCond.Update = true;
          }
          AddStateBefore(binStateCond);
          break;
        case ChainingExpression chainingExpression:
          var chainExp = new ChainingExpression(cloner, chainingExpression);
          var chainStateCond = new StateCondition(new AttributedExpression(chainExp));
          chainStateCond.SetStmtState("INVARIANT");
          AddStateBefore(chainStateCond);
          break;
        default:
          Console.WriteLine("ProgramStatement.cs (UpdateStateBefore(List<AttributedExpression> inv)): " + attE.E.GetType);
          break;
      }
    }
  }

  public void AddStateBefore(StateCondition sc) {
    foreach (var lstStCond in StateBefore) {
      lstStCond.Add(sc);
    }
  }

  /*
   * NormalizeEntailmentInvariant: Update the variables to ensure consistency within the verifier.
   * Ensure that there are no conflicting requirements for the same variable, such as requires s == 1 && s == 2.
   */
  public void NormalizeEntailmentInvariant() {
    var flagConvert = false;
    foreach (var ent in Entailments) {
      if (ent.IsInvariant) {
        var lhsCond = ent.LHSCondition;
        lhsCond.Reverse();
        for (int i = 0; i < lhsCond.Count; i++) {
          var atrExp = lhsCond[i];
          if(atrExp == null) {
            continue;
          }
          switch (atrExp.E) {
            case BinaryExpr binaryExpr:
              if (binaryExpr.Op == BinaryExpr.Opcode.Eq) {
                var lhsName = binaryExpr.E0;
                var tmpLst = ent.RHSCondition;
                for (int j = 0; j < tmpLst.Count; j++) {
                  var rhsExp = tmpLst[j].E;
                  if (rhsExp.ToString().Contains(lhsName.ToString())) {
                    var tmpRhs = Convert(rhsExp, lhsName.ToString(), binaryExpr.E1);
                    tmpLst[j] = new AttributedExpression(tmpRhs);
                    flagConvert = true;
                  }
                }
                if(flagConvert) { 
                  lhsCond.Remove(atrExp);
                  flagConvert = false;
                  i--;
                }
              }
              break;
          }
        }

        lhsCond.Reverse();
      }
    }
  }
  
  public Expression Convert(Expression originalExpr, string varName, Expression subsExpr) {
    Expression ret = originalExpr;
    switch (originalExpr) {
      case NameSegment nameSegment:
        if (nameSegment.Name == varName) {
          ret = new ParensExpression(originalExpr.Tok, subsExpr);

        }
        break;
      case LiteralExpr:
        break;
      case ThisExpr:
        break;
      case FreshExpr freshExpr:
        var freshE = Convert(freshExpr.E, varName, subsExpr);
        ret = new FreshExpr(freshExpr.Tok, freshE, freshExpr.At);
        break;
      case BinaryExpr binaryExpr:
        var binE0 = Convert(binaryExpr.E0, varName, subsExpr);
        var binE1 = Convert(binaryExpr.E1, varName, subsExpr);
        ret = new BinaryExpr(binaryExpr.Tok, binaryExpr.Op, binE0, binE1);
        break;
      case ChainingExpression chainingExpression:
        var chainOp = new List<Expression>();
        foreach (var expOp in chainingExpression.Operands) {
          var tmpOp = Convert(expOp, varName, subsExpr);
          chainOp.Add(tmpOp);
        }

        var chainPrefLim = new List<Expression>();
        foreach (var expPrefLim in chainingExpression.PrefixLimits) {
          var tmpPref = Convert(expPrefLim, varName, subsExpr);
          chainPrefLim.Add(tmpPref);
        }

        ret = new ChainingExpression(chainingExpression.Tok, chainOp, chainingExpression.Operators,
          chainingExpression.OperatorLocs, chainPrefLim);
        break;
      case ParensExpression parensExpression:
        var parE = Convert(parensExpression.E, varName, subsExpr);
        ret = new ParensExpression(parensExpression.tok, parE);
        break;
      case UnaryOpExpr unaryOpExpr:
        var unaE = Convert(unaryOpExpr.E, varName, subsExpr);
        ret = new UnaryOpExpr(unaryOpExpr.tok, unaryOpExpr.Op, unaE);
        break;
      case SeqSelectExpr seqSelectExpr:
        var seqE0 = Convert(seqSelectExpr.E0, varName, subsExpr);
        var seqE1 = Convert(seqSelectExpr.E1, varName, subsExpr);
        ret = new SeqSelectExpr(seqSelectExpr.tok, seqSelectExpr.SelectOne, seqSelectExpr.Seq, seqE0, seqE1,
          seqSelectExpr.CloseParen);
        break;
      case ForallExpr forallExpr:
        var forRange = Convert(forallExpr.Range, varName, subsExpr);
        var forTerm = Convert(forallExpr.Term, varName, subsExpr);
        ret = new ForallExpr(forallExpr.tok, forallExpr.RangeToken, forallExpr.BoundVars, forRange, forTerm,
          forallExpr.Attributes);
        break;
      case ExistsExpr existsExpr:
        var exisRange = Convert(existsExpr.Range, varName, subsExpr);
        var exisTerm = Convert(existsExpr.Term, varName, subsExpr);
        ret = new ExistsExpr(existsExpr.tok, existsExpr.RangeToken, existsExpr.BoundVars, exisRange, exisTerm,
          existsExpr.Attributes);
        break;
      case TypeTestExpr typeTestExpr:
        var typeTestE = Convert(typeTestExpr.E, varName, subsExpr);
        ret = new TypeTestExpr(typeTestExpr.Tok, typeTestE, typeTestExpr.ToType);
        break;
      case ConversionExpr conversionExpr:
        var converE = Convert(conversionExpr.E, varName, subsExpr);
        ret = new ConversionExpr(conversionExpr.Tok, converE, conversionExpr.ToType, conversionExpr.messagePrefix);
        break;
      case ApplySuffix applySuffix:
        var applySufArgs = new List<Expression>();
        if(applySuffix.Args != null) {
          foreach (var exp in applySuffix.Args) {
            applySufArgs.Add(Convert(exp, varName, subsExpr));
          }
        }

        else if (applySuffix.Bindings != null) {
          foreach (var actual in applySuffix.Bindings.ArgumentBindings) {
            applySufArgs.Add(Convert(actual.Actual, varName, subsExpr));
          }
        }

        ret = new ApplyExpr(applySuffix.Tok, applySuffix.Lhs, applySufArgs, applySuffix.CloseParen);
        
        break;
      case OldExpr oldExpr:
        var oldE = Convert(oldExpr.E, varName, subsExpr);
        ret = new OldExpr(oldExpr.Tok, oldE, oldExpr.At);
        break;
        
      default:
        if (originalExpr == null) {
          Console.WriteLine("ProgramStatement (Convert): Null");
        } else {
          Console.WriteLine("ProgramStatement (Convert): " + originalExpr.GetType());
        }
        break;
        
    }

    return ret;
  }
  
  /*
   * @method ComputeState
   * Compute the State of different types of statements
   */
  public void ComputeState(Statement s) {
    switch (s) {
      case IfStmt ifStmt:
        ComputeIfStmt(ifStmt);
        break;
      case BlockStmt bcStmt:
        ComputeBlockStmt(bcStmt);
        break;
      case UpdateStmt upStmt:
        ComputeUpdateStmt(upStmt);
        break;
      case VarDeclStmt varStmt:
        ComputeVarDeclStmt(varStmt);
        break;
      case ForLoopStmt forStmt:
        ComputeForLoopStmt(forStmt);
        break;
      case WhileStmt whileStmt:
        ComputeWhileLoopStmt(whileStmt);
        break;
      case ReturnStmt retStmt:
        ComputeReturnStmt(retStmt);
        break;
      case BreakStmt brStmt:
        ComputeBreakStmt(brStmt);
        break;
      case AlternativeStmt alternativeStmt:
        ComputeAlternativeStmt(alternativeStmt);
        break;
      case AlternativeLoopStmt alternativeLoopStmt:
        ComputeAlternativeLoopStmt(alternativeLoopStmt);
        break;
      case AssertStmt assertStmt:
        ComputeVerificationStmt(assertStmt);
        break;
      case AssumeStmt assumeStmt:
        ComputeVerificationStmt(assumeStmt);
        break;
      case ExpectStmt expectStmt:
        ComputeVerificationStmt(expectStmt);
        break;
      case RevealStmt revealStmt:
        ComputeVerificationStmt(revealStmt);
        break;
      case CalcStmt calcStmt:
        ComputeVerificationStmt(calcStmt);
        break;
      case AssignSuchThatStmt assignSuchThatStmt:
        ComputeAssignSuchThatStmt(assignSuchThatStmt);
        break;
      case NestedMatchStmt nestedMatchStmt:
        ComputeNestedMatchStmt(nestedMatchStmt);
        break;
      default:
        if(s!=null){
          Console.WriteLine("ProgramStatement.cs (ComputeState): " + s.GetType());
        }
        break;
    }
  }

  public void ComputeVerificationStmt(Statement s) {
    List<List<StateCondition>> veriLst = new List<List<StateCondition>>();
    foreach (var lst in this.StateBefore) {
      veriLst.Add(lst.ToList());
    }
    VerificationStmt.Add(s, veriLst);
  }
  public void ComputeNestedMatchStmt(NestedMatchStmt nestedMatchStmt) {
    var tmpStateBefore = new List<List<StateCondition>>(this.StateBefore);
    var finalStateBefore = new List<List<StateCondition>>();
    var source = nestedMatchStmt.Source;
    var cases = nestedMatchStmt.Cases;

    /*List used if hsa the default case*/
    var guardLst = new List<Expression>();
    
    foreach (var caseNest in cases) {
      var tmpLstBody = new List<Statement>();

      Expression guard = null;
      switch (caseNest.Pat) {
        case IdPattern idPattern:
          if (idPattern.Id == "_v0") {
            Expression guardV0Exp = null;
            for (int i = 0; i < guardLst.Count; i++) {
              var guarExp = guardLst[i];
              var tmpGuarExp = new UnaryOpExpr(guarExp.Tok, UnaryOpExpr.Opcode.Not, guarExp);
              if (i == 0) {
                guardV0Exp = tmpGuarExp;
              }
              else{
                guardV0Exp = new BinaryExpr(guardV0Exp.Tok, BinaryExpr.Opcode.And, guardV0Exp, tmpGuarExp);
              }
            }

            guard = new ParensExpression(guardV0Exp.Tok, guardV0Exp);
          }
          else {
            string suffixName = string.Empty;
            suffixName = string.Concat(suffixName, idPattern.Ctor.ToString(), "?");
            var idPattBody = idPattern.Arguments;
            var dataType = idPattern.Ctor.Destructors;
            for (int i = 0; i < idPattBody.Count; i++) {
              var lhs = idPattBody[i];
              var lhss = new AutoGhostIdentifierExpr(idPattern.Tok, lhs.ToString());
              var lhssLst = new List<Expression>();
              lhssLst.Add(lhss);

              var rhs = dataType[i];
              var exprRhs = new ExprDotName(idPattern.Tok, source, rhs.ToString(), null);
              exprRhs.Type = rhs.Type;
              lhss.Type = rhs.Type;
              var rhss = new ExprRhs(exprRhs);
              var rhssLst = new List<AssignmentRhs>();
              rhssLst.Add(rhss);
              var UpdArg = new UpdateStmt(caseNest.RangeToken, lhssLst, rhssLst);
              tmpLstBody.Add(UpdArg);
            }

            var guardExpr = new ExprDotName(caseNest.Tok, source, suffixName, null);
            guard = new ParensExpression(caseNest.Tok, guardExpr);
          }
          break;
        
        case LitPattern litPattern:
          var guardLitExpr = new BinaryExpr(caseNest.Tok, BinaryExpr.Opcode.Eq, source, litPattern.OrigLit);
          guard = new ParensExpression(caseNest.Tok, guardLitExpr);
          break;
      }
      
      guardLst.Add(guard);
      
      var blcIF = new BlockStmt(caseNest.RangeToken, tmpLstBody.Concat(caseNest.Body).ToList());

      /*Considering each case nest as a IfStmt*/
      var tmpIf = new IfStmt(caseNest.RangeToken, false, guard, blcIF, null);
      var sc = new StatementContext(tmpIf, tmpStateBefore);
      this.AddStatementContext(sc);
      this.UpdateStateBefore(sc);
      ComputeState(blcIF);
      var tmpStateAfterGuard = new List<List<StateCondition>>(this.StateBefore);
      finalStateBefore.AddRange(tmpStateAfterGuard);
    }
    
    UpdateStateBefore(finalStateBefore);
  }
  public void ComputeAlternativeStmt(AlternativeStmt alternativeStmt) {
    var tmpStateBefore = new List<List<StateCondition>>(this.StateBefore);
    var finalStateBefore = new List<List<StateCondition>>();
    var guardAlt = alternativeStmt.Alternatives;
    foreach (var guard in guardAlt) {
      var blcIF = new BlockStmt(guard.RangeToken, guard.Body);
      /*Considering each alternative as IfStmt*/
      var tmpIF = new IfStmt(guard.RangeToken, guard.IsBindingGuard, guard.Guard, blcIF, null);
      var sc = new StatementContext(tmpIF, tmpStateBefore);
      this.AddStatementContext(sc);
      this.UpdateStateBefore(sc);
      ComputeState(blcIF);
      var tmpStateAfterGuard = new List<List<StateCondition>>(this.StateBefore);
      finalStateBefore.AddRange(tmpStateAfterGuard);
    }

    UpdateStateBefore(finalStateBefore);
  }

  public void ComputeAlternativeLoopStmt(AlternativeLoopStmt alternativeLoopStmt) {
    var tmpStateBefore = new List<List<StateCondition>>(this.StateBefore);
    var finalStateBefore = new List<List<StateCondition>>();
    var guardAlt = alternativeLoopStmt.Alternatives;
    foreach (var guard in guardAlt) {
      var tmpBody = new BlockStmt(guard.RangeToken, guard.Body);
      /*Consider each alternative as a WhileStmt*/
      var tmpWhile = new WhileStmt(guard.RangeToken, guard.Guard, alternativeLoopStmt.Invariants,
        alternativeLoopStmt.Decreases, alternativeLoopStmt.Mod, tmpBody);
      ComputeWhileLoopStmt(tmpWhile, tmpStateBefore);
      var tmpStateAfterGuard = new List<List<StateCondition>>(this.StateBefore);
      finalStateBefore.AddRange(tmpStateAfterGuard);
    }
    
    UpdateStateBefore(finalStateBefore);
  }

  public void ComputeBreakStmt(BreakStmt s) {
    var sc = new StatementContext(s, this.StateBefore);
    this.AddStatementContext(sc);
    this.UpdateStateBefore(sc);
    this.ControlFlow = sc;
  }

  public void ComputeAssignSuchThatStmt(AssignSuchThatStmt s) {
    var sc = new StatementContext(s, this.StateBefore);
    this.AddStatementContext(sc);
    this.UpdateStateBefore(sc);
  }

  public void ComputeReturnStmt(ReturnStmt s) {
    ComputeState(s.HiddenUpdate);
    if (this.ProgramStmt[^1].Stmt.Equals(s.HiddenUpdate)) {
      foreach (var lst in this.ProgramStmt[^1].Condition) {
        lst.SetStmtState("RETURN");
      }
    }
  }
  
  public void ComputeWhileLoopStmt(WhileStmt s) {
    /*Check invariants before entering on the While loop*/
    UpdateEntailment(s.Invariants, 1);
    
    /*Compute state inside While loop*/
    var sc = new StatementContext(s, this.StateBefore);
    this.AddStatementContext(sc);
    this.UpdateStateBefore(sc);
    ComputeState(s.Body);
    UpdateEntailment(s.Invariants, 2);

    if (ControlFlow != null) {
      var stmtActual = (BreakStmt)ControlFlow.Stmt;
      var stmtActualBreak = stmtActual.TargetStmt;
      if (DetermineBreak(stmtActualBreak, s)) {
        RemoveBreakState();
        this.ControlFlow = null;
      }
    }
    
    /*Exit while loop*/
    var scExit = new StatementContext("LOOP_EXIT", this.StateBefore, s.Guard, s);
    this.AddStatementContext(scExit);
    this.UpdateStateBefore(scExit);
    UpdateEntailment(s.Invariants, 3);
    this.UpdateStateBefore(s.Invariants);
  }
  
  public void ComputeWhileLoopStmt(WhileStmt s, List<List<StateCondition>> actualStateBefore) {
     
    /*Check invariants before entering on the While loop*/
    UpdateEntailment(s.Invariants, 1);
    
    /*Compute state inside While loop*/
    var sc = new StatementContext(s, actualStateBefore);
    this.AddStatementContext(sc);
    this.UpdateStateBefore(sc);
    ComputeState(s.Body);
    UpdateEntailment(s.Invariants, 2);

    if (ControlFlow != null) {
      var stmtActual = (BreakStmt)ControlFlow.Stmt;
      var stmtActualBreak = stmtActual.TargetStmt;
      if (DetermineBreak(stmtActualBreak, s)) {
        RemoveBreakState();
        this.ControlFlow = null;
      }
    }
    
    /*Exit while loop*/
    var scExit = new StatementContext("LOOP_EXIT", this.StateBefore, s.Guard, s);
    this.AddStatementContext(scExit);
    this.UpdateStateBefore(scExit);
    UpdateEntailment(s.Invariants, 3);
    this.UpdateStateBefore(s.Invariants);
  }

  public void RemoveBreakState() {
    var stmtCont = this.ProgramStmt.Find(x => x.Equals(this.ControlFlow));
    foreach (var sc in stmtCont.Condition) {
      sc.SetControlFlow(false);
    }
  }
  public void ComputeForLoopStmt(ForLoopStmt s) {
    /*Add initialization do index*/
    var e0Index = new NameSegment(s.LoopIndex.Tok, s.LoopIndex.Name, null);
    e0Index.Type = s.LoopIndex.Type;
    var index = new BinaryExpr(s.LoopIndex.Tok, BinaryExpr.Opcode.Eq, e0Index, s.Start);
    var scIndex = new StatementContext(s, index, this.StateBefore, true);
    this.AddStatementContext(scIndex);
    this.UpdateStateBefore(scIndex);
    this.UpdateVariables(scIndex.GetVariables());
    
    /*Check invariants before entering on the For loop*/
    UpdateEntailment(s.Invariants, 1);
    
    var sc = new StatementContext(s, this.StateBefore);
    this.AddStatementContext(sc);
    this.UpdateStateBefore(sc);
    ComputeState(s.Body);
    
    /*Add an index = index + 1 */
    var indexInc = new BinaryExpr(s.LoopIndex.Tok, BinaryExpr.Opcode.Add, e0Index, new LiteralExpr(s.LoopIndex.Tok, 1));
    var indexExp = new BinaryExpr(s.LoopIndex.Tok, BinaryExpr.Opcode.Eq, e0Index, indexInc);
    var scIndexInc = new StatementContext(s, indexExp, this.StateBefore, false);
    this.AddStatementContext(scIndexInc);
    this.UpdateStateBefore(scIndexInc);
    
    UpdateEntailment(s.Invariants, 2);
    
    if (ControlFlow != null) {
      var stmtActual = (BreakStmt)ControlFlow.Stmt;
      var stmtActualBreak = stmtActual.TargetStmt;
      if (DetermineBreak(stmtActualBreak, s)) {
        RemoveBreakState();
        this.ControlFlow = null;
      }
    }
    
    /*Exit for loop*/
    var e0 = new NameSegment(s.LoopIndex.Tok, s.LoopIndex.Name, null);
    var expr = new BinaryExpr(s.Tok, BinaryExpr.Opcode.Lt, e0, s.End);
    var scExit = new StatementContext("LOOP_EXIT", this.StateBefore, expr, s);
    this.AddStatementContext(scExit);
    this.UpdateStateBefore(scExit);
    UpdateEntailment(s.Invariants, 3);
    this.UpdateStateBefore(s.Invariants);
  }

  public bool DetermineBreak(Statement breakStmt, Statement origStmt) {
    if (breakStmt.Equals(origStmt)) {
      return true;
    }
    var ret = false;
    switch (breakStmt) {
      case BlockStmt blockStmt:
        foreach (var body in blockStmt.Body) {
          return DetermineBreak(body, origStmt);
        }
        break;
      default:
        Console.WriteLine("ProgramStatement(DetermineBreak) : "+ breakStmt.GetType());
        break;
    }
    return ret;
  }

  public void ComputeVarDeclStmt(VarDeclStmt s) {
    if(s.Update != null) {
      ComputeState(s.Update);
    }
  }
  public void ComputeUpdateStmt(UpdateStmt s) {
    var sc = new StatementContext(s, this.StateBefore);
    this.AddStatementContext(sc);
    this.UpdateStateBefore(sc);
    this.UpdateVariables(sc.GetVariables());
  }

  public void UpdateVariables(List<Formal> variables) {
    foreach (var vars in variables) {
      if (!Variables.Exists(x => vars.Name.Contains(x.Name))) {
        Variables.Add(vars);
      }
    }
  }

  public void ComputeBlockStmt(BlockStmt s) {
    foreach (var lstBlock in s.Body) {
      ComputeState(lstBlock);
    }
  }

  public void ComputeIfStmt(IfStmt s) {
    var tmpStateBefore = new List<List<StateCondition>>(this.StateBefore);
    var sc = new StatementContext(s, this.StateBefore);
    //If
    this.AddStatementContext(sc);
    
    //Then
    this.UpdateStateBefore(sc);
    ComputeState(s.Thn);
    var tmpStateAfterIf = new List<List<StateCondition>>(this.StateBefore);
    
    //Else
    if (s.Els != null) {
      if (s.Guard != null) {
        var notGuard = new UnaryOpExpr(s.Tok, UnaryOpExpr.Opcode.Not, s.Guard);
        if (s.Els.GetType() != typeof(Microsoft.Dafny.IfStmt)) {
          /*
           * If not is an else-if statement, add an empty "ELSE" StatementContext
           * I can also add state = !Guard
           */
          var scElse = new StatementContext("ELSE", tmpStateBefore, notGuard, s);
          AddStatementContext(scElse);
          this.UpdateStateBefore(scElse);
        } else {
          var scElse = new StatementContext("ELIF", tmpStateBefore, notGuard, s);
          AddStatementContext(scElse);
          this.UpdateStateBefore(scElse);
        }
      }
      ComputeState(s.Els);
    } else {
      var notGuard = new UnaryOpExpr(s.Tok, UnaryOpExpr.Opcode.Not, s.Guard);
      var scElse = new StatementContext("ELSE", tmpStateBefore, notGuard,s);
      AddStatementContext(scElse);
      this.UpdateStateBefore(scElse);
    }
    //In the end, I need to save the stateAfter of if and else
    var tmpStateAfterElse = new List<List<StateCondition>>(this.StateBefore);
    UpdateStateBefore(tmpStateAfterIf, tmpStateAfterElse);
  }
  
  public void UpdateEntailment() {
    for (int i =0; i < this.StateBefore.Count; i++) {
      var lst = this.StateBefore[i];
      var ent = new Entailment();
      var entState = new EntailmentStateCondition();
      foreach (var stCond in lst) {
        ent.AddLHSCondition(stCond.Condition);
        entState.AddLHSCondition(stCond);
      }
      ent.SetRHSCondition(this.Ens);
      ent.SetInvariant(false);
      entState.SetInvariant(false);
      
      foreach (var (stmt, stateLst) in VerificationStmt) {
        foreach (var state in stateLst) {
          if(lst.Count - state.Count + 1 >= 0) {
            if (Enumerable.Range(0, lst.Count - state.Count + 1)
                .Any(i => lst.Skip(i).Take(state.Count).SequenceEqual(state))) {
              ent.AddVerificationStmt(stmt);
              entState.AddVerificationStmt(stmt);
            }
          }
        }
      }
      
      Entailments.Add(ent);
      EntailmentStateConditions.Add(entState);

      
    }
  }
  
  /*
   * <parameters>
   *  <parameter> proof: enum(1: Initialization, 2:Maintenance, 3:Termination)
   * </parameters>
   */
  public void UpdateEntailment(List<AttributedExpression> invariants, int proof) {
    
    for (int i =0; i < this.StateBefore.Count; i++) {
      var lst = this.StateBefore[i];
      var ent = new Entailment();
      var entState = new EntailmentStateCondition();
      foreach (var stCond in lst) {
        ent.AddLHSCondition(stCond.Condition);
        entState.AddLHSCondition(stCond);
      }

      if (proof == 1 || proof == 2) {
        ent.NewRHSCondition(invariants);
      }
      else if (proof == 3) {
        ent.NewRHSCondition(Ens);
      }
      ent.SetInvariant(true);
      entState.SetInvariant(true);
      if (proof == 2 || proof == 3) {
        ent.SetLHSInvariantCondition(invariants);
      }
      
      foreach (var (stmt, stateLst) in VerificationStmt) {
        foreach (var state in stateLst) {
          if(lst.Count - state.Count + 1 >= 0) {
            if (Enumerable.Range(0, lst.Count - state.Count + 1)
                .Any(i => lst.Skip(i).Take(state.Count).SequenceEqual(state))) {
              ent.AddVerificationStmt(stmt);
              entState.AddVerificationStmt(stmt);
            }
          }
        }
      }
      
      Entailments.Add(ent);
      EntailmentStateConditions.Add(entState);
    }
  }

  public List<Entailment> GetEntailments() {
    return this.Entailments;
  }

  public List<EntailmentStateCondition> GetEntailmentStateConditions() {
    return EntailmentStateConditions;
  }

  public List<Formal> GetVariables() {
    return Variables;
  }
  
}