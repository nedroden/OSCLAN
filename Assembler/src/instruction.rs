use std::collections::HashMap;
use enum_stringify::EnumStringify;

macro_rules! register_instruction{
    ($instructions: ident, $mnemonic: expr, $operands: expr) => {
        $instructions.insert($mnemonic, Instruction{
            mnemonic: $mnemonic,
            operands: $operands
        });
    };
}

pub struct Register {
    pub width: char,
    pub number: u8,
}

pub struct Instruction {
    pub mnemonic: Mnemonic,
    pub operands: Vec<Vec<OperandType>>,
}

#[derive(Eq, PartialEq, Hash, EnumStringify)]
pub enum Mnemonic {
    Mov,
    Add,
    Bl,
    Svc,
    Ret
}

pub enum Operand {
    Register(Register),
    Immediate(u32),
    Label(String),
    Address { base: u8, offset: i8 },
}

pub enum OperandType {
    Register,
    Immediate,
    Label,
}

pub fn load_instructions() -> HashMap<Mnemonic, Instruction> {
    let mut instructions: HashMap<Mnemonic, Instruction> = HashMap::new();

    register_instruction!(instructions, Mnemonic::Mov, vec![
        vec![OperandType::Register, OperandType::Immediate],
        vec![OperandType::Register, OperandType::Label],
    ]);
    register_instruction!(instructions, Mnemonic::Ret, Vec::new());
    register_instruction!(instructions, Mnemonic::Bl, Vec::new());

    instructions
}

pub fn write_instruction() -> u64 {
    todo!()
}