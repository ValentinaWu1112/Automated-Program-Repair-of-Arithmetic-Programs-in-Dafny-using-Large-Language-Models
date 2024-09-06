using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Dafny;

namespace DafnyDriver.BlockCode;

/*
 * BlockLine
 *
 * <summary>
 *  Block for the lines with executable statements, excluding comments, empty lines, and other non-executable content,
 *  in the methods with failed verifications
 * </summary>
 *
 * <fields>
 *  <field> FailProgs: list of members that fail the verification
 *  <field> DirectoryPath: To change the path for save the block
 *  <field> FileName: Name of the dafny program file without extention
 *  <field> BlockCode: List to store the valid lines
 * </fields>
 *
 * IMPORTANT:
 * 1. Change the path to the correct directory where your All_Bugs_Code\Code_Block are located.
 *    Look for occurrences of 'DirectoryPath = @"PATH\TO\All_Bugs_Code\CodeBlockMeth"' in the file and update them accordingly.
 * 
 * Ensure all references to 'DirectoryPath = @"PATH\TO\All_Bugs_Code\CodeBlockMeth"' are updated to reflect the new path.
 * This is crucial to avoid errors when running the program.
 */

public class BlockLineMethod {
  
  public List<ICanVerify> FailProgs;
  public string DirectoryPath = @"PATH\TO\All_Bugs_Code\CodeBlockMeth";
  public string FileName;
  public List<int> BlockCode = new List<int>();
  
  public BlockLineMethod(List<ICanVerify> failProgs, string nameFile) {
    this.FailProgs = failProgs;
    this.FileName = nameFile.Split('.')[0];
    this.IterateProg();
    WriteListToFile(DirectoryPath, FileName + ".txt");
  }
  
  public void IterateProg() {
    foreach (var decl in FailProgs) {
      switch (decl) {
        case Method m:
          IterateStatement(m.Body);
          break;
      }
    }
  }

  public void IterateStatement(Statement s) {
    switch (s) {
      case IfStmt ifStmt:
        //If
        if(ifStmt.Guard != null) {
          BlockCode.Add(ifStmt.Guard.Tok.line);
        } else {
          BlockCode.Add(ifStmt.Tok.line);
        }
        //Then
        IterateStatement(ifStmt.Thn);
        //Else
        var els = ifStmt.Els;
        if (els != null) {
          BlockCode.Add(els.Tok.line);
          IterateStatement(els);
        }
        break;
      case BlockStmt bcStmt:
        var bcLst = bcStmt.Body;
        for (int i = 0; i < bcLst.Count(); i++) {
          IterateStatement(bcLst[i]);
        }
        break;
      case UpdateStmt upStmt:
        this.BlockCode.Add(upStmt.Tok.line);
        break;
      case VarDeclStmt varStmt:
        this.BlockCode.Add(varStmt.Tok.line);
        break;
      case ForLoopStmt forStmt:
        //Guard
        this.BlockCode.Add(forStmt.Tok.line);
        //Body
        IterateStatement(forStmt.Body);
        break;
      case WhileStmt whileStmt:
        //Guard
        this.BlockCode.Add(whileStmt.Guard.Tok.line);
        //Body
        IterateStatement(whileStmt.Body);
        break;
      case ReturnStmt retStmt:
        this.BlockCode.Add(retStmt.Tok.line);
        break;
      case BreakStmt brStmt:
        this.BlockCode.Add(brStmt.Tok.line);
        break;
      case AlternativeStmt alternativeStmt:
        this.BlockCode.Add(alternativeStmt.Tok.line);
        var guardAlt = alternativeStmt.Alternatives;
        for (int i = 0; i < guardAlt.Count(); i++) {
          var guard = guardAlt[i];
          var blcIF = new BlockStmt(guard.RangeToken, guard.Body);
          var tmpIF = new IfStmt(guard.RangeToken, guard.IsBindingGuard, guard.Guard, blcIF, null);
          IterateStatement(tmpIF);
        }
        break;
      case AlternativeLoopStmt alternativeLoopStmt:
        this.BlockCode.Add(alternativeLoopStmt.Tok.line);
        var guardLoop = alternativeLoopStmt.Alternatives;
        for (int i = 0; i < guardLoop.Count(); i++) {
          var guard = guardLoop[i];
          var tmpBody = new BlockStmt(guard.RangeToken, guard.Body);
          var tmpWhile = new WhileStmt(guard.RangeToken, guard.Guard, alternativeLoopStmt.Invariants,
            alternativeLoopStmt.Decreases, alternativeLoopStmt.Mod, tmpBody);
          IterateStatement(tmpWhile);
        }
        break;
      case AssertStmt assertStmt:
        //this.BlockCode.Add(assertStmt.Tok.line);
        break;
      case AssumeStmt assumeStmt:
        //this.BlockCode.Add(assumeStmt.Tok.line);
        break;
      case ExpectStmt expectStmt:
        //this.BlockCode.Add(expectStmt.Tok.line);
        break;
      case RevealStmt revealStmt:
        //Console.WriteLine("BlockLine(IterateStatement): RevealStmt");
        break;
      case CalcStmt calcStmt:
        //Console.WriteLine("BlockLine(IterateStatement): CalcStmt");
        break;
      case AssignSuchThatStmt assignSuchThatStmt:
        this.BlockCode.Add(assignSuchThatStmt.Tok.line);
        break;
      case NestedMatchStmt nestedMatchStmt:
        BlockCode.Add(nestedMatchStmt.Tok.line);
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

          var tmpIf = new IfStmt(caseNest.RangeToken, false, guard, blcIF, null);
          IterateStatement(tmpIf);
        }
    
        break;
      default:
        if (s != null) {
          Console.WriteLine("BlockCode.cs (IterateStatement): " + s.GetType());
        }
        break;
    }
  }
  
  public void WriteListToFile(string directoryPath, string fileName)
  {
    try
    {
      // Ensure the directory exists
      if (!Directory.Exists(directoryPath)) {
        Directory.CreateDirectory(directoryPath);
      }

      // Combine directory path and filename to get the full path
      string fullPath = Path.Combine(directoryPath, fileName);

      // Write all lines to the file
      var content = "[" + string.Join(",", BlockCode) + "]";
      File.WriteAllText(fullPath, content);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"An error occurred: {ex.Message} + {fileName}");
    }
  }
}
