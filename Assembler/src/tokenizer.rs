use std::fs;
use std::path::PathBuf;
use anyhow::{anyhow};

#[derive(Debug)]
pub struct Token {
    pub token_type: TokenType,
    pub value: String
}

#[derive(Debug, PartialEq, Eq)]
pub enum TokenType {
    Comma,
    Label,
    Identifier,
    Lbracket,
    Rbracket,
    Number,
    ExclamationMark,
    Directive,
    Colon,
    NewLine,
}

pub struct Tokenizer<'a> {
    pub tokens: Vec<Token>,
    source: std::str::Chars<'a>,
    current_char: Option<char>,
}

impl<'a> Tokenizer<'a> {
    pub fn new() -> Self {
        Tokenizer {
            tokens: Vec::new(),
            current_char: Option::from('\0'),
            source: "".chars(),
        }
    }

    pub fn tokenize(&mut self, path: PathBuf) -> anyhow::Result<()> {
        let contents = match self.get_file(path) {
            Ok(contents) => contents,
            Err(e) => return Err(e)
        };

        self.source = Box::leak(contents.into_boxed_str()).chars();
        self.current_char = self.source.next();

        while !self.is_at_eol() {
            let current_char = self.current_char.unwrap();

            if current_char == ';' {
                self.skip_comment();
                continue;
            }

            if current_char == 0x0A as char {
                self.tokens.push(Token { token_type: TokenType::NewLine, value: "".to_string() });
                continue
            }

            if current_char.is_whitespace() {
                self.advance();
                continue;
            }

            if self.is_at_identifier() {
                self.parse_identifier()?;
                continue;
            }

            let token = match current_char {
                '.' => self.parse_directive()?,
                '#' => self.parse_number()?,
                ',' => Token { token_type: TokenType::Comma, value: String::new(), },
                ':' => Token { token_type: TokenType::Colon, value: String::new(), },
                _ => panic!("unexpected character: {}", current_char)
            };

            self.tokens.push(token);
            self.advance()
        }

        Ok(())
    }

    fn skip_comment(&mut self) {
        while !self.is_at_eol() && self.current_char.unwrap() != '\n' {
            self.advance();
        }

        if !self.is_at_eol() {
            self.advance();
        }
    }

    fn parse_identifier(&mut self) -> anyhow::Result<()> {
        let mut chars: Vec<char> = Vec::new();

        while !self.is_at_eol() && self.is_at_identifier() {
            chars.push(self.current_char.unwrap());
            self.advance();
        }

        let value = chars.iter().collect::<String>();
        let token = Token {
            token_type: TokenType::Identifier,
            value,
        };

        self.tokens.push(token);

        Ok(())
    }

    fn parse_directive(&mut self) -> anyhow::Result<Token> {
        self.advance();

        let mut chars: Vec<char> = Vec::new();

        while !self.is_at_eol() && self.is_at_identifier() {
            chars.push(self.current_char.unwrap());
            self.advance();
        }

        let directive = chars.iter().collect::<String>();
        let token = Token {
            token_type: TokenType::Directive,
            value: directive,
        };

        Ok(token)
    }

    fn parse_number(&mut self) -> anyhow::Result<Token> {
        self.advance();

        let mut chars: Vec<char> = Vec::new();

        while !self.is_at_eol() && self.current_char.unwrap().is_alphanumeric() {
            chars.push(self.current_char.unwrap());
            self.advance();
        }

        let value = chars.iter().collect::<String>();

        match get_number_as_int(&value) {
            Ok(num) => Ok(Token {
                token_type: TokenType::Number,
                value: num.to_string(),
            }),
            Err(e) => Err(anyhow!("Invalid number: {}", value))
        }
    }

    fn advance(&mut self) {
        self.current_char = self.source.next();
    }

    fn is_at_eol(&self) -> bool {
        self.current_char.is_none()
    }

    fn is_at_identifier(&self) -> bool {
        self.current_char.unwrap().is_alphanumeric() || self.current_char.unwrap() == '_'
    }

    fn get_file(&self, path: PathBuf) -> anyhow::Result<String> {
        match fs::read_to_string(path) {
            Ok(content) => Ok(content),
            Err(e) => Err(e.into()),
        }
    }
}

fn get_number_as_int(number: &str) -> anyhow::Result<i64> {
    if number.starts_with("0x") {
        return match i64::from_str_radix(&number[2..], 16) {
            Ok(num) => Ok(num),
            Err(_) => Err(anyhow!("Invalid hexadecimal number: {}", number)),
        }
    }

    if number.starts_with("0b") {
        return match i64::from_str_radix(&number[2..], 2) {
            Ok(num) => Ok(num),
            Err(_) => Err(anyhow!("Invalid binary number: {}", number)),
        }
    }

    match number.parse::<i64>() {
        Ok(num) => Ok(num),
        Err(_) => Err(anyhow!("Invalid number: {}", number)),
    }
}