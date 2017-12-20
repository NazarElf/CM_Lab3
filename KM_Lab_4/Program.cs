using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KM_Lab_4
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            DeviceNode CP = new DeviceNode(2, 6, 2);
            DeviceNode NB = new DeviceNode(1);
            DeviceNode OM = new DeviceNode(3);
            DeviceNode SB = new DeviceNode(6);
            DeviceNode NA = new DeviceNode(25);
            DeviceNode DC = new DeviceNode(50);
            CP.Transition.Add(NB, 1);
            //CP.Transition.Add(CP, 0.5);
            NB.Transition.Add(CP, 0.5);
            NB.Transition.Add(OM, 0.25); // if removing for test, change value from 0.25 to 0.5
            OM.Transition.Add(NB, 1);
            NB.Transition.Add(SB, 0.25);//
            SB.Transition.Add(NB, 0.4); //
            SB.Transition.Add(DC, 0.3); // can be removed for tests
            SB.Transition.Add(NA, 0.3); //
            NA.Transition.Add(SB, 1);   //
            DC.Transition.Add(SB, 1);   //
            M m = new M(CP, NB, OM, SB, NA, DC);

            double[] firstState = new double[462];
            double[] res = new double[462];
            firstState[0] = 1;
            for (int i = 0; i < 10000; i++)
            {
                res = multiply(m.probabilities, firstState);
                res.CopyTo(firstState, 0);
            }
            double[] load = new double[8];
            for (int i = 0; i < 462; i++)
            {
                if (m.States[i].DeviceStates[0].onProc >= 1)
                    load[2] += res[i];
                if (m.States[i].DeviceStates[0].onProc == 2)
                    load[1] += res[i];
                if (m.States[i].DeviceStates[0].onProc == 1)
                    load[0] += res[i];
                for (int j = 0; j < 5; j++)
                {
                    if (m.States[i].DeviceStates[j + 1].onProc >= 1)
                        load[3 + j] += res[i];
                }
            }
            int theMostPossible = 0;
            double MostPossibility = 0.0;
            int iter = 0;
            foreach (var item in res)
            {
                if(MostPossibility< item)
                {
                    MostPossibility = item;
                    theMostPossible = iter;
                }
                iter++;
            }

            Console.WriteLine(m.States[theMostPossible]);
            Console.Read();
        }

        public static double[] multiply(double[,] aMatrix, double[] aVector)
        {
            double[] rVector = new double[462];

            for (int i = 0; i < 462; i++)
            {
                rVector[i] = 0;
                for (int j = 0; j < 462; j++)
                {
                    rVector[i] += aMatrix[i, j] * aVector[j];
                }
            }
            return rVector;
        }
    }

    class DeviceNode
    {
        //for count delta time that == 1/5..1/10 of min tau
        public static double minTau = double.PositiveInfinity;
        //Built in Random generator
        Random r = new Random();
        //Number of tasks that can be processing at the same time
        public ushort TASKS_PER_PROC { get; private set; }
        //Queue of tasks
        public ushort queue { get; set; }
        //Time to compleate task on proc
        public double tau { get; private set; }
        //Probabilities of Transitions
        public Dictionary<DeviceNode, double> Transition = new Dictionary<DeviceNode, double>();
        //Count of tasks onProc just in time
        public ushort onProc = 0;

        public DeviceNode(double tau, ushort queue = 0, ushort maxParalelTasks = 1)
        {
            if (tau < minTau)
                minTau = tau;
            if (maxParalelTasks == 0)
                throw new Exception("Device should have at least 1 thread of tasks");
            TASKS_PER_PROC = maxParalelTasks;
            this.tau = tau;
            this.queue = queue;
            while(this.queue > 0 && onProc < TASKS_PER_PROC)
            {
               this.queue--; onProc++;
            }
        }

        public void AddTask()
        {
            if (onProc < TASKS_PER_PROC)
                onProc++;
            else
                queue++;
        }
        public void RemoveTask()
        {
            if (queue != 0)
                queue--;
            else if (onProc != 0)
                onProc--;
            else throw new Exception("nothing to remvoe");
        }
    }

    class M
    {
        public double[,] probabilities;
        public List<State> States = new List<State>();

        public M(params DeviceNode[] firstState)
        {
            probabilities = new double[462,462];
            Generate(firstState);
        }

        public State Generate(DeviceNode[] root)
        {
            State newState = new State(root);
            foreach (var item in States)
            {
                if (item == newState)
                    return item;
            }
            States.Add(newState);
            List<Dictionary<State, double>> stat = new List<Dictionary<State, double>>();
            List<double?> lambdas = new List<double?>();
            for (int i = 0; i < root.Length; ++i)
            {
                Dictionary<State, double> sIntencivity1 = new Dictionary<State, double>();

                if (root[i].onProc != 0)
                {
                    DeviceNode[] next = new DeviceNode[root.Length];
                    root.CopyTo(next, 0);
                    ushort t1 = next[i].onProc;
                    ushort t2 = next[i].queue;
                    if (next[i].queue == 0)
                        next[i].onProc--;
                    else
                        next[i].queue--;
                    for (int j = 0; j < next[i].Transition.Count; j++)
                    {
                        next[i].Transition.Keys.ElementAt(j).AddTask();
                        sIntencivity1.Add(Generate(next),next[i].Transition.Values.ElementAt(j));
                        next[i].Transition.Keys.ElementAt(j).RemoveTask();
                    }
                    next[i].onProc = t1; next[i].queue = t2;
                    State st = new State(next);
                    lambdas.Add(1.0 / root[i].tau);
                }
                else { lambdas.Add(null); }
                stat.Add(sIntencivity1);
            }
            if (newState == new State(new DeviceState(4, 2, 0), new DeviceState(0, 0, 1), new DeviceState(0, 0, 1), new DeviceState(0, 0, 1), new DeviceState(0, 0, 1), new DeviceState(0, 0, 1)))
                ;
            Dictionary<State, double> sIntencivity2 = new Dictionary<State, double>();
            for (int i = 0; i < lambdas.Count; i++)
            {
                if (lambdas[i] != null)

                    foreach (var item in stat[i])
                    {
                        if (item.Key != newState)
                        {
                            bool contain = false;
                            foreach (var comp in sIntencivity2.Keys)
                            {
                                if (comp == item.Key)
                                {
                                    contain = !contain;
                                    break;
                                }

                            }
                            if (!contain)
                                sIntencivity2.Add(item.Key, newState.DeviceStates[i].onProc * item.Value * lambdas[i] ?? 0);
                            else
                                sIntencivity2[item.Key] += newState.DeviceStates[i].onProc * item.Value * lambdas[i] ?? 0;
                        }
                    }
            }

            Dictionary<State, double> sProbabilities = new Dictionary<State, double>();
            double lambda = 0;
            for (int i = 0; i < sIntencivity2.Count; i++)
            {
                if(sIntencivity2.Keys.ElementAt(i) != newState)
                    lambda += sIntencivity2.Values.ElementAt(i);
            }
            double T = 1 / lambda;
            double deltaT = T * DeviceNode.minTau/5;
            double p0 = Math.Exp(-lambda * deltaT);
            sProbabilities.Add(newState, p0);
            double[] xi = new double[sIntencivity2.Count];
            double xiSum = 0;
            for (int i = 0; i < sIntencivity2.Count; i++)
            {
                xi[i] = 1 - Math.Exp(-sIntencivity2.Values.ElementAt(i) * deltaT);
                xiSum += xi[i];
            }
            for (int i = 0; i < sIntencivity2.Count; i++)
            {
                sProbabilities.Add(sIntencivity2.Keys.ElementAt(i), (1 - p0) * (xi[i] / xiSum));
            }

            foreach (var item in sProbabilities)
            {
                probabilities[findIndex(item.Key), findIndex(newState)] = item.Value;
            }

            return newState;
        }

        private int findIndex(State inner)
        {
            int res = 0;
            foreach (var item in States)
            {
                if (item == inner)
                    return res;
                res++;
            }
            throw new Exception("NotFound");
        }
    }

    struct State
    {
        public DeviceState[] DeviceStates;
        public State(params DeviceNode[] Nodes)
        {
            DeviceStates = new DeviceState[Nodes.Length];
            for (int i = 0; i < Nodes.Length; i++)
            {
                DeviceStates[i] = new DeviceState(Nodes[i].queue,
                                                  Nodes[i].onProc,
                                                  (ushort)(Nodes[i].TASKS_PER_PROC - Nodes[i].onProc));
            }
        }

        public State(params DeviceState[] States)
        {
            DeviceStates = new DeviceState[States.Length];
            States.CopyTo(DeviceStates, 0);
        }

        public override string ToString()
        {
            string res = "< ";
            foreach (var deviceState in DeviceStates)
            {
                res += $"<{deviceState.queue},{deviceState.onProc},{deviceState.freeResource}> ";
            }
            return res + ">";
        }

        public static bool operator ==(State l, State r)
        {
            if (l.DeviceStates.Length != r.DeviceStates.Length)
                return false;
            for (int i = 0; i < l.DeviceStates.Length; i++)
            {
                if (l.DeviceStates[i] != r.DeviceStates[i])
                    return false;
            }
            return true;
        }
        public static bool operator !=(State l, State r)
        {
            return !(l == r);
        }
    }

    struct DeviceState
    {
        public ushort queue { get; private set; }
        public ushort onProc { get; private set; }
        public ushort freeResource { get; private set; }
        public DeviceState(ushort q, ushort oP, ushort fR)
        {
            queue = q;
            onProc = oP;
            freeResource = fR;
        }

        public static bool operator ==(DeviceState l, DeviceState r)
        {
            return (l.freeResource == r.freeResource && l.onProc == r.onProc && l.queue == r.queue);
        }

        public static bool operator !=(DeviceState l, DeviceState r)
        {
            return (l.freeResource != r.freeResource || l.onProc != r.onProc || l.queue != r.queue);
        }

        public override string ToString()
        {
            return $"<{queue},{onProc},{freeResource}>";
        }
    }

    //static class Count
    //{
    //    public static uint Factor(uint n)
    //    {
    //        uint res = 1;
    //        for (uint i = 1; i < n + 1; i++)
    //        {
    //            res *= i;
    //        }
    //        return res;
    //    }
    //}
}
