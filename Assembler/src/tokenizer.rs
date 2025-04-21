use std::fs;
use std::path::PathBuf;

#[derive(Debug)]
pub struct Token {
    token_type: TokenType,
    value: String
}

#[derive(Debug)]
pub enum TokenType {
    Comma,
    Label,
    Register,
    Identifier,
    Lbracket,
    Rbracket,
    Number,
    ExclamationMark,
    Directive,
    Colon,
}

pub struct Tokenizer {
    pub tokens: Vec<Token>,
    source: std::str::Chars<'static>,
    current_char: Option<char>,
}

impl Tokenizer {
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

    fn skip_pattern(&mut self, pattern: &str) -> anyhow::Result<()> {
        self.advance_size(pattern.len());
        Ok(())
    }

    fn parse_identifier(&mut self) -> anyhow::Result<()> {
        let mut chars: Vec<char> = Vec::new();

        while !self.is_at_eol() && self.is_at_identifier() {
            chars.push(self.current_char.unwrap());
            self.advance();
        }

        let directive = chars.iter().collect::<String>();
        let token = Token {
            token_type: TokenType::Identifier,
            value: directive,
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

        while !self.is_at_eol() && self.current_char.unwrap().is_numeric() {
            chars.push(self.current_char.unwrap());
            self.advance();
        }

        let directive = chars.iter().collect::<String>();
        let token = Token {
            token_type: TokenType::Number,
            value: directive,
        };

        Ok(token)
    }

    fn advance(&mut self) {
        self.current_char = self.source.next();
    }

    fn advance_size(&mut self, n: usize) {
        for i in 0..n {
            if self.is_at_eol() {
                break;
            }

            self.advance();
        }
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