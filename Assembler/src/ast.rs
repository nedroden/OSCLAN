#[derive(Debug)]
pub struct AstNode<'a> {
    pub node_type: NodeType,
    pub value: String,
    pub children: Vec<&'a AstNode<'a>>,
}

#[derive(Debug)]
pub enum NodeType {
    Root,
    Instruction,
    Directive,
    Label,
    Register,
    Immediate,
}

impl<'a> AstNode<'a> {
    pub fn new(node_type: NodeType, value: String) -> AstNode<'a> {
        AstNode {
            node_type,
            value,
            children: Vec::new(),
        }
    }

    pub fn add_child(&mut self, child: &'a AstNode<'a>) {
        self.children.push(child);
    }
}