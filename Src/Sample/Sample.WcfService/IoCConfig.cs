using System;
using IFramework.Autofac;
//using IFramework.Autofac;
using IFramework.Config;
using IFramework.EntityFramework.Config;
using IFramework.IoC;
using IFramework.Unity;
using Sample.CommandHandler.Community;
using Sample.Domain;
using Sample.Persistence;
using Sample.Persistence.Repositories;

namespace Sample.WcfService
{
    /// <summary>
    ///     Specifies the Unity configuration for the main container.
    /// </summary>
    public class IoCConfig
    {
        private static readonly Lazy<IContainer> Container = new Lazy<IContainer>(() =>
        {
            Configuration.Instance
                         //.UseAutofacContainer()
                         //.RegisterAssemblyTypes(System.Reflection.Assembly.GetExecutingAssembly().FullName)
                         .UseUnityContainer()
                         .RegisterCommonComponents()
                         .UseLog4Net()
                         .UseJsonNet();

            var container = IoCFactory.Instance.CurrentContainer;
            RegisterTypes(container, Lifetime.Hierarchical);
            return container;
        });

        /// <summary>Registers the type mappings with the Unity container.</summary>
        /// <param name="container">The unity container to configure.</param>
        /// <param name="lifetime"></param>
        /// <remarks>
        ///     There is no need to register concrete types such as controllers or API controllers (unless you want to
        ///     change the defaults), as Unity allows resolving a concrete type even if it was not previously registered.
        /// </remarks>
        public static void RegisterTypes(IContainer container, Lifetime lifetime)
        {
            // NOTE: To load from web.config uncomment the line below. Make sure to add a Microsoft.Practices.Unity.Configuration to the using statements.
            // container.LoadConfiguration();

            // TODO: Register your types here
            // container.RegisterType<IProductRepository, ProductRepository>();

            Configuration.Instance
                         .RegisterDefaultEventBus(container, lifetime)
                         .RegisterEntityFrameworkComponents(container, lifetime);
            
            container.RegisterType<IService1, Service1>(new InterfaceInterceptorInjection(),
                                                        new InterceptionBehaviorInjection<UnityLogInterceptor>());

            container.RegisterType<SampleModelContext, SampleModelContext>(lifetime);
            container.RegisterType<ICommunityRepository, CommunityRepository>(lifetime,
                                                                              new InterfaceInterceptorInjection(),
                                                                              new InterceptionBehaviorInjection<UnityLogInterceptor>());
            container.RegisterType<CommunityCommandHandler, CommunityCommandHandler>(lifetime);
        }

        #region Unity Container

        /// <summary>
        ///     Gets the configured Unity container.
        /// </summary>
        public static IContainer GetConfiguredContainer()
        {
            return Container.Value;
        }

        #endregion
    }
}