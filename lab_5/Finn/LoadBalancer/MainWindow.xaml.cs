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
using System.Xml.Linq;

namespace LoadBalancer
{
    public partial class MainWindow : Window
    {
        private int step_numb;
        private List<Node> node_list;
        private Dictionary<int, int> loads_info;
        public MainWindow()
        {
            InitializeComponent();

            step_numb = 0;
            loads_info = new Dictionary<int, int>();

            Random rnd = new Random();
            Node node0 = new Node(0, true, rnd.Next(1, 101));
            Node node1 = new Node(1, false, rnd.Next(1, 101));
            Node node2 = new Node(2, false, rnd.Next(1, 101));
            Node node3 = new Node(3, false, rnd.Next(1, 101));
            Node node4 = new Node(4, false, rnd.Next(1, 101));

            node0.Childs = new List<Node>() { node1, node2};
            node0.Parents = new List<Node>() { node4 };
            node0.ParentWaiting = new List<Node>() { node4 };

            node1.Childs = new List<Node>() { node3, node4 };
            node1.Parents = new List<Node>() { node0 };
            node1.ParentWaiting = new List<Node>() { node0 };

            node2.Childs = new List<Node>() { node3 };
            node2.Parents = new List<Node>() { node0 };
            node2.ParentWaiting = new List<Node>() { node0 };

            node3.Childs = new List<Node>() { node4 };
            node3.Parents = new List<Node>() { node1, node2 };
            node3.ParentWaiting = new List<Node>() { node1, node2 };

            node4.Childs = new List<Node>() { node0 };
            node4.Parents = new List<Node>() { node3 };
            node4.ParentWaiting = new List<Node>() { node3 };

            node_list = new List<Node>() { node0, node1, node2, node3, node4 };
        }

        private void btn_next_step_Click(object sender, RoutedEventArgs e)
        {
            this.tb_log.Text += $"Шаг: {step_numb}\n";
            switch (step_numb)
            {
                case 0:
                    {
                        this.tb_log.Text += "Балансировка запущена...\n";
                        this.tb_log.Text += $"Инициализатор: p{node_list[0].Id}\n";
                        this.tb_log.Text += "Значения нагрузки узлов сети:\n";
                        foreach (Node node in node_list)
                        {
                            this.tb_log.Text += $"p{node.Id} = {node.Load}\n";
                        }
                        this.p0.Background = Brushes.Red;
                        this.p0.Content = $"p0\nInc: [0]\nNinc: []";
                        loads_info[0] = node_list[0].Load;
                        this.p1.Background = Brushes.Green;
                        this.p1.Content = $"p1\nInc: [1]\nNinc: []";
                        this.p2.Background = Brushes.Green;
                        this.p2.Content = $"p2\nInc: [2]\nNinc: []";
                        this.p3.Background = Brushes.Green;
                        this.p3.Content = $"p3\nInc: [3]\nNinc: []";
                        this.p4.Background = Brushes.Green;
                        this.p4.Content = $"p4\nInc: [4]\nNinc: []";

                        this.tb_log.Text += $"Узел p{node_list[0].Id} добавил информацию о нагрузке: {node_list[0].Load}\n";
                        break;
                    }
                case 1:
                    {
                        this.p0.Background = Brushes.Gray;
                        this.p0.Content = $"p0\nInc: [0]\nNinc: []";
                        this.p1.Background = Brushes.Red;
                        this.p1.Content = $"p1\nInc: [0,1]\nNinc: [1]";
                        loads_info[1] = node_list[1].Load;
                        this.p2.Background = Brushes.Red;
                        this.p2.Content = $"p2\nInc: [0,2]\nNinc: [2]";
                        loads_info[2] = node_list[2].Load;
                        this.p3.Background = Brushes.Green;
                        this.p3.Content = $"p3\nInc: [3]\nNinc: []";
                        this.p4.Background = Brushes.Green;
                        this.p4.Content = $"p4\nInc: [4]\nNinc: []";

                        this.tb_log.Text += $"Узел p{node_list[1].Id} добавил информацию о нагрузке: {node_list[1].Load}\n";
                        this.tb_log.Text += $"Узел p{node_list[2].Id} добавил информацию о нагрузке: {node_list[2].Load}\n";
                        break;
                    }
                case 2:
                    {
                        this.p0.Background = Brushes.Gray;
                        this.p0.Content = $"p0\nInc: [0]\nNinc: []";
                        this.p1.Background = Brushes.Gray;
                        this.p1.Content = $"p1\nInc: [0,1]\nNinc: [1]";
                        this.p2.Background = Brushes.Gray;
                        this.p2.Content = $"p2\nInc: [0,2]\nNinc: [2]";
                        this.p3.Background = Brushes.Red;
                        loads_info[3] = node_list[3].Load;
                        this.p3.Content = $"p3\nInc: [0,1,2,3]\nNinc: [1,2,3]";
                        this.p4.Background = Brushes.Red;
                        this.p4.Content = $"p4\nInc: [4]\nNinc: []";

                        this.tb_log.Text += $"Узел p{node_list[3].Id} добавил информацию о нагрузке: {node_list[3].Load}\n";
                        break;
                    }
                case 3:
                    {
                        this.p0.Background = Brushes.Gray;
                        this.p0.Content = $"p0\nInc: [0]\nNinc: []";
                        this.p1.Background = Brushes.Gray;
                        this.p1.Content = $"p1\nInc: [0,1]\nNinc: [1]";
                        this.p2.Background = Brushes.Gray;
                        this.p2.Content = $"p2\nInc: [0,2]\nNinc: [2]";
                        this.p3.Background = Brushes.Gray;
                        this.p3.Content = $"p3\nInc: [0,1,2,3]\nNinc: [1,2,3]";
                        this.p4.Background = Brushes.Red;
                        loads_info[4] = node_list[4].Load;
                        this.p4.Content = $"p4\nInc: [0,1,2,3,4]\nNinc: [1,2,3,4]";

                        this.tb_log.Text += $"Узел p{node_list[4].Id} добавил информацию о нагрузке: {node_list[4].Load}\n";
                        break;
                    }
                case 4:
                    {
                        this.p0.Background = Brushes.Red;
                        this.p0.Content = $"p0\nInc: [0,1,2,3,4]\nNinc: [0,1,2,3,4]";
                        this.p1.Background = Brushes.Gray;
                        this.p1.Content = $"p1\nInc: [0,1]\nNinc: [1]";
                        this.p2.Background = Brushes.Gray;
                        this.p2.Content = $"p2\nInc: [0,2]\nNinc: [2]";
                        this.p3.Background = Brushes.Gray;
                        this.p3.Content = $"p3\nInc: [0,1,2,3]\nNinc: [1,2,3]";
                        this.p4.Background = Brushes.Gray;
                        this.p4.Content = $"p4\nInc: [0,1,2,3,4]\nNinc: [1,2,3,4]";

                        this.tb_log.Text += $"Узел p{node_list[0].Id} получил информацию о нагрузках.\n";
                        this.tb_log.Text += $"Узел p{node_list[0].Id} распределяет нагрузки:\n";
                        int total_load = 0;
                        foreach(var load in loads_info)
                        {
                            total_load += load.Value;
                        }
                        int average_load = total_load / node_list.Count;
                        int rem_load = total_load % node_list.Count;

                        this.tb_log.Text += $"Суммарная нагрузка {total_load}.\n";
                        this.tb_log.Text += $"Средняя нагрузка {average_load}.\n";
                        this.tb_log.Text += $"Обновленные значения нагрузки узлов сети:\n";
                        
                        foreach (Node node in node_list)
                        {
                            int temp;
                            if (rem_load != 0) 
                            { 
                                temp = average_load + 1;
                                rem_load -= 1;
                            }
                            else { temp = average_load; }
                            this.tb_log.Text += $"p{node.Id} = {temp}\n";
                        }
                        break;
                    }
                default:
                    {
                        this.tb_log.Text += "Балансировка завершена.\n";
                        this.btn_next_step.IsEnabled = false;
                        break;  
                    }
            }

            this.tb_log.Text += "\n";
            step_numb++;
        }
    }
}
