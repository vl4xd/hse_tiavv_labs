using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DistributedBalancing
{
    public partial class MainWindow : Window
    {
        private List<Node> nodes = new List<Node>();
        private Dictionary<int, TreeViewItem> treeViewItems = new Dictionary<int, TreeViewItem>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeNetwork();
            DisplayTree();
        }

        private void InitializeNetwork()
        {
            for (int i = 0; i < 7; i++)
                nodes.Add(new Node(i, LogMessage));

            ConnectNodes(0, 1);
            ConnectNodes(0, 2);
            ConnectNodes(1, 3);
            ConnectNodes(1, 4);
            ConnectNodes(2, 5);
            ConnectNodes(2, 6);

            nodes[0].IsInitiator = true;

            Random random = new Random();
            foreach (var node in nodes)
            {
                node.Load = random.Next(10, 100); // Загрузка от 10 до 100
            }
        }

        private void ConnectNodes(int parentId, int childId)
        {
            var parent = nodes.First(n => n.Id == parentId);
            var child = nodes.First(n => n.Id == childId);

            parent.Neighbors.Add(child);
            child.Neighbors.Add(parent);
        }

        private void DisplayTree()
        {
            TreeViewNodes.Items.Clear();
            treeViewItems.Clear();

            foreach (var node in nodes)
            {
                var item = new TreeViewItem { Header = $"Node {node.Id} (Load: {node.Load})" };
                treeViewItems[node.Id] = item;

                if (node.Id == 0)
                    TreeViewNodes.Items.Add(item);
                else
                {
                    var parent = nodes.FirstOrDefault(n => n.Neighbors.Contains(node) && n.Id < node.Id);
                    if (parent != null)
                        treeViewItems[parent.Id].Items.Add(item);
                }
            }
        }

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(message + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            });
        }

        private void StartBalancing_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Запуск балансировки...");
            var rootNode = nodes.First(n => n.IsInitiator);
            rootNode.StartWave();

            Dispatcher.Invoke(() => DisplayTree());
        }
    }

    public class Node
    {
        public int Id { get; }
        public List<Node> Neighbors { get; } = new List<Node>();
        public bool IsInitiator { get; set; }
        public int Load { get; set; }

        static public int TotalLoad { get; set; }
        public int Counter { get; private set; }
        private Node Predecessor { get; set; }
        private readonly Action<string> logAction;
        private List<int> LoadsReceived { get; } = new List<int>();

        public Node(int id, Action<string> logAction)
        {
            Id = id;
            Load = new Random().Next(10, 100); // Генерируем случайную загрузку
            this.logAction = logAction;
        }

        public void StartWave()
        {
            TotalLoad = Load; // Инициализируем свою нагрузку
            logAction($"Узел {Id}: инициирует алгоритм с начальной нагрузкой = {Load}.");

            foreach (var neighbor in Neighbors)
                neighbor.ReceiveMessage(this);
        }


        public void ReceiveMessage(Node sender)
        {
            if (Predecessor == null)
            {
                Predecessor = sender; // Устанавливаем предшественника
                logAction($"Узел {Id}: получил сообщение от узла {sender.Id}.");

                // Рассылаем сообщения всем соседям, кроме отправителя
                foreach (var neighbor in Neighbors.Where(n => n != sender))
                    neighbor.ReceiveMessage(this);

                // Если это лист (нет других соседей), сразу отправляем эхо
                if (Neighbors.Count == 1)
                    SendEcho();
            }
            else
            {
                Counter++; // Увеличиваем счётчик для полученных сообщений
                if (Counter == Neighbors.Count - 1) // Все соседи, кроме предшественника, ответили
                    SendEcho();
            }
        }

        private void SendEcho()
        {
            logAction($"Узел {Id}: отправляет эхо узлу {Predecessor?.Id} с нагрузкой {Load}.");
            Predecessor?.ReceiveEcho(this, Load); // Передаём нагрузку предшественнику
        }


        public void ReceiveEcho(Node sender, int load)
        {
            //logAction($"Predcessor = {Predecessor?.Id}");
            Counter++;
            TotalLoad += load; // Учитываем нагрузку от соседа

            logAction($"Узел {Id}: получил эхо от узла {sender.Id}, текущая суммарная нагрузка = {TotalLoad}.");

            if (Counter == 2) // Все соседи отправили эхо
            {
                if (IsInitiator)
                {
                    logAction($"Инициатор {Id}: завершил сбор данных, общая нагрузка = {TotalLoad}.");
                    BalanceLoad(TotalLoad); // Выполняем балансировку
                }
                else
                {
                    SendEcho(); // Отправляем эхо предшественнику
                }
            }
        }


        private void BalanceLoad(int totalLoad)
        {
            int numNodes = CountSubtreeNodes(); // Узнаём количество узлов в поддереве
            int averageLoad = totalLoad / numNodes;
            int leftoverLoad = totalLoad % numNodes;

            // Текущий узел получает свою долю нагрузки
            Load = averageLoad + leftoverLoad; ;

            logAction($"Узел {Id}: новая нагрузка = {Load}, распределяет остальную нагрузку ({totalLoad - Load}) соседям.");

            // Распределяем нагрузку между соседями, исключая предшественника
            foreach (var neighbor in Neighbors.Where(n => n != Predecessor))
            {
                int loadForNeighbor = averageLoad * neighbor.CountSubtreeNodes() + Math.Min(leftoverLoad, neighbor.CountSubtreeNodes());
                leftoverLoad -= Math.Min(leftoverLoad, neighbor.CountSubtreeNodes());
                neighbor.BalanceLoad(loadForNeighbor);
            }
        }

        public void AdjustLoad(int newLoad)
        {
            int delta = Load - newLoad; // Разница между старой и новой нагрузкой
            Load = newLoad; // Применяем новую нагрузку

            logAction($"Узел {Id}: старая нагрузка = {Load + delta}, новая нагрузка = {Load}, перемещено = {delta}.");
        }

        private int CountSubtreeNodes()
        {
            int count = 1; // Считаем текущий узел
            foreach (var neighbor in Neighbors.Where(n => n != Predecessor))
            {
                count += neighbor.CountSubtreeNodes();
            }
            return count;
        }
    }
}
