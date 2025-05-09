use std::collections::HashMap;
use crate::ast::{AstNode, NodeType};
use crate::instruction::{load_instructions, Instruction, Mnemonic};
use crate::tokenizer::{Token, TokenType};

pub struct Parser<'a> {
    tokens: &'a Vec<Token>,
    current_index: usize,
    instructions: HashMap<Mnemonic, Instruction>
}

impl<'a> Parser<'a> {
    pub fn new(tokens: &'a Vec<Token>) -> Self {
        Parser {
            tokens,
            current_index: 0,
            instructions: load_instructions(),
        }
    }

    pub fn build_ast(&mut self) -> anyhow::Result<AstNode> {
        let mut root_node = AstNode::new(NodeType::Root, String::from(""));

        while !self.is_at_eol() {
            let token = &self.tokens[self.current_index];
            println!("{:?}", token);

            // We're at an empty line
            if self.is_at(TokenType::NewLine) {
                self.advance();
                continue;
            }

            let result = match token.token_type {
                TokenType::Identifier if self.relative_read_token(1)?.token_type == TokenType::Colon => self.parse_label(),
                TokenType::Identifier => self.parse_identifier(),
                TokenType::Directive => self.parse_directive(),
                _ => return Err(anyhow::anyhow!("Token of type {:?} at position {:?} not implemented", token.token_type, token.position)),
            };

            match result {
                Ok(node) => root_node.add_child(node),
                Err(err) => return Err(err),
            }

            // TODO: Kijken of dit dingen misschien stuk heeft gemaakt
            self.advance();
        }

        Ok(root_node)
    }

    fn is_register(&self, identifier: &str) -> bool {
        let regex = regex::Regex::new(r"^([RWX])").unwrap();

        if !regex.is_match(&identifier.to_uppercase()) {
            return false;
        }

        identifier[1..].chars().all(|c| c.is_digit(10)) || identifier[1..].eq_ignore_ascii_case("zr")
    }

    fn is_at(&self, token_type: TokenType) -> bool {
        if !self.is_at_eol() {
            if self.tokens[self.current_index].token_type == token_type {
                return true
            }
        }

        false
    }

    fn is_at_eol(&self) -> bool {
        self.current_index >= self.tokens.len()
    }

    fn parse_label(&mut self) -> anyhow::Result<AstNode> {
        let label_node = AstNode::new(NodeType::Label, String::from(&self.tokens[self.current_index].value));

        self.move_cursor(2);

        Ok(label_node)
    }

    fn parse_identifier(&mut self) -> anyhow::Result<AstNode> {
        let token = self.read_token()?.value.to_uppercase();
        let is_instruction = self.instructions.iter().any(|t| t.0.to_string().to_uppercase() == token);

        let mut instruction = match is_instruction {
            true => AstNode::new(NodeType::Instruction, token),
            false => return Err(anyhow::anyhow!("Expected an instruction, instead got token with value '{:?}'", token))
        };

        self.advance();

        while !self.is_at_eol() && !self.is_at(TokenType::NewLine) {
            let current_token = self.read_token()?;
            let (current_position, current_type) = (current_token.position, current_token.token_type);

            let operand_token = match current_type {
                TokenType::Identifier => current_token,
                TokenType::Number => current_token,
                _ => return Err(anyhow::anyhow!("Unexpected token of type {:?} at position {:?}", current_type, current_token.position))
            };

            instruction.children.push(match current_type {
                _ if self.is_register(&current_token.value.clone()) => AstNode::new(NodeType::Register, operand_token.value.to_string()),
                TokenType::Identifier => AstNode::new(NodeType::Label, operand_token.value.to_string()),
                TokenType::Number => AstNode::new(NodeType::Immediate, operand_token.value.to_string()),
                _ => return Err(anyhow::anyhow!("Unexpected token of type {:?} at position {:?}", current_type, current_token.position)),
            });

            self.advance();

            if !self.is_at_eol() && !self.is_at(TokenType::NewLine) && !self.is_at(TokenType::Comma) {
                return Err(anyhow::anyhow!("Expected a comma after operand at position {:?}, instead got '{:?}'", current_position, current_type))
            }

            self.advance();
        }

        if !self.is_at_eol() && !self.is_at(TokenType::NewLine) {
            return Err(anyhow::anyhow!("Expected newline at position {:?}", self.tokens[self.current_index].position))
        }

        self.advance();

        Ok(instruction)
    }

    fn parse_directive(&mut self) -> anyhow::Result<AstNode> {
        let directive = self.read_token();
        let argument = self.relative_read_token(1);

        let mut node = AstNode::new(NodeType::Directive, directive?.value.to_string());

        let operand = match self.relative_read_token(1)?.token_type {
            TokenType::Identifier => AstNode::new(NodeType::Label, argument?.value.to_string()),
            TokenType::Number => AstNode::new(NodeType::Immediate, argument?.value.to_string()),
            unexpected => return Err(anyhow::anyhow!("Unexpected token (of type {:?}) used as operand.", unexpected))
        };

        node.children.push(operand);

        self.move_cursor(2);

        if !self.is_at(TokenType::NewLine) {
            return Err(anyhow::anyhow!("Expected a newline after directive operand"));
        }

        self.advance();

        Ok(node)
    }

    fn advance(&mut self) {
        self.current_index += 1;
    }

    fn move_cursor(&mut self, delta: usize) {
        self.current_index += delta;
    }

    fn read_token(&self) -> anyhow::Result<&Token> {
        if self.is_at_eol() {
            return Err(anyhow::anyhow!("Unexpected EOL"));
        }

        Ok(&self.tokens[self.current_index])
    }

    fn relative_read_token(&self, delta: usize) -> anyhow::Result<&Token> {
        if self.current_index + delta >= self.tokens.len() {
            return Err(anyhow::anyhow!("Unexpected EOF"));
        }

        Ok(&self.tokens[self.current_index + delta])
    }

    fn print_context(&self) {
        println!("{:?} - {:?} - {:?}", self.tokens[self.current_index-1], self.read_token(), self.relative_read_token(1));
    }
}