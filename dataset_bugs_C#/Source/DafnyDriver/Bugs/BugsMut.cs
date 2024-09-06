using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Dafny;

using static DafnyDriver.Bugs.FilesUtils;

namespace DafnyDriver.Bugs;

/*
 * BugsMut
 *
 * <summary>
 *  Introduce Bugs
 * </summary>
 *
 * <fields>
 *  <field> Program: Original program of the dafny
 *  <field> Members: All members declared in the dafny program
 *  <field> IndexFile: To manage the index of the file
 *  <field> PointActual: To determine which member we are working with
 * </fields>
 *
 * IMPORTANT:
 * 1. Change the path to the correct directory where your Bugs_Code/Hints are located.
 *    Look for occurrences of 'Path Bugs_Code/Hints' in the file and update them accordingly.
 * 2. Change the path to the correct directory where your Bugs_Code/Mutations are located.
 *    Look for occurrences of 'Path Bugs_Code/Mutations' in the file and update them accordingly.
 * 
 * Ensure all references to 'Path Bugs_Code/Mutations' and 'Path Bugs_Code/Hints' are updated to reflect the new path.
 * This is crucial to avoid errors when running the program.
 */
public class BugsMut {
  public Program Program;
  public Dictionary<Declaration, AccessibleMember> Members;
  public int IndexFile = 0;
  public Declaration PointActual = null;
  public BugsMut(Program p) {
    Program = p;
    Members = p.DefaultModuleDef.AccessibleMembers;
    this.Introduce_Bugs();
  }

  public void Introduce_Bugs() {
    foreach (var (decl,accessibleMember) in Members) {
      switch (decl) {
        case Method m:
          PointActual = m;
          Methods_Bugs(m);
          break;
      }
    } 
  }

  public void Methods_Bugs(Method m) {
    var body = m.Body.Body;
    foreach (var stmt in body) {
      IterateStmt(stmt);
    }
  }

  public void IterateStmt(Statement s) {
    switch (s) {
      case UpdateStmt updateStmt:
        Modify(updateStmt);
        break;
      case VarDeclStmt varDeclStmt:
        Modify(varDeclStmt);
        break;
      case BlockStmt blockStmt:
        var body = blockStmt.Body;
        foreach (var statement in body) {
          IterateStmt(statement);  
        }
        break;
      case IfStmt ifStmt:
        IterateStmt(ifStmt.Thn);
        IterateStmt(ifStmt.Els);
        break;
      case ForLoopStmt forLoopStmt:
        IterateStmt(forLoopStmt.Body);
        break;
      case WhileStmt whileStmt:
        IterateStmt(whileStmt.Body);
        break;
      case ReturnStmt returnStmt:
        Modify(returnStmt);
        break;
      case AlternativeStmt alternativeStmt:
        var alterStmt = alternativeStmt.Alternatives;
        foreach (var guardedAlternative in alterStmt) {
          var lstBody = guardedAlternative.Body;
          foreach (var stmt in lstBody) {
            IterateStmt(stmt);
          }
        }
        break;
    }
  }

  public Expression IsExpression(Expression rhs) {
    Expression ret = null;
    switch (rhs) {
      case BinaryExpr binaryExpr:
        if (binaryExpr.Op == BinaryExpr.Opcode.Add || binaryExpr.Op == BinaryExpr.Opcode.Sub ||
            binaryExpr.Op == BinaryExpr.Opcode.Div || binaryExpr.Op == BinaryExpr.Opcode.Mod ||
            binaryExpr.Op == BinaryExpr.Opcode.Mul) {
          ret = binaryExpr;
        }
        break;
      case ApplySuffix applySuffix:
        var tmpApplyRet = IsExpression(applySuffix.Lhs);
        if (tmpApplyRet != null) {
          ret = applySuffix;
        }
      break;
      default:
        Console.WriteLine("BugsMut (IsBinaryExpression - ExprRhs): " + rhs.GetType());
        break;
    }
    return ret;
  }

  public void Modify(Statement s) {
    switch (s) {
      case UpdateStmt updateStmt:
        var lstRhsUpd = updateStmt.Rhss;
        foreach (var rhs in lstRhsUpd) {
          switch (rhs) {
            case ExprRhs exprRhs:
              var tmpExpr = IsExpression(exprRhs.Expr);
              if (tmpExpr != null) {
                MutationsBugs(updateStmt, rhs, tmpExpr, 1);
                //LinearCombination
                MutationsBugs(updateStmt, rhs, tmpExpr, 2);
                MutationsBugs(updateStmt, rhs, tmpExpr, 3);
                MutationsBugs(updateStmt, rhs, tmpExpr, 4);
              }
              break;
          }
          
        }
        break;
      case VarDeclStmt varDeclStmt:
        switch (varDeclStmt.Update) {
          case UpdateStmt updateStatement:
            var lstRhsVar = updateStatement.Rhss;
            foreach (var rhs in lstRhsVar) {
              switch (rhs) {
                case ExprRhs exprRhs:
                  var tmpExpr = IsExpression(exprRhs.Expr);
                  if (tmpExpr != null) {
                    MutationsBugs(varDeclStmt, rhs, tmpExpr, 1);
                    //LinearCombination
                    MutationsBugs(varDeclStmt, rhs, tmpExpr, 2);
                    MutationsBugs(varDeclStmt, rhs, tmpExpr, 3);
                    MutationsBugs(varDeclStmt, rhs, tmpExpr, 4);
                  }
                  break;
              }
            }
            break;
          case AssignSuchThatStmt assignSuchThatStmt:
            Console.WriteLine("BugsMut(Modify): AssignSuchThatStmt");
            break;
          case AssignOrReturnStmt assignOrReturnStmt:
            Console.WriteLine("BugsMut(Modify): AssignOrReturnStmt");
            break;
        }
        
        break;
      case ReturnStmt returnStmt:
        var lstRhsRet = returnStmt.Rhss;
        foreach (var rhs in lstRhsRet) {
          switch (rhs) {
            case ExprRhs exprRhs:
              var tmpExpr = IsExpression(exprRhs.Expr);
              if (tmpExpr != null) {
                MutationsBugs(returnStmt, rhs, tmpExpr, 1);
                //LinearCombination
                MutationsBugs(returnStmt, rhs, tmpExpr, 2);
                MutationsBugs(returnStmt, rhs, tmpExpr, 3);
                MutationsBugs(returnStmt, rhs, tmpExpr, 4);
              }
              break;
          }
          
        }
        break;
    }
  }

  public void MutationsBugs(Statement s, AssignmentRhs origAssgnRhs, Expression rhs, int type) {
    Expression tmpExpr = null;
    switch (rhs) {
      case BinaryExpr binaryExpr:
        var varExpr = GetVariables(binaryExpr);
        switch (type) {
          case 1: //modify signal
            tmpExpr = ModifySignal(binaryExpr);
            break;
          case 2: //change random coeficients
            tmpExpr = ChangeVariablesLiteral(binaryExpr);
            break;
          case 3: //swap variables
            tmpExpr = SwapVariables(binaryExpr, varExpr);
            break;
          case 4 : //combine all type of introduce bugs:
            tmpExpr = CombineAllMut(binaryExpr, varExpr);
            break;
        }
        break;
    }

    switch (s) {
      case UpdateStmt updateStmt:
        var lstRhs = new List<AssignmentRhs>();
        foreach (var assignRhs in updateStmt.Rhss) {
          if (assignRhs.Equals(origAssgnRhs)) {
            switch (assignRhs) {
              case ExprRhs exprRhs:
                lstRhs.Add(new ExprRhs(tmpExpr, exprRhs.Attributes));
                break;
            }
          } else {
            lstRhs.Add(assignRhs);
          }
        }

        var newUpdStmt = new UpdateStmt(updateStmt.RangeToken, updateStmt.Lhss, lstRhs);
        
        MutationToFileWithoutHint(updateStmt, newUpdStmt);
        MutationToFile(updateStmt, newUpdStmt);
        
        break;
      
      case VarDeclStmt varDeclStmt:
        var updStmt = varDeclStmt.Update;
        switch (varDeclStmt.Update) {
          case UpdateStmt updateStatement:
            var lstVarRhs = new List<AssignmentRhs>();
            foreach (var assignRhs in updateStatement.Rhss) {
              if (assignRhs.Equals(origAssgnRhs)) {
                switch (assignRhs) {
                  case ExprRhs exprRhs:
                    lstVarRhs.Add(new ExprRhs(tmpExpr, exprRhs.Attributes));
                    break;
                }
              } else {
                lstVarRhs.Add(assignRhs);
              }
            }

            updStmt = new UpdateStmt(updateStatement.RangeToken, updateStatement.Lhss, lstVarRhs);
            break;
          case AssignSuchThatStmt assignSuchThatStmt:
            Console.WriteLine("BugsMut(Modify): AssignSuchThatStmt");
            break;
          case AssignOrReturnStmt assignOrReturnStmt:
            Console.WriteLine("BugsMut(Modify): AssignOrReturnStmt");
            break;
        }
        var newVarDeclStmt = new VarDeclStmt(varDeclStmt.RangeToken, varDeclStmt.Locals, updStmt);
        MutationToFileWithoutHint(varDeclStmt, newVarDeclStmt);
        MutationToFile(varDeclStmt, newVarDeclStmt);
        
        break;
      case ReturnStmt returnStmt:
        var lstRhsRet = new List<AssignmentRhs>();
        foreach (var assignRhs in returnStmt.Rhss) {
          if (assignRhs.Equals(origAssgnRhs)) {
            switch (assignRhs) {
              case ExprRhs exprRhs:
                lstRhsRet.Add(new ExprRhs(tmpExpr, exprRhs.Attributes));
                break;
            }
          } else {
            lstRhsRet.Add(assignRhs);
          }
        }

        var newRetStmt = new ReturnStmt(returnStmt.RangeToken, lstRhsRet);
        
        MutationToFileWithoutHint(returnStmt, newRetStmt);
        MutationToFile(returnStmt, newRetStmt);
        break;
      
    }
  }
  
  public Expression ModifySignal(Expression e) {
    Expression ret = e;
    switch (e) {
      case BinaryExpr binaryExpr:
        var tmpE0 = ModifySignal(binaryExpr.E0);
        var tmpE1 = ModifySignal(binaryExpr.E1);
        ret = new BinaryExpr(binaryExpr.Tok, ChangeSignal(binaryExpr.Op), tmpE0, tmpE1);
        break;
    }
    return ret;
  }

  private static Random random = new Random();
  public Expression ChangeVariablesLiteral(Expression e) {
    Expression ret = e;
    switch (e) {
      case BinaryExpr binaryExpr:
        var tmpE0 = ChangeVariablesLiteral(binaryExpr.E0);
        var tmpE1 = ChangeVariablesLiteral(binaryExpr.E1);
        ret = new BinaryExpr(binaryExpr.Tok, binaryExpr.Op, tmpE0, tmpE1);
        break;
      case LiteralExpr literalExpr:
        int numberInt = 1;
        if (int.TryParse(literalExpr.Value.ToString(), out numberInt)) {
          ret = new LiteralExpr(literalExpr.Tok, random.Next(numberInt*-1, numberInt));
        } 
        //ret = new LiteralExpr(literalExpr.Tok, random.Next(literalExpr.Value));
        break;
    }
    return ret;
  }

  public Expression SwapVariables(Expression e, List<Expression> varExpr) {
    Expression ret = e;
    switch (e) {
      case BinaryExpr binaryExpr:
        var tmpE0 = SwapVariables(binaryExpr.E0, varExpr);
        var tmpE1 = SwapVariables(binaryExpr.E1, varExpr);
        ret = new BinaryExpr(binaryExpr.Tok, binaryExpr.Op, tmpE0, tmpE1);
        break;
      case NameSegment:
        var ind = random.Next(0, varExpr.Count);
        ret = varExpr[ind];
        break;
    }
    return ret;
  }

  public Expression CombineAllMut(Expression e, List<Expression> varExpr) {
    Expression ret = e;
    switch (e) {
      case BinaryExpr binaryExpr:
        var tmpE0 = CombineAllMut(binaryExpr.E0, varExpr);
        var tmpE1 = CombineAllMut(binaryExpr.E1, varExpr);
        ret = new BinaryExpr(binaryExpr.Tok, ChangeSignal(binaryExpr.Op), tmpE0, tmpE1);
        break;
      case NameSegment:
        var ind = random.Next(0, varExpr.Count);
        ret = varExpr[ind];
        break;
      case LiteralExpr literalExpr:
        int numberInt = 1;
        if (int.TryParse(literalExpr.Value.ToString(), out numberInt)) {
          ret = new LiteralExpr(literalExpr.Tok, random.Next(numberInt*-1, numberInt));
        } 
        //ret = new LiteralExpr(literalExpr.Tok, random.Next(literalExpr.Value));
        break;
    }
    return ret;
  }
  public BinaryExpr.Opcode ChangeSignal(BinaryExpr.Opcode signal) {
    switch (signal) {
      case BinaryExpr.Opcode.Add:
        return BinaryExpr.Opcode.Sub;
      case BinaryExpr.Opcode.Div:
        return BinaryExpr.Opcode.Mul;
      case BinaryExpr.Opcode.Mod:
        return BinaryExpr.Opcode.Div;
      case BinaryExpr.Opcode.Sub:
        return BinaryExpr.Opcode.Add;
      case BinaryExpr.Opcode.Mul:
        return BinaryExpr.Opcode.Div;
      default:
        Console.WriteLine("BugsMut(ChangeSignal): " + signal);
        return signal;
    }
  }

  public List<Expression> GetVariables(Expression e) {
    var ret = new List<Expression>();
    switch (e) {
      case NameSegment nameSegment:
        ret.Add(nameSegment);
        break;
      case BinaryExpr binaryExpr:
        ret.AddRange(GetVariables(binaryExpr.E0));
        ret.AddRange(GetVariables(binaryExpr.E1));
        break;
    }
    return ret;
  }
  public void MutationToFile(Statement original, Statement replace) {
    var replaceString = replace.ToString();
    replaceString = replaceString.EndsWith("\n") ? replaceString.Substring(0, replaceString.Length - 1) + "buggy line\n" : string.Concat(replaceString, "//buggy line\n");

    Uri uriPath = new Uri(Program.FullName);
    var codeMutString = ReplacePartOfFile(uriPath.LocalPath, original.StartToken.line, original.StartToken.col,
      original.EndToken.line, original.EndToken.col, replaceString);

    /*Path Bugs_Code/Hints*/
    var directoryPath = @"C:\Users\filip\Documents\UPorto\MEIC\2ºAno\Thesis_Project\dataset\DafnyBench\ground_truth\Bugs_Code\Hints\"+GetType();
    
    var baseFileName = Path.GetFileNameWithoutExtension(Program.Name);
    var newFileName = $"{baseFileName}_{IndexFile}.dfy";
    IndexFile++;
    WriteCodeToFile(directoryPath, newFileName, codeMutString);
  }
  
  public void MutationToFileWithoutHint(Statement original, Statement replace) {
    var replaceString = replace.ToString();
    //replaceString = replaceString.EndsWith("\n") ? replaceString.Substring(0, replaceString.Length - 1) + "buggy line\n" : string.Concat(replaceString, "//buggy line\n");

    Uri uriPath = new Uri(Program.FullName);
    var codeMutString = ReplacePartOfFile(uriPath.LocalPath, original.StartToken.line, original.StartToken.col,
      original.EndToken.line, original.EndToken.col, replaceString);

    /*Path Bugs_Code/Mutations*/
    var directoryPath = @"C:\Users\filip\Documents\UPorto\MEIC\2ºAno\Thesis_Project\dataset\DafnyBench\ground_truth\Bugs_Code\Mutations\"+GetType();
    var baseFileName = Path.GetFileNameWithoutExtension(Program.Name);
    var newFileName = $"{baseFileName}_{IndexFile}.dfy";
    /*Does not increment IndexFile*/
    WriteCodeToFile(directoryPath, newFileName, codeMutString);
  }

  public string GetType() {
    var ret = string.Empty;
    switch (PointActual) {
      case Method m:
        var outs = m.Outs;
        if (outs.Count>0) {
          for (int i = 0; i < outs.Count; i++) {
            ret = string.Concat(ret, outs[i].Type.ToString());
            if (i < outs.Count - 1) {
              ret = string.Concat(ret, "_");
            }
          }
        } else {
          ret = "void";
        }
        break;
    }

    return ret;
  }
}