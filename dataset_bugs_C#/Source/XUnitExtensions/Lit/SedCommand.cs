using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XUnitExtensions.Lit {
  public class SedCommand : ILitCommand {

    private readonly string regexp;
    private readonly string replaceBy;
    private readonly string file;


    private SedCommand(string regexp, string replaceBy, string file) {
      this.regexp = regexp;
      this.replaceBy = replaceBy;
      this.file = file;
    }

    public static ILitCommand Parse(string[] args) {
      if (args.Length != 2) {
        throw new ArgumentException($"Wrong number of arguments for sed: {args.Length}");
      }
      var regexpReplace = args[0];
      var delimitCharacter = regexpReplace[1];
      var part = $@"(?:\\{delimitCharacter}|[^{delimitCharacter}])";
      string supportedRegexReplace = @$"s{delimitCharacter}({part}+){delimitCharacter}({part}*){delimitCharacter}";
      var parseRegex = new Regex(supportedRegexReplace);
      var match = parseRegex.Match(regexpReplace);
      if (match == null) {
        throw new NotImplementedException("No support for sed " + regexpReplace + ". Only support for " +
                                          supportedRegexReplace);
      }
      var regexp = match.Groups[1].Value;
      var replaceBy = match.Groups[2].Value;
      var file = args[1];
      return new SedCommand(regexp, replaceBy, file);
    }

    public Task<int> Execute(TextReader inputReader,
      TextWriter outputWriter, TextWriter errorWriter) {
      var fileContent = File.ReadAllText(file);
      try {
        var stdOutput = Regex.Replace(fileContent, "(?m)" + regexp, replaceBy);
        outputWriter.Write(stdOutput);
        return Task.FromResult(0);
      } catch (Exception e) {
        outputWriter.WriteLine(e.ToString());
        return Task.FromResult(1);
      }
    }

    public override string ToString() {
      return $"sedCommand {regexp} {replaceBy} {file}";
    }
  }
}
