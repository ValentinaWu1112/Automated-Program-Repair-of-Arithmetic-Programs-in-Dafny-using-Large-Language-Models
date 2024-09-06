#!/bin/bash

# Execute in Git Bash
# Define the folder path (converted to Unix format)
FOLDER_PATH="/Path/To/Correct_Code"

# Define the Dafny executable path (already in Unix format)
DAFNY_EXEC="/Path/To/dataset_bugs_C#/Binaries/Dafny.exe"

# Define the solver path (already in Unix format)
SOLVER_PATH="/Path/To/dataset_bugs_C#/Binaries/z3/bin/z3.exe"

# Change to the specified folder
cd "$FOLDER_PATH" || { echo "Failed to navigate to folder: $FOLDER_PATH"; exit 1; }

# Loop through each file in the folder
for file in *.dfy; do
  if [[ -f "$file" ]]; then
    # Execute the Dafny verification command
    "$DAFNY_EXEC" verify --solver-path "$SOLVER_PATH" --verification-time-limit "20" "$file"
  fi
done
