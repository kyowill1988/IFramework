﻿
using IFramework.Command;
using IFramework.Config;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Threading.Tasks;
using IFramework.MessageQueue.ServiceBus;

namespace Sample.CommandService
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        static ILogger _Logger;
        static IEventPublisher _EventPublisher;
        static ICommandBus _CommandBus;
        static IMessageConsumer _CommandConsumer1;
        static IMessageConsumer _CommandConsumer2;
        static IMessageConsumer _CommandConsumer3;
        //static IMessageConsumer _CommandDistributor;
        static IMessageConsumer _DomainEventConsumer;
        //static IMessageConsumer _ApplicationEventConsumer;

        static WebApiApplication()
        {
            try
            {
                Configuration.Instance.UseLog4Net();
                _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(WebApiApplication));

                //_CommandDistributor = new CommandDistributor("tcp://127.0.0.1:5000",
                //                                                new string[] { 
                //                                                    "tcp://127.0.0.1:5001"
                //                                                    , "tcp://127.0.0.1:5002"
                //                                                    , "tcp://127.0.0.1:5003"
                //                                                }
                //                                               );

                Configuration.Instance
                             .CommandHandlerProviderBuild(null, "CommandHandlers")
                             .RegisterDisposeModule()
                             .RegisterMvc();
                const string serviceBusConnectionString = @"Endpoint=sb://iframework.servicebus.chinacloudapi.cn/;StsEndpoint=https://iframework-sb.accesscontrol.chinacloudapi.cn/;SharedSecretIssuer=owner;SharedSecretValue=DfDIfwLDgVK4Ujx0iDmuUAFxYIkX+iFSnQFqw5BtpSw=";

                _EventPublisher = new EventPublisher(serviceBusConnectionString, "eventTopic");
                _EventPublisher.Start();

                IoCFactory.Instance.CurrentContainer
                      .RegisterInstance(typeof(IEventPublisher)
                                        , _EventPublisher
                                        , new ContainerControlledLifetimeManager());

                var eventHandlerProvider = IoCFactory.Resolve<IHandlerProvider>("AsyncDomainEventProvider");
                _DomainEventConsumer = new EventSubscriber(serviceBusConnectionString, 
                                                           eventHandlerProvider, 
                                                           "eventSubscriber1", 
                                                           "eventTopic");
                _DomainEventConsumer.Start();

                //_ApplicationEventConsumer = IoCFactory.Resolve<IMessageConsumer>("ApplicationEventConsumer");
                //_ApplicationEventConsumer.Start();


               
                //serviceBusConnectionString = "Endpoint=sb://iframework.servicebus.chinacloudapi.cn/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=jGR+cxcbZqQLQcmF+xRxnOMYXiVDVI5AXC3nY9B4lW8=";
                var commandHandlerProvider = IoCFactory.Resolve<ICommandHandlerProvider>();
                
                var linearCommandManager = IoCFactory.Resolve<ILinearCommandManager>();
                _CommandBus = new CommandBus(commandHandlerProvider, linearCommandManager, serviceBusConnectionString,
                                             new string[] { "commandqueue1", "commandqueue2", "commandqueue3" }, "replyTopic", "replySubscritpion", false);
                _CommandBus.Start();

                IoCFactory.Instance.CurrentContainer
                        .RegisterInstance(typeof(ICommandBus)
                                          , _CommandBus
                                          , new ContainerControlledLifetimeManager());

                _CommandConsumer1 = new CommandConsumer(commandHandlerProvider, serviceBusConnectionString, "commandqueue1");
                _CommandConsumer2 = new CommandConsumer(commandHandlerProvider, serviceBusConnectionString, "commandqueue2");
                _CommandConsumer3 = new CommandConsumer(commandHandlerProvider, serviceBusConnectionString, "commandqueue3");
                _CommandConsumer1.Start();
                _CommandConsumer2.Start();
                _CommandConsumer3.Start();

      
            }
            catch (Exception ex)
            {
                _Logger.Error(ex.GetBaseException().Message, ex);
            }

        }

        // ZeroMQ Application_Start
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_End()
        {
            try
            {
                Task.WaitAll(
                    Task.Factory.StartNew(() => _CommandConsumer1.Stop()),
                    Task.Factory.StartNew(() => _CommandConsumer2.Stop()),
                    Task.Factory.StartNew(() => _CommandConsumer3.Stop()),
                    Task.Factory.StartNew(() => _CommandBus.Stop()),
                   // Task.Factory.StartNew(() => _CommandDistributor.Stop()),
                    Task.Factory.StartNew(() => _EventPublisher.Stop()),
                    Task.Factory.StartNew(() => _DomainEventConsumer.Stop())
                   // Task.Factory.StartNew(() => _ApplicationEventConsumer.Stop())
                   );
            }
            catch (Exception ex)
            {
                _Logger.Error(ex.GetBaseException().Message, ex);
            }

        }
        // EQueue Application_Start
        /*
        public static List<CommandConsumer> CommandConsumers = new List<CommandConsumer>();
         
        protected void Application_Start()
        {
            try
            {
                Configuration.Instance.UseLog4Net();

                Configuration.Instance
                             .CommandHandlerProviderBuild(null, "CommandHandlers")
                             .RegisterMvc();

                global::EQueue.Configuration
                .Create()
                .UseAutofac()
                .UseLog4Net()
                .UseJsonNet()
                .RegisterFrameworkComponents();

                new BrokerController().Initialize().Start();
                var consumerSettings = ConsumerSettings.Default;
                consumerSettings.MessageHandleMode = MessageHandleMode.Sequential;
                var producerPort = 5000;
                IEventPublisher eventPublisher = new EventPublisher("domainevent", 
                                                                    consumerSettings.BrokerAddress,
                                                                    producerPort);
                IoCFactory.Instance.CurrentContainer.RegisterInstance(typeof(IEventPublisher), eventPublisher);

                var eventHandlerProvider = IoCFactory.Resolve<IHandlerProvider>("AsyncDomainEventSubscriber");
                IMessageConsumer domainEventSubscriber = new DomainEventSubscriber("domainEventSubscriber1",
                                                                                   consumerSettings,
                                                                                   "DomainEventSubscriber",
                                                                                   "domainevent",
                                                                                   eventHandlerProvider);
                domainEventSubscriber.Start();
                IoCFactory.Instance.CurrentContainer.RegisterInstance("DomainEventConsumer", domainEventSubscriber);



                var commandHandlerProvider = IoCFactory.Resolve<ICommandHandlerProvider>();
                var commandConsumer1 = new CommandConsumer("consumer1", consumerSettings, 
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                var commandConsumer2 = new CommandConsumer("consumer2", consumerSettings,
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                var commandConsumer3 = new CommandConsumer("consumer3", consumerSettings,
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                commandConsumer1.Start();
                commandConsumer2.Start();
                commandConsumer3.Start();

                CommandConsumers.Add(commandConsumer1);
                CommandConsumers.Add(commandConsumer2);
                CommandConsumers.Add(commandConsumer3);

                ICommandBus commandBus = new CommandBus("CommandBus",
                                                        commandHandlerProvider,
                                                        IoCFactory.Resolve<ILinearCommandManager>(),
                                                        consumerSettings.BrokerAddress,
                                                        producerPort,
                                                        consumerSettings,
                                                        "CommandBus",
                                                        "Reply", 
                                                        "Command",
                                                        true);
                IoCFactory.Instance.CurrentContainer.RegisterInstance(typeof(ICommandBus),
                                                                      commandBus,
                                                                      new ContainerControlledLifetimeManager());
                commandBus.Start();

                AreaRegistration.RegisterAllAreas();
                WebApiConfig.Register(GlobalConfiguration.Configuration);
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.GetBaseException().Message, ex);
            }
        }
        */
        protected void Application_Error(object sender, EventArgs e)
        {

            Exception ex = Server.GetLastError().GetBaseException(); //获取错误
            _Logger.Error(ex.Message, ex);
        }
    }
}