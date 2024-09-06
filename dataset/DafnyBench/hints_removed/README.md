# Dataset hints_removed Directory

1. **Original_Code**: Original Dafny code from the DafnyBench benchmark
2. **Correct_Code**: Dafny codes that passed verification from the `Original_Code`
3. **Fail_Code**: Information of Dafny codes that did not pass verification from the `Original_Code`
   
   - **Code**: The Dafny code not verified from the `Original_Code`
   - **MessageError**: Error messages describing why the code in `Fail_Code/Code` failed verification.

4. **Other Code**: Codes with errors other than verification errors, such as syntax errors

5. **Bugs_Code**: Insert mutation bugs into the Correct_Code

   - **Hints**: Codes with the hint `//buggy line` on the original buggy line are organized into directories named after the return type in the failed method to later analyze results by type and identify the specific buggy line number.
   - **Mutations**: Codes without hint `//buggy line` should be used to run the scripts.

6. **All_Bugs_Code**: Codes with inserted bugs, without directory division by return type, to facilitate running the APR program directly afterward
   
   - **Mutation**: Code from `Bugs_Code/Mutations` without directory division
   - **Code**: The introduction of bugs may not have resulted in valid bugs. For example, if a statement is changed from `res := x*y` to `res := y*x`, this change does not create a valid bug, as it does not cause the program to fail. Therefore, the directory `Code` contains a selection of programs that failed to pass verification.
   - **CodeBlockMethod**: List of lines of the failed method with valid instructions, ignoring comments and empty lines.
   - **FaultLocalization**: List of suspicious buggy lines of the failed method resulting after running the script `apr.sh`

7. **Fail_Bugs_Code**: Codes where mutations were not inserted because there were no arithmetic expressions.

8. **Repair**: Information about repairs for evaluation

   - **LLM ModelName**: Named with the name of the model
   - **Result**: The file is divided into three parts: the first number represents the repair attempt, the second represents the buggy line, and the final element is the patch that fixes the program.
