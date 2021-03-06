﻿using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyDataStructs;

namespace OSsimulator
{
    public enum States { New, Running, Waiting, Ready, Terminated }
    public enum Scheduling { FIFO, RR, SJF }

    public class system // Maintains status of overall system and stores important parameters
    {
        // Member Fields
            // Quantum
            // Scheduling type
        static Scheduling sched;
        static Queue<pcb> newProcesses;
        
        // Constructors
        public system(string fileName)
        {
            // stores the process read in
            newProcesses = new Queue<pcb>();

            //Creates a thread where it gets meatadata info
            //Waits for the thread to finish
            Thread getProgram = new Thread(system.readProgram);
            getProgram.Start(fileName);
            getProgram.Join();
         }

        // Methods
            // Initialize: Boots system, populates metadata and I/O cycle times, begins scheduling/processing
        static void readProgram(Object file)
        {
            // reads in meta data from file "program.txt"
            string f = file.ToString();
            metadata ourProgram = new metadata(f);

            //This is where the program inputs the inforamtion
            ourProgram.readFromFile(ref newProcesses, sched);

            //Exits the thread
            Thread.CurrentThread.Abort();
        }

        public void populateProcesor(ref processor p)
        {
            //While there are processes in the ready queue
            while(newProcesses.Count() != 0)
            {
                pcb temp = newProcesses.Dequeue();

                //Gets the PID number
                int i = temp.getPID();
                Console.Write("PID {0} - Enter System\n", i);

                //Switches the state from new to ready
                temp.updateState(States.Ready);

                //Passes it to the processor
                p.creatPCB(temp);

            }

            // sets Scheduler Type
            p.setScheduleType(sched);
        }
    }

    public class metadata // Reads in data from specified file, saves it in array that later populates Class::Processor member queue. Functions as simulated non-volatile hard disk storage
    {
        // Member Fields
            // Queue<Type PCB>
        StreamReader fin;
        string fileName;

        // Constructors
        public metadata(string f)
        {
            fileName = f;
        }

        // Reads in process from file
        public  void readFromFile(ref Queue<pcb> newPCBS, Scheduling schedUsed)
        {
            char[] buffer = new char[50];
            int i =0;

            try
            {
                fin = new StreamReader(fileName);

                fin.Read(buffer, 0, 11);

                while((char)fin.Read() == 'A')
                {
                    //Sets fin to correct location
                    fin.Read(buffer, 0, 9);

                    //Creates new pcb
                    pcb temp = new pcb(ref fin, schedUsed, i, i);

                    //Enqueues it
                    newPCBS.Enqueue(temp);

                    //Gets blank
                    fin.Read();
                    i++;
                }

                fin.Close();
            }

            catch (Exception e)
            {
                Console.WriteLine("Invalid path or filename in configuration file");
            }

        }

        // Methods  
            // Read: Populates storage array and Class::System members (Potentially a constructor)
    }

    public class clock // Dependent on System Time
    {
        // Member Fields
        int processorTime;  
        int monitorTime;
        int hardDriveTime;
        int printerTime;
        int keyBoardTime;
        Stopwatch stopwatch;

        // Constructors

        // default constructor
        public clock()
        {
            processorTime = 0;
            monitorTime = 0;
            hardDriveTime = 0;
            printerTime = 0;
            keyBoardTime = 0;
            stopwatch = new Stopwatch();
        }

        // Methods  
 
        // sets Clock values dependening on the Config file
        public void setClock(int procTime, int mTime, int hTime, int prTime, int kTime)
        {
            processorTime = procTime;
            monitorTime = mTime;
            hardDriveTime = hTime;
            printerTime = prTime;
            keyBoardTime = kTime;
        }

        // Starts the stopwatch
        public void startSW()
        {
            stopwatch.Start();
        }

        // Stops the stopwatch
        public void stopSW()
        {
            stopwatch.Stop();
        }

        // retuns a string containing the number of nanonseconds elapsed from start to stop
        public string getElapsedTime()
        {
            return (((double)(stopwatch.Elapsed.TotalMilliseconds * 1000000)).ToString("(0.00 nSec)"));
        }

        // cycles to milliseconds
        public int convert(string type, int cycles)
        {
            // converts processor time
            if (type == "processor")
            {
                return (processorTime * cycles);
            }

            // converts monitor time
            if (type == "monitor")
            {
                return (monitorTime * cycles);
            }

            // converts hard drive time
            if (type == "hard drive")
            {
                return (hardDriveTime * cycles);
            }

            // converts printer time
            if (type == "printer")
            {
                return (printerTime * cycles);
            }

            // converts keyboard time
            if (type == "keyboard")
            {
                return (keyBoardTime * cycles);
            }

            // converts anytime you want
            if (type == "manual")
            {
                return cycles;
            }

            else
                return 0;
        }

        // creates a delay to simulate time
        public void delay(string type, int cycles)
        {
            Thread.Sleep(convert(type, cycles));
        }

    }

    public class interruptManager // Interrupts will be handled by an Interrupt Management class.  The class will keep track of all processes that are currently waiting on an interrupt.  The class will implement a pulling method that checks if any of the interrupts have gone off, and then handle them appropriately. 
    {
        // Member Fields
            // Number of pending interrupts
            int pendingInterrupts;
		    // Class Clock
            clock interrClock;
            // Main processor
            processor Processor;

        // Constructors
            public interruptManager(ref processor Proc, int procTime, int mTime, int hTime, int prTime, int kTime)
        {
            Processor = Proc;
            interrClock = new clock();
            interrClock.setClock(procTime, mTime, hTime, prTime, kTime);
            int pendingInterrupts = 0;

        }

        // Methods  
		    // Create: Adds a new interrupt 
            // Type: "printer", "hard drive", "monitor", "keyboard", etc
            // Cycles: number of cycles needed
            // IO: "Input" or "Output"
            // PIDNum: What Pid it is from
            public void newInterrupt(string type, int cycles, string IO, int PIDNum)
            {
                string info;
                // adds one to the amount of pending interrupts
                pendingInterrupts++;
                // creates the interrupt information
                info = "Pid" + PIDNum + " - " + IO + ", " + type + 
                    "completed (" + interrClock.convert(type, cycles) + " mSec)";
                // notifies the OS
                service(info);
                // decrements the amount of pending interrupts
                pendingInterrupts--;
            }

		    // Signals the OS that an Interrupt needs to be managed
            public void service(string info)
            {
                // sets the Interrupt
                Processor.setInterrupt(info);
            }
    }

    public class processor
    {
        // Member Fields
            int cycleTime;
            PriorityQueue<pcb> readyQueue;
            Queue<pcb> waitingQueue;
            clock procClock;
            Logger procLogger;
            Scheduling sched;

            // Interrupt Flag
            bool interruptFlag;

            // Interrupt Info
           string interruptInfo;

        // Constructors
           public processor(ref Logger logger, int procTime, int mTime, int hTime, int prTime, int kTime)
        {
            procLogger = logger;
            procClock = new clock();
            procClock.setClock(procTime, mTime, hTime, prTime, kTime);
            interruptFlag = false;
            interruptInfo = "";
            readyQueue = new PriorityQueue<pcb>();;
            waitingQueue = new Queue<pcb>();
        }

        // Methods

            // Update: Updates priority values by iterating through ready queue, runs scheduling methods
            public void update(){
            if (sched == Scheduling.RR)
            {
                // Loop update and run every quantum
                // Loop while there are still PCBs on the priority queue
                while(readyQueue.Count() != 0)
                        {
                        // Check waiting queue for 'ready' PCBs and reload them into the ready queue, otherwise put them back in waiting
                        if(waitingQueue.Count() != 0)
                            {
                            pcb check = waitingQueue.Dequeue();
                            if(check.state == States.Ready)
                                {
                                readyQueue.Enqueue(check);
                                }
                            else
                                {
                                waitingQueue.Enqueue(check);
                                }
                            }

                        // Update Priority Queue (Done Automatically)

                        // Dequeue PCB
                        pcb temp = readyQueue.Dequeue();
                        temp.updateState(States.Running);

                        // Run through processes of first-priority PCB until cycle quantum reached or PCB finished
                        int cycleCounter = 0;
                        while( !temp.finished() && cycleCounter < Program.quantum && !interruptFlag)
                                {
                                // Increment Counter
                                cycleCounter++;

                                // If interrupt occurs, set state, set interrupt bool, and enqueue on waiting queue
                                if(temp.currentJob.action != pcb.Actions.Process)
                                        {
                                        interruptFlag = true;
                                        temp.updateState(States.Waiting);
                                        waitingQueue.Enqueue(temp);
                                        run(0);
                                        }
                                else
                                    {

                                    // Run processes
                                    run(temp.currentJob.cycleLength);

                                    // Update cycle times for both priority level of PCB and process itself
                                    temp.currentJob.cycleLength = (temp.currentJob.cycleLength - cycleCounter);
                                    temp.priority = (temp.priority - cycleCounter);
                                    }
                                }

                        // Put PCB back on queue if not finished
                        if( !temp.finished() && !interruptFlag )
                                {
                                temp.updateState(States.Ready);
                                readyQueue.Enqueue(temp);
                                }
                        }
            
            }
            else // SJF (no preemption) and FIFO (order already set for both)
            {
                // Loop update and run every quantum
                // Loop while there are still PCBs on the priority queue
                while(readyQueue.Count() != 0)
                        {
                        // Check waiting queue for 'ready' PCBs and reload them into the ready queue, otherwise put them back in waiting
                        if(waitingQueue.Count() != 0)
                            {
                            pcb check = waitingQueue.Dequeue();
                            if(check.state == States.Ready)
                                {
                                readyQueue.Enqueue(check);
                                }
                            else
                                {
                                waitingQueue.Enqueue(check);
                                }
                            }

                        // Update Priority Queue (Done Automatically)
        
                        // Dequeue PCB
                        pcb temp = readyQueue.Dequeue();
                        temp.updateState(States.Running);

                        // Run through processes of first-priority PCB until PCB finished
                        int cycleCounter = 0;
                        while( !temp.finished() && !interruptFlag)
                                {
                                // Increment Counter
                                cycleCounter++;

                                // If interrupt occurs, set state, set interrupt bool, and enqueue on waiting queue
                                if(temp.currentJob.action != pcb.Actions.Process)
                                        {
                                        interruptFlag = true;
                                        temp.updateState(States.Waiting);
                                        waitingQueue.Enqueue(temp);
                                        run(0);
            }
                                else
                                    {

                                    // Run processes
                                    run(temp.currentJob.cycleLength);

                                    // Update cycle times for both priority level of PCB and process itself
                                    temp.currentJob.cycleLength = (temp.currentJob.cycleLength - cycleCounter);
                                    temp.priority = (temp.priority - cycleCounter);
                                    }
            }

                        }       
            }
            }

            // Run(process), prints status to console
            public void run(int cycles)
            {
                int runTime = cycles * cycleTime;

                for (int i = 0; i < runTime; i++)
                {
                    // if there is an interrupt it will be managed
                    if (interruptFlag)
                        manageInterrupt(interruptInfo);

                    // else it sleeps for one ms
                    Thread.Sleep(1);
                }
                 string status = "PID " + " - Processing (" + runTime +  "mSec)";
                procLogger.print(status);
            }


            // Manage Interrupt
            public void manageInterrupt(string info)
            {
                // arguments needed to get from the pcb
                string action = "";
                int cycles = 0;
                string device = "";

                // waits for something to be in the WaitingQueue
                while (waitingQueue.Count != 0) ;

                // creates temp and gets the first element in the waiting queue
                pcb temp = waitingQueue.Dequeue();
                temp.getCurrentInfo(ref action, ref cycles, ref device);
                procClock.delay(device, cycles);

                // changes the state and puts it back in the ready queue
                temp.updateState(States.Ready);
                waitingQueue.Enqueue(temp);


                // print message containing interrupt information
                procLogger.print(info);

                // reset the flag
                interruptFlag = false;
            }

            // Set Interrupt
            public void setInterrupt(string info)
            {
                // sets the interrupt flag
                interruptFlag = true;
                interruptInfo = info;
            }


            // creates a PCB
            internal void creatPCB(pcb temp)
            {
                readyQueue.Enqueue(temp);
            }

            // sets the scedulat type
            internal void setScheduleType(Scheduling s)
            {
                sched = s;                      
            }
    }

    public class pcb : IComparable<pcb>
    {
        public enum Type { Keyboard, Monitor, HD }
        public enum Actions { Process, Input, Output }

        //Jobs are created from <action>(<device>)<cycleLength> from program file
        public struct Job
        {
            // member fields
            public int cycleLength;
            public Actions action;
            Type? device;

            // Constructor
            public Job(int cl, Actions a, Type? d)
            {
                cycleLength = cl;
                action = a;
                device = d;
            }
            
            // Constructor
            public Job(Job r)
            {
                cycleLength = r.cycleLength;
                action = r.action;
                device = r.device;
            }

            // gets Job information
            public void getInfo(ref int c, ref string a, ref string t)
            {
                c = cycleLength;
                a = action.ToString();
                t = device.ToString();
            }

            //Used for testing purposes
            public void print()
            {
                switch(action)
                {
                    case Actions.Process:
                        Console.Write("P(");
                        break;
                    case Actions.Input:
                        Console.Write("I(");
                        break;
                    case Actions.Output:
                        Console.Write("O(");
                        break;
                }

                switch(device)
                {
                    case null:
                        Console.Write("run)");
                        break;
                    case Type.HD:
                        Console.Write("hard drive)");
                        break;
                    case Type.Keyboard:
                        Console.Write("keyboard)");
                        break;
                    case Type.Monitor:
                        Console.Write("monitor)");
                        break;
                }

                Console.Write(cycleLength);
            }

        }

        // Member Fields
        int pidNum;
        public States state;
        private Scheduling sched;
        public int priority;
        private Queue<Job> upcomingJobs;
        public Job currentJob;
        public bool moreJobs;

        // Constructors

        public pcb(ref StreamReader fin, Scheduling schedulingType, int p, int pid = -1)
        {
            pidNum = pid;
            state = States.New;
            upcomingJobs = new Queue<Job>();


            int cl = 0;
            Actions a = Actions.Process;
            Type? d = null;
            char[] buffer = new char[80];

            //Assigns the scheduling type
            if (schedulingType == Scheduling.RR)
            {
                sched = Scheduling.RR;
                priority = p;
            }
            else if(schedulingType == Scheduling.SJF)
            {
                sched = Scheduling.SJF;
                priority = 0;
            }
            else
            {
                sched = Scheduling.FIFO;
                priority = p;
            }

            while(getNextJob(ref fin, ref cl, ref  a, ref d))
            {
                Job j;
                j = new Job(cl, a, d);
                upcomingJobs.Enqueue(j);

                // Adds all the process times to set for shortest job first
                if(schedulingType == Scheduling.SJF || a == Actions.Process)
                {
                    priority += cl;
                }
            }
            currentJob = new Job();
            moreJobs = this.getNewJob();
            fin.Read(buffer, 0, 7);
        }

        public pcb(pcb r)
        {
            this.pidNum = r.pidNum;
            this.state = r.state;
            this.sched = r.sched;
            this.priority = r.priority;
            this.upcomingJobs = r.upcomingJobs;
            this.currentJob = r.currentJob;
        }

        public int getPID()
        {
            return this.pidNum;
        }

        private bool getNextJob(ref StreamReader fin, ref int cl,ref Actions a,ref Type? d)
        {
            char charRead;
            char[] buffer = new char[80];
            string str = "";

            // Gets the space
            charRead = (char) fin.Read();
            charRead = (char)fin.Read();

            //Reads in the component letter
            switch(charRead)
            {
                case 'P':
                    a = Actions.Process;
                    d = null;
                    fin.Read(buffer, 0, 5);
                    break;
                case 'I':
                    a = Actions.Input;
                    charRead = (char) fin.Read();
                    break;
                case 'O':
                    a = Actions.Output;
                    charRead = (char)fin.Read();
                    break;
                case 'A':
                    return false;
                case 'S':
                    return false;
            }

            // Reads in the descriptors
            if(a != Actions.Process)
            {
                charRead = (char) fin.Read();

                if(charRead == 'h')
                {
                    d = Type.HD;
                    fin.Read(buffer, 0, 10);
                }
                else if(charRead == 'm')
                {
                    d = Type.Monitor;
                    fin.Read(buffer, 0, 7);
                }
                else
                {
                    d = Type.Keyboard;
                    fin.Read(buffer, 0, 8);
                }
            }
            
            // Gets the cycle length
            while((char) fin.Peek() != ';')
        {
                charRead = (char)fin.Read();
                str += charRead;
            }
            cl = int.Parse(str);
            charRead = (char)fin.Read();

            return true;
        }

        public bool getNewJob()
        {
            //If there are no more jobs to complete then return false
            if(upcomingJobs.Count() == 0)
                return false;

            currentJob = upcomingJobs.Dequeue();
            return true;
        }

        public void getCurrentInfo(ref string t,ref  int c, ref string s)
        {
            currentJob.getInfo(ref c, ref t, ref s);
        }

        public int CompareTo(pcb other)
        {
            if (this.priority < other.priority) return -1;
            else if (this.priority > other.priority) return 1;
            else return 0;
        }

        public void print()
        {
            while(upcomingJobs.Count != 0)
            {
                Job tmp = new Job(upcomingJobs.Dequeue());
                tmp.print();
            }
        }

        // Methods
		    // Data Logging: Every time a PCB is manipulated or modified, it logs the event to the hard drive and/or monitor depending on the configuration file


        public bool finished()
            {
            if(upcomingJobs.Count() != 0)
                {
                return false;
                }
            return false;
            }

        internal void updateState(States s)
        {
            this.state = s;
        }
    }


    public class Logger
    {
        // Member Fields

        // Where to log
        string log;

        // varaible to write to file
        StreamWriter sw;

        // Default Constructor
        public Logger(string type)
        {
            // assigns log type
            log = type;
            // intializes the streamwriter
            sw = new StreamWriter("results.txt");
            // flushes the buffer, needed to write to file
            sw.AutoFlush = true;
        }

        public void print(string line)
        {
            // Writes to the Monitor
            if ( (log == "Log to Monitor") || (log == "Log to Both") )
            {
                Console.WriteLine(line);
            }

            // Writes to the File
            if ((log == "Log to File") || (log == "Log to Both"))
            {
                sw.WriteLine(line);
            }
        }
    }

    class Program
    {
        public static int quantum;
        private static int procTime;
        private static int monTime;
        private static int hdTime;
        private static int prinTime;
        private static int keybTime;
        private static string log;
        private static string procSch;
        private static string filePath;
        private static string memoryType;

        static void Main(string[] args)
        {

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                string fileName;
                fileName = args[0];
                system ourOS;


                // Temp Program Holds

                // Initialize System
                // Initialize Classes
                // Read-in, populate memory, set configuration
                readInConfig(fileName);
                clock Clock = new clock();
                Clock.setClock(procTime, monTime, hdTime, prinTime, keybTime);
                Logger logger = new Logger(log);
                sw.Stop();
                Console.WriteLine("{0}{1}",
                    "SYSTEM - Boot, set up ",((double)(sw.Elapsed.TotalMilliseconds * 1000000)).ToString("(0.00 nSec)"));
                ourOS = new system("program.txt");
                processor Proc = new processor(ref logger, procTime, monTime, hdTime, prinTime, keybTime);
                ourOS.populateProcesor(ref Proc);
                interruptManager InterrMan = new interruptManager
                    (ref Proc, procTime, monTime, hdTime, prinTime, keybTime);



                // Run Programs
                Proc.update();
                
               
            }

            catch (Exception e)
            {
                Console.WriteLine("No Config file specified");
                Console.WriteLine("Commandline Prompt Example: Simulator config.txt");
            }

            Console.WriteLine("Press any key to Exit.");
            Console.ReadKey();   
             
        }

        static void readInConfig(String file)
        {
            char[] buffer = new char[80];

            try
            {
                StreamReader sr = new StreamReader(file);

                // skips over 2 information lines
                sr.ReadLine();
                sr.ReadLine();

                // skips over text and reads in the quantum as an int
                sr.Read(buffer, 0, 18);
                quantum = int.Parse(sr.ReadLine());

                // skips over text and reads in the scheduling type
                sr.Read(buffer, 0, 22);
                procSch = sr.ReadLine();

                // skips over text and reads in the file path
                sr.Read(buffer, 0, 11);
                filePath = sr.ReadLine();

                // skips over text and reads in processor cycle time as an int
                sr.Read(buffer, 0, 29);
                procTime = int.Parse(sr.ReadLine());

                // skips over text and read in monitor display time as an int
                sr.Read(buffer, 0, 29);
                monTime = int.Parse(sr.ReadLine());

                // skips over text and reads in hard drive cycle time as an int
                sr.Read(buffer, 0, 30);
                hdTime = int.Parse(sr.ReadLine());

                // skips over text and reads in printer cycle time as an int
                sr.Read(buffer, 0, 27);
                prinTime = int.Parse(sr.ReadLine());

                // skips over text and reads in keyboard cycle time as an int
                sr.Read(buffer, 0, 28);
                keybTime = int.Parse(sr.ReadLine());

                // skips over text and reads in memory type
                sr.Read(buffer, 0, 13);
                memoryType = sr.ReadLine();

                // skips over text and reads in log type
                sr.Read(buffer, 0, 5);
                log = sr.ReadLine();

                sr.Close();
            }

            // Prints message if file not opened;
            catch (Exception e)
            {
                Console.WriteLine("{0} {1}" , e.Message, "\r\n");
            }
        }
    }
}
