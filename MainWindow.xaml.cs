using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ast.tool;

namespace ast
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void Mybutton_click(object sender, RoutedEventArgs a)
        {
            String strValue = ts.Text;
            String test_str = "([loadport] like 'VN') AND 5 + 6 AND (1==1)";

            Scanner scanner = new Scanner(test_str);

            Token token = new Token();
            AstNode node;
            try {
                scanner.scan(ref token);
                node = scanner.binexpr(0, ref token);
                scanner.setJson(strValue);
                scanner.interpretAST2(ref node);
            } catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            MessageBox.Show("here");
        }
    }
}
