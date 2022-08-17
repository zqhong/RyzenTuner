using RyzenTuner.Common.Processor;

namespace RyzenTuner.Common.Container
{
    /**
     * Refer: https://csharpindepth.com/articles/singleton#cctor
     */
    public static class AppContainer
    {
        private static readonly Container Container;

        static AppContainer()
        {
            Container = new Container();

            Container.Register(() => new HardwareMonitor())
                .AsSingleton();

            Container.Register(() => new PowerConfig())
                .AsSingleton();

            Container.Register(() => new AmdProcessor())
                .AsSingleton();
        }

        public static HardwareMonitor HardwareMonitor()
        {
            return Container.Resolve<HardwareMonitor>();
        }

        public static PowerConfig PowerConfig()
        {
            return Container.Resolve<PowerConfig>();
        }

        public static AmdProcessor AmdProcessor()
        {
            return Container.Resolve<AmdProcessor>();
        }
    }
}