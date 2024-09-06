using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dafny;

namespace DafnyDriver.APR;

/*
 * Entailment
 * <fields> 
 *  <field> LHSCondition: conditions of the left-hand side to entail the RHSCondition
 *  <field> LHSInvariantCondition: conditions of the left-hand side, especially for the loop invariants to entail the RHSCondition
 *  <field> RHSCondition: conditions of the right-hand side
 *  <field> VerificationStmt: To control the verification statements (assert, assume, calc, etc.)
 *  <field> IsInvariant: if is a entailment to verify invariants
 * </fields>
 */
public class Entailment {
  public List<AttributedExpression> LHSCondition;
  public List<AttributedExpression> LHSInvariantCondition;
  public List<AttributedExpression> RHSCondition;
  public List<Statement> VerificationStmt = new List<Statement>();
  public bool IsInvariant;
  public Cloner Cloner = new Cloner();

  public Entailment() {
    this.LHSCondition = new List<AttributedExpression>();
    this.LHSInvariantCondition = new List<AttributedExpression>();
    this.RHSCondition = new List<AttributedExpression>();
  }

  public void AddVerificationStmt(Statement s) {
    if(!VerificationStmt.Contains(s)) {
      VerificationStmt.Add(s);
    }
  }

  public List<Statement> GetVerificationStmt() {
    return VerificationStmt;
  }
  public void AddLHSCondition(AttributedExpression e) {
    this.LHSCondition.Add(e);
  }

  public void AddLHSCondition(List<AttributedExpression> lhs) {
    LHSCondition.AddRange(lhs);
  }
  
  public void AddRHSCondition(AttributedExpression e) {
    this.RHSCondition.Add(e);
  }

  public void SetRHSCondition(List<AttributedExpression> l) {
    RHSCondition = l.ToList();
  }
  
  public void SetLHSInvariantCondition(List<AttributedExpression> l) {
    LHSInvariantCondition = l.ToList();
  }

  public void NewRHSCondition(List<AttributedExpression> l) {
    Expression rhs = null;
    foreach (var attE in l) {
      switch (attE.E) {
        case FreshExpr freshExpr:
          rhs = new FreshExpr(Cloner, freshExpr);
          break;
        case BinaryExpr binaryExpr:
          rhs = new BinaryExpr(Cloner, binaryExpr);
          break;
        case UnaryOpExpr unaryOpExpr:
          rhs = new UnaryOpExpr(Cloner, unaryOpExpr);
          break;
        case ChainingExpression chainingExpression:
          rhs = new ChainingExpression(Cloner, chainingExpression);
          break;
        case ForallExpr forallExpr:
          rhs = new ForallExpr(Cloner, forallExpr);
          break;
        case ExistsExpr existsExpr:
          rhs = new ExistsExpr(Cloner, existsExpr);
          break;
        case TypeTestExpr typeTestExpr:
          rhs = new TypeTestExpr(Cloner, typeTestExpr);
          break;
        case ConversionExpr conversionExpr:
          rhs = new ConversionExpr(Cloner, conversionExpr);
          break;
        case OldExpr oldExpr:
          rhs = new OldExpr(Cloner, oldExpr);
          break;
        case ApplySuffix applySuffix:
          rhs = new ApplySuffix(Cloner, applySuffix);
          break;
        default:
          Console.WriteLine("Entailment.cs: " + attE.E.GetType());
          /*This does not guarantee that modifications to the expression will not adversely affect the results.
           Therefore, it is necessary to create a new expression in the program.*/
          rhs = attE.E;
          break;
      }
      this.RHSCondition.Add(new AttributedExpression(rhs));
    }
  }

  public void SetInvariant(bool i) {
    this.IsInvariant = i;
  }
}