using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace LoadBalancer
{
    public class Node
    {
        public int Id;
        public List<int> Inc;
        public List<int> Ninc;
        public int Load;
        public List<Node> Childs;
        public List<Node> Parents;
        public bool isInitialiser;
        public int LoadsInfo;
        public List<Node> ParentWaiting;

        public Node(int _id, bool _isInitialiser, int _load)
        {
            this.Id = _id;
            this.Inc = new List<int>() { this.Id };
            this.Ninc = new List<int>();
            this.Load = _load;
            this.isInitialiser = _isInitialiser;
            this.LoadsInfo = this.Load;
        }

        public void SendMessage (ref Node child_node)
        {
            //
            foreach(var id in this.Inc)
            {
                int find_indx = child_node.Inc.BinarySearch(id);
                if (find_indx < 0)
                {
                    child_node.Inc.Insert(~find_indx, id);
                }
            }
            //
            foreach (var id in this.Ninc)
            {
                int find_indx = child_node.Ninc.BinarySearch(id);
                if (find_indx < 0)
                {
                    child_node.Ninc.Insert(~find_indx, id);
                }
            }
            child_node.ParentWaiting.Remove(this);
            if (child_node.ParentWaiting.Count == 0)
            {
                int find_indx = child_node.Ninc.BinarySearch(child_node.Id);
                child_node.Ninc.Insert(~find_indx, child_node.Id);
            }
            //
        }
    }
}
