# Neoc

Compiler for Neoc (neo-COBOL), a toy language that compiles to MSIL.

## Context

I have been wanting to build a compiler for some time, however the main challenge has been the code generation part. The original plan was to build a compiler for Apple Silicon chips, however for now the idea is to build something that compiles to MSIL. Once that is done, the idea is to introduce AArch64 code generation as well.

## Feedback

My experience with Go and compiler construction is somewhat limited, so if you have any thoughts, feel free to share them by opening up a pull request or an issue.

Also, my main focus at this point is on getting things working, so the code will definitely not be as clean as it should be.

## Language specifications

### Types

| **Name**    | **Size**  | **Description**                                    |
| ----------- | --------- | -------------------------------------------------- |
| `string(n)` | `n` bytes | Field containing ASCII characters.                 |
| `int(n)`    | `n` bytes | Signed integer value.                              |
| `uint(n)`   | `n` bytes | Unsigned integer value.                            |
| `struct`    | -         | Implicit struct, only allowed in certain contexts. |

A struct can be turned into an array by appending `(n)`, where `n` denotes the number of occurrences, e.g., `struct(10)` for a list of ten items. Build-in type names are case-insensitive, hence `string` is equivalent to `STRING`.

### Keywords

The language has the following (case-insensitive) keywords:

| **Keyword** | **Description**                                                                                |
| ----------- | ---------------------------------------------------------------------------------------------- |
| declare     | Used for declaring variables, structures procedures.                                           |
| public      | Modifier used to make a procedure/struct visible outside the current file.                     |
| private     | Modifier used to make a procedure/struct invisible outside the current file.                   |
| struct      | Denotes a data structure.                                                                      |
| begin       | Start of a block.                                                                              |
| end         | End of a block.                                                                                |
| init        | Initializes a struct/type, e.g., `init string(10)` to initialize a string of 10 characters.    |
| anon        | Used in combination with `declare`, used when initializing an implicit struct.                 |
| ret         | Returns from the procedure.                                                                    |
| print       | Prints the arguments to stdout.                                                                |
| if          | Denotes a block of code that is only executed when the expression holds true.                  |
| else        | Denotes a block of code that is only executed if the preceding `if` statement is not executed. |
| then        | Start of the block of code that is executed in an `if-else` construction.                      |
| space       | Shorthand that denotes an arbitrary number of spaces.                                          |
| zero        | Shorthand that denotes an arbitrary number of zeros.                                           |
| ok          | Alias of `zero`, only used in combination with `ret`.                                          |
| for         | While-loop.                                                                                    |
| increment   | Increments a scalar value by 1                                                                 |
| decrement   | Decrements a scalar value by 1                                                                 |

## Todo

- [ ] Code generation (AArch64 assembly)
- [ ] Clean up pointer mess in parser
- [ ] Refactoring
- [ ] Analyzer
- [ ] Optimizer
- [ ] Imports
- [ ] Statistics (e.g., number of tokens per token type)
- [ ] Syntax highlighting in VS Code
- [ ] Unit tests (once tokenizer and parser are done)
