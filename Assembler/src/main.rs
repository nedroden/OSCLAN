use std::path::PathBuf;
use structopt::StructOpt;
use crate::tokenizer::{Tokenizer};

mod tokenizer;
mod generator;
mod ast;
mod parser;

#[derive(Debug, StructOpt)]
#[structopt(name = "osclan assembler", about = "Experimental assembler for the AArch64 architecture")]
struct Options {
    #[structopt(parse(from_os_str), default_value = "./examples/example.s")]
    target: PathBuf
}

fn main() {
    let opt = Options::from_args();

    println!("Assembling file {}", opt.target.to_str().unwrap());

    // Step 1: Tokenization
    let mut tokenizer = Tokenizer::new();
    match tokenizer.tokenize(opt.target) {
        Ok(()) => println!("{:?}", tokenizer.tokens),
        Err(e) => {
            eprintln!("An error occurred: {}", e);
            std::process::exit(1);
        }
    };

    // Step 2: Syntactic analysis

    // Step 3: Semantic analysis?

    // Step 4: Code generation
}
