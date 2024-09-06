# Benchmark

## Datasets Overview

Our dataset utilizes two distinct benchmarks:

### 1. DafnyBench Dataset

We used DafnyBench as our primary benchmark, which is a collection of over 750 Dafny programs designed for formal software evaluation [DafnyBench](https://github.com/sun-wendy/DafnyBench). This dataset includes:
- **ground_truth**: This folder contains source programs compiled by scraping GitHub with the label 'language: Dafny' via the GitHub API. The content was collected until the end of 2023. The dataset also integrates programs from the Clover benchmark [Clover](https://github.com/ChuyueSun/Clover/tree/main) and the Dafny-Synthesis benchmark [dafny-synthesis](https://github.com/Mondego/dafny-synthesis/tree/master).
- **hint_removed**: This folder contains results from applying a process described in the paper to the programs in the 'ground_truth' folder. This process involves removing hints, invariants, and assertions from the original programs, prompting a large language model (LLM) to regenerate these hints, and then verifying them using Dafny.

### 2. Recursive Benchmark

The second benchmark, [Recursive Benchmark](https://github.com/maple-repair/recursive-benchmark), was processed by translating the original code into the dataset using ChatGPT. However, we did not use this dataset for analysis in this project.

- **Original Paper**: For further reading on the Recursive Benchmark, refer to the original paper: [Supporting Controlled Experimentation with Testing Techniques: An Infrastructure and its Potential Impact](https://link.springer.com/article/10.1007/s10664-005-3861-2).
- **Original Dataset**: You can access the original dataset at [https://maple-repair.github.io/](https://maple-repair.github.io/).
- **Translation**: The translation of the original code of the Recursive Benchmark to Dafny code was performed using [ChatGPT](https://chat.openai.com/share/2d9d3dd2-ab91-4858-9d6f-bfedb45c3787).



# Transformation Rules for Arithmetic Expressions

This document outlines the rules for transforming arithmetic expressions in the dafny code obtained from the datasets: [DafnyBench](https://github.com/ChuyueSun/Clover/tree/main). 

## Transformation Rules

### Change operators

1. **Addition to Subtraction**
   - Replace addition (`+`) with subtraction (`-`).
   - Example: `a + b` becomes `a - b`.

2. **Subtraction to Addition**
   - Replace subtraction (`-`) with addition (`+`).
   - Example: `a - b` becomes `a + b`.

3. **Multiplication to Division**
   - Replace multiplication (`*`) with division (`/`).
   - Example: `a * b` becomes `a / b`.

4. **Division to Multiplication**
   - Replace division (`/`) with multiplication (`*`).
   - Example: `a / b` becomes `a * b`.

5. **Modulo to Multiplication**
    - Replace modulo(`%`) with multiplication(`*`).
    - Example: `a % b` becomes `a * b`

### Change coefficients

- Replace each coefficient in the expression with a random number within the range of `-coefficient` to `+coefficient`.

- Example: `x := a % 2` could become `x := a % 1`

### Rearrange variables

- Randomly change and use variables defined in the program within the expression.
- Example: `sum := sum + i * i` could become `sum := i + i * sum`

### Combine all the above

- Apply a combination of the previous three techniques
- Example: `sum := sum + 2 * i` could become `sum := i - 0 / sum`