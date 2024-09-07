# Automated Program Repair of Arithmetic Programs in Dafny using Large Language Models

**Author:** Valentina Wu  
**Email:** [up201907483@edu.fe.up.pt](mailto:up201907483@edu.fe.up.pt)  
**Supervisor:** Prof. Alexandra Mendes  
**Degree:** Master in Informatics and Computing Engineering  
**University:** Faculdade de Engenharia, Universidade do Porto

## Abstract

As our daily lives are increasingly dependent on software, it becomes essential to guarantee that systems behave as expected, as software failures can cause loss of information, financial loss, and even loss of life.

In software development, testing plays a crucial role. While developers typically use software tests, these may not sufficiently express and verify the system's intended behavior. To provide stronger software reliability guarantees, programs can be annotated with formal specifications to verify whether they demonstrate the expected behavior. Developers can use these specifications as the basis for verifying that the program does indeed behave as intended. Verification-aware languages, such as Dafny, support the writing of formal specifications and the formal verification of such programs. When it is not possible to verify the program, either the specification or the program is incorrect.

Automated Program Repair (APR) is a technique that automates the process of identifying and fixing errors in code. It encompasses a diversity of steps: finding the bug, producing source code-level patches to address these bugs, and using an oracle to validate the patch, often in the form of a test suite. While tests are more commonly employed, other oracles, such as formal specifications, can be used. 

This paper aims to implement a tool that uses APR based on strong correctness criteria, using logical constructs such as pre- and post-conditions, and invariants to repair programs written in Dafny. In this work, the formal specification is assumed to be correct, and, therefore, it will guide the repair process for the arithmetic bugs.

To reach our goals, we use and adapt existing approaches for fault localization and generation of the patches. For bug localization, we use the formal specification and Hoare rules to assess the logical constraint of the program state and identify the errors.
For patch generation, we explore using Large Language Models (LLMs), specifically GPT4, LlaMa3, and Mistral7b.
The faulty statements will be replaced by the generated patches, and the resulting program will be verified against its specifications. 

Real examples of Dafny programs will be gathered from DafnyBench to evaluate the tool's effectiveness. We achieved 89.6\% accuracy in identifying the buggy lines in the dataset, with GPT-4 performing the best, successfully repairing 74.18\% of the buggy programs, followed by LlaMa 3 and Mistral 7B, which achieved 47.07\% and 45.85\% repair rates, respectively.

**Keywords:** Automated Program Repair, Fault Localization, Large Language Model (LLM), Dafny, Formal Verification

## Project Information

This project is based on the open-source Dafny repository:

- [Dafny - Official Repository](https://github.com/dafny-lang/dafny)

I have created a fork of this repository for the purpose of this project:

- [Valentina Wu's Dafny Fork](https://github.com/ValentinaWu1112/dafny)

The implementation of the project is stored in a this repository.

## Getting Started

To get started with this project, follow these steps:

1. **Install Dafny**: Follow the installation instructions for Dafny available at [Dafny Installation](https://dafny.org/latest/Installation).

2. **Install Rider**: Use Rider to view and build Dafny projects. Rider supports C# and is an integrated development environment (IDE) suitable for working with Dafny code.

3. **Install LM Studio**: LM Studio is used to run local Large Language Models (LLMs).

4. **Install Python**: Python is required to run Jupyter notebooks. Make sure Python is installed and set up on your system.

5. **Install Z3**: Z3 is a theorem prover used for verification. Follow the installation instructions at [Z3 Installation](https://github.com/Z3Prover/z3).

## Local Models and API Keys

### Local Models

For running local LLMs, you can use the following models:
- **Llama3**: [lmstudio-community/Meta-Llama-3-8B-Instruct-GGUF](https://huggingface.co/lmstudio-community/Meta-Llama-3-8B-Instruct-GGUF)
- **Mistral7b**: [TheBloke/Mistral-7B-Instruct-v0.2-GGUF](https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF)
- **Llemma7b**: [TheBloke/llemma_7b-GGUF](https://huggingface.co/TheBloke/llemma_7b-GGUF)

### API Key for GPT-4

To use GPT-4, you need an API key. Make sure to obtain your API key from the service provider and set it up according to their documentation. This key will be required to make API requests and interact with GPT-4.

## Instructions to Run the Program

1. **Install Dependencies**  
   After installing the required tools, ensure that Z3 is placed in the `Binaries` directory within the [dafny](dafny) directory. If the `Binaries` directory does not exist, create it inside the [dafny](dafny) directory and place Z3 there.

2. **Configure Models**
 
    2.1 Using Local Models

    - Choose one of the local models described in [Local Models](#local-models).
    - Install the local model in LM Studio.
    - In LM Studio, select the installed model and click 'Start Server' in the Local Server tab.
    - Extract the base_url, API key, and model name from the example `chat(python)` provided by LM Studio.
    - Open [APR.cs](dafny\Source\DafnyDriver\APR\APR.cs) and change to the appropriate model. 
    - Update the url, model name, and API key in the model configuration. For example, if you are using the Llama model, update the information in [RepairLlama3.cs](dafny\Source\DafnyDriver\APR\Model\RepairLlama3.cs). Currently, the setup is `model = "LM Studio Community/Meta-Llama-3-8B-Instruct-GGUF"` and the model is initialized with `Models = new APIModels(model, "lm-studio", "http://localhost:1234/v1")`. Verify this information in LM Studio and adjust it accordingly if it differs.

    2.2 Using GPT-4 Model
    - To use the GPT-4 model, you will need an API Key. Set this key as an environment variable.
    - In [APR.cs](dafny\Source\DafnyDriver\APR\APR.cs), select [RepairGPT4o](dafny\Source\DafnyDriver\APR\Model\RepairGPT4o.cs((dafny\Source\DafnyDriver\APR\Model\RepairGPT4o.cs) as your model.

### With Rider

3. **Configure Rider**  
   Open Rider and select the solution file `Dafny.sln`. 
   - Go to the 'Run Configuration' settings.
   - Set the 'Program arguments' to:
     ```
     verify --solver-path "Path\To\dafny\Binaries\z3\bin\z3.exe" --verification-time-limit "20" "Path\To\DafnyProgram.dfy"
     ```

4. **Run the Project**  
   Click the 'Run' button at the bottom of Rider and select 'Dafny' to execute the program.

### Without Rider

3. **Build the Program**  
   Navigate to the [dafny](dafny) directory and build the program.

4. **Run the Command**  
   Use the following command to run the program:

   ```
   $DAFNY_EXEC" verify --solver-path "$SOLVER_PATH" --verification-time-limit 20 "$file
   ```

    - Set `DAFNY_EXEC` to the path of `Dafny.exe`, e.g., `/Path/To/dafny/Binaries/Dafny.exe`.
    - Set `SOLVER_PATH` to the path of `z3.exe`, e.g., `/Path/To/dafny/Binaries/z3/bin/z3.exe`
    - Replace `"$file"` with the path to your Dafny program file.

## Procedure for Dataset Preparation and Result 

To run the program using the hints_removed dataset:

1. Execute the script [`divide_bench.sh`](bash/divide_bench.sh) and set the variable `initialize = true` in [`CliCompilation.cs`](dataset_bugs_C%23/Source/DafnyDriver/CliCompilation.cs). You will also need to modify the file to specify the paths where the data should be saved. This will differentiate the codes into two directories: `Correct_Code` for code without verification errors and `Fail_Code` for code with verification errors, such as unproven post-conditions.
 Next, run the cells labelled `# Other_Code dir 1` and `# Other_Code dir 2` in the [`bench.ipynb`](dataset\DafnyBench\hints_removed\bench.ipynb) notebook to extract the codes with syntax errors and save them in the `Other_Code` directory.

2. Move on, the insertion of bugs in the codes from the `Correct_Code` directory will be saved in `Bugs_Code` by running the script [`mutation.sh`](bash/mutation.sh) and setting the variable `bugs_mut=true` in [`CliCompilation.cs`](dataset_bugs_C%23/Source/DafnyDriver/CliCompilation.cs). Be sure to update the file to specify the correct paths for saving the data. The modified code will be stored in different subdirectories to distinguish by return type.


3. After that, run the code cell `# Bugs_Code\Mutations ->  All_Bugs_Code\Mutations` in the [`bench.ipynb`](dataset\DafnyBench\hints_removed\bench.ipynb) to merge all code files under `All_Bugs_Code\Mutations` without separating them by return type directories, facilitating the tool's execution for subsequent result analysis. Afterward, run the code cell labeled `# Fail_Bugs_Code Dir` in [`bench.ipynb`](dataset\DafnyBench\hints_removed\bench.ipynb) to identify codes where the mutation failed to insert since the program does not have an arithmetic expression.

4. Inside the `All_Bugs_Code\Mutation` directory, run the [`divide_bench1.sh`](bash\divide_bench1.sh) script and set `analyze=true` in [`CliCompilation.cs`](dataset_bugs_C%23/Source/DafnyDriver/CliCompilation.cs) to obtain code with valid mutation insertions—i.e., code that fails verification. Moreover, save them in the `All_Bugs_code\Code` directory.

5. Finally, execute the script [`apr.sh`](bash\apr.sh) to run the tool for the dataset in `All_Bugs_code\Code` directory.

To evaluate the tool, we need to do additional steps:

1. Uncomment the lines related to evaluating the tool in [`FaultLocalization.cs`](dafny/Source/DafnyDriver/APR/FaultLocalization.cs) and [`Model_LLM`](dafny/Source/DafnyDriver/APR/Model), and specify the folder path for recording the fault localization and repair fix results before running the [`apr.sh`](bash/apr.sh) script. Note that `Model_LLM` contains different programs to execute with various LLM models, so make sure to configure it according to the specific model you wish to use.

2. Run the [`codeblock.sh`](bash\codeblock.sh) bash script and set `blockCodeMet = true` in [`CliCompilation.cs`](dataset_bugs_C%23/Source/DafnyDriver/CliCompilation.cs) to have the block of lines with instructions valid of the failed method saved in `All_Bugs_Code\Code_Block_Method` to compare and evaluate the fault localization.

3. Execute each cell in the Jupyter notebooks: [`fault_loc.ipynb`](dataset\DafnyBench\hints_removed\fault_loc.ipynb), [`repair.ipynb`](dataset\DafnyBench\hints_removed\repair.ipynb), [`repair1.ipynb`](dataset\DafnyBench\hints_removed\repair1.ipynb) to obtain the graphics that appear in the *Result Chapter*.

The process is based on the files inside [`dataset/hints_removed`](dataset\DafnyBench\hints_removed). If you want to run the process using [`dataset/ground_truth`](dataset\DafnyBench\ground_truth), the procedure is the same, but you need to ensure that the files related to modifying the dataset directories are located in [`dataset/ground_truth`](dataset\DafnyBench\ground_truth) with the same names. Additionally, when you make changes to the C# programs, such as modifying variables, updating folder paths, or selecting an LLM model, you must rebuild the project after making these changes.


