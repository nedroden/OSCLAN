use std::collections::HashMap;
use crate::ast::{AstNode, NodeType};
use crate::instruction::{load_instructions, Instruction, Mnemonic};
use crate::tokenizer::{Token, TokenType};

pub struct Parser<'a> {
    tokens: &'a Vec<Token>,
    current_token: Option<&'a Token>,
    instructions: HashMap<Mnemonic, Instruction>
}

impl<'a> Parser<'a> {
    pub fn new(tokens: &'a Vec<Token>) -> Self {
        Parser {
            tokens,
            current_token: tokens.iter().next(),
            instructions: load_instructions(),
        }
    }

    pub fn build_ast(&mut self) -> anyhow::Result<AstNode> {
        let mut root_node = AstNode::new(NodeType::Root, String::from(""));

        while let Some(token) = self.current_token {
            println!("{:?}", token);

            let result = match token.token_type {
                TokenType::Identifier => self.parse_identifier(),
                TokenType::Directive => self.parse_directive(),
                _ => return Err(anyhow::anyhow!("Token of type {:?} not implemented", token.token_type)),
            };

            match result {
                Ok(node) => root_node.add_child(node),
                Err(err) => return Err(err),
            }
        }

        // TODO: fix this
        Ok(root_node)
    }

    fn is_register(&self, identifier: &str) -> bool {
        let regex = regex::Regex::new(r"^([RWX])").unwrap();

        if !regex.is_match(&identifier.to_uppercase()) {
            return false;
        }

        identifier[1..].chars().all(|c| c.is_digit(10)) || identifier[1..].eq_ignore_ascii_case("zr")
    }

    fn consume(&mut self, token_type: TokenType) -> anyhow::Result<&Token> {
        if let Some(token) = self.current_token {
            if token.token_type != token_type {
                return Err(anyhow::anyhow!("unexpected token of type '{:?}', expected '{:?}, at position {:?}'", token.token_type, token_type, token.position));
            }

            self.current_token = self.tokens.iter().next();

            return Ok(token)
        }

        Err(anyhow::anyhow!("Expected token of type {:?}, but found EOF", token_type))
    }

    fn is_at(&self, token_type: TokenType) -> bool {
        if let Some(token) = self.current_token {
            if token.token_type == token_type {
                return true
            }
        }

        false
    }

    fn is_at_eol(&self) -> bool {
        self.current_token.is_none()
    }

    fn parse_identifier(&mut self) -> anyhow::Result<AstNode> {
        let token = self.consume(TokenType::Identifier)?;
        let is_instruction = self.instructions.iter().any(|t| t.0.to_string().to_uppercase() == token.value.to_uppercase());

        let mut instruction = match is_instruction {
            true => AstNode::new(NodeType::Instruction, token.value.to_string()),
            false => return Err(anyhow::anyhow!("Expected an instruction"))
        };

        while !self.is_at_eol() && !self.is_at(TokenType::NewLine) {
            let current_token_type = self.current_token.unwrap().token_type;

            let operand_token = match current_token_type {
                TokenType::Identifier => self.consume(TokenType::Identifier)?,
                TokenType::Number => self.consume(TokenType::Number)?,
                _ => return Err(anyhow::anyhow!("Unexpected token of type {:?} at position {:?}", current_token_type, token.position))
            };

            if self.is_register(&token.value) {
                let operand_node = AstNode::new(NodeType::Register, operand_token.value.to_string());
                instruction.children.push(operand_node);
            }

            if !self.is_at_eol() && !self.is_at(TokenType::NewLine) {
                self.consume(TokenType::Comma)?;
            }
        }

        if !self.is_at(TokenType::Comma) {
            self.consume(TokenType::NewLine)?;
        }

        // Err(anyhow::anyhow!("Unexpected token of type '{:?}'", token))
        Ok(instruction)
    }

    // TODO: Finish implementation
    fn parse_directive(&mut self) -> anyhow::Result<AstNode> {
        _ = self.consume(TokenType::Directive)?;

        if self.is_at(TokenType::Identifier) {
            _ = self.consume(TokenType::Number)?;
        } else {
            _ = self.consume(TokenType::Identifier)?;
        }

        _ = self.consume(TokenType::NewLine)?;

        Ok(AstNode::new(NodeType::Directive, String::from("")))
    }
}