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
            ts_json.AcceptsReturn = true;
            ts.AcceptsReturn = true;
            ts.Text = "([Field1] like 'CN') AND (5 + 6) AND (1==1)";
            ts_json.Text = @"{
                'Field1': 'CNSHA'
                }";
        }

        public void Mybutton_click(object sender, RoutedEventArgs a)
        {
            //String strValue = ts.Text;
            String test_str = ts.Text;

            Scanner scanner = new Scanner(test_str);
            try {
                if (scanner.checkRule())
                    MessageBox.Show("rule is ok");
                else
                    MessageBox.Show("rule is not ok");
            } catch (Exception e)
            {
                MessageBox.Show("Rule format wrong " + e.Message);
            }
            
        }

        public void Mybutton2_click(object sender, RoutedEventArgs a)
        {
            String strValue = ts_json.Text;  //actual value from json
            String test_str = ts.Text;      //rule text

            Scanner scanner = new Scanner(test_str);
            try {
                if (scanner.validateRule(strValue))
                {
                    MessageBox.Show("rule evaluated : True");
                }
                else
                {
                    MessageBox.Show("rule evaluated : False");
                }
            } catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}
