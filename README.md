# boolean_checker

Wanna to add some business rule in our system when saving data. Don't want to hard code those rules.

Rules like "[field1] > 0 and [field2] < 10 or [field3] like 'CN%'" could be added to a configuration table 
and be checked when saving.

So need to intepret these rules like "[field1] > 0 and [field2] < 10 or [field3] like 'CN%'" in the program.

It's like a basic AST(Abstract Syntax Tree) problem.
Got real inspiration from https://github.com/DoctorWkt/acwj.

Begin writting an boolean_checker in C#. 

Should support：

1.computing operator + and - and * and /

2.logic operators like 'AND' and 'OR' and 'LIKE' and '==' and '!=' and '>' and '<' and '>=' and '<='

3.Parentheses （ and )

4.Able to check the rule is syntax correct.

5.replace [field] with real value by a json input, do boolean check and return TRUE or FALSE

