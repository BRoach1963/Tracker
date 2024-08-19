namespace Tracker.Classes
{
    public class SystemStats(double appCpu, double sysCpu, double appMem, double sysMem)
    {
        public double AppCpu = appCpu;
        public double AppMem = appMem;
        public double SysMem = sysMem;
        public double SysCpu = sysCpu;
    }
}
