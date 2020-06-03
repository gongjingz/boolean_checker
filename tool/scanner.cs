using System;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

namespace ast.tool
{

    public class Scanner
    {
        private String scan_str;  //需要扫描字符串
        private int ipos;  //扫描当前位置
        private int iLine;  //当前所在行
        private int Putback;  //暂存字符
        private const int EOF = -1;
        private String json_str;

        private Dictionary<String , String> value_dic; 
        public Scanner(String in_str)
        {
            this.scan_str = in_str;
            ipos = 0;
            iLine = 1;
            Putback = 0;
            value_dic = new Dictionary<String, String>();
        }

        //precedency of all the node type
        private int[] OpPrec = {
            0, 10, 10,			// T_EOF, A_AND = 1, A_OR
            20, 20,	30, 30, 0,		// A_ADD, A_SUBTRACT, A_MULTIPLY, A_DIVIDE, A_INTLIT
            11, 11,	11, 11, 11, 11,		// A_EQUALEQUAL, A_NOTEQUAL, A_LESSTHAN, A_LESSEQUAL, A_LARGETHAN, A_LARGEEQUAL, 
            20,		// A_LIKE,
            0, 0,  //A_LRND, A_RRND,
            10,        //A_EQUAL
            0, 0,    //T_VAR,T_STR,
            20, //T_SUBSTR
            1   //T_COMMA
        };

        //set actual field value from json
        private void setJson(String json_str)
        {
            String temp_name = "";
            this.json_str = json_str;
            JsonTextReader reader = new JsonTextReader(new StringReader(json_str));
            reader.Read();
            if (reader.TokenType != JsonToken.StartObject)
            {
                 throw new ArgumentException("invalid Json format line: " + reader.LineNumber + " position:" + reader.LinePosition);
            }
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    return;
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        temp_name = reader.Value.ToString();
                    }
                    else {
                        if (temp_name != "")
                        {
                            value_dic.Add(temp_name, reader.Value.ToString());
                            temp_name = "";
                        }
                    }
                    //throw new ArgumentException("invalid json format Token: " + reader.TokenType + " Value: " + reader.Value);
                }               
            }
            if (reader.TokenType != JsonToken.EndObject)
            {
                 throw new ArgumentException("invalid Json format line: " + reader.LineNumber + " position:" + reader.LinePosition);
            }
        }

        //get the next char from rule text
        private int next()
        {
            int c;

            if (Putback > 0)
            {
                c = Putback;
                Putback = 0;
                return c;
            }
            if (ipos == scan_str.Length)
                return EOF;
            c = scan_str.Substring(ipos,1).ToCharArray()[0];
            ipos += 1;
            

            if ('\n' == c)
                iLine++;
            return c;
        }

        //put one char back 
        private void putback(int c) 
        {
            this.Putback  = c;
        }

        //skip space and \r\n\t\f in the rule text
        private int skip()
        {
            int c;
            c = next();
            while (' ' == c || '\t' == c || '\n' == c || '\r' == c || '\f' == c)
            {
                c = next();
            }
            return c;

        }

        //scan identifier in the rule text, such as [field1]
        private int scanident(int c, out String buf, int lim = 128) {
            int i = 0;
            c = next();
            StringBuilder strbuilder = new StringBuilder(20);
            // Allow digits, alpha and underscores
            while (Char.IsLetter((char)c) || Char.IsDigit((char)c) || '_' == c) {
                // Error if we hit the identifier length limit,
                // else append to buf[] and get next character
                if (lim - 1 == i) {
                    throw new ArgumentException("identifier too long on line " +  iLine);
                    //sexit(1);
                } else if (i < lim - 1) {
                    strbuilder.Append((char)c);
                }
                c = next();
            }
            if (']' != c)
            {
                throw new ArgumentException("identifier definition not end" + iLine);
            }
            buf = strbuilder.ToString();
            return (i);
        }

        //scan str enclosed by single quotation mark in the rule text, such as 'CN'
        public int scanstr(int c, out String buf, int lim = 128) {
            int i = 0;
            c = next();
            StringBuilder strbuilder = new StringBuilder(20);
            // Allow digits, alpha and underscores and %
            while (Char.IsLetter((char)c) || Char.IsDigit((char)c) || '_' == c || '%' == c) {
                // Error if we hit the identifier length limit,
                // else append to buf[] and get next character
                if (lim - 1 == i) {
                    throw new ArgumentException("identifier too long on line " +  iLine);
                    //sexit(1);
                } else if (i < lim - 1) {
                    strbuilder.Append((char)c);
                }
                c = next();
            }
            if ('\'' != c)
            {
                //Console.WriteLine("identifier definition not end", iLine);
                throw new ArgumentException("identifier definition not end at position:" +  ipos);
            }

            buf = strbuilder.ToString();
            return (i);
        }

        //scan keyword in the rule text, such as 'like' or 'substr'
        public int scankeyword(int c, out String buf, int lim = 128) {
        int i = 0;
        StringBuilder strbuilder = new StringBuilder(20);
        // Allow digits, alpha and underscores
        while (Char.IsLetter((char)c) || Char.IsDigit((char)c) || '_' == c) {
            // Error if we hit the identifier length limit,
            // else append to buf[] and get next character
            if (lim - 1 == i) {
                throw new ArgumentException("identifier too long on line " + iLine);
                //sexit(1);
            } else if (i < lim - 1) {
                strbuilder.Append((char)c);
            }
            c = next();
        }
        putback(c);
        // We hit a non-valid character, put it back.
        // NUL-terminate the buf[] and return the length
        //putback(c);
        //buf[i] = '\0';
        buf = strbuilder.ToString();
        return (i);
        }

        //scan int from the rule text , such as 10,100
        public int scanint(int c) {
            int k, val = 0;

            // Convert each character into an int value
            while ((k = chrpos("0123456789", c)) >= 0) {
                val = val * 10 + k;
                c = next();
            }

            // We hit a non-integer character, put it back.
            putback(c);
            return val;
        }        

        //get the token type 
        private TOKEN_TYPE keyword(String buf) {
           switch (buf.ToUpper()) {
               
                //case "ELSE":
                //return (TOKEN_TYPE.T_ELSE);
                //break;
                //case "IF":
                //return (TOKEN_TYPE.T_IF);
                //break;
                //case "PRINT":
                //return (TOKEN_TYPE.T_PRINT);
                case "SUBSTR":
                return (TOKEN_TYPE.T_SUBSTR);
                //break;
                case "AND":
                return (TOKEN_TYPE.T_AND);
                //break;
                case "OR":
                return (TOKEN_TYPE.T_OR);
                //break;
                case "LIKE":
                return (TOKEN_TYPE.T_LIKE);
                //break;
            }
            return (0);
        }

        //scan the whole text for the next token
        private int scan(ref Token t)
        {
            int c;
            t = new Token();
            c = skip();
            switch(c) {
                case EOF:
                    t.token = TOKEN_TYPE.T_EOF;
                    return 0;
                case '+':
                    t.token = TOKEN_TYPE.T_ADD;
                    break;
                case '-':
                    t.token = TOKEN_TYPE.T_SUBTRACT;
                    break;
                case '*':
                    t.token = TOKEN_TYPE.T_MULTIPLY;
                    break;
                case '/':
                    t.token = TOKEN_TYPE.T_DIVIDE;
                    break;
                case '(':
                    t.token = TOKEN_TYPE.T_LRND;
                    break;
                case ')':
                    t.token = TOKEN_TYPE.T_RRND;
                    break;
                case ',':
                    t.token = TOKEN_TYPE.T_COMMA;
                    break;
                case '=':
                    if ((c = next()) == '=') {
                        t.token = TOKEN_TYPE.T_EQUALEQUAL;
                    } else {
                        putback(c);
                        throw new ArgumentException("Unrecognised character " + (char)c + " on line " + iLine
                        + " at position " + ipos);
                    }
                    break;
                case '!':
                    if ((c = next()) == '=') {
                        t.token = TOKEN_TYPE.T_NOTEQUAL;
                    } else {
                         throw new ArgumentException("Unrecognised character " + (char)c + " on line " + iLine
                        + " at position " + ipos);
                        //fatalc("Unrecognised character", c);
                    }
                    break;
                case '<':
                    if ((c = next()) == '=') {
                        t.token = TOKEN_TYPE.T_LESSEQUAL;
                    } else {
                        putback(c);
                        t.token = TOKEN_TYPE.T_LESSTHAN;
                    }
                    break;
                case '>':
                    if ((c = next()) == '=') {
                        t.token = TOKEN_TYPE.T_LARGEEQUAL;
                    } else {
                        putback(c);
                        t.token = TOKEN_TYPE.T_LARGETHAN;
                    }
                    break;
                default:
                    //int num1;
                    if (Char.IsDigit((char)c) == true)
                    {
                        t.intvalue = scanint(c);
                        t.token = TOKEN_TYPE.T_INTLIT;
                        break;
                    } else if (c == '[')  //[var]
                    {
                        string buf;
                        scanident(c, out buf);
                        
                        t.token = TOKEN_TYPE.T_VAR;
                        t.strName = buf;
                        break;
                    }  else if (c == '\'')  //[str]
                    {
                        string buf;
                        scanstr(c, out buf);
                        
                        t.token = TOKEN_TYPE.T_STR;
                        t.strName = buf;
                        break;
                    } else
                    {
                        string buf;
                        scankeyword(c, out buf);
                        TOKEN_TYPE tokentype = keyword(buf);
                        if (tokentype != 0) {
                            t.token = tokentype;
                            break;
                        }
                    }

                    //Console.WriteLine("Unrecognised character %c on line %d\n", c, iLine);
                    throw new ArgumentException("Unrecognised character " + (char)c + " on line " + iLine
                        + " at position " + ipos);
                    //return 0;
            }
            return 1;
        }

            

        private int chrpos(String s, int c) {
            int p;
            p = s.IndexOf((char)c);
            if (p >= 0)
            {
                return p;
            }
            else 
                return - 1;
        }

        //when parenthesis
        private AstNode rnd(ref Token token)
        {
            AstNode left, right;
            int ptp = 0;
            scan(ref token);
            left = binexpr(ptp, ref token);
            if (token.token == TOKEN_TYPE.T_RRND)
            {
                right = AstNode.mkNodewithNoLeaf(ASTNODE_TYPE.A_RRND);
                left = AstNode.mkNode(ASTNODE_TYPE.A_LRND, left, right, 0, "");
                scan(ref token);
                return left;
            }
            else
            {
                //Console.WriteLine("invalid round");
                throw new ArgumentException("invalid parenthesis at position " + ipos);
                //return null;
            }
            
        }

        //for substr function in the rule text
        public AstNode mksubstr(ref Token token)
        {
            AstNode left; //, right;
            int ptp = 0;
            scan(ref token); //skip "SUBSTR"
            if (token.token == TOKEN_TYPE.T_LRND) {
                scan(ref token); //skip "("
                left = binexpr(ptp, ref token);
                if (token.token == TOKEN_TYPE.T_RRND)
                {
                    //right = AstNode.mkNodewithNoLeaf(ASTNODE_TYPE.A_RRND);
                    left = AstNode.mkNode(ASTNODE_TYPE.A_SUBSTR, left.leftLeaf, left.rightLeaf, 0, "");
                    scan(ref token); //skip ")"
                    return left;
                }
            }

                //Console.WriteLine("invalid round");
                throw new ArgumentException("invalid parenthesis at position " + ipos);
                //return null;
            
        }

        //generate the ast tree
        private AstNode binexpr(int ptp, ref Token token) {
            AstNode left, right;
            TOKEN_TYPE tokentype;
            if (token.token == TOKEN_TYPE.T_LRND) {
                //scan(ref token);
                left = rnd(ref token);
            }
            else if (token.token == TOKEN_TYPE.T_SUBSTR)
            {
                left = mksubstr(ref token);
            }
            else 
            // Get the integer literal on the left.
            // Fetch the next token at the same time.
                left = primary(ref token);
            // If no tokens left, return just the left node
            tokentype = token.token;
            if (tokentype == TOKEN_TYPE.T_EOF || tokentype == TOKEN_TYPE.T_RRND)
                return (left);
            //int ptp_now = op_precedence(tokentype);

            // While the precedence of this token is
            // more than that of the previous token precedence
            while (op_precedence(tokentype) > ptp) {
                // Fetch in the next integer literal
                scan(ref token);
                //tokentype = token.token;
               
                // Recursively call binexpr() with the
                // precedence of our token to build a sub-tree
                right = binexpr(op_precedence(tokentype), ref token);

                // Join that sub-tree with ours. Convert the token
                // into an AST operation at the same time.
                left = AstNode.mkNode((ASTNODE_TYPE)tokentype, left, right, 0, "");

                // Update the details of the current token.
                // If no tokens left, return just the left node
                tokentype = token.token;
                if (tokentype == TOKEN_TYPE.T_EOF || tokentype == TOKEN_TYPE.T_RRND)
                    return (left);
            }

            // Return the tree we have when the precedence
            // is the same or lower
            return (left);
        }

        //get the left leaf of an operator
        private AstNode primary(ref Token token) {
            AstNode n;
            // For an INTLIT token, make a leaf AST node for it
            // and scan in the next token. Otherwise, a syntax error
            // for any other token type.
            switch (token.token) {
                case TOKEN_TYPE.T_INTLIT:
                    n = AstNode.mkastleaf(ASTNODE_TYPE.A_INTLIT, token.intvalue);
                    scan(ref token);
                    return (n);
                case TOKEN_TYPE.T_VAR:
                    n = AstNode.mkastleaf(ASTNODE_TYPE.A_VAR, token.strName);
                    scan(ref token);
                    return(n);
                case TOKEN_TYPE.T_STR:
                    n = AstNode.mkastleaf(ASTNODE_TYPE.A_STR, token.strName);
                    scan(ref token);
                    return(n);
                //case TOKEN_TYPE.T_LRND:
                //    n = AstNode.mkNodewithNoLeaf(ASTNODE_TYPE.A_LRND);
                //    scan(ref token);
                //    return(n);
                //case TOKEN_TYPE.T_RRND:
                //    n = AstNode.mkNodewithNoLeaf(ASTNODE_TYPE.A_RRND);
                //    scan(ref token);
                //    return(n);
                default:

                    //Console.WriteLine("syntax error on line {0:G}", iLine);
                    throw new ArgumentException("syntax error on line " +  iLine + " at position " + ipos);
                    //Application.exit();
                    //return(null);
                //fprintf(stderr, "syntax error on line %d\n", Line);
                //exit(1);
                    
            }
        }

        // Convert a binary operator token into an AST operation.
        // We rely on a 1:1 mapping from token to AST operation
        //public ASTNODE_TYPE arithop(TOKEN_TYPE tokentype) {
        //    if (tokentype > TOKEN_TYPE.T_EOF && tokentype < TOKEN_TYPE.T_INTLIT)
        //        return ((ASTNODE_TYPE)tokentype);
            //fatald("Syntax error, token", tokentype);
            
        //}

        // Operator precedence for each token. Must
        // match up with the order of tokens in defs.h
        //A_AND = 1, A_OR, A_ADD, A_SUBTRACT, A_MULTIPLY, A_DIVIDE, A_INTLIT,
        //A_EQUALEQUAL, A_NOTEQUAL, A_LESSTHAN, A_LESSEQUAL, A_LARGETHAN, A_LARGEEQUAL, 
        //A_LIKE, 
        //A_LRND, A_RRND,
        //A_EQUAL

        // Check that we have a binary operator and
        // return its precedence.
        private int op_precedence(TOKEN_TYPE tokentype) {
            int prec = OpPrec[(int)tokentype];
            if (prec == 0) {
                //Console.WriteLine("Syntax error, token {0:G}", tokentype);
                throw new ArgumentException("Syntax error, token " +  tokentype + " at position " + ipos);
            }
                //fatald("Syntax error, token", tokentype);
            return (prec);
        }

        

        private int interpretAST(AstNode n) {
            int leftval = 0, rightval = 0;

            if (n.leftLeaf != null) leftval = interpretAST(n.leftLeaf);
            if (n.rightLeaf != null) rightval = interpretAST(n.rightLeaf);

            switch (n.op) {
                case ASTNODE_TYPE.A_ADD:      return (leftval + rightval);
                case ASTNODE_TYPE.A_SUBTRACT: return (leftval - rightval);
                case ASTNODE_TYPE.A_MULTIPLY: return (leftval * rightval);
                case ASTNODE_TYPE.A_DIVIDE:   return (leftval / rightval);
                case ASTNODE_TYPE.A_INTLIT:   return (n.intValue);
                case ASTNODE_TYPE.A_LRND: return (leftval);
                case ASTNODE_TYPE.A_RRND: return 0;
                case ASTNODE_TYPE.A_AND: return Convert.ToInt32((leftval > 0?true:false)  && (rightval > 0?true:false));
                case ASTNODE_TYPE.A_OR: return Convert.ToInt32((leftval>0?true:false)  || (rightval>0?true:false));
                case ASTNODE_TYPE.A_EQUALEQUAL: return Convert.ToInt32(leftval  == rightval?true:false);
                case ASTNODE_TYPE.A_NOTEQUAL: return Convert.ToInt32(leftval  != rightval?true:false);
                case ASTNODE_TYPE.A_LESSTHAN: return Convert.ToInt32(leftval  < rightval?true:false);
                case ASTNODE_TYPE.A_LESSEQUAL: return Convert.ToInt32(leftval  <= rightval?true:false);
                case ASTNODE_TYPE.A_LARGETHAN: return Convert.ToInt32(leftval  > rightval? true:false);
                case ASTNODE_TYPE.A_LARGEEQUAL: return Convert.ToInt32(leftval  >= rightval?true:false);
                //case ASTNODE_TYPE.A_LIKE: return leftval.
                default:
                    throw new ArgumentException("Unknown AST operator " +  n.op);
                //fprintf(stderr, "Unknown AST operator %d\n", n->op);
                //return(1);
            }
        }


        //check the rule sequence
        public bool checkRule()
        {
            Token token = new Token();
            AstNode node;
            scan(ref token);
            node = binexpr(0, ref token);
            if (checkAST(ref node))
                return true;
            else
                return false;
        }
        //check the rule 
        public bool checkAST(ref AstNode n) {
            AstNode leftval , rightval ;

            if (n.leftLeaf != null)  {
                if (!checkAST(ref n.leftLeaf))
                    return false;
            }
            if (n.rightLeaf != null) {
                if (!checkAST(ref n.rightLeaf))
                    return false;
            } 
            leftval = n.leftLeaf;
            rightval = n.rightLeaf;
            switch (n.op) {
                case ASTNODE_TYPE.A_ADD:      
                case ASTNODE_TYPE.A_SUBTRACT: 
                case ASTNODE_TYPE.A_MULTIPLY: 
                case ASTNODE_TYPE.A_DIVIDE:  
                case ASTNODE_TYPE.A_LESSTHAN: 
                case ASTNODE_TYPE.A_LESSEQUAL: 
                case ASTNODE_TYPE.A_LARGETHAN: 
                case ASTNODE_TYPE.A_LARGEEQUAL: 
                    if (!isDigitNode(leftval))
                        throw new ArgumentException("Syntax error, field " +  descAstNode(leftval));
                    if (!isDigitNode(rightval))
                        throw new ArgumentException("Syntax error, field " +  descAstNode(rightval) );
                    return true; 
                case ASTNODE_TYPE.A_EQUALEQUAL: 
                case ASTNODE_TYPE.A_NOTEQUAL: 
                case ASTNODE_TYPE.A_LIKE: 
                    if (!isValueNode(leftval) && !isComputeNode(leftval))
                        throw new ArgumentException("Syntax error, field " +  descAstNode(leftval) );
                    if (!isValueNode(rightval) && !isComputeNode(rightval))
                        throw new ArgumentException("Syntax error, field " +  descAstNode(rightval) );
                    return true; 
                case ASTNODE_TYPE.A_LRND: 
                    n.intValue = leftval.intValue;
                    n.bValue = leftval.bValue;
                    n.strName = leftval.strName;
                    //n.op = leftval.op;
                    return true;
                case ASTNODE_TYPE.A_RRND: return true;
                case ASTNODE_TYPE.A_AND: 
                case ASTNODE_TYPE.A_OR: 
                    if (!isLogicNode(leftval))
                        throw new ArgumentException("Syntax error, field " +  descAstNode(leftval) );
                    if (!isLogicNode(rightval))
                        throw new ArgumentException("Syntax error, field " +  descAstNode(rightval) );
                    return true; 
                case ASTNODE_TYPE.A_VAR: 
                case ASTNODE_TYPE.A_INTLIT:   
                case ASTNODE_TYPE.A_STR: 
                case ASTNODE_TYPE.A_COMMA: 
                    return true;
                case ASTNODE_TYPE.A_SUBSTR: 
                    if (!isSubstrNode(n))
                        throw new ArgumentException("Syntax error, field " +  descAstNode(leftval) );
                    return true;
                    //break;
                default:
                    throw new ArgumentException("Syntax error, field " +  descAstNode(n) );
                    //return false;
                //fprintf(stderr, "Unknown AST operator %d\n", n->op);
                //return true;
            }
            //return true;
        }


        //check if the node is substr functions
        private bool isSubstrNode(AstNode n)
        {
            if (n.leftLeaf.op == ASTNODE_TYPE.A_COMMA) {
                //substr(istr, istart, ilen)
                if (isDigitNode(n.rightLeaf) && isDigitNode(n.leftLeaf.rightLeaf) && isValueNode(n.leftLeaf.leftLeaf))
                    return true;
            }
            else if (isValueNode(n.leftLeaf) && isDigitNode(n.rightLeaf))
            {
                //substr(istr, istart)
                return true;
            }
            throw new ArgumentException("Syntax error, field " +  descAstNode(n) + " not a substr node" );
            //return false;
        }

        //check the node is a logic node
        private bool isLogicNode(AstNode n)
        {
            if (n.op == ASTNODE_TYPE.A_AND ||
                n.op == ASTNODE_TYPE.A_OR ||
                n.op == ASTNODE_TYPE.A_EQUALEQUAL ||
                n.op == ASTNODE_TYPE.A_NOTEQUAL ||
                n.op == ASTNODE_TYPE.A_LESSTHAN ||
                n.op == ASTNODE_TYPE.A_LESSEQUAL ||
                n.op == ASTNODE_TYPE.A_LARGETHAN ||
                n.op == ASTNODE_TYPE.A_LARGEEQUAL ||
                n.op == ASTNODE_TYPE.A_LIKE)
                return true;
            else if (n.op == ASTNODE_TYPE.A_LRND)
            {
                if (isLogicNode(n.leftLeaf))
                {
                    return true;
                }
            }
            throw new ArgumentException("Syntax error, field " +  descAstNode(n) + " not a logic node" );
                //return false;
        }

        //describe the node with full text
        private String descAstNode(AstNode n)
        {
            String leftval = "", rightval ="";
            if (n.leftLeaf != null)  leftval = descAstNode(n.leftLeaf);
            if (n.rightLeaf != null)  rightval = descAstNode(n.rightLeaf);
            switch (n.op) {
                case ASTNODE_TYPE.A_ADD:      return (leftval + " + " + rightval);
                case ASTNODE_TYPE.A_SUBTRACT: return (leftval + " - "  + rightval);
                case ASTNODE_TYPE.A_MULTIPLY: return (leftval + " * " +  rightval);
                case ASTNODE_TYPE.A_DIVIDE:   return (leftval + " / " + rightval);
                case ASTNODE_TYPE.A_INTLIT:   return (n.intValue.ToString());
                case ASTNODE_TYPE.A_STR:   return ("'" + n.strName + "'");
                case ASTNODE_TYPE.A_VAR:   return ("[" + n.strName + "]");
                case ASTNODE_TYPE.A_LRND: return "(" + leftval ;
                case ASTNODE_TYPE.A_RRND: return " ) ";
                case ASTNODE_TYPE.A_AND: return (leftval + " AND " + rightval);
                case ASTNODE_TYPE.A_OR: return (leftval + " OR " + rightval);
                case ASTNODE_TYPE.A_EQUALEQUAL: return (leftval + " == " + rightval);
                case ASTNODE_TYPE.A_NOTEQUAL: return (leftval + " != " + rightval);
                case ASTNODE_TYPE.A_LESSTHAN: return (leftval + " < " + rightval);
                case ASTNODE_TYPE.A_LESSEQUAL: return (leftval + " <= " + rightval);
                case ASTNODE_TYPE.A_LARGETHAN: return (leftval + " > " + rightval);
                case ASTNODE_TYPE.A_LARGEEQUAL: return (leftval + " >=" + rightval);
                case ASTNODE_TYPE.A_LIKE: return leftval + " LIKE "  + rightval;
                case ASTNODE_TYPE.A_COMMA: return leftval + " , "  + rightval;
                case ASTNODE_TYPE.A_SUBSTR: return "SUBSTR(" + leftval + "," + rightval + ")";
                default:
                    throw new ArgumentException("Unknown AST operator " +  n.op);
                //fprintf(stderr, "Unknown AST operator %d\n", n->op);
                //return("");
            }

        }

        //check the node is a computation node
        private bool isComputeNode(AstNode n)
        {
            if (n.op == ASTNODE_TYPE.A_ADD ||
                n.op == ASTNODE_TYPE.A_SUBTRACT ||
                n.op == ASTNODE_TYPE.A_MULTIPLY ||
                n.op == ASTNODE_TYPE.A_DIVIDE)
                return true;
            else if (n.op == ASTNODE_TYPE.A_LRND)
            {
                if (isComputeNode(n.leftLeaf))
                {
                    return true;
                }
            }
            return false;
        }
        ////check the node is a digit node
        private bool isDigitNode(AstNode n)
        {
            if (n.op == ASTNODE_TYPE.A_VAR ||
                n.op == ASTNODE_TYPE.A_INTLIT || isComputeNode(n))
                return true;
            else if (n.op == ASTNODE_TYPE.A_LRND)
            {
                if (isDigitNode(n.leftLeaf))
                {
                    return true;
                }
            }
            return false;
        }

        ////check the node is a value node
        private bool isValueNode(AstNode n)
        {
            if (n.op == ASTNODE_TYPE.A_VAR ||
                n.op == ASTNODE_TYPE.A_INTLIT ||
                n.op == ASTNODE_TYPE.A_STR ||
                n.op == ASTNODE_TYPE.A_SUBSTR)
                return true;
            else if (n.op == ASTNODE_TYPE.A_LRND)
            {
                if (isValueNode(n.leftLeaf))
                {
                    return true;
                }
            }  
            return false;
        }

        //if a node is a digit node, try parse the strName to intValue
        private bool parseVartoInt(ref AstNode n)
        {
            if (n.op == ASTNODE_TYPE.A_VAR) 
            {
                if(!int.TryParse(n.strName, out n.intValue))
                    throw new ArgumentException("Syntax error, invalid number " +  descAstNode(n));
                else
                    return true;
            }
            if (n.op == ASTNODE_TYPE.A_INTLIT || isComputeNode(n))
                return true;
            throw new ArgumentException("Syntax error, invalid number " +  descAstNode(n));
        }

        ////if a node is a value node, try parse the  intValue to strName 
        private bool parseVartoStr(ref AstNode n)
        {
            if (n.op == ASTNODE_TYPE.A_VAR || n.op == ASTNODE_TYPE.A_STR || n.op == ASTNODE_TYPE.A_SUBSTR) 
            {
                return true;
            }
            if (n.op == ASTNODE_TYPE.A_INTLIT || isComputeNode(n)) {
                n.strName = n.intValue.ToString();
                return true;
            }
            throw new ArgumentException("Syntax error, invalid parameter " +  n.op );
        }

        //validate rule with actual value in json
        public bool validateRule(String json_str)
        {
            Token token = new Token();
            AstNode node;
            setJson(json_str);
            scan(ref token);
            node = binexpr(0, ref token);
            interpretAST2(ref node);
            if (node.bValue == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //interpret rule with actual value
        private void interpretAST2(ref AstNode n) {
            AstNode leftval , rightval ;
            if (n.leftLeaf != null)  interpretAST2(ref n.leftLeaf);
            if (n.rightLeaf != null)  interpretAST2(ref n.rightLeaf);
            leftval = n.leftLeaf;
            rightval = n.rightLeaf;
            switch (n.op) {
                case ASTNODE_TYPE.A_ADD:    
                    if (parseVartoInt(ref leftval) && parseVartoInt(ref rightval))
                        n.intValue = leftval.intValue + rightval.intValue;
                    break;
                case ASTNODE_TYPE.A_SUBTRACT: 
                    if (parseVartoInt(ref leftval) && parseVartoInt(ref rightval))
                        n.intValue = leftval.intValue - rightval.intValue;
                    break;
                case ASTNODE_TYPE.A_MULTIPLY: 
                    if (parseVartoInt(ref leftval) && parseVartoInt(ref rightval))
                        n.intValue = leftval.intValue * rightval.intValue;
                    break;
                case ASTNODE_TYPE.A_DIVIDE:  
                    if (parseVartoInt(ref leftval) && parseVartoInt(ref rightval))
                        n.intValue = leftval.intValue / rightval.intValue;
                    break;
                case ASTNODE_TYPE.A_INTLIT:   
                    n.bValue = n.intValue > 0? true:false;
                    return ;
                case ASTNODE_TYPE.A_LRND: 
                    //左括号时，取左叶的值到当前节点
                    n.intValue = leftval.intValue;
                    n.bValue = leftval.bValue;
                    n.strName = leftval.strName;
                    //n.op = leftval.op;
                    return ;
                case ASTNODE_TYPE.A_RRND: return ;
                case ASTNODE_TYPE.A_AND: 
                    if (isLogicNode(leftval) && isLogicNode(rightval))
                        n.bValue = leftval.bValue  && rightval.bValue;
                    break;
                case ASTNODE_TYPE.A_OR: 
                    if (isLogicNode(leftval) && isLogicNode(rightval))
                        n.bValue = leftval.bValue || rightval.bValue;
                    break;
                case ASTNODE_TYPE.A_EQUALEQUAL: 
                    if (leftval.op == ASTNODE_TYPE.A_INTLIT || isComputeNode(leftval))
                    {
                        if (rightval.op == ASTNODE_TYPE.A_VAR) {
                            parseVartoInt(ref rightval);
                            n.bValue = leftval.intValue  == rightval.intValue ?true:false;
                        }
                        else if (rightval.op == ASTNODE_TYPE.A_INTLIT || isComputeNode(rightval))
                            n.bValue = leftval.intValue  == rightval.intValue ?true:false;
                    }  
                    else if (leftval.op == ASTNODE_TYPE.A_STR || leftval.op == ASTNODE_TYPE.A_SUBSTR)
                    {
                        n.bValue = leftval.strName  == rightval.strName ?true:false;
                    }  
                    else if (leftval.op == ASTNODE_TYPE.A_VAR)
                    {
                        if (rightval.op == ASTNODE_TYPE.A_INTLIT || isComputeNode(rightval)) {
                            parseVartoInt(ref leftval);
                            n.bValue = leftval.intValue  == rightval.intValue ?true:false;
                        }
                        else if (rightval.op == ASTNODE_TYPE.A_VAR || rightval.op == ASTNODE_TYPE.A_STR)
                        {
                            n.bValue = leftval.strName  == rightval.strName ?true:false;
                        }
                    }
                    //n.bValue = leftval.bValue  == rightval.bValue?true:false;
                    break;
                case ASTNODE_TYPE.A_NOTEQUAL: 
                    if (leftval.op == ASTNODE_TYPE.A_INTLIT || isComputeNode(leftval))
                    {
                        if (rightval.op == ASTNODE_TYPE.A_VAR) {
                            parseVartoInt(ref rightval);
                            n.bValue = leftval.intValue  != rightval.intValue ?true:false;
                        }
                        else if (rightval.op == ASTNODE_TYPE.A_INTLIT || isComputeNode(rightval))
                            n.bValue = leftval.intValue  != rightval.intValue ?true:false;
                    }  
                    else if (leftval.op == ASTNODE_TYPE.A_STR || leftval.op == ASTNODE_TYPE.A_SUBSTR)
                    {
                        n.bValue = leftval.strName  != rightval.strName ?true:false;
                    }  
                    else if (leftval.op == ASTNODE_TYPE.A_VAR)
                    {
                        if (rightval.op == ASTNODE_TYPE.A_INTLIT || isComputeNode(rightval)) {
                            parseVartoInt(ref leftval);
                            n.bValue = leftval.intValue  != rightval.intValue ?true:false;
                        }
                        else if (rightval.op == ASTNODE_TYPE.A_VAR || rightval.op == ASTNODE_TYPE.A_STR)
                        {
                            n.bValue = leftval.strName  != rightval.strName ?true:false;
                        }
                    }
                    break;
                case ASTNODE_TYPE.A_LESSTHAN: 
                    if (parseVartoInt(ref leftval) && parseVartoInt(ref rightval))
                        n.bValue = (leftval.intValue  < rightval.intValue ) ?true:false;
                    break;
                case ASTNODE_TYPE.A_LESSEQUAL: 
                    if (parseVartoInt(ref leftval) && parseVartoInt(ref rightval))
                        n.bValue = leftval.intValue  <= rightval.intValue?true:false;
                    break;
                case ASTNODE_TYPE.A_LARGETHAN: 
                    if (parseVartoInt(ref leftval) && parseVartoInt(ref rightval))
                        n.bValue = leftval.intValue  > rightval.intValue?true:false;
                    break;
                case ASTNODE_TYPE.A_LARGEEQUAL: 
                    if (parseVartoInt(ref leftval) && parseVartoInt(ref rightval))
                        n.bValue = leftval.intValue  >= rightval.intValue?true:false;
                    break;
                case ASTNODE_TYPE.A_LIKE: 
                    if (parseVartoStr(ref leftval) && parseVartoStr(ref rightval))
                        n.bValue = leftval.strName.IndexOf(rightval.strName) >= 0? true:false ;
                    break;
                case ASTNODE_TYPE.A_VAR: 
                    //n.bValue = leftval.intValue  >= rightval.intValue?true:false;
                    String var_value = "";
                    if (value_dic.TryGetValue(n.strName, out var_value) == false)
                        {
                            throw new ArgumentException("Syntax error, field " +  n.strName + " has no mapping value" );
                        }
                    else
                        n.strName = var_value;
                    break;
                case ASTNODE_TYPE.A_STR: 
                case ASTNODE_TYPE.A_COMMA:
                    //n.bValue = leftval.intValue  >= rightval.intValue?true:false;
                    break;
                case ASTNODE_TYPE.A_SUBSTR:
                    if (isSubstrNode(n)) {
                        if (n.leftLeaf.op == ASTNODE_TYPE.A_COMMA) {
                            String original_str = n.leftLeaf.leftLeaf.strName;
                            int istart = n.leftLeaf.rightLeaf.intValue;
                            int ilen = n.rightLeaf.intValue;
                            n.strName = original_str.Substring(istart, ilen);
                        }
                        else {
                            String original_str = n.leftLeaf.strName;
                            int istart = n.rightLeaf.intValue;
                            n.strName = original_str.Substring(istart);
                        }
                    }
                    //n.bValue = leftval.intValue  >= rightval.intValue?true:false;
                    break;
                default:
                    throw new ArgumentException("Unknown AST operator " + n.op);
                //fprintf(stderr, "Unknown AST operator %d\n", n->op);
                //return;
            }
        }

                
    }
}