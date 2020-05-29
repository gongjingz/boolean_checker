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

        public void setJson(String json_str)
        {
            String temp_name = "";
            JsonTextReader reader = new JsonTextReader(new StringReader(json_str));
            while (reader.Read())
            {
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
                    Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);
                }
                else
                {
                    Console.WriteLine("Token: {0}", reader.TokenType);
                }
            }
            this.json_str = json_str;
        }

        public int next()
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

        public void putback(int c) 
        {
            this.Putback  = c;
        }

        public int skip()
        {
            int c;
            c = next();
            while (' ' == c || '\t' == c || '\n' == c || '\r' == c || '\f' == c)
            {
                c = next();
            }
            return c;

        }

        //
        public int scanident(int c, out String buf, int lim = 128) {
            int i = 0;
            c = next();
            StringBuilder strbuilder = new StringBuilder(20);
            // Allow digits, alpha and underscores
            while (Char.IsLetter((char)c) || Char.IsDigit((char)c) || '_' == c) {
                // Error if we hit the identifier length limit,
                // else append to buf[] and get next character
                if (lim - 1 == i) {
                    Console.WriteLine("identifier too long on line {0:G}", iLine);
                    //sexit(1);
                } else if (i < lim - 1) {
                    strbuilder.Append((char)c);
                }
                c = next();
            }
            if (']' != c)
            {
                Console.WriteLine("identifier definition not end", iLine);
            }
            buf = strbuilder.ToString();
            return (i);
        }

        //取一个字符串
        public int scanstr(int c, out String buf, int lim = 128) {
            int i = 0;
            c = next();
            StringBuilder strbuilder = new StringBuilder(20);
            // Allow digits, alpha and underscores and %
            while (Char.IsLetter((char)c) || Char.IsDigit((char)c) || '_' == c || '%' == c) {
                // Error if we hit the identifier length limit,
                // else append to buf[] and get next character
                if (lim - 1 == i) {
                    Console.WriteLine("identifier too long on line {0:G}", iLine);
                    //sexit(1);
                } else if (i < lim - 1) {
                    strbuilder.Append((char)c);
                }
                c = next();
            }
            if ('\'' != c)
            {
                Console.WriteLine("identifier definition not end", iLine);
                throw new ArgumentException("identifier definition not end at position:" +  ipos);
            }

            buf = strbuilder.ToString();
            return (i);
        }

        public int scankeyword(int c, out String buf, int lim = 128) {
        int i = 0;
        StringBuilder strbuilder = new StringBuilder(20);
        // Allow digits, alpha and underscores
        while (Char.IsLetter((char)c) || Char.IsDigit((char)c) || '_' == c) {
            // Error if we hit the identifier length limit,
            // else append to buf[] and get next character
            if (lim - 1 == i) {
                Console.WriteLine("identifier too long on line {0:G}", iLine);
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

        static TOKEN_TYPE keyword(String buf) {
           switch (buf.ToUpper()) {
               
                case "ELSE":
                return (TOKEN_TYPE.T_ELSE);
                //break;
                case "IF":
                return (TOKEN_TYPE.T_IF);
                //break;
                case "PRINT":
                return (TOKEN_TYPE.T_PRINT);
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

        public int scan(ref Token t)
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

        //括号处理
        public AstNode rnd(ref Token token)
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

        //二元操作符处理
        public AstNode binexpr(int ptp, ref Token token) {
            AstNode left, right;
            TOKEN_TYPE tokentype;
            if (token.token == TOKEN_TYPE.T_LRND) {
                //scan(ref token);
                left = rnd(ref token);
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

        public AstNode primary(ref Token token) {
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
        private int[] OpPrec = {
            0, 10, 10,			// T_EOF, A_AND = 1, A_OR
            20, 20,	30, 30, 0,		// A_ADD, A_SUBTRACT, A_MULTIPLY, A_DIVIDE, A_INTLIT
            11, 11,	11, 11, 11, 11,		// A_EQUALEQUAL, A_NOTEQUAL, A_LESSTHAN, A_LESSEQUAL, A_LARGETHAN, A_LARGEEQUAL, 
            20,		// A_LIKE,
            0, 0,  //A_LRND, A_RRND,
            10,        //A_EQUAL
            0, 0    //T_VAR,T_STR,

        };

        // Check that we have a binary operator and
        // return its precedence.
        public int op_precedence(TOKEN_TYPE tokentype) {
            int prec = OpPrec[(int)tokentype];
            if (prec == 0) {
                Console.WriteLine("Syntax error, token {0:G}", tokentype);
                throw new ArgumentException("Syntax error, token " +  tokentype + " at position " + ipos);
            }
                //fatald("Syntax error, token", tokentype);
            return (prec);
        }

        

        public int interpretAST(AstNode n) {
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
                Console.WriteLine( "Unknown AST operator {0:G}", n.op);
                //fprintf(stderr, "Unknown AST operator %d\n", n->op);
                return(1);
            }
        }

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
                        throw new ArgumentException("Syntax error, field " +  leftval.op );
                    if (!isDigitNode(rightval))
                        throw new ArgumentException("Syntax error, field " +  rightval.op );
                    return true; 
                case ASTNODE_TYPE.A_EQUALEQUAL: 
                case ASTNODE_TYPE.A_NOTEQUAL: 
                case ASTNODE_TYPE.A_LIKE: 
                    if (!isValueNode(leftval))
                        throw new ArgumentException("Syntax error, field " +  leftval.op );
                    if (!isValueNode(rightval))
                        throw new ArgumentException("Syntax error, field " +  rightval.op );
                    return true; 
                case ASTNODE_TYPE.A_LRND: 
                    n.intValue = leftval.intValue;
                    n.bValue = leftval.bValue;
                    n.strName = leftval.strName;
                    n.op = leftval.op;
                    return true;
                case ASTNODE_TYPE.A_RRND: return true;
                case ASTNODE_TYPE.A_AND: 
                case ASTNODE_TYPE.A_OR: 
                    if (!isLogicNode(leftval))
                        throw new ArgumentException("Syntax error, field " +  leftval.op );
                    if (!isLogicNode(rightval))
                        throw new ArgumentException("Syntax error, field " +  rightval.op );
                    return true; 
                case ASTNODE_TYPE.A_VAR: 
                case ASTNODE_TYPE.A_INTLIT:   
                case ASTNODE_TYPE.A_STR: 
                    return true;
                    //break;
                default:
                    throw new ArgumentException("Syntax error, field " +  n.op );
                    //return false;
                //fprintf(stderr, "Unknown AST operator %d\n", n->op);
                //return true;
            }
            //return true;
        }

        public bool isLogicNode(AstNode n)
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
            else    
                return false;
        }

        public bool isComputeNode(AstNode n)
        {
            if (n.op == ASTNODE_TYPE.A_ADD ||
                n.op == ASTNODE_TYPE.A_SUBTRACT ||
                n.op == ASTNODE_TYPE.A_MULTIPLY ||
                n.op == ASTNODE_TYPE.A_DIVIDE)
                return true;
            else    
                return false;
        }
        //检测是否为数字节点
        public bool isDigitNode(AstNode n)
        {
            if (n.op == ASTNODE_TYPE.A_VAR ||
                n.op == ASTNODE_TYPE.A_INTLIT || isComputeNode(n))
                return true;
            else    
                return false;
        }

        public bool isValueNode(AstNode n)
        {
            if (n.op == ASTNODE_TYPE.A_VAR ||
                n.op == ASTNODE_TYPE.A_INTLIT ||
                n.op == ASTNODE_TYPE.A_STR)
                return true;
            else    
                return false;
        }

        public bool parseVartoInt(ref AstNode n)
        {
            if (n.op == ASTNODE_TYPE.A_VAR) 
            {
                if(!int.TryParse(n.strName, out n.intValue))
                    throw new ArgumentException("Syntax error, invalid number " +  n.strName );
                else
                    return true;
            }
            if (n.op == ASTNODE_TYPE.A_INTLIT || isComputeNode(n))
                return true;
            throw new ArgumentException("Syntax error, invalid number " +  n.strName );
        }


        public void interpretAST2(ref AstNode n) {
            //if (!checkAST(ref n))
            //{
            //    return;
            //}
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
                    n.op = leftval.op;
                    return ;
                case ASTNODE_TYPE.A_RRND: return ;
                case ASTNODE_TYPE.A_AND: 
                    n.bValue = leftval.bValue  && rightval.bValue;
                    break;
                case ASTNODE_TYPE.A_OR: 
                    n.bValue = leftval.bValue  || rightval.bValue;
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
                    else if (leftval.op == ASTNODE_TYPE.A_STR)
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
                    else if (leftval.op == ASTNODE_TYPE.A_STR)
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
                    //n.bValue = leftval.intValue  >= rightval.intValue?true:false;
                    break;
                default:
                Console.WriteLine( "Unknown AST operator {0:G}", n.op);
                //fprintf(stderr, "Unknown AST operator %d\n", n->op);
                return;
            }
        }

                
    }
}