using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightInject.Tests
{
    using System.Reflection;
    using System.Web;
    

    using LightInject.SampleLibrary;
    using LightInject.Web;

    using Xunit;

    
    public class WebTests
    {
        private static ServiceContainer serviceContainer;

        static WebTests()
        {
            serviceContainer = new ServiceContainer();
            serviceContainer.Register<IFoo, Foo>(new PerScopeLifetime());
            serviceContainer.EnablePerWebRequestScope();
        }
                
        [Fact]
        public void ShouldReturnSameInstanceWithinSingleRequest()
        {
            var mockHttpApplication = CreateWebApplication();

            var firstInstance = serviceContainer.GetInstance<IFoo>();
            var secondInstance = serviceContainer.GetInstance<IFoo>();
            mockHttpApplication.EndRequest();

            Assert.Equal(firstInstance, secondInstance);           
        }

        private static MockHttpApplication CreateWebApplication()
        {
            var request = new HttpRequest(null, "http://tempuri.org", null);
            var response = new HttpResponse(null);
            HttpContext context = new HttpContext(request, response);
            HttpContext.Current = context;
            var mockHttpApplication = new MockHttpApplication(new LightInjectHttpModule(), false);
            return mockHttpApplication;
        }


        [Fact]
        public void ShouldReturnMultipleInstancesForMultipleRequests()
        {            
            var firstInstance = GetInstanceWithinWebRequest();
            var secondInstance = GetInstanceWithinWebRequest();
            
            Assert.NotSame(firstInstance, secondInstance);
        }

        [Fact]
        public void ShouldHandleMultipleThreads()
        {
            ParallelInvoker.Invoke(10, () => GetInstanceWithinWebRequest());
        }

        [Fact]
        public void ShouldRegisterModule()
        {
            LightInjectHttpModuleInitializer.Initialize();
        }



        //[Fact]
        //public void GetInstance_WithoutBeginRequest_ThrowsMeaningfulException()
        //{
        //    var mockHttpApplication = new MockHttpApplication(null, false);
        //    mockHttpApplication.BeginRequest();
        //    var exception = Assert.Throws<InvalidOperationException>(() => serviceContainer.GetInstance<IFoo>());
        //    Assert.Contains("Unable to locate a scope manager for the current HttpRequest.", exception.Message);            
        //}

        

        private static IFoo GetInstanceWithinWebRequest()
        {
            serviceContainer.EnablePerWebRequestScope();
            var mockHttpApplication = CreateWebApplication();
            
            IFoo firstInstance = serviceContainer.GetInstance<IFoo>();
            mockHttpApplication.EndRequest();
            mockHttpApplication.Dispose();
            return firstInstance;
        }

        public class MockHttpApplication : HttpApplication
        {
            private static readonly object EndEventHandlerKey;            

            private static readonly FieldInfo ContextField;
            private readonly IHttpModule module;
            private readonly bool passNullAsSender;

            public MockHttpApplication(IHttpModule module, bool passNullAsSender) 
            {
                if (module != null)
                {
                    module.Init(this);
                }
                this.module = module;
                this.passNullAsSender = passNullAsSender;
            }
           
            static MockHttpApplication()
            {                
                EndEventHandlerKey = typeof(HttpApplication).GetField("EventEndRequest", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);                
                ContextField = typeof(HttpApplication).GetField(
                    "_context",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }
           
            
            private void SetContext(HttpContext context)
            {
                ContextField.SetValue(this, context);
            }


            public new void EndRequest()
            {
                if (module != null)
                {
                    if (passNullAsSender)
                    {
                        this.Events[EndEventHandlerKey].DynamicInvoke(null, null);
                    }
                    else
                    {
                        this.Events[EndEventHandlerKey].DynamicInvoke(this, null);
                    }
                }                                
                HttpContext.Current = null;
                SetContext(null);
            }

            public override void Dispose()
            {
                if (module != null)
                {
                    module.Dispose();
                }               
                base.Dispose();
            }
        }
    }
}
