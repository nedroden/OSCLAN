#[derive(Debug)]
pub struct AstNode {
    pub node_type: NodeType,
    pub value: String,
    pub children: Vec<AstNode>,
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

impl AstNode {
    pub fn new(node_type: NodeType, value: String) -> AstNode {
        AstNode {
            node_type,
            value,
            children: Vec::new(),
        }
    }

    pub fn add_child(&mut self, child: AstNode) {
        self.children.push(child);
    }
}