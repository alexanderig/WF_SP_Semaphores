using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace Semaphores
{

    public partial class SemaphoresForm : Form
    {

        class MyTasksParam
        {
            public MyTasksParam(string mess)
            {
                isCancelTask = false;
                TskMessage = mess;
            }
            public bool isCancelTask { get; set; }
            public string TskMessage { get; set; }
        }

        List<MyTasksParam> workingtasks;
        List<Task> disposetasks;
        const int maxsize = 100;
        Semaphore semaphore;
        public SynchronizationContext curcontext;
        int Index { get; set; }
        int curSize { get; set; }
        List<Task> tasklist;
        public SemaphoresForm()
        {
            InitializeComponent();
            semaphore = new Semaphore(0, 100, "1A9191BF-AA26-46E1-BB85-BDA396BC6469");
            Index = 0;
            curSize = 0;
            curcontext = SynchronizationContext.Current;
            tasklist = new List<Task>();
            workingtasks = new List<MyTasksParam>();
            disposetasks = new List<Task>();
        }

        private void SomeJobBusy(Object obj)
        {
            Semaphore sem = Semaphore.OpenExisting("1A9191BF-AA26-46E1-BB85-BDA396BC6469");
            MyTasksParam tobj = (MyTasksParam)obj;
            sem.WaitOne();

        }


        private void SomeJob(Object obj)
        {
            try
            {
                Semaphore sem = Semaphore.OpenExisting("1A9191BF-AA26-46E1-BB85-BDA396BC6469");
                MyTasksParam tobj = (MyTasksParam)obj;
                string str = tobj.TskMessage;
                curcontext.Send(t => listBox2.Items.Add(str), null);
                sem.WaitOne();
                curcontext.Send(t => listBox2.Items.Remove(str), null);
                int counter = 0, idx = 0;
                str = str.Substring(0, (str.IndexOf('>') + 2)) + counter;
                curcontext.Send(t => listBox1.Items.Add(str), null);
                workingtasks.Add(tobj);
                
                while (counter < 101)
                {

                    curcontext.Send(t => {
                        idx = listBox1.Items.IndexOf(str);
                         if (idx > -1)
                         {
                            str = str.Substring(0, (str.IndexOf('>') + 2)) + counter + '%';
                            listBox1.Items.RemoveAt(idx);
                            listBox1.Items.Insert(idx, str);
                         }

                    }, null);
                   
                    Thread.Sleep(150);
                    if (tobj.isCancelTask)
                        break;
                    counter++;
                }
                curcontext.Send(t => listBox1.Items.Remove(str), null);
                if(!tobj.isCancelTask)
                sem.Release();
                workingtasks.Remove(tobj);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        

        private void button1_Click(object sender, EventArgs e)
        {
            if (Index < maxsize)
            {
                string mess = "Задача " + (Index + 1) + " --> создана";
                string mess2 = "Задача " + (Index + 1) + " --> ожидает";
                MyTasksParam obj = new MyTasksParam(mess2);
                tasklist.Add(new Task(SomeJob, obj));
                listBox3.Items.Add(mess);
                Index++;
                
            }
            else
                MessageBox.Show("Achieved MaxSize of 100 tasks");
           
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            int nvalue = (int)numericUpDown1.Value;
            if (nvalue - curSize == 1)
                  semaphore.Release(1);
            else if(workingtasks.Count > nvalue)
                workingtasks[0].isCancelTask = true;
            else
            {
                MyTasksParam obj = new MyTasksParam("IdleTask");
                disposetasks.Add(Task.Factory.StartNew(SomeJobBusy, obj));
                
            }
            curSize = nvalue;            
        }

        private void listBox3_DoubleClick(object sender, EventArgs e)
        {
            if (tasklist.Count > 0 && listBox3.SelectedIndex >= 0)
            {
                int idx = listBox3.SelectedIndex;
                tasklist.ElementAt(idx).Start();
                disposetasks.Add(tasklist.ElementAt(idx));
                tasklist.RemoveAt(idx);
                listBox3.Items.RemoveAt(idx);
            }
        }

        ~SemaphoresForm()
        {
            foreach (var tsk in disposetasks)
            {
                tsk.Dispose();                
            }
        }
    }
}
