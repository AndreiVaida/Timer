using Ninject;
using Timer.Repository;
using Timer.Service;

namespace Timer.General {
    public static class NinjectKernel {
        private static readonly StandardKernel _kernel = new();

        static NinjectKernel() {
            _kernel.Bind<DateTimeProvider>().To<DateTimeProviderImpl>().InSingletonScope();
            _kernel.Bind<ActivityRepository>().To<ActivityRepositoryImpl>().InSingletonScope();
            _kernel.Bind<ActivityService>().To<ActivityServiceImpl>().InSingletonScope();
        }

        public static StandardKernel Kernel => _kernel;
    }
}
