# boolean_checker

Wanna to add some business rule in our system when saving data. Don't want to hard code those rules.\r\n
Rules like "[field1] > 0 and [field2] < 10 or [field3] like 'CN%'" could be added to a configuration table 
and be checked when saving.\r\n
So need to intepret these rules like "[field1] > 0 and [field2] < 10 or [field3] like 'CN%'" in the program.\r\n
It's like a basic AST(Abstract Syntax Tree) problem.\r\n
Got real inspiration from https://github.com/DoctorWkt/acwj.\r\n
Will write an boolean_checker in C#.\r\n
\r\n
