namespace Tracker.Classes
{
    public class SystemStats
    {
        public double AppCpu;
        public double AppMem;
        public double SysMem;
        public double SysCpu;

        public SystemStats(double appCpu, double sysCpu, double appMem, double sysMem)
        {
            AppCpu = appCpu;
            AppMem = appMem;
            SysMem = sysMem;
            SysCpu = sysCpu;
        }
    }
}
