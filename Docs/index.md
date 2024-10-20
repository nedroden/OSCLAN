# OSCLAN

Compiler for OSCLAN (Old-SChool LANguage), a business-oriented toy language that compiles to AArch64 Assembly.

![Build](https://github.com/nedroden/OSCLAN/actions/workflows/build.yml/badge.svg)

## Context

I have been wanting to build a compiler for some time, however the main challenge has been the code generation part. The original plan was to build a compiler for Apple Silicon chips, however it might turn out that it would be easier to target something like MSIL first.

If you have any thoughts, feel free to share them by opening up a pull request or an issue. My main focus at this point is on getting things working, so the code will definitely not be as clean as it should be. There is no real plan involved here; I'm just making everything up as I go along.

## Usage

1. Open a terminal
2. `$ ./build.sh`
3. `./osclanc -i simple.osc` to compile `Examples/simple.osc` (the directory is currently hardcoded)

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

### Directives
| **Directive** | **Possible values** | **Description** |
| --- | --- | --- |
| import | _any string_ |  Imports a module. |
| mangler | enable, disable | Enables/disables the identifier mangler. |
| module | _any string_ | Sets the name of the current module. |

## Todo

- [ ] Code generation (AArch64 assembly)
- [ ] Finish parser
  - [ ] Increment/decrement
  - [ ] Expressions
  - [ ] If-else statements
  - [ ] Loops
- [ ] Clean up pointer mess in parser
- [ ] Refactoring
- [ ] Semantic analyzer
- [ ] Optimizer
- [ ] Imports and modules
- [ ] Statistics (e.g., number of tokens per token type)
- [ ] Syntax highlighting in VS Code
- [ ] Unit tests (once tokenizer and parser are done)
- [ ] Assembly code blocks

**Note:** optimizer and more advanced analyzer will be added _after_ a basic code generator.

## Technical Documentation

To build the docs, ensure that you have docfx installed. Then, in a terminal, run the `build-docs.sh` script. In order to view the docs, open the `Docs/_site/index.html` file in a web browser.