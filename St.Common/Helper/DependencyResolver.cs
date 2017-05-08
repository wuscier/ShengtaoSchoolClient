using System;
using Autofac;

namespace St.Common
{
    public class DependencyResolver
    {
        private DependencyResolver()
        {
        }

        public static DependencyResolver Current = new DependencyResolver();

        private static IContainer _container;
        public static void SetContainer(IContainer container)
        {
            if (_container == null)
            {
                _container = container;
            }
        }

        public object GetService(Type type)
        {
            return _container.Resolve(type);
        }

        public T GetService<T>()
        {
            return _container.Resolve<T>();
        }

        public IContainer Container
        {
            get
            {
                if (_container == null)
                    throw new NullReferenceException("未设置容器。");

                return _container;
            }
        }
    }
}