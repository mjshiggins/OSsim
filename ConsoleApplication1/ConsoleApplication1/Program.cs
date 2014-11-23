
// github VS test
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MyDataStructs;

namespace OSsimulator
{
    public class system // Maintains status of overall system and stores important parameters
    {
        // Member Fields
            // Quantum
            // Scheduling type
        
        // Constructors
        public system()
        {

        }

        // Methods
            // Initialize: Boots system, populates metadata and I/O cycle times, begins scheduling/processing
		    // Run:  Initiates processing, hands off control to processor
		    // Shut Down
    }

    public class metadata // Reads in data from specified file, saves it in array that later populates Class::Processor member queue. Functions as simulated non-volatile hard disk storage
    {
        // Member Fields
            // Queue<Type PCB>

        // Constructors
        public metadata()
        {

        }

        // Methods  
            // Read: Populates storage array and Class::System members (Potentially a constructor)
    }

    public class clock // Dependent on System Time
    {
        // Member Fields
		    // Time started
		    // Time stopped
		    // Stopwatch interval
		    // Started (yes or no)

        // Constructors
        public clock()
        {

        }

        // Methods   
		    // Start: Starts the timer
		    // Stop: Stops the timer
            // GetElapsedTime: Gets time elapsed from start
		    // StopWatch: Creates a stopwatch with desired value
		    // Convert: Converts cycles to ms
    }

    public class interruptManager // Interrupts will be handled by an Interrupt Management class.  The class will keep track of all processes that are currently waiting on an interrupt.  The class will implement a pulling method that checks if any of the interrupts have gone off, and then handle them appropriately. 
    {
        // Member Fields
            // Number of pending interrupts
		    // Queue <Type Interrupt>
		    // Class Clock

        // Constructors
        public interruptManager()
        {

        }

        // Methods  
		    // Create: Adds a new interrupt into the queue
		    // Remove: Removes interrupt after being serviced
		    // Service: Prints status to console 
    }

    public struct interrupt
    {
        // Member Fields
		    // Process number
            // Interrupt description
		    // Time needed
    }

    public class processor
    {
        // Member Fields
		    // Cycle time
            // New Queue <Type PCB>
		    // Ready Queue <Type PCB>
            // Running Queue <Type PCB>
            // Waiting Queue <Type PCB>
		    // Class::Timer

        // Constructors
        public processor()
        {

        }

        // Methods
		    // Swap Processes(P in processor, 1st P in Queue), prints status to console
            // Create(process), prints status to console
            // Remove(process), prints status to console
            // Manage I/O, prints status to console
            // Run(process), prints status to console
            // Enqueue 
    }

    public class pcb
    {
        private enum States { New, Running, Waiting, Ready, Terminated}
        private enum Actions { Process, Input, Output }
        private enum Type { Keyboard, Monitor, HD }

        struct Job
        {
            int cycleLength;
            Actions action;
            Type? device;

            public Job(int cl, Actions a, Type? d)
            {
                cycleLength = cl;
                action = a;
                device = d;
            }
            
            public Job(Job r)
            {
                cycleLength = r.cycleLength;
                action = r.action;
                device = r.device;
            }


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
        private States state;
		private int  remainingCycles;
		    // I/O Status info: Flag for waiting for interrupt, I/O requirements
        private Queue<Job> upcomingJobs;

        // Constructors

        public pcb(ref StreamReader fin)
        {
            state = States.New;
            remainingCycles = 0;
            upcomingJobs = new Queue<Job>();
            int cl = 0;
            Actions a = Actions.Process;
            Type? d = null;
            char[] buffer = new char[80];

            while(getNextJob(ref fin, ref cl, ref  a, ref d))
            {
                Job j;
                j = new Job(cl, a, d);
                upcomingJobs.Enqueue(j);
            }

            fin.Read(buffer, 0, 6);
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
    }

    public class scheduler // Manages Queues in Class Processor
    {
        // Member Fields
		    // Priority Queue<PCB>
		    // Schedule type
		    // Preemptive (yes or no)

        // Constructors
        public scheduler()
        {

        }

        // Methods (This needs to be reevaluated for multiple scheduling types)
        	// Functions:
		    // Enqueue //Enqueue depending on the scheduling type.
		    // Dequeue //Gets the next PCB in the queue
    }

    public class device // Accepts process as argument that requires use of hardware I/O
    {
        // Member Fields
		    // Cycle Time

        // Constructors
        public device()
        {

        }

        // Methods
		    // Interrupt: Calls the appropriate function in the Class::Interrupt

    }

    public class monitor : device
    {
        // Member Fields

        // Constructors
        public monitor()
        {

        }

        // Methods
            // Output: Simulates Output to monitor, prints status to console
    }

    public class printer : device
    {
        // Member Fields

        // Constructors
        public printer()
        {

        }

        // Methods
            // Output: Simulates Output to printer, prints status to console

    }

    public class hd : device
    {
        // Member Fields

        // Constructors
        public hd()
        {

        }

        // Methods
        	// Output: Simulates Output to hard drive, prints status to console
		    // Input: Simulates Input from hard drive, prints status to console
    }

    class keyboard : device
    {
        // Member Fields

        // Constructors
        public keyboard()
        {

        }

        // Methods
        	// Input: Simulates input from keyboard, prints status to console
		    // Interrupt: Calls interrupt function
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Temp Program Holds
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();

            // Initialize System
                // Initialize Classes
                // Read-in, populate memory, set configuration
            // Run
                    // Hand over control to processing module
                        // Process threads, I/O, interrupt monitoring
                    // Loop until end of metadata
            // Shutdown             
        }
    }
}
