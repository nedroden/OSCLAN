use crate::ast::{AstNode, NodeType};
use crate::tokenizer::{Token, TokenType};

pub struct Parser<'a> {
    tokens: &'a Vec<Token>,
    current_token: Option<&'a Token>,
}

impl<'a> Parser<'a> {
    pub fn new(tokens: &'a Vec<Token>) -> Self {
        Parser {
            tokens,
            current_token: tokens.iter().next(),
        }
    }

    pub fn build_ast(&mut self) -> anyhow::Result<AstNode> {
        let mut root_node = AstNode::new(NodeType::Root, "".to_string());

        while let Some(token) = self.current_token {
            println!("{:?}", token);

            let result = match token.token_type {
                TokenType::Identifier => self.parse_identifier(),
                _ => Err(anyhow::anyhow!("Token of type {:?} not implemented", token.token_type)),
            };

            match result {
                Ok(node) => root_node.add_child(&node),
                Err(err) => return Err(err)
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

    fn consume(&self, token_type: TokenType) -> anyhow::Result<&Token> {
        if let Some(token) = self.current_token {
            if token.token_type != token_type {
                return Err(anyhow::anyhow!("Unexpected token: {:?}", token_type));
            }

            return Ok(token)
        }

        Err(anyhow::anyhow!("Expected token of type {:?}, but found EOF", token_type))
    }

    fn is_at_eol(&self) -> bool {
        self.current_token.is_none()
    }

    fn parse_identifier(&self) -> anyhow::Result<AstNode<'a>> {
        let token = self.consume(TokenType::Identifier)?;

        if self.is_register(&token.value) {
            return Ok(AstNode { node_type: NodeType::Register, value: token.value.to_string(), children: vec![] });
        }

        todo!()
    }
}