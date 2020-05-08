using System;

namespace ast.tool
{

    public enum TOKEN_TYPE 
    {
        T_EOF,
        T_AND, T_OR, T_ADD, T_SUBTRACT, T_MULTIPLY, T_DIVIDE, T_INTLIT,
        T_EQUALEQUAL, T_NOTEQUAL, T_LESSTHAN, T_LESSEQUAL, T_LARGETHAN, T_LARGEEQUAL, 
        T_LIKE, 
        T_LRND, T_RRND,
        T_EQUAL,
        T_VAR, T_STR,
        T_IF, T_ELSE,T_PRINT

    }

    
    public enum ASTNODE_TYPE
    {
        A_AND = 1, A_OR, A_ADD, A_SUBTRACT, A_MULTIPLY, A_DIVIDE, A_INTLIT,
        A_EQUALEQUAL, A_NOTEQUAL, A_LESSTHAN, A_LESSEQUAL, A_LARGETHAN, A_LARGEEQUAL, 
        A_LIKE, 
        A_LRND, A_RRND,
        A_EQUAL,
        A_VAR,A_STR
    }

    //
    public struct Token
    {
        public TOKEN_TYPE token;
        public int intvalue;
        public String strName;
    }

    //Abstract Syntax Tree 
    public class AstNode
    {
        public ASTNODE_TYPE op;  //节点功能
        public AstNode leftLeaf; //左枝
        public AstNode rightLeaf; //右枝
        public bool bValue;   //bool value
        public int intValue;  //整数值
        public String strName; //字符串值

        private AstNode(ASTNODE_TYPE op, AstNode leftLeaf, AstNode rightLeaf, int intValue , String strName, bool bValue = false)
        {
            this.op = op;
            this.leftLeaf = leftLeaf;
            this.rightLeaf = rightLeaf;
            this.intValue = intValue;
            this.strName = strName;
            this.bValue = bValue;
        }


        public static AstNode mkNode(ASTNODE_TYPE in_op, AstNode leftLeaf, AstNode rightLeaf, int intValue , String strName, bool bValue =false) 
        {
            return new AstNode(in_op, leftLeaf, rightLeaf, intValue, strName, bValue);
        }

        public static AstNode mkNodewithNoLeaf(ASTNODE_TYPE in_op) 
        {
            return new AstNode(in_op, null, null, 0, "");
        }

        public static AstNode mkNodeLeft(ASTNODE_TYPE in_op , AstNode in_left, int intValue)
        {
            return new AstNode(in_op, in_left, null, intValue, "");
        } 

        public static AstNode mkNodeRight(ASTNODE_TYPE in_op , AstNode in_right, int intValue)
        {
            return new AstNode(in_op, null, in_right, intValue, "");
        } 

        // Make an AST leaf node

        public static AstNode mkastleaf(ASTNODE_TYPE op, int intvalue) {
            return (mkNode(op, null, null, intvalue, ""));
        }

        public static AstNode mkastleaf(ASTNODE_TYPE op, String strName) {
            return (mkNode(op, null, null, 0, strName));
        }

        public static AstNode mkastleaf(ASTNODE_TYPE op, bool bValue) {
            return (mkNode(op, null, null, 0,"", bValue));
        }

        // Make a unary AST node: only one child
        public static AstNode mkastunary(ASTNODE_TYPE op, AstNode left, int intvalue) {
            return (mkNode(op, left, null, intvalue, ""));
        }
        
    }
}
