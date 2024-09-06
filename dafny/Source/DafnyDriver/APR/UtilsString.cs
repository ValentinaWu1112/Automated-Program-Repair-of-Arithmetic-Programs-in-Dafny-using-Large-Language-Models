using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Dafny;

namespace DafnyDriver.APR;

public class UtilsString {
  public static bool userDefined = false;
  public static List<string> userDefinedLst = new List<string>();

  public static void SetUserDefinedLst(List<string> upd) {
    userDefinedLst = upd;
  }
  
  public static List<string> GetUserDefinedLst() {
    return userDefinedLst;
  }

  public static string FormalToString(List<Formal> ins, List<string> datatype) {
    string ret = String.Empty;
    for (int j=0; j<ins.Count; j++) {
      var i = ins[j];
      switch (i.Type) {
        case UserDefinedType userDefinedType:
          if (!datatype.Contains(userDefinedType.Name)) {
            string typeStr = String.Empty;
            //typeStr = string.Concat("datatype ", userDefinedType.Name, " = ");
            var resClass = userDefinedType.ResolvedClass;
            typeStr = string.Concat(typeStr, TypeToString(resClass));
            ret = string.Concat(typeStr, ret);
          } else {
            ret = string.Concat(ret, string.Concat(i.Name, ":", i.Type.ToString()));
          }
          break;
          
        default:
          ret = string.Concat(ret, string.Concat(i.Name, ":", i.Type.ToString()));
          break;
      }
      if (j < ins.Count - 1) {
        ret = string.Concat(ret, ",");
      }
    }

    return ret;
  }
  public static string TypeToString(TopLevelDecl resClass) {
    string ret = String.Empty;
    switch (resClass) {
      case IndDatatypeDecl indDatatypeDecl:
        userDefinedLst.Add(indDatatypeDecl.Name);
        ret = string.Concat("datatype ", indDatatypeDecl.Name);
        var typeParamIndLst = indDatatypeDecl.TypeArgs;
        for (int j = 0; j < typeParamIndLst.Count; j++) {
          var tmp = typeParamIndLst[j];
          userDefinedLst.Add(tmp.Name);
          if (j == 0) {
            ret = string.Concat(ret, "<");
          }

          ret = string.Concat(ret, tmp.ToString());

          if (j < typeParamIndLst.Count - 1) {
            ret = string.Concat(ret, ",");
          } else {
            ret = string.Concat(ret, ">\n");
          }
        }
        ret = string.Concat(ret, " = ");
        var ctorsInd = indDatatypeDecl.Ctors;
        for (int i = 0; i < ctorsInd.Count; i++) {
          var ctor = ctorsInd[i];
          ret = string.Concat(ret, ctor.Name);
          var formalTmp = ctor.Formals;
          if (formalTmp.Count > 0) {
            ret = string.Concat(ret, "(", FormalToString(formalTmp, new List<string>(userDefinedLst)), ")");
          }

          if (i < ctorsInd.Count - 1) {
            ret = string.Concat(ret, " | ");
          } else {
            ret = string.Concat(ret, "\n");
          }
        }
        break;
      case CoDatatypeDecl coDatatypeDecl:
        userDefinedLst.Add(coDatatypeDecl.Name);
        ret = string.Concat("codatatype ", coDatatypeDecl.Name);
        var typeParamCoLst = coDatatypeDecl.TypeArgs;
        for (int j = 0; j < typeParamCoLst.Count; j++) {
          var tmp = typeParamCoLst[j];
          userDefinedLst.Add(tmp.Name);
          if (j == 0) {
            ret = string.Concat(ret, "<");
          }

          ret = string.Concat(ret, tmp.ToString());

          if (j < typeParamCoLst.Count - 1) {
            ret = string.Concat(ret, ",");
          } else {
            ret = string.Concat(ret, ">\n");
          }
        }

        ret = string.Concat(ret, " = ");
        var ctorsCo = coDatatypeDecl.Ctors;
        for (int i = 0; i < ctorsCo.Count; i++) {
          var ctor = ctorsCo[i];
          ret = string.Concat(ret, ctor.Name);
          var formalTmp = ctor.Formals;
          if (formalTmp.Count > 0) {
            ret = string.Concat(ret, "(", FormalToString(formalTmp, new List<string>(userDefinedLst)), ")");
          }

          if (i < ctorsCo.Count - 1) {
            ret = string.Concat(ret, " | ");
          } else {
            ret = string.Concat(ret, "\n");
          }
        }
        break;
      case NewtypeDecl newtypeDecl:
        userDefinedLst.Add(newtypeDecl.Name);
        ret = string.Concat("newtype ", newtypeDecl.Name);
        
        ret = string.Concat(ret, " = ");

        var varNew = newtypeDecl.Var;
        var exprNew = newtypeDecl.Constraint;

        ret = string.Concat(ret, varNew.Name, " | ", exprNew.ToString(), "\n");
        break;
      default:
        Console.WriteLine("UtilsString (TypeToSting): "+ resClass.GetType());
        break;
    }
    return ret;
  }
  
  public static string SpecToString(List<AttributedExpression> e, string spec) {
    string ret = String.Empty;
    foreach (var exp in e) {
      if (exp == null) {
        continue;
      }
      ret = string.Concat(ret, string.Concat(spec, exp.E.ToString(),"\n"));
    }

    return ret;
  }

  
  
  public static string VerificationStmtToString(List<Statement> verificationStmt) {
    if (verificationStmt.Count == 0) {
      return String.Empty;
    }
    string ret = String.Empty;
    foreach (var stmt in verificationStmt) {
      ret = string.Concat(ret, stmt.ToString(), "\n");
    }
    return ret;
  }
  
  
  
  
  public static string MarkBuggyLine(string filePath, int lineNumber)
  {
    string[] lines = File.ReadAllLines(filePath);
    if (lineNumber < 1 || lineNumber > lines.Length)
    {
      throw new ArgumentOutOfRangeException(nameof(lineNumber), "Line number is out of range");
    }
    string line = lines[lineNumber - 1];
    line = line.TrimEnd('\r', '\n');
    line += " //buggy line\n";
    lines[lineNumber - 1] = line;
    return string.Join(Environment.NewLine, lines);
  }
  
  public static string ReplaceLineAndGetContent(string filePath, int lineNumber, string newContent)
  {
    if (lineNumber <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(lineNumber), "Line number must be greater than 0.");
    }

    // Read all lines from the file
    var lines = File.ReadAllLines(filePath);
        
    if (lineNumber > lines.Length)
    {
      throw new ArgumentOutOfRangeException(nameof(lineNumber), "Line number exceeds the number of lines in the file.");
    }

    // Replace the specific line with new content
    lines[lineNumber - 1] = newContent;
        
    // Join all lines into a single string
    return string.Join(Environment.NewLine, lines);
  }
  
  public static string InsertStringAtFile(string filePath, int endLine, int endColumn, string textToInsert) {
    string[] lines = File.ReadAllLines(filePath);
    if (endLine < 1 || endLine > lines.Length)
    {
      throw new ArgumentOutOfRangeException(nameof(endLine), "The end line is out of the file's line range.");
    }
    string targetLine = lines[endLine - 1];
    if (endColumn < 0 || endColumn > targetLine.Length)
    {
      throw new ArgumentOutOfRangeException(nameof(endColumn), "The end column is out of the target line's range.");
    }

    lines[endLine - 1] = targetLine.Substring(0, endColumn) + '\n' + textToInsert + targetLine.Substring(endColumn);
    return string.Join(Environment.NewLine, lines);
  }
}